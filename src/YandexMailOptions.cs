using System.Net.Mail;

namespace Yandex.Cloud;

/// <summary>
/// Provides options for <see cref="YandexPostbox"/>.
/// </summary>
public record YandexMailOptions
{
	/// <summary>
	/// Gets or sets default From mail address.
	/// This can be email or «name &lt;email&gt;».
	/// </summary>
	public string? Default
	{
		get => DefaultAddress?.ToString();
		set => DefaultAddress = value != null ? new(value) : null;
	}

	/// <summary>
	/// Gets or set default From <see cref="MailAddress"/>.
	/// </summary>
	public MailAddress? DefaultAddress { get; set; }
}