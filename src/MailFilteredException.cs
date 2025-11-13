namespace Yandex.Cloud;

/// <summary>
/// Represents an exception that occurs when a mail message is filtered and not sent.
/// </summary>
public class MailFilteredException : Exception
{
	/// <summary>
	/// Initializes a new instance of the <see cref="MailFilteredException"/> class.
	/// </summary>
	public MailFilteredException() : base("Mail message was filtered and not sent.") { }

	/// <summary>
	/// Initializes a new instance of the <see cref="MailFilteredException"/> class with a specified error message.
	/// </summary>
	/// <param name="message">The error message that explains the reason for the exception.</param>
	public MailFilteredException(string message) : base(message) { }
}