using System;
using System.Data;
using System.Data.Common;
using System.Reflection;

namespace LinqToDB.DataProvider.SapHana
{
	using Data;
	using Configuration;
	using System;
	using System.IO;
	using LinqToDB.Common;

	public static class SapHanaTools
	{
		static readonly Lazy<IDataProvider> _hanaDataProvider     = DataConnection.CreateDataProvider<SapHanaDataProvider>();
		static readonly Lazy<IDataProvider> _hanaOdbcDataProvider = DataConnection.CreateDataProvider<SapHanaOdbcDataProvider>();

		public static void ResolveSapHana(string path)
		{
			new AssemblyResolver(
				path,
			DetectedProviderName == ProviderName.SapHanaNative
						? SapHanaProviderAdapter.AssemblyName
						: OdbcProviderAdapter.AssemblyName);
		}

		public static void ResolveSapHana(Assembly assembly)
		{
			new AssemblyResolver(assembly, assembly.FullName!);
		}

		public static IDataProvider GetDataProvider(string? providerName = null, string? assemblyName = null)
		{
			if (assemblyName == SapHanaProviderAdapter.AssemblyName) return _hanaDataProvider.Value;
			if (assemblyName == OdbcProviderAdapter.AssemblyName)    return _hanaOdbcDataProvider.Value;

			switch (providerName)
			{
				case ProviderName.SapHanaOdbc  : return _hanaOdbcDataProvider.Value;
				case ProviderName.SapHanaNative: return _hanaDataProvider.Value;
			}

			if (DetectedProviderName == ProviderName.SapHanaNative)
				return _hanaDataProvider.Value;

			return _hanaOdbcDataProvider.Value;
		}

		#region CreateDataConnection

		public static DataConnection CreateDataConnection(string connectionString, string? providerName = null)
		{
			return new DataConnection(GetDataProvider(providerName), connectionString);
		}

		public static DataConnection CreateDataConnection(DbConnection connection, string? providerName = null)
		{
			return new DataConnection(GetDataProvider(providerName), connection);
		}

		public static DataConnection CreateDataConnection(DbTransaction transaction, string? providerName = null)
		{
			return new DataConnection(GetDataProvider(providerName), transaction);
		}

#endregion

		private static string? _detectedProviderName;
		public  static string  DetectedProviderName =>
			_detectedProviderName ??= DetectProviderName();

		static string DetectProviderName()
		{
			var path = typeof(SapHanaTools).Assembly.GetPath();

			if (File.Exists(Path.Combine(path, $"{SapHanaProviderAdapter.AssemblyName}.dll")))
				return ProviderName.SapHanaNative;

			return ProviderName.SapHanaOdbc;
		}

		internal static IDataProvider? ProviderDetector(IConnectionStringSettings css, string connectionString)
		{
			if (connectionString.IndexOf("HDBODBC", StringComparison.OrdinalIgnoreCase) >= 0)
				return _hanaOdbcDataProvider.Value;

			switch (css.ProviderName)
			{
				case SapHanaProviderAdapter.ClientNamespace:
				case "Sap.Data.Hana.v4.5"                  :
				case "Sap.Data.Hana.Core"                  :
				case "Sap.Data.Hana.Core.v2.1"             :
				case ProviderName.SapHanaNative            : return _hanaDataProvider.Value;
				case ProviderName.SapHanaOdbc              : return _hanaOdbcDataProvider.Value;
				case ""                                    :
				case null                                  :
					if (css.Name.Contains("Hana"))
						goto case ProviderName.SapHana;
					break;
				case ProviderName.SapHana                  :
					if (css.Name.IndexOf("ODBC", StringComparison.OrdinalIgnoreCase) >= 0)
						return _hanaOdbcDataProvider.Value;

					return GetDataProvider();
			}

			return null;
		}

		public static BulkCopyType DefaultBulkCopyType { get; set; } = BulkCopyType.MultipleRows;
	}
}
