using System.Net.Mail;

namespace Yandex.Cloud;

/// <summary>
/// Provides mail message validation.
/// </summary>
public interface IMailFilter
{
	/// <summary>
	/// Determines if mail message should be sent or skipped.
	/// </summary>
	/// <param name="message">Mail message to validate.</param>
	/// <returns><c>True</c> if message should be sent. <c>False</c> if message should be skipped.</returns>
	bool ShouldSend(MailMessage message);
}