using Microsoft.Extensions.FileProviders;

namespace Yandex.Cloud;

/// <summary>
/// Provides access to a file storage.
/// </summary>
public interface IStorage
{
	/// <summary>
	/// Returns all files list.
	/// </summary>
	Task<ImmutableArray<IStorageFile>> ListAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// Returns files list with specified prefix.
	/// </summary>
	/// <param name="prefix">Optional filter prefix.</param>
	Task<ImmutableArray<IStorageFile>> ListAsync(string? prefix, CancellationToken cancellationToken = default);

	/// <summary>
	/// Returns file by path.
	/// </summary>
	/// <param name="path">File path.</param>
	Task<IStorageFile> GetAsync(string path, CancellationToken cancellationToken = default);

	/// <summary>
	/// Returns if file exists by path.
	/// </summary>
	/// <param name="path">File path.</param>
	Task<bool> ExistsAsync(string path, CancellationToken cancellationToken = default);

	/// <summary>
	/// Returns public URL file can be accessed by.
	/// </summary>
	/// <param name="path">File path.</param>
	Task<string?> GetPublicUrlAsync(string path, string? fileName = null, bool download = false, CancellationToken cancellationToken = default);

	/// <summary>
	/// Creates file with specified path and content.
	/// </summary>
	/// <param name="path">File path.</param>
	/// <param name="stream">File content.</param>
	Task CreateAsync(string path, Stream stream, CancellationToken cancellationToken = default);

	/// <summary>
	/// Moves file with specified path to destination path.
	/// </summary>
	/// <param name="path">File path.</param>
	/// <param name="destinationPath">File destination path.</param>
	Task<bool> MoveAsync(string path, string destinationPath, CancellationToken cancellationToken = default);

	/// <summary>
	/// Deletes file by path.
	/// </summary>
	/// <param name="path">File path.</param>
	/// <returns><c>True</c> if file was deleted. Otherwise, <c>false</c>.</returns>
	Task<bool> DeleteAsync(string path, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a file in storage.
/// </summary>
public interface IStorageFile : IFileInfo
{
	/// <summary>
	/// Gets file or directory full path.
	/// </summary>
	string FullName { get; }
}