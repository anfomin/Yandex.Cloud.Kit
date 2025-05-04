namespace Yandex.Cloud;

/// <summary>
/// Provides information about a Yandex.Cloud Postbox event.
/// </summary>
public record YandexPostboxEvent
{
	/// <summary>
	/// Gets event identifier.
	/// </summary>
	public required string EventId { get; init; }

	/// <summary>
	/// Gets event type.
	/// </summary>
	public required Type EventType { get; init; }

	/// <summary>
	/// Gets mail message info.
	/// </summary>
	public required MailData Mail { get; init; }

	/// <summary>
	/// Gets delivery data when <see cref="EventType"/> is <see cref="Type.Delivery"/>.
	/// </summary>
	public DeliveryData? Delivery { get; init; }

	/// <summary>
	/// Gets bounce data when <see cref="EventType"/> is <see cref="Type.Bounce"/>.
	/// </summary>
	public BounceData? Bounce { get; init; }

	/// <summary>
	/// Represents PostBox event types.
	/// </summary>
	public enum Type
	{
		/// <summary>
		/// Message was sent.
		/// </summary>
		Send,

		/// <summary>
		/// Message was delivered.
		/// </summary>
		Delivery,

		/// <summary>
		/// Message was rejected.
		/// </summary>
		Bounce
	}

	/// <summary>
	/// Provides mail message information.
	/// </summary>
	public record MailData
	{
		/// <summary>
		/// Gets message timestamp.
		/// </summary>
		public required DateTime Timestamp { get; init; }

		/// <summary>
		/// Gets Postbox identity identifier.
		/// </summary>
		public required string IdentityId { get; init; }

		/// <summary>
		/// Gets message identifier.
		/// </summary>
		public required string MessageId { get; init; }
	}

	/// <summary>
	/// Provides delivery informatin.
	/// </summary>
	public record DeliveryData
	{
		/// <summary>
		/// Gets delivery timestamp.
		/// </summary>
		public required DateTime Timestamp { get; init; }

		/// <summary>
		/// Gets recipient emails.
		/// </summary>
		public required string[] Recipients { get; init; }

		/// <summary>
		/// Gets delivery processing time in milliseconds.
		/// </summary>
		public int ProcessingTimeMillis { get; init; }
	}

	public record BounceData
	{
		/// <summary>
		/// Gets bounce timestamp.
		/// </summary>
		public required DateTime Timestamp { get; init; }

		/// <summary>
		/// Gets bounce type.
		/// </summary>
		public required BounceType BounceType { get; init; }

		/// <summary>
		/// Gets information about bounces recipients.
		/// </summary>
		public required BounceRecipient[] BouncedRecipients { get; init; }
	}

	/// <summary>
	/// Provides information abount bounced recipient.
	/// </summary>
	public record BounceRecipient
	{
		/// <summary>
		/// Gets recipient email address.
		/// </summary>
		public required string EmailAddress { get; init; }

		/// <summary>
		/// Gets SMTP status.
		/// </summary>
		public string? Status { get; init; }

		/// <summary>
		/// Gets SMTP diagnostic description.
		/// </summary>
		public string? DiagnosticCode { get; init; }
	}

	/// <summary>
	/// Represents bounce type.
	/// </summary>
	public enum BounceType
	{
		/// <summary>
		/// Message is undelivered.
		/// </summary>
		Permenent
	}

	/// <summary>
	/// Represents bounce subtype.
	/// </summary>
	public enum BounceSubType
	{
		/// <summary>
		/// See diagnostics code.
		/// </summary>
		Undetermined,

		/// <summary>
		/// Recipient mail address is in stop list.
		/// </summary>
		Suppressed
	}
}