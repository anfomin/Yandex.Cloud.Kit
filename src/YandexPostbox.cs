using System.Net;
using System.Net.Http.Json;
using System.Net.Mail;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using System.Threading.RateLimiting;
using Aws4RequestSigner;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Yandex.Cloud;

/// <summary>
/// Provides email sending using Yandex.Cloud Postbox.
/// </summary>
public class YandexPostbox : IMailService, IDisposable, IAsyncDisposable
{
	readonly TimeSpan _rateWindow = TimeSpan.FromSeconds(1);
	static readonly JsonSerializerOptions JsonOptions = new()
	{
		Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
		PropertyNameCaseInsensitive = true
	};

	readonly ILogger _logger;
	readonly IHttpClientFactory _clientFactory;
	readonly YandexCloudOptions _cloudOptions;
	readonly YandexMailOptions _mailOptions;
	readonly IEnumerable<IMailFilter> _filters;
	readonly IEnumerable<IMailModifier> _modifiers;
	readonly FixedWindowRateLimiter _rateLimiter;

	public YandexPostbox(
		ILogger<YandexPostbox> logger,
		IHttpClientFactory clientFactory,
		IOptions<YandexCloudOptions> cloudOptions,
		IOptions<YandexMailOptions> mailOptions,
		IEnumerable<IMailFilter>? filters = null,
		IEnumerable<IMailModifier>? modifiers = null)
	{
		var options = cloudOptions.Value;
		if (string.IsNullOrEmpty(options.AccountKey))
			throw new ArgumentException("Yandex.Cloud option AccountKey is required", nameof(cloudOptions));
		if (string.IsNullOrEmpty(options.SecretKey))
			throw new ArgumentException("Yandex.Cloud option SecretKey is required", nameof(cloudOptions));

		_logger = logger;
		_clientFactory = clientFactory;
		_cloudOptions = options;
		_mailOptions = mailOptions.Value;
		_filters = filters ?? [];
		_modifiers = modifiers ?? [];
		_rateLimiter = new(new()
		{
			PermitLimit = _mailOptions.RateLimit,
			Window = _rateWindow,
			QueueLimit = int.MaxValue,
		});
	}

	public void Dispose()
		=> _rateLimiter.Dispose();

	public ValueTask DisposeAsync()
		=> _rateLimiter.DisposeAsync();

	public async Task<string> SendMailAsync(MailMessage message, CancellationToken cancellationToken = default)
	{
		// apply filters and modifiers
		if (_filters.Any(f => !f.ShouldSend(message)))
			throw new MailFilteredException();
		foreach (var modifier in _modifiers)
			modifier.Apply(message);

		// create request
		var data = GetMessageData(message);
		var request = new HttpRequestMessage(HttpMethod.Post, "https://postbox.cloud.yandex.net/v2/email/outbound-emails")
		{
			Content = JsonContent.Create(data, null, JsonOptions)
		};
		using (var signer = new AWS4RequestSigner(_cloudOptions.AccountKey, _cloudOptions.SecretKey))
			await signer.Sign(request, "ses", _cloudOptions.Region);

		// make request
		using var lease = await _rateLimiter.AcquireAsync(1, cancellationToken);
		var client = _clientFactory.CreateClient();
		var result = await Retry.InvokeAsync(async ct =>
			{
				var response = await client.PostAsJsonAsync("https://postbox.cloud.yandex.net/v2/email/outbound-emails", data, JsonOptions, ct);
				await EnsureSuccessAsync(response);
				return await response.Content.ReadFromJsonAsync<SuccessResult>(JsonOptions, ct);
			},
			retryCount: 5,
			retryDelay: _rateWindow,
			retryWhen: ex =>
			{
				if (ex is YandexPostboxException { Error: YandexPostboxError.TooManyRequests })
				{
					_logger.LogDebug("Yandex.Cloud Postbox rate limit exceeded, retrying in {Delay} second", _rateWindow);
					return true;
				}
				return false;
			},
			cancellationToken: cancellationToken)
			?? throw new JsonException("JSON content required");;
		return result.MessageId;
	}

	static async Task EnsureSuccessAsync(HttpResponseMessage response)
	{
		if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.NotFound or HttpStatusCode.TooManyRequests)
		{
			var errorResult = await response.Content.ReadFromJsonAsync<ErrorResult>(JsonOptions);
			YandexPostboxError? error = errorResult switch
			{
				{ Code: "BadRequestException" } => YandexPostboxError.BadRequest,
				{ Code: "BadRequestException: sender is not allowed" } => YandexPostboxError.SenderNotAllowed,
				{ Code: "AccountSuspendedException" } => YandexPostboxError.AccountSuspended,
				{ Code: "SendingPausedException" } => YandexPostboxError.SendingPaused,
				{ Code: "MessageRejected" } => YandexPostboxError.MessageRejected,
				{ Code: "MailFromDomainNotVerifiedException" } => YandexPostboxError.MailFromDomainNotVerified,
				{ Code: "NotFoundException" } => YandexPostboxError.NotFound,
				{ Code: "TooManyRequestsException" } => YandexPostboxError.TooManyRequests,
				{ Code: "LimitExceededException" } => YandexPostboxError.LimitExceeded,
				_ => null
			};
			if (errorResult is not null && error is not null)
			{
				string message = $"Postbox {errorResult.Code}";
				if (errorResult.Message is { } msg)
					message += $": {msg}";
				throw new YandexPostboxException(message, error.Value);
			}
		}
		response.EnsureSuccessStatusCode();
	}

	object GetMessageData(MailMessage message)
	{
		if (string.IsNullOrEmpty(message.Subject))
			throw new ArgumentException("Message subject is required", nameof(message));
		if (string.IsNullOrEmpty(message.Body))
			throw new ArgumentException("Message body is required", nameof(message));

		var from = message.From
			?? _mailOptions.DefaultAddress
			?? throw new ArgumentException("Mail default address is required");
		object content;
		if (message.ReplyToList.Count == 0 && message.Attachments.Count == 0)
		{
			object body = message.IsBodyHtml
				? new { Html = new TextValue(message.Body) }
				: new { Text = new TextValue(message.Body) };
			content = new
			{
				Simple = new
				{
					Subject = new TextValue(message.Subject),
					Headers = message.Headers.AllKeys
						.NotNull()
						.Select(key => new
						{
							Name = key,
							Value = message.Headers[key]
						})
						.ToArray(),
					Body = body
				}
			};
		}
		else
		{
			var mimeMessage = CreateMimeMessage(message, from);
			using var ms = new MemoryStream();
			mimeMessage.WriteTo(ms);
			content = new
			{
				Raw = new
				{
					Data = Convert.ToBase64String(ms.GetBuffer(), 0, (int)ms.Length)
				}
			};
		}
		return new
		{
			FromEmailAddress = from.ToString(),
			Destination = new
			{
				ToAddresses = message.To.Select(a => a.ToString()).ToArray(),
				CcAddresses = message.CC.Select(a => a.ToString()).ToArray(),
				BccAddresses = message.Bcc.Select(a => a.ToString()).ToArray()
			},
			Content = content
		};
	}

	/// <summary>
	/// Creates correct <see cref="MimeMessage"/> from <see cref="MailMessage"/>.
	/// </summary>
	static MimeMessage CreateMimeMessage(MailMessage message, MailAddress from)
	{
		const int maxLineLength = 998 / 2 - 2;
		var body = message.Body.AsSpan();
		if (TextLineExceeds(body, maxLineLength))
		{
			// limit body line length
			StringBuilder result = new(body.Length);
			if (!message.IsBodyHtml)
			{
				result.AppendLine("<!DOCTYPE html>");
				result.AppendLine("<html>");
				result.AppendLine("<head>");
				result.AppendLine("</head>");
				result.AppendLine("<body>");
			}

			foreach (var bodyLine in body.EnumerateLines())
			{
				ReadOnlySpan<char> line = bodyLine;
				if (!message.IsBodyHtml && bodyLine.ContainsAny(['<', '>']))
					line = bodyLine.ToString().Replace("<", "&lt;").Replace(">", "&gt;");

				while (line.Length > maxLineLength)
				{
					int open = line.IndexOf('<');
					if (open == -1)
					{
						var sub = LimitLine(line, maxLineLength);
						result.Append(sub);
						result.AppendLine();
						line = line[sub.Length..];
					}
					else
					{
						int close = line[(open + 1)..].IndexOf('>') + open + 1;
						if (open != 0)
						{
							var sub = LimitLine(line, Math.Min(open, maxLineLength));
							result.Append(sub);
							result.AppendLine();
							line = line[sub.Length..];
						}
						else if (close == -1)
						{
							result.Append(line);
							line = string.Empty;
						}
						else
						{
							result.Append(line[open..(close + 1)]);
							result.AppendLine();
							line = line[(close + 1)..];
						}
					}
				}
				result.Append(line);
				result.AppendLine();
			}

			if (!message.IsBodyHtml)
			{
				result.AppendLine("</body>");
				result.AppendLine("<html>");
			}

			MailMessage copy = new()
			{
				HeadersEncoding = message.HeadersEncoding,
				Subject = message.Subject,
				SubjectEncoding = message.SubjectEncoding,
				Body = result.ToString(),
				BodyEncoding = message.BodyEncoding,
				BodyTransferEncoding = message.BodyTransferEncoding,
				IsBodyHtml = true,
				Priority = message.Priority,
			};
			if (message.Sender != null)
				copy.Sender = message.Sender;
			if (message.From != null)
				copy.From = message.From;
			foreach (var address in message.To)
				copy.To.Add(address);
			foreach (var address in message.CC)
				copy.CC.Add(address);
			foreach (var address in message.Bcc)
				copy.Bcc.Add(address);
			foreach (var address in message.ReplyToList)
				copy.ReplyToList.Add(address);
			foreach (var field in message.Headers.AllKeys)
				copy.Headers.Set(field, message.Headers[field]);
			foreach (var attachment in message.Attachments)
				copy.Attachments.Add(attachment);
			foreach (var view in message.AlternateViews)
				copy.AlternateViews.Add(view);
			message = copy;
		}

		var msg = MimeMessage.CreateFromMailMessage(message);
		if (message.From == null)
		{
			msg.Headers.Replace(HeaderId.From, string.Empty);
			msg.From.Add((MailboxAddress)from);
		}

		return msg;
	}

	/// <summary>
	/// Checks if any line in the text exceeds the specified maximum length.
	/// </summary>
	static bool TextLineExceeds(ReadOnlySpan<char> text, int maxLength)
	{
		foreach (var range in text.Split('\n'))
		{
			var line = text[range];
			if (line.Length > maxLength)
				return true;
		}
		return false;
	}

	/// <summary>
	/// Limits the line length to the specified maximum length.
	/// </summary>
	static ReadOnlySpan<char> LimitLine(ReadOnlySpan<char> line, int maxLength)
	{
		if (line.Length <= maxLength)
			return line;

		var sub = line[..maxLength];
		if (line[maxLength] == '\n')
			return sub;

		int space = sub.LastIndexOf(' ');
		return space == -1 ? sub : sub[..space];
	}

	record TextValue(string Data, string Charset = "UTF-8");

	record SuccessResult(string MessageId);

	record ErrorResult(string Code, string? Message = null);
}