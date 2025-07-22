using System.Net.Mail;

namespace Yandex.Cloud;

/// <summary>
/// Provides mail message modification before it is sent by <see cref="IMailService"/>.
/// </summary>
public interface IMailModifier
{
	/// <summary>
	/// Applies mail message modification.
	/// </summary>
	/// <param name="message">Message to modify.</param>
	void Apply(MailMessage message);
}