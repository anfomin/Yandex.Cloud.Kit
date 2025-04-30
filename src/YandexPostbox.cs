using System.Net.Http.Json;
using System.Net.Mail;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using Aws4RequestSigner;
using Microsoft.Extensions.Options;

namespace Yandex.Cloud;

/// <summary>
/// Provides email sending using Yandex.Cloud Postbox.
/// </summary>
public class YandexPostbox(
	IHttpClientFactory clientFactory,
	IOptions<YandexCloudOptions> cloudOptions,
	IOptions<YandexMailOptions> mailOptions
) : IMailService
{
	static readonly JsonSerializerOptions JsonOptions = new()
	{
		Encoder = JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All)
	};
	readonly IHttpClientFactory _clientFactory = clientFactory;
	readonly YandexCloudOptions _cloudOptions = cloudOptions.Value;
	readonly MailAddress _defaultAddress = mailOptions.Value.DefaultAddress ?? throw new ArgumentException("Mail default address is required");

	/// <inheritdoc />
	public async Task<Result<string>> SendMailAsync(IEnumerable<MailAddress> to, string subject, string bodyHtml, CancellationToken cancellationToken = default)
	{
		ArgumentException.ThrowIfNullOrEmpty(subject);
		ArgumentException.ThrowIfNullOrEmpty(bodyHtml);

		var requestData = new
		{
			FromEmailAddress = _defaultAddress.ToString(),
			Destination = new
			{
				ToAddresses = to
					.Select(a => a.ToString())
					.ToArray()
			},
			Content = new
			{
				Simple = new
				{
					Subject = CreateJsonText(subject),
					Body = new
					{
						Html = CreateJsonText(bodyHtml)
					}
				}
			}
		};
		var request = new HttpRequestMessage(HttpMethod.Post, "https://postbox.cloud.yandex.net/v2/email/outbound-emails")
		{
			Content = JsonContent.Create(requestData, null, JsonOptions)
		};
		using (var signer = new AWS4RequestSigner(_cloudOptions.AccountKey, _cloudOptions.SecretKey))
			await signer.Sign(request, "ses", "ru-central1");

		var client = _clientFactory.CreateClient();
		using var response = await client.SendAsync(request, cancellationToken);
		if (!response.IsSuccessStatusCode)
		{
			var error = await response.Content.ReadFromJsonAsync<JsonNode>(JsonOptions, cancellationToken);
			return Result<string>.Error(error?["message"]?.GetValue<string>() ?? $"Invalid status code {response.StatusCode}");
		}

		var responseContent = await response.Content.ReadFromJsonAsync<JsonNode>(JsonOptions, cancellationToken)
			?? throw new JsonException("JSON content required");
		return responseContent["MessageId"]!.GetValue<string>();
	}

	static object CreateJsonText(string text)
		=> new
		{
			Charset = "UTF-8",
			Data = text
		};
}