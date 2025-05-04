using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;

namespace Yandex.Cloud;

/// <summary>
/// Provides text to speech conversion.
/// </summary>
public class YandexSpeechKit
{
	static readonly JsonSerializerOptions JsonOptions = new()
	{
		Encoder = JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All),
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase
	};
	readonly ILogger _logger;
	readonly IHttpClientFactory _clientFactory;
	readonly YandexCloudOptions _options;

	public YandexSpeechKit(
		ILogger<YandexSpeechKit> logger,
		IHttpClientFactory clientFactory,
		IOptions<YandexCloudOptions> options)
	{
		_logger = logger;
		_clientFactory = clientFactory;
		_options = options.Value;
		if (string.IsNullOrEmpty(_options.ApiKey))
			throw new ArgumentException("Yandex.Cloud option ApiKey is required", nameof(options));
	}

	/// <summary>
	/// Converts the specified text to speech and writes the result to the output stream.
	/// </summary>
	/// <param name="text">Text to convert.</param>
	/// <param name="output">Audio output stream.</param>
	/// <param name="volume">Speech volume.</param>
	public async Task ConvertTextToSpeechAsync(string text, Stream output, (SpeechVolumeType Type, double Value)? volume = null, CancellationToken cancellationToken = default)
	{
		_logger.LogDebug("Converting text to speech: {Text}", text);
		var client = CreateClient();
		using var response = await client.PostAsJsonAsync("utteranceSynthesis",
			new
			{
				Text = text,
				OutputAudioSpec = new
				{
					ContainerAudio = new
					{
						ContainerAudioType = "MP3"
					}
				},
				LoudnessNormalizationType = volume?.Type switch
				{
					SpeechVolumeType.MaxPeak => "MAX_PEAK",
					_ => "LUFS"
				},
				Hints = new object[]
				{
					new
					{
						Voice = "jane",
						Volume = volume?.Value
					}
				}
			},
			JsonOptions,
			cancellationToken);
		response.EnsureSuccessStatusCode();

		var result = await response.Content.ReadFromJsonAsync<JsonNode>(JsonOptions, cancellationToken);
		string base64 = result?["result"]?["audioChunk"]?["data"]?.GetValue<string>()
			?? throw new InvalidOperationException("Failed to get audio data from response");
		byte[] bytes = Convert.FromBase64String(base64);
		await output.WriteAsync(bytes.AsMemory(), cancellationToken);
	}

	HttpClient CreateClient()
	{
		var client = _clientFactory.CreateClient();
		client.BaseAddress = new Uri("https://tts.api.cloud.yandex.net/tts/v3/");
		client.Timeout = TimeSpan.FromMilliseconds(30_000);
		client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Api-Key", _options.ApiKey);
		return client;
	}
}

/// <summary>
/// Represents different speech volume types.
/// </summary>
public enum SpeechVolumeType
{
	LUFS,
	MaxPeak
}