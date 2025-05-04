namespace Yandex.Cloud;

/// <summary>
/// Provides options for <see cref="YandexDataStream"/>.
/// </summary>
public record YandexDataStreamOptions
{
	/// <summary>
	/// Gets or sets the Yandex.Cloud steam database identifier.
	/// </summary>
	public string? DatabaseId { get; set; }

	/// <summary>
	/// Gets or sets data stream polling interval.
	/// </summary>
	public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(30);
}