using System.Net.Mail;

namespace Yandex.Cloud;

/// <summary>
/// Service for sending email messages.
/// </summary>
public interface IMailService
{
	/// <summary>
	/// Sends email message.
	/// </summary>
	/// <param name="to">Destination addresses.</param>
	/// <param name="subject">Email subject.</param>
	/// <param name="bodyHtml">Email body in HTML format.</param>
	Task<Result<string>> SendMailAsync(IEnumerable<MailAddress> to, string subject, string bodyHtml, CancellationToken cancellationToken = default);
}

public static class MailServiceExtensions
{
	/// <summary>
	/// Sends email message.
	/// </summary>
	/// <param name="to">Destination address.</param>
	/// <param name="subject">Email subject.</param>
	/// <param name="bodyHtml">Email body in HTML format.</param>
	public static Task<Result<string>> SendMailAsync(this IMailService mailService, MailAddress to, string subject, string bodyHtml, CancellationToken cancellationToken = default)
		=> mailService.SendMailAsync([to], subject, bodyHtml, cancellationToken);
}