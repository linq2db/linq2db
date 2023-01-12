using System;
using System.Data.Common;

namespace LinqToDB.DataProvider
{
	using Common.Internal.Cache;
	using Configuration;
	using Data;

	abstract class ProviderDetectorBase<TProvider,TVersion,TConnection>
		where TProvider   : struct, Enum
		where TVersion    : struct, Enum
		where TConnection : IDisposable
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
		/// <param name="provider"></param>
		/// <param name="connectionString"></param>
		/// <returns>Detected SQL Server version.</returns>
		/// <remarks>Uses cache to avoid unwanted connections to Database.</remarks>
		public TVersion? DetectServerVersion(TProvider provider, string connectionString)
		{
			var version = _providerCache.GetOrCreate(connectionString, entry =>
			{
				entry.SlidingExpiration = Common.Configuration.Linq.CacheSlidingExpiration;

				using var conn = CreateConnection(provider, connectionString);

				switch (conn)
				{
					case DbConnection       c : c.Open(); break;
					case IConnectionWrapper m : m.Open(); break;
					default                   : throw new InvalidOperationException();
				}

				return DetectServerVersion(conn);
			});

			return version;
		}

		public DataOptions CreateOptions(DataOptions options, string connectionString, TVersion dialect, TProvider provider)
		{
			if (dialect.Equals(AutoDetectVersion))
			{
				if (TryGetCachedServerVersion(connectionString, out var version))
					dialect = version ?? DefaultVersion;
				else
					return options.WithOptions<ConnectionOptions>(o => o with
					{
						ConnectionString    = connectionString,
						DataProviderFactory = () =>
						{
							var v = DetectServerVersion(provider, connectionString);
							return GetDataProvider(provider, v ?? DefaultVersion, connectionString);
						}
					});
			}

			return options.UseConnectionString(GetDataProvider(provider, dialect, null), connectionString);
		}

		public    abstract IDataProvider? DetectProvider     (IConnectionStringSettings css, string connectionString);
		public    abstract IDataProvider  GetDataProvider    (TProvider provider, TVersion version, string? connectionString);
		public    abstract TVersion?      DetectServerVersion(TConnection connection);
		protected abstract TConnection    CreateConnection   (TProvider provider, string connectionString);
	}
}
