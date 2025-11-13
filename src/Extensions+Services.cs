using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Yandex.Cloud;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for the Yandex.Cloud services registration.
/// </summary>
public static class YandexCloudExtensions
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

	extension(IYandexCloudBuilder builder)
	{
		/// <summary>
		/// Registers Yandex.Cloud object storage as <see cref="IStorage"/>.
		/// </summary>
		/// <param name="configure">The action used to configure storage options.</param>
		public IYandexCloudBuilder AddStorage(Action<YandexStorageOptions> configure)
		{
			builder.Services.TryAddTransient<IStorage, YandexStorage>();
			builder.Services.Configure(configure);
			return builder;
		}

		/// <summary>
		/// Registers Yandex.Cloud object storage as <see cref="IStorage"/>.
		/// </summary>
		/// <param name="config">The configuration being bound to <see cref="YandexStorageOptions"/>.</param>
		public IYandexCloudBuilder AddStorage(IConfiguration config)
		{
			builder.Services.TryAddTransient<IStorage, YandexStorage>();
			builder.Services.Configure<YandexStorageOptions>(config);
			return builder;
		}

		/// <summary>
		/// Registers Yandex.Cloud Postbox as <see cref="IMailService"/>.
		/// </summary>
		/// <param name="configure">The action used to configure mail options.</param>
		public IYandexCloudBuilder AddPostbox(Action<YandexMailOptions> configure)
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
		public IYandexCloudBuilder AddPostbox(IConfiguration config)
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
		public IYandexCloudBuilder AddPostboxFilter<T>()
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
		public IYandexCloudBuilder AddPostboxFilter<T>(Func<IServiceProvider, T> implementationFactory)
			where T : class, IMailFilter
		{
			builder.Services.TryAddEnumerable(ServiceDescriptor.Scoped<IMailFilter, T>(implementationFactory));
			return builder;
		}

		/// <summary>
		/// Registers Yandex.Cloud Postbox <see cref="IMailFilter"/> with a specific implementation type.
		/// </summary>
		/// <param name="implementationType">Mail filter implementation type.</param>
		public IYandexCloudBuilder AddPostboxFilter(Type implementationType)
		{
			builder.Services.TryAddEnumerable(ServiceDescriptor.Scoped(typeof(IMailFilter), implementationType));
			return builder;
		}

		/// <summary>
		/// Registers Yandex.Cloud Postbox <see cref="IMailModifier"/>.
		/// </summary>
		/// <typeparam name="T">Mail modifier type.</typeparam>
		public IYandexCloudBuilder AddPostboxModifier<T>()
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
		public IYandexCloudBuilder AddPostboxModifier<T>(Func<IServiceProvider, T> implementationFactory)
			where T : class, IMailModifier
		{
			builder.Services.TryAddEnumerable(ServiceDescriptor.Scoped<IMailModifier, T>(implementationFactory));
			return builder;
		}

		/// <summary>
		/// Registers Yandex.Cloud Postbox <see cref="IMailModifier"/> with a specific implementation type.
		/// </summary>
		/// <param name="implementationType">Mail modifier implementation type.</param>
		public IYandexCloudBuilder AddPostboxModifier(Type implementationType)
		{
			builder.Services.TryAddEnumerable(ServiceDescriptor.Scoped(typeof(IMailModifier), implementationType));
			return builder;
		}

		/// <summary>
		/// Registers Yandex.Cloud <see cref="YandexDataStream"/> service.
		/// </summary>
		/// <param name="configure">The action used to configure data streams.</param>
		public IYandexCloudBuilder AddDataStream(Action<YandexDataStreamOptions> configure)
		{
			builder.Services.TryAddTransient<YandexDataStream>();
			builder.Services.Configure(configure);
			return builder;
		}

		/// <summary>
		/// Registers Yandex.Cloud <see cref="YandexDataStream"/> service.
		/// </summary>
		/// <param name="config">The configuration being bound to <see cref="YandexDataStreamOptions"/>.</param>
		public IYandexCloudBuilder AddDataStream(IConfiguration config)
		{
			builder.Services.TryAddTransient<YandexDataStream>();
			builder.Services.Configure<YandexDataStreamOptions>(config);
			return builder;
		}

		/// <summary>
		/// Registers Yandex.Cloud <see cref="YandexSpeechKit"/> service.
		/// </summary>
		public IYandexCloudBuilder AddSpeechKit()
		{
			builder.Services.TryAddTransient<YandexSpeechKit>();
			return builder;
		}
	}
}