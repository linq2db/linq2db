using System.Data;
using System.Reflection;

namespace LinqToDB.DataProvider.SapHana
{
	using Data;
	using Configuration;
	using System;
	using LinqToDB.Common;

	public static class SapHanaTools
	{
		/// <summary>
		/// Default provider to use for SAP HANA connection.
		/// Default value: not set (<c>null</c>).
		/// </summary>
		public static SapHanaProvider? Provider { get; set; }

		private static readonly Lazy<IDataProvider> _hana1DataProvider = new (() =>
		{
			var provider = new SapHanaDataProvider(ProviderName.SapHanaNative, SapHanaVersion.SapHana1);

			DataConnection.AddDataProvider(provider);

			return provider;
		}, true);

		private static readonly Lazy<IDataProvider> _hana2sps04DataProvider = new (() =>
		{
			var provider = new SapHanaDataProvider(ProviderName.SapHanaNative, SapHanaVersion.SapHana2sps04);

			DataConnection.AddDataProvider(provider);

			return provider;
		}, true);

		private static readonly Lazy<IDataProvider> _hana1OdbcDataProvider = new (() =>
		{
			var provider = new SapHanaOdbcDataProvider(SapHanaVersion.SapHana1);

			DataConnection.AddDataProvider(provider);

			return provider;
		}, true);

		private static readonly Lazy<IDataProvider> _hana2sps04OdbcDataProvider = new (() =>
		{
			var provider = new SapHanaOdbcDataProvider(SapHanaVersion.SapHana2sps04);

			DataConnection.AddDataProvider(provider);

			return provider;
		}, true);

		public static bool AutoDetectProvider { get; set; } = true;

		public static void ResolveSapHana(string path)
		{
			new AssemblyResolver(
				path,
				(Provider ?? SapHanaProvider.Odbc) == SapHanaProvider.Native ? SapHanaProviderAdapter.AssemblyName : OdbcProviderAdapter.AssemblyName);
		}

		public static void ResolveSapHana(Assembly assembly)
		{
			new AssemblyResolver(assembly, assembly.FullName!);
		}

		public static IDataProvider GetDataProvider(string? providerName = null, string? assemblyName = null, SapHanaProvider? provider = null, SapHanaVersion? version = null)
		{
			var dataProvider = providerName != null ? getDataProvider(providerName) : null;

			if (dataProvider != null)
				return dataProvider;

			if (provider == SapHanaProvider.Native || assemblyName == SapHanaProviderAdapter.AssemblyName)
				return version == SapHanaVersion.SapHana2sps04 ? _hana2sps04DataProvider.Value : _hana1DataProvider.Value;

			return version == SapHanaVersion.SapHana2sps04 ? _hana2sps04OdbcDataProvider.Value : _hana1OdbcDataProvider.Value;

			static IDataProvider? getDataProvider(string providerName)
			{
				// resolve explicit provider version
				switch (providerName)
				{
					case ProviderName.SapHanaOdbc        : return _hana1OdbcDataProvider.Value;
					case ProviderName.SapHana2SPS04Odbc  : return _hana2sps04OdbcDataProvider.Value;
					case ProviderName.SapHanaNative      : return _hana1DataProvider.Value;
					case ProviderName.SapHana2SPS04Native: return _hana2sps04DataProvider.Value;
				}

				return null;
			}
		}

		#region CreateDataConnection

		public static DataConnection CreateDataConnection(string connectionString, string? providerName = null, SapHanaProvider? provider = null, SapHanaVersion? version = null)
		{
			return new DataConnection(GetDataProvider(providerName: providerName, provider: provider, version: version), connectionString);
		}

		public static DataConnection CreateDataConnection(IDbConnection connection, string? providerName = null, SapHanaProvider? provider = null, SapHanaVersion? version = null)
		{
			return new DataConnection(GetDataProvider(providerName: providerName, provider: provider, version: version), connection);
		}

		public static DataConnection CreateDataConnection(IDbTransaction transaction, string? providerName = null, SapHanaProvider? provider = null, SapHanaVersion? version = null)
		{
			return new DataConnection(GetDataProvider(providerName: providerName, provider: provider, version: version), transaction);
		}

#endregion

		// TODO: v4: remove
		private static string? _detectedProviderName;
		[Obsolete("Property is obsoleted and could return wrong value")]
		public  static string  DetectedProviderName => _detectedProviderName ??= DetectProviderName();

		static string DetectProviderName()
		{
#if NETFRAMEWORK || NETCOREAPP
			return ProviderName.SapHanaNative;
#else
			return ProviderName.SapHanaOdbc;
#endif
		}

		internal static IDataProvider? ProviderDetector(IConnectionStringSettings css, string connectionString)
		{
			var provider = Provider ?? SapHanaProvider.Native;

			if (connectionString.IndexOf("HDBODBC", StringComparison.InvariantCultureIgnoreCase) >= 0)
				provider = SapHanaProvider.Odbc;
			if (css.Name.IndexOf("ODBC", StringComparison.InvariantCultureIgnoreCase) >= 0)
				provider = SapHanaProvider.Odbc;

			var version = SapHanaVersion.SapHana1;

			switch (css.ProviderName)
			{
				case SapHanaProviderAdapter.ClientNamespace:
				case "Sap.Data.Hana.v4.5"                  :
				case "Sap.Data.Hana.Core"                  :
				case "Sap.Data.Hana.Core.v2.1"             :
				case ProviderName.SapHanaNative            :
					provider = SapHanaProvider.Native;
					goto case ProviderName.SapHana;
				case ProviderName.SapHanaOdbc              :
					provider = SapHanaProvider.Odbc;
					goto case ProviderName.SapHana;
				case ""                                    :
				case null                                  :
					if (css.Name.Contains("Hana"))
						goto case ProviderName.SapHana;
					break;
				case ProviderName.SapHana                  :
					version = GetAutoDetectedVersion(css, connectionString, provider, version) ?? version;

					return GetDataProvider(providerName: css.ProviderName, provider: provider, version: version);
			}

			return null;
		}

		private static SapHanaVersion? GetAutoDetectedVersion(IConnectionStringSettings css, string connectionString, SapHanaProvider provider, SapHanaVersion version)
		{
			if (AutoDetectProvider)
			{
				// try native provider
				try
				{
					var cs = string.IsNullOrWhiteSpace(connectionString) ? css.ConnectionString : connectionString;
					string hanaVersion;

					if (provider == SapHanaProvider.Native)
					{
						using (var conn = SapHanaProviderAdapter.GetInstance().CreateConnection(cs))
						{
							conn.Open();

							using (var cmd = conn.CreateCommand())
							{
								cmd.CommandText = "SELECT VERSION FROM SYS.M_DATABASE";
								hanaVersion = Converter.ChangeTypeTo<string>(cmd.ExecuteScalar());
							}
						}
					}
					else
					{
						using (var conn = OdbcProviderAdapter.GetInstance().CreateConnection(cs))
						{
							conn.Open();

							using (var cmd = conn.CreateCommand())
							{
								cmd.CommandText = "SELECT VERSION FROM SYS.M_DATABASE";
								hanaVersion = Converter.ChangeTypeTo<string>(cmd.ExecuteScalar());
							}
						}
					}

					// https://www.blue.works/en/hana-version-numbering-explained/
					var parts    = hanaVersion.Split('.');
					var major    = int.Parse(parts[0]);
					var minor    = int.Parse(parts[1]);
					var revision = int.Parse(parts[2]);
					if (major == 2
						|| (major == 2 && minor > 0)
						|| (major == 2 && minor == 0 && revision >= 40))
						return SapHanaVersion.SapHana2sps04;
					return SapHanaVersion.SapHana1;
				}
				catch
				{
				}
			}

			return null;
		}

		public static BulkCopyType DefaultBulkCopyType { get; set; } = BulkCopyType.MultipleRows;
	}
}
