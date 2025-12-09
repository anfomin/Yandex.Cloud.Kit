namespace Yandex.Cloud;

/// <summary>
/// Exception thrown when Yandex.Cloud Postbox returned specific <paramref name="error"/>.
/// </summary>
/// <param name="message">The error message that explains the reason for the exception.</param>
/// <param name="error">Yandex.Cloud Postbox error type.</param>
/// <param name="innerException">The exception that is the cause of the current exception.</param>
public class YandexPostboxException(string message, YandexPostboxError error, Exception? innerException = null)
	: Exception(message, innerException)
{
	/// <summary>
	/// Gets Yandex.Cloud Postbox error type.
	/// </summary>
	public YandexPostboxError Error { get; } = error;
}

/// <summary>
/// Represents Yandex.Cloud Postbox error type.
/// </summary>
public enum YandexPostboxError
{
	/// <summary>
	/// Malformed request.
	/// </summary>
	BadRequest,

	/// <summary>
	/// Sends is not allowed for configured address.
	/// </summary>
	SenderNotAllowed,

	/// <summary>
	/// Service account disallowed to send emails forever.
	/// </summary>
	AccountSuspended,

	/// <summary>
	/// Services account disallowed to send emails temporarily.
	/// </summary>
	SendingPaused,

	/// <summary>
	/// Message contains invalid data.
	/// </summary>
	MessageRejected,

	/// <summary>
	/// <c>From</c> mail address is not verified.
	/// </summary>
	MailFromDomainNotVerified,

	/// <summary>
	/// Requested resource not found.
	/// </summary>
	NotFound,

	/// <summary>
	/// Sending quota exceeded.
	/// </summary>
	TooManyRequests,

	/// <summary>
	/// Sending limit exceeded.
	/// </summary>
	LimitExceeded
}