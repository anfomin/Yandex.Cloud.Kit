namespace Yandex.Cloud;

/// <summary>
/// Regulates normalization level.
/// </summary>
/// <param name="Normalization">Type of loudness normalization.</param>
/// <param name="Value">
/// Changes in a range:
/// <list type="bullet">
/// <item>For <see cref="AudioNormalization.LUFS"/>: <c>[-145;0)</c>, default is <c>-19</c>.</item>
///	<item>For <see cref="AudioNormalization.MaxPeak"/>: <c>(0;1]</c>, default is <c>0.7</c>.</item>
/// </list>
/// </param>
public record struct AudioVolume(AudioNormalization Normalization, double Value)
{
	/// <summary>
	/// Default normalization level for <see cref="AudioNormalization.LUFS"/>.
	/// </summary>
	public static readonly AudioVolume LUFS = new(AudioNormalization.LUFS, -19);

	/// <summary>
	/// Default normalization level for <see cref="AudioNormalization.MaxPeak"/>.
	/// </summary>
	public static readonly AudioVolume MaxPeak = new(AudioNormalization.MaxPeak, 0.7);
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