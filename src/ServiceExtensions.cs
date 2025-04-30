using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Yandex.Cloud;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Provides extension methods for registering Yandex.Cloud services.
/// </summary>
public static class YandexServiceExtensions
{
	/// <summary>
	/// Adds Yandex.Cloud <see cref="Sdk"/> and <see cref="YandexCloudOptions"/>.
	/// </summary>
	/// <returns>Builder for other Yandex.Cloud services registration.</returns>
	public static IYandexCloudBuilder AddYandexCloud(this IServiceCollection services, IConfiguration configuration)
	{
		services.Configure<YandexCloudOptions>(configuration);
		services.TryAddSingleton<Sdk>(s =>
		{
			var options = s.GetRequiredService<IOptions<YandexCloudOptions>>().Value;
			if (options.AuthorizedKey == null)
				throw new ApplicationException("Yandex.Cloud configuration AuthorizedKey is not set");
			return new Sdk(options.AuthorizedKey.CreateCredentialsProvider());
		});
		return new YandexCloudBuilder(services);
	}

	/// <summary>
	/// Registers Yandex.Cloud object storage as <see cref="IStorage"/>.
	/// </summary>
	/// <param name="configure">The action used to configure storage options.</param>
	public static IYandexCloudBuilder AddYandexStorage(this IYandexCloudBuilder builder, Action<YandexStorageOptions> configure)
	{
		builder.Services.TryAddTransient<IStorage, YandexStorage>();
		builder.Services.Configure(configure);
		return builder;
	}

	/// <summary>
	/// Registers Yandex.Cloud object storage as <see cref="IStorage"/>.
	/// </summary>
	/// <param name="config">The configuration being bound to <see cref="YandexStorageOptions"/>.</param>
	public static IYandexCloudBuilder AddYandexStorage(this IYandexCloudBuilder builder, IConfiguration config)
	{
		builder.Services.TryAddTransient<IStorage, YandexStorage>();
		builder.Services.Configure<YandexStorageOptions>(config);
		return builder;
	}

	/// <summary>
	/// Registers Yandex.Cloud Postbox as <see cref="IMailService"/>.
	/// </summary>
	/// <param name="configure">The action used to configure mail options.</param>
	public static IYandexCloudBuilder AddYandexPostbox(this IYandexCloudBuilder builder, Action<YandexMailOptions> configure)
	{
		builder.Services.TryAddTransient<IMailService, YandexPostbox>();
		builder.Services.Configure(configure);
		return builder;
	}

	/// <summary>
	/// Registers Yandex.Cloud Postbox as <see cref="IMailService"/>.
	/// </summary>
	/// <param name="config">The configuration being bound to <see cref="YandexMailOptions"/>.</param>
	public static IYandexCloudBuilder AddYandexPostbox(this IYandexCloudBuilder builder, IConfiguration config)
	{
		builder.Services.TryAddTransient<IMailService, YandexPostbox>();
		builder.Services.Configure<YandexMailOptions>(config);
		return builder;
	}

	/// <summary>
	/// Registers Yandex.Cloud <see cref="YandexSpeechKit"/> service.
	/// </summary>
	public static IYandexCloudBuilder AddYandexSpeechKit(this IYandexCloudBuilder builder)
	{
		builder.Services.TryAddTransient<YandexSpeechKit>();
		return builder;
	}
}