namespace Yandex.Cloud;

/// <summary>
/// Provides options for Yandex Cloud.
/// </summary>
public record YandexCloudOptions
{
	/// <summary>
	/// Gets or sets authorized key options.
	/// </summary>
	public YandexAuthorizedKey? AuthorizedKey { get; set; }

	/// <summary>
	/// Gets or sets account key.
	/// </summary>
	public string? AccountKey { get; set; }

	/// <summary>
	/// Gets or sets account secret key.
	/// </summary>
	public string? SecretKey { get; set; }

	/// <summary>
	/// Gets or sets API key.
	/// </summary>
	public string? ApiKey { get; set; }
}