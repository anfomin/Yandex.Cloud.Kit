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

	/// <summary>
	/// Gets or sets email send rate limit per second.
	/// Default is <c>1</c> and equals to Yandex.Postbox default rate limit.
	/// </summary>
	public int RateLimit
	{
		get;
		set
		{
			if (value <= 0)
				throw new ArgumentException("RateLimit must be greater than zero.", nameof(RateLimit));
			field = value;
		}
	} = 1;
}