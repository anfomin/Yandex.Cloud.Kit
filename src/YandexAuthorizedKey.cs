using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.IdentityModel.Tokens;
using Yandex.Cloud.Credentials;

namespace Yandex.Cloud;

/// <summary>
/// Represents an authorized key for Yandex.Cloud.
/// </summary>
public partial record YandexAuthorizedKey
{
	[JsonPropertyName("id")]
	public required string Id { get; init; }

	[JsonPropertyName("service_account_id")]
	public required string ServiceAccountId { get; init; }

	[JsonPropertyName("created_at")]
	public DateTime? CreatedAt { get; init; }

	[JsonPropertyName("key_algorithm")]
	public string? KeyAlgorithm { get; init; }

	[JsonPropertyName("public_key")]
	public string? PublicKey
	{
		get;
		init => field = ParseKey(value);
	}

	[JsonPropertyName("private_key")]
	public required string PrivateKey
	{
		get;
		init => field = ParseKey(value);
	}

	/// <summary>
	/// Creates IAM JWT credentials provider using the private key and service account ID.
	/// </summary>
	/// <returns></returns>
	public IamJwtCredentialsProvider CreateCredentialsProvider()
	{
		using var rsa = RSA.Create();
		rsa.ImportFromPem(PrivateKey);
		var key = new RsaSecurityKey(rsa.ExportParameters(true)) { KeyId = Id };
		return new IamJwtCredentialsProvider(key, ServiceAccountId);
	}

	/// <summary>
	/// Parses private or public key value from string replacing '-' with '/'.
	/// This method used to load configuration for systemd service.
	/// </summary>
	[return: NotNullIfNotNull(nameof(value))]
	static string? ParseKey(string? value)
		=> value == null ? null : DashRegex().Replace(value, match =>
			match.Groups["prefix"].Value
			+ new string('/', match.Groups["dashes"].Length)
			+ match.Groups["suffix"].Value
		);

	[GeneratedRegex(@"(?<prefix>[\w\+])(?<dashes>-{1,2})(?<suffix>[\w\+])")]
	private static partial Regex DashRegex();
}