using System.Globalization;
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
/// Provides text to speech conversion via Yandex.Cloud SpeechKit.
/// </summary>
public class YandexSpeechKit
{
	static readonly JsonSerializerOptions JsonOptions = new()
	{
		Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
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
	/// Synthesizes the specified text into speech and writes the result to the <paramref name="output"/> stream.
	/// </summary>
	/// <param name="text">Text to synthesis.</param>
	/// <param name="output">Audio output stream.</param>
	/// <param name="voice">Voice for speech synthesis. Default is <see cref="Voice.Russian.Marina"/>.</param>
	/// <param name="volume">
	/// Regulates normalization level:
	/// <list type="bullet">
	/// <item>For <see cref="AudioNormalization.LUFS"/> volume changes in a range <c>[-145;0)</c>, default is <c>-19</c>.</item>
	///	<item>For <see cref="AudioNormalization.MaxPeak"/> volume changes in a range <c>(0;1]</c>, default is <c>0.7</c>.</item>
	/// </list>
	/// </param>
	/// <param name="speed">Changes speaker's speed in a range <c>[0.1;3]</c>. Default is <c>1</c>.</param>
	/// <param name="pitchShift">
	/// Increases (or decreases) speaker's pitch, measured in Hz, in a range <c>[-1000;1000]</c>. Default is <c>0</c>.
	/// </param>
	/// <param name="container">Output container type. Default is <see cref="AudioContainer.Mp3"/>.</param>
	/// <param name="unsafeMode">
	/// Automatically split long text to several utterances and bill accordingly.
	/// Some degradation in service quality is possible. Default is <c>false</c>.
	/// </param>
	public async Task SynthesizeAsync(string text, Stream output,
		Voice? voice = null,
		AudioVolume? volume = null,
		double speed = 1,
		int pitchShift = 0,
		AudioContainer container = AudioContainer.Mp3,
		bool unsafeMode = false,
		CancellationToken cancellationToken = default)
	{
		_logger.LogDebug("Converting text to speech: {Text}", text);
		voice ??= Voice.Russian.Marina;
		List<object> hints = [new { Voice = voice.Key }];
		if (voice.RoleType is { } role)
			hints.Add(new { Role = role });
		if (volume is { } v)
			hints.Add(new { Volume = v.Value.ToString(CultureInfo.InvariantCulture) });
		if (Math.Abs(speed - 1) >= 0.01)
			hints.Add(new { Speed = Math.Round(speed, 2).ToString(CultureInfo.InvariantCulture) });
		if (pitchShift != 0)
			hints.Add(new { PitchShift = pitchShift.ToString(CultureInfo.InvariantCulture) });

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
				Hints = hints,
				UnsafeMode = unsafeMode
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