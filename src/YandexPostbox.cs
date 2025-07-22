using System.Net.Http.Json;
using System.Net.Mail;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using Aws4RequestSigner;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Yandex.Cloud;

/// <summary>
/// Provides email sending using Yandex.Cloud Postbox.
/// </summary>
public class YandexPostbox : IMailService
{
	static readonly JsonSerializerOptions JsonOptions = new()
	{
		Encoder = JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All)
	};
	readonly IHttpClientFactory _clientFactory;
	readonly YandexCloudOptions _cloudOptions;
	readonly YandexMailOptions _mailOptions;
	readonly IEnumerable<IMailFilter> _filters;
	readonly IEnumerable<IMailModifier> _modifiers;

	public YandexPostbox(
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

		_clientFactory = clientFactory;
		_cloudOptions = options;
		_mailOptions = mailOptions.Value;
		_filters = filters ?? [];
		_modifiers = modifiers ?? [];
	}

	/// <inheritdoc />
	public async Task<string> SendMailAsync(MailMessage message, CancellationToken cancellationToken = default)
	{
		if (_filters.Any(f => !f.ShouldSend(message)))
			throw new MailFilteredException();
		foreach (var modifier in _modifiers)
			modifier.Apply(message);

		var data = GetMessageData(message);
		var request = new HttpRequestMessage(HttpMethod.Post, "https://postbox.cloud.yandex.net/v2/email/outbound-emails")
		{
			Content = JsonContent.Create(data, null, JsonOptions)
		};
		using (var signer = new AWS4RequestSigner(_cloudOptions.AccountKey, _cloudOptions.SecretKey))
			await signer.Sign(request, "ses", _cloudOptions.Region);

		var client = _clientFactory.CreateClient();
		using var response = await client.SendAsync(request, cancellationToken);
		if (!response.IsSuccessStatusCode)
		{
			var error = await response.Content.ReadFromJsonAsync<JsonNode>(JsonOptions, cancellationToken);
			string errorMessage = error?["message"]?.GetValue<string>() ?? $"Invalid status code {response.StatusCode}";
			throw new InvalidOperationException(errorMessage);
		}

		var responseContent = await response.Content.ReadFromJsonAsync<JsonNode>(JsonOptions, cancellationToken)
			?? throw new JsonException("JSON content required");
		return responseContent["MessageId"]!.GetValue<string>();
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
			var mimeMessage = MimeMessage.CreateFromMailMessage(message);
			if (message.From == null)
			{
				mimeMessage.Headers.Replace(HeaderId.From, string.Empty);
				mimeMessage.From.Add((MailboxAddress)from);
			}
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

	record TextValue(string Data, string Charset = "UTF-8");
}