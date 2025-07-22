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
	public static IYandexCloudBuilder AddStorage(this IYandexCloudBuilder builder, Action<YandexStorageOptions> configure)
	{
		builder.Services.TryAddTransient<IStorage, YandexStorage>();
		builder.Services.Configure(configure);
		return builder;
	}

	/// <summary>
	/// Registers Yandex.Cloud object storage as <see cref="IStorage"/>.
	/// </summary>
	/// <param name="config">The configuration being bound to <see cref="YandexStorageOptions"/>.</param>
	public static IYandexCloudBuilder AddStorage(this IYandexCloudBuilder builder, IConfiguration config)
	{
		builder.Services.TryAddTransient<IStorage, YandexStorage>();
		builder.Services.Configure<YandexStorageOptions>(config);
		return builder;
	}

	/// <summary>
	/// Registers Yandex.Cloud Postbox as <see cref="IMailService"/>.
	/// </summary>
	/// <param name="configure">The action used to configure mail options.</param>
	public static IYandexCloudBuilder AddPostbox(this IYandexCloudBuilder builder, Action<YandexMailOptions> configure)
	{
		builder.Services.AddHttpClient();
		builder.Services.TryAddTransient<IMailService, YandexPostbox>();
		builder.Services.Configure(configure);
		return builder;
	}

	/// <summary>
	/// Registers Yandex.Cloud Postbox as <see cref="IMailService"/>.
	/// </summary>
	/// <param name="config">The configuration being bound to <see cref="YandexMailOptions"/>.</param>
	public static IYandexCloudBuilder AddPostbox(this IYandexCloudBuilder builder, IConfiguration config)
	{
		builder.Services.AddHttpClient();
		builder.Services.TryAddTransient<IMailService, YandexPostbox>();
		builder.Services.Configure<YandexMailOptions>(config);
		return builder;
	}

	/// <summary>
	/// Registers Yandex.Cloud Postbox <see cref="IMailFilter"/>.
	/// </summary>
	/// <typeparam name="T">Mail filter type.</typeparam>
	public static IYandexCloudBuilder AddPostboxFilter<T>(this IYandexCloudBuilder builder)
		where T : class, IMailFilter
	{
		builder.Services.TryAddEnumerable(ServiceDescriptor.Scoped<IMailFilter, T>());
		return builder;
	}

	/// <summary>
	/// Registers Yandex.Cloud Postbox <see cref="IMailFilter"/> with a factory.
	/// </summary>
	/// <param name="implementationFactory">A factory to create new instances of the filter implementation.</param>
	/// <typeparam name="T">Mail filter type.</typeparam>
	public static IYandexCloudBuilder AddPostboxFilter<T>(this IYandexCloudBuilder builder, Func<IServiceProvider, T> implementationFactory)
		where T : class, IMailFilter
	{
		builder.Services.TryAddEnumerable(ServiceDescriptor.Scoped<IMailFilter, T>(implementationFactory));
		return builder;
	}

	/// <summary>
	/// Registers Yandex.Cloud Postbox <see cref="IMailFilter"/> with a specific implementation type.
	/// </summary>
	/// <param name="implementationType">Mail filter implementation type.</param>
	public static IYandexCloudBuilder AddPostboxFilter(this IYandexCloudBuilder builder, Type implementationType)
	{
		builder.Services.TryAddEnumerable(ServiceDescriptor.Scoped(typeof(IMailFilter), implementationType));
		return builder;
	}

	/// <summary>
	/// Registers Yandex.Cloud Postbox <see cref="IMailModifier"/>.
	/// </summary>
	/// <typeparam name="T">Mail modifier type.</typeparam>
	public static IYandexCloudBuilder AddPostboxModifier<T>(this IYandexCloudBuilder builder)
		where T : class, IMailModifier
	{
		builder.Services.TryAddEnumerable(ServiceDescriptor.Scoped<IMailModifier, T>());
		return builder;
	}

	/// <summary>
	/// Registers Yandex.Cloud Postbox <see cref="IMailModifier"/> with a factory.
	/// </summary>
	/// <param name="implementationFactory">A factory to create new instances of the modifier implementation.</param>
	/// <typeparam name="T">Mail modifier type.</typeparam>
	public static IYandexCloudBuilder AddPostboxModifier<T>(this IYandexCloudBuilder builder, Func<IServiceProvider, T> implementationFactory)
		where T : class, IMailModifier
	{
		builder.Services.TryAddEnumerable(ServiceDescriptor.Scoped<IMailModifier, T>(implementationFactory));
		return builder;
	}

	/// <summary>
	/// Registers Yandex.Cloud Postbox <see cref="IMailModifier"/> with a specific implementation type.
	/// </summary>
	/// <param name="implementationType">Mail modifier implementation type.</param>
	public static IYandexCloudBuilder AddPostboxModifier(this IYandexCloudBuilder builder, Type implementationType)
	{
		builder.Services.TryAddEnumerable(ServiceDescriptor.Scoped(typeof(IMailModifier), implementationType));
		return builder;
	}

	/// <summary>
	/// Registers Yandex.Cloud <see cref="YandexDataStream"/> service.
	/// </summary>
	/// <param name="configure">The action used to configure data streams.</param>
	public static IYandexCloudBuilder AddDataStream(this IYandexCloudBuilder builder, Action<YandexDataStreamOptions> configure)
	{
		builder.Services.TryAddTransient<YandexDataStream>();
		builder.Services.Configure(configure);
		return builder;
	}

	/// <summary>
	/// Registers Yandex.Cloud <see cref="YandexDataStream"/> service.
	/// </summary>
	/// <param name="config">The configuration being bound to <see cref="YandexDataStreamOptions"/>.</param>
	public static IYandexCloudBuilder AddDataStream(this IYandexCloudBuilder builder, IConfiguration config)
	{
		builder.Services.TryAddTransient<YandexDataStream>();
		builder.Services.Configure<YandexDataStreamOptions>(config);
		return builder;
	}

	/// <summary>
	/// Registers Yandex.Cloud <see cref="YandexSpeechKit"/> service.
	/// </summary>
	public static IYandexCloudBuilder AddSpeechKit(this IYandexCloudBuilder builder)
	{
		builder.Services.TryAddTransient<YandexSpeechKit>();
		return builder;
	}
}