using System.Net;
using System.Net.Http.Headers;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

namespace Yandex.Cloud;

/// <summary>
/// Provides access to Yandex.Cloud object storage.
/// </summary>
public class YandexStorage : IStorage, IDisposable
{
	static readonly TimeSpan PublicUrlExpiry = TimeSpan.FromDays(1);
	const char PathDelimiter = '/';
	readonly AmazonS3Client _client;
	readonly string _bucketName;
	readonly List<WeakReference<File>> _fileRefs = [];

	public YandexStorage(IOptions<YandexCloudOptions> cloudOptions, IOptions<YandexStorageOptions> storageOptions)
	{
		var options = cloudOptions.Value;
		if (string.IsNullOrEmpty(options.AccountKey))
			throw new ArgumentException("Yandex.Cloud option AccountKey is required", nameof(cloudOptions));
		if (string.IsNullOrEmpty(options.SecretKey))
			throw new ArgumentException("Yandex.Cloud option SecretKey is required", nameof(cloudOptions));

		var credentials = new BasicAWSCredentials(options.AccountKey, options.SecretKey);
		_client = new AmazonS3Client(credentials, new AmazonS3Config
		{
			ServiceURL = "https://s3.yandexcloud.net"
		});
		_bucketName = storageOptions.Value.Bucket ?? throw new ArgumentException("Yandex.Cloud storage option Bucket is required", nameof(storageOptions));
	}

	/// <inheritdoc />
	public void Dispose()
	{
		foreach (var fileRef in _fileRefs)
		{
			if (fileRef.TryGetTarget(out var file))
				file.Dispose();
		}
		_fileRefs.Clear();
		_client.Dispose();
	}

	/// <inheritdoc />
	public Task<ImmutableArray<IStorageFile>> ListAsync(CancellationToken cancellationToken = default)
		=> ListAsync(null, cancellationToken);

	/// <inheritdoc />
	public async Task<ImmutableArray<IStorageFile>> ListAsync(string? prefix, CancellationToken cancellationToken = default)
	{
		var response = await _client.ListObjectsV2Async(new() { BucketName = _bucketName, Prefix = prefix }, cancellationToken);
		return response.S3Objects
			.Select(r => new File(r))
			.Cast<IStorageFile>()
			.ToImmutableArray();
	}

	/// <inheritdoc />
	public async Task<IStorageFile> GetAsync(string path, CancellationToken cancellationToken = default)
	{
		try
		{
			var response = await _client.GetObjectAsync(_bucketName, path, cancellationToken);
			var file = new File(response);
			_fileRefs.Add(new(file));
			return file;
		}
		catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
		{
			return new NotFoundFile(path);
		}
	}

	/// <inheritdoc />
	public async Task<bool> ExistsAsync(string path, CancellationToken cancellationToken = default)
	{
		try
		{
			await _client.GetObjectMetadataAsync(_bucketName, path, cancellationToken);
			return true;
		}
		catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
		{
			return false;
		}
	}

	/// <inheritdoc />
	public async Task<string?> GetPublicUrlAsync(string path, string? fileName = null, bool download = false, CancellationToken cancellationToken = default)
	{
		try
		{
			await _client.GetObjectMetadataAsync(_bucketName, path, cancellationToken);
		}
		catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
		{
			return null;
		}

		var request = new GetPreSignedUrlRequest()
		{
			BucketName = _bucketName,
			Key = path,
			Expires = DateTime.UtcNow.Add(PublicUrlExpiry)
		};
		if (!string.IsNullOrEmpty(fileName))
		{
			request.ResponseHeaderOverrides.ContentDisposition = new ContentDispositionHeaderValue(download ? "attachment" : "inline")
			{
				FileNameStar = fileName
			}.ToString();
		}
		return await _client.GetPreSignedURLAsync(request);
	}

	/// <inheritdoc />
	public async Task CreateAsync(string path, Stream stream, CancellationToken cancellationToken = default)
	{
		ArgumentException.ThrowIfNullOrEmpty(path);
		if (path.EndsWith('/'))
			throw new ArgumentException("Create subpath can not end with '/'", nameof(path));

		if (GetDirectoryPath(path) is string dir)
			await CreateDirectoryAsync(dir, cancellationToken);
		await _client.PutObjectAsync(new()
		{
			BucketName = _bucketName,
			Key = path,
			InputStream = stream,
			AutoCloseStream = false
		}, cancellationToken);
	}

	/// <inheritdoc />
	public async Task<bool> MoveAsync(string path, string destinationPath, CancellationToken cancellationToken = default)
	{
		if (!await ExistsAsync(path, cancellationToken))
			return false;

		if (GetDirectoryPath(destinationPath) is string dir)
			await CreateDirectoryAsync(dir, cancellationToken);
		await _client.CopyObjectAsync(new()
		{
			SourceBucket = _bucketName,
			SourceKey = path,
			DestinationBucket = _bucketName,
			DestinationKey = destinationPath
		}, cancellationToken);
		await _client.DeleteObjectAsync(_bucketName, path, cancellationToken);
		return true;
	}

	/// <inheritdoc />
	public async Task<bool> DeleteAsync(string path, CancellationToken cancellationToken = default)
	{
		try
		{
			var response = await _client.DeleteObjectAsync(_bucketName, path, cancellationToken);
			return response.HttpStatusCode == HttpStatusCode.NoContent;
		}
		catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
		{
			return false;
		}
	}

	/// <summary>
	/// Returns directory path for specified <paramref name="path"/>.
	/// </summary>
	/// <param name="path">Path to get directory for.</param>
	static string? GetDirectoryPath(string path)
	{
		var parts = path.Split(PathDelimiter, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.RemoveEmptyEntries);
		return parts.Length <= 1 ? null : parts.SkipLast(1).Join(PathDelimiter);
	}

	/// <summary>
	/// Creates directory for specified <paramref name="path"/>.
	/// </summary>
	/// <param name="path">Directory path to create.</param>
	async Task CreateDirectoryAsync(string path, CancellationToken cancellationToken)
	{
		string dirPath = "";
		foreach (string part in path.Split(PathDelimiter, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.RemoveEmptyEntries))
		{
			dirPath += part + PathDelimiter;
			try
			{
				await _client.GetObjectMetadataAsync(_bucketName, dirPath, cancellationToken);
			}
			catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
			{
				await _client.PutObjectAsync(new() { BucketName = _bucketName, Key = dirPath }, cancellationToken);
			}
		}
	}

	/// <summary>
	/// Represents file in Yandex.Cloud object storage.
	/// </summary>
	public class File : IStorageFile, IDisposable
	{
		readonly GetObjectResponse? _getResponse;
		bool _disposed;

		public string Name { get; }
		public string FullName { get; }
		public bool Exists => true;
		public bool IsDirectory { get; }
		public long Length { get; }
		public DateTimeOffset LastModified { get; }
		public string? PhysicalPath => null;

		public File(S3Object obj)
		{
			Name = Path.GetFileName(obj.Key.TrimEnd('/'));
			FullName = obj.Key;
			IsDirectory = obj.Key.EndsWith('/');
			Length = obj.Size;
			LastModified = obj.LastModified;
		}

		public File(GetObjectResponse response)
		{
			Name = Path.GetFileName(response.Key.TrimEnd('/'));
			FullName = response.Key;
			IsDirectory = response.Key.EndsWith('/');
			Length = response.ContentLength;
			LastModified = response.LastModified;
			_getResponse = response;
		}

		public Stream CreateReadStream()
		{
			if (IsDirectory)
				throw new IOException("Can not create read stream for directory");
			if (_getResponse == null)
				throw new InvalidOperationException("Can not create read stream for listed object.");
			return _getResponse.ResponseStream;
		}

		public void Dispose()
		{
			if (_disposed)
				return;
			_getResponse?.Dispose();
			_disposed = true;
		}
	}

	/// <summary>
	/// Represents not found file in Yandex.Cloud object storage.
	/// </summary>
	public class NotFoundFile(string path) : NotFoundFileInfo(Path.GetFileName(path)), IStorageFile
	{
		public string FullName { get; } = path;
	}
}