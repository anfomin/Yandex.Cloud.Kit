using Microsoft.Extensions.DependencyInjection;

namespace Yandex.Cloud;

/// <summary>
/// Provides Yandex.Cloud services registration.
/// </summary>
public interface IYandexCloudBuilder
{
	/// <summary>
	/// Gets the <see cref="IServiceCollection"/> instance used to register Yandex.Cloud services.
	/// </summary>
	public IServiceCollection Services { get; }
}

internal class YandexCloudBuilder(IServiceCollection services) : IYandexCloudBuilder
{
	public IServiceCollection Services { get; } = services;
}