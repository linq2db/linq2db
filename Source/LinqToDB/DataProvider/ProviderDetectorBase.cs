using System;
using System.Data.Common;

using LinqToDB.Tools;

namespace LinqToDB.DataProvider
{
	using System.Data;

	using Common.Internal.Cache;
	using Data;

	abstract class ProviderDetectorBase<TProvider,TVersion>
		where TProvider   : struct, Enum
		where TVersion    : struct, Enum
	{
		protected ProviderDetectorBase(TVersion autoDetectVersion, TVersion defaultVersion)
		{
			AutoDetectVersion = autoDetectVersion;
			DefaultVersion    = defaultVersion;
		}

		public TVersion AutoDetectVersion  { get; set; }
		public TVersion DefaultVersion     { get; set; }
		public bool     AutoDetectProvider { get; set; } = true;

		static readonly MemoryCache<string,TVersion?> _providerCache = new(new());

		/// <summary>
		/// Clears provider version cache.
		/// </summary>
		public static void ClearCache()
		{
			_providerCache.Clear();
		}

		public bool TryGetCachedServerVersion(string connectionString, out TVersion? version)
		{
			return _providerCache.TryGetValue(connectionString, out version);
		}

		/// <summary>
		/// Connects to database and parses version information.
		/// </summary>
		/// <returns>Detected database server/dialect version.</returns>
		/// <remarks>Uses cache to avoid unwanted connections to Database.</remarks>
		public TVersion? DetectServerVersion(ConnectionOptions options, TProvider provider)
		{
			if (options.DbConnection != null)
				return DetectVersion(options, options.DbConnection);

			if (options.DbTransaction?.Connection != null)
				return DetectVersion(options, options.DbTransaction.Connection);

			if (options.ConnectionString == null)
				throw new InvalidOperationException("Connection string is not provided.");

			var version = ProviderDetectorBase<TProvider, TVersion>._providerCache.GetOrCreate(options.ConnectionString, entry =>
			{
				entry.SlidingExpiration = Common.Configuration.Linq.CacheSlidingExpiration;

				using var conn = CreateConnection(provider, options.ConnectionString);
				return DetectVersion(options, conn);
			});

			return version;

			TVersion? DetectVersion(ConnectionOptions options, DbConnection cn)
			{
				if (cn.State != ConnectionState.Open)
				{
					if (options.ConnectionInterceptor == null)
					{
						cn.Open();
					}
					else
					{
						using (ActivityService.Start(ActivityID.ConnectionInterceptorConnectionOpening))
							options.ConnectionInterceptor.ConnectionOpening(new(null), cn);

						cn.Open();

						using (ActivityService.Start(ActivityID.ConnectionInterceptorConnectionOpened))
							options.ConnectionInterceptor.ConnectionOpened(new(null), cn);
					}
				}

				return DetectServerVersion(cn);
			}
		}

		public DataOptions CreateOptions(DataOptions options, TVersion dialect, TProvider provider)
		{
			if (dialect.Equals(AutoDetectVersion))
			{
				if (options.ConnectionOptions.ConnectionString == null)
					throw new InvalidOperationException("Connection string is not provided.");

				if (TryGetCachedServerVersion(options.ConnectionOptions.ConnectionString, out var version))
					dialect = version ?? DefaultVersion;
				else
					return options.WithOptions<ConnectionOptions>(o => o with
					{
						DataProviderFactory = o =>
						{
							var v = DetectServerVersion(o, provider);
							return GetDataProvider(o, provider, v ?? DefaultVersion);
						}
					});
			}

			return options.UseDataProvider(GetDataProvider(options.ConnectionOptions, provider, dialect));
		}

		public    abstract IDataProvider? DetectProvider     (ConnectionOptions options);
		public    abstract IDataProvider  GetDataProvider    (ConnectionOptions options, TProvider provider, TVersion version);
		public    abstract TVersion?      DetectServerVersion(DbConnection connection);
		protected abstract DbConnection   CreateConnection   (TProvider provider, string connectionString);
	}
}
