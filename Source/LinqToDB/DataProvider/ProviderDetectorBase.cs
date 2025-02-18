﻿using System;
using System.Data;
using System.Data.Common;

using LinqToDB.Common.Internal.Cache;
using LinqToDB.Data;
using LinqToDB.Tools;

namespace LinqToDB.DataProvider
{
	abstract class ProviderDetectorBase<TProvider,TVersion>
		where TProvider   : struct, Enum
		where TVersion    : struct, Enum
	{
		readonly bool _hasVersioning;

		/// <summary>
		/// Creates provider instance factory with instance registration it in <see cref="DataConnection"/>.
		/// </summary>
		protected internal static Lazy<IDataProvider> CreateDataProvider<T>()
			where T : IDataProvider, new()
		{
			return new(() =>
			{
				var provider = new T();
				DataConnection.AddDataProvider(provider);
				return provider;
			}, true);
		}

		protected ProviderDetectorBase()
		{
			_hasVersioning = false;
		}

		protected ProviderDetectorBase(TVersion autoDetectVersion, TVersion defaultVersion)
		{
			AutoDetectVersion = autoDetectVersion;
			DefaultVersion    = defaultVersion;
			_hasVersioning    = true;
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

			var connectionString = TryGetConnectionString(options);

			if (connectionString == null)
				throw new InvalidOperationException("Connection string is not provided.");

			var version = _providerCache.GetOrCreate(connectionString, entry =>
			{
				entry.SlidingExpiration = Common.Configuration.Linq.CacheSlidingExpiration;

				using var conn = CreateConnection(provider, connectionString);
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
			if (_hasVersioning && dialect.Equals(AutoDetectVersion))
			{
				var connectionString = TryGetConnectionString(options.ConnectionOptions);

				if (connectionString == null)
					throw new InvalidOperationException("Connection string is not provided.");

				if (TryGetCachedServerVersion(connectionString, out var version))
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

		private static string? TryGetConnectionString(ConnectionOptions options)
		{
			return options.ConnectionString
								?? (options.ConfigurationString != null
									? DataConnection.GetConnectionString(options.ConfigurationString)
									: null);
		}

		public    abstract IDataProvider? DetectProvider     (ConnectionOptions options);
		public    abstract IDataProvider  GetDataProvider    (ConnectionOptions options, TProvider provider, TVersion version);
		public    abstract TVersion?      DetectServerVersion(DbConnection connection);
		protected abstract DbConnection   CreateConnection   (TProvider provider, string connectionString);
	}
}
