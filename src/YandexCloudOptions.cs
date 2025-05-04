namespace Yandex.Cloud;

/// <summary>
/// Provides authorization options for Yandex.Cloud.
/// Not all properties are required.
/// </summary>
public record YandexCloudOptions
{
	/// <summary>
	/// Gets or sets authorized key options. Required for:
	/// <list type="bullet">
	///	<item><see cref="Sdk"/></item>
	/// </list>
	/// </summary>
	public YandexAuthorizedKey? AuthorizedKey { get; set; }

	/// <summary>
	/// Gets or sets account key. Required for:
	/// <list type="bullet">
	///	<item><see cref="YandexStorage"/></item>
	///	<item><see cref="YandexPostbox"/></item>
	/// <item><see cref="YandexDataStream"/></item>
	/// </list>
	/// </summary>
	public string? AccountKey { get; set; }

	/// <summary>
	/// Gets or sets account secret key. Required for:
	/// <list type="bullet">
	///	<item><see cref="YandexStorage"/></item>
	///	<item><see cref="YandexPostbox"/></item>
	/// <item><see cref="YandexDataStream"/></item>
	/// </list>
	/// </summary>
	public string? SecretKey { get; set; }

	/// <summary>
	/// Gets or sets API key.
	/// <list type="bullet">
	///	<item><see cref="YandexSpeechKit"/></item>
	/// </list>
	/// </summary>
	public string? ApiKey { get; set; }

	/// <summary>
	/// Gets or sets region. Default is "ru-central1".
	/// </summary>
	public string Region { get; set; } = "ru-central1";

	/// <summary>
	/// Get or sets cloud folder identifier. Required for:
	/// <list type="bullet">
	///	<item><see cref="YandexDataStream"/></item>
	/// </list>
	/// </summary>
	public string? FolderId { get; set; }
}