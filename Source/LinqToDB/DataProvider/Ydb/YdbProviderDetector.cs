using System;
using System.Data.Common;

using LinqToDB.Data;

namespace LinqToDB.DataProvider.Ydb
{
	/// <summary>
	/// Determines which DataProvider to use for YDB.
	/// Currently, there is only one implementation available: <see cref="YdbDataProvider"/>.
	/// However, the structure is designed to be future-proof—modeled after other Linq To DB providers.
	/// </summary>
	sealed class YdbProviderDetector
		: ProviderDetectorBase<YdbProviderDetector.Provider, YdbProviderDetector.Version>
	{
		/// <summary>
		/// Enumerates subtypes of the ADO provider.
		/// Currently, it contains a single value reserved for future expansion.
		/// </summary>
		internal enum Provider { }

		/// <summary>
		/// YDB server version enumeration, in case version-based behavior becomes relevant in the future
		/// (e.g., versions like 23.1, 24.2, etc.). For now, always returns <see cref="Default"/>.
		/// </summary>
		internal enum Version
		{
			/// <summary>Use the default version.</summary>
			Default,

			/// <summary>Auto-detect the version (equivalent to <see cref="Default"/>).</summary>
			AutoDetect = Default
		}

		public YdbProviderDetector()
			: base(Version.AutoDetect, Version.Default)
		{
		}

		// ---------------------------------------------------------------------
		// Singleton instance of YdbDataProvider (lazily created).
		// ---------------------------------------------------------------------
		static readonly Lazy<IDataProvider> _ydbDataProvider =
			CreateDataProvider<YdbDataProvider>();

		// ---------------------------------------------------------------------
		//  DetectProvider
		// ---------------------------------------------------------------------
		public override IDataProvider? DetectProvider(ConnectionOptions options)
		{
			switch (options.ProviderName)
			{
				// Explicit provider name specified
				case "YDB":
					return _ydbDataProvider.Value;

				// Configuration specified only "YDB"
				case "":
				case null:
					if (options.ConfigurationString == "YDB")
						goto case YdbProviderAdapter.ClientNamespace;
					break;

				case YdbProviderAdapter.ClientNamespace:
				case var providerName when providerName.Contains("YDB",
						StringComparison.OrdinalIgnoreCase) ||
					   providerName.Contains(YdbProviderAdapter.AssemblyName,
						StringComparison.OrdinalIgnoreCase):
					return _ydbDataProvider.Value;
			}

			// If AutoDetectProvider is enabled, return the default DataProvider
			// (YDB currently does not support multiple SQL dialects or versions)
			if (AutoDetectProvider)
				return _ydbDataProvider.Value;

			return null;
		}

		// ---------------------------------------------------------------------
		//  GetDataProvider
		// ---------------------------------------------------------------------
		public override IDataProvider GetDataProvider(
			ConnectionOptions options, Provider provider, Version version)
		{
			// No version distinctions yet – always return the same DataProvider
			return _ydbDataProvider.Value;
		}

		// ---------------------------------------------------------------------
		//  DetectServerVersion
		// ---------------------------------------------------------------------
		public override Version? DetectServerVersion(DbConnection connection)
		{
			// YDB currently has a single SQL dialect,
			// so version detection is unnecessary.
			return Version.Default;
		}

		// ---------------------------------------------------------------------
		//  CreateConnection
		// ---------------------------------------------------------------------
		protected override DbConnection CreateConnection(Provider provider, string connectionString)
		{
			return YdbProviderAdapter.GetInstance().CreateConnection(connectionString);
		}
	}
}
