using System.Security.Cryptography;
using System.Text.Json.Serialization;
using Microsoft.IdentityModel.Tokens;
using Yandex.Cloud.Credentials;

namespace Yandex.Cloud;

/// <summary>
/// Represents an authorized key for Yandex.Cloud.
/// Supports deserializing from JSON.
/// </summary>
public record YandexAuthorizedKey
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
	public string? PublicKey { get; init; }

	[JsonPropertyName("private_key")]
	public required string PrivateKey { get; init; }

	/// <summary>
	/// Creates IAM JWT credentials provider using the private key and service account ID.
	/// </summary>
	/// <returns></returns>
	public IamJwtCredentialsProvider CreateCredentialsProvider()
	{
		using var rsa = RSA.Create();
		rsa.ImportFromPem(PrivateKey);
		var key = new RsaSecurityKey(rsa.ExportParameters(true)) { KeyId = Id };
		return new(key, ServiceAccountId);
	}
}