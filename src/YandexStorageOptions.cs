namespace Yandex.Cloud;

/// <summary>
/// Provides options for <see cref="YandexStorage"/>
/// </summary>
public record YandexStorageOptions
{
	/// <summary>
	/// Gets or sets the Yandex.Cloud storage bucket name.
	/// </summary>
	public string? Bucket { get; set; }
}