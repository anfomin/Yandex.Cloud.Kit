using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using Microsoft.Extensions.Options;

namespace Yandex.Cloud;

/// <summary>
/// Provides text to speech conversion.
/// </summary>
public class YandexSpeechKit
{
	static readonly JsonSerializerOptions JsonOptions = new()
	{
		Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		Converters =
		{
			new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseUpper)
		}
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
	/// Converts the specified text to speech and writes the result to the <paramref name="output"/> stream.
	/// </summary>
	/// <param name="text">Text to convert.</param>
	/// <param name="output">Audio output stream.</param>
	/// <param name="volume">
	/// Regulates normalization level:
	/// <list type="bullet">
	/// <item>For <see cref="AudioNormalization.LUFS"/> volume changes in a range <c>[-145;0)</c>, default is <c>-19</c>.</item>
	///	<item>For <see cref="AudioNormalization.MaxPeak"/> volume changes in a range <c>(0;1]</c>, default is <c>0.7</c>.</item>
	/// </list>
	/// </param>
	/// <param name="container">Output container type.</param>
	public async Task ConvertTextToSpeechAsync(string text, Stream output,
		(AudioNormalization Normalization, double Value)? volume = null,
		AudioContainer container = AudioContainer.Mp3,
		CancellationToken cancellationToken = default)
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
						ContainerAudioType = container
					}
				},
				LoudnessNormalizationType = volume?.Normalization,
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
/// Type of loudness normalization.
/// </summary>
public enum AudioNormalization
{
	/// <summary>
	/// The type of normalization based on EBU R 128 recommendation.
	/// </summary>
	LUFS,

	/// <summary>
	/// The type of normalization, wherein the gain is changed to bring the highest PCM sample value or analog signal peak to a given level.
	/// </summary>
	MaxPeak
}

/// <summary>
/// Audio container types.
/// </summary>
public enum AudioContainer
{
	/// <summary>
	/// Data is encoded using MPEG-1/2 Layer III and compressed using the MP3 container format.
	/// </summary>
	Mp3,

	/// <summary>
	/// Audio bit depth 16-bit signed little-endian (Linear PCM).
	/// </summary>
	Wav,

	/// <summary>
	/// Data is encoded using the OPUS audio codec and compressed using the OGG container format.
	/// </summary>
	OggOpus
}