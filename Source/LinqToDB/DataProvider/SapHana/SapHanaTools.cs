using System;
using System.Data.Common;
using System.IO;
using System.Reflection;

using JetBrains.Annotations;

namespace LinqToDB.DataProvider.SapHana
{
	using Common;
	using Data;
	using Configuration;
	using Extensions;

	[PublicAPI]
	public static class SapHanaTools
	{
		static readonly Lazy<IDataProvider> _hanaDataProvider     = DataConnection.CreateDataProvider<SapHanaDataProvider>();
		static readonly Lazy<IDataProvider> _hanaOdbcDataProvider = DataConnection.CreateDataProvider<SapHanaOdbcDataProvider>();

		public static void ResolveSapHana(string path)
		{
			_ = new AssemblyResolver(
				path,
				DetectedProviderName == ProviderName.SapHanaNative
					? SapHanaProviderAdapter.AssemblyName
					: OdbcProviderAdapter.AssemblyName);
		}

		public static void ResolveSapHana(Assembly assembly)
		{
			_ = new AssemblyResolver(assembly, assembly.FullName!);
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

		internal static IDataProvider? ProviderDetector(ConnectionOptions options)
		{
			if (options.ConnectionString?.IndexOf("HDBODBC", StringComparison.OrdinalIgnoreCase) >= 0)
				return _hanaOdbcDataProvider.Value;

			switch (options.ProviderName)
			{
				case SapHanaProviderAdapter.ClientNamespace:
				case "Sap.Data.Hana.v4.5"                  :
				case "Sap.Data.Hana.Core"                  :
				case "Sap.Data.Hana.Core.v2.1"             :
				case ProviderName.SapHanaNative            : return _hanaDataProvider.Value;
				case ProviderName.SapHanaOdbc              : return _hanaOdbcDataProvider.Value;
				case ""                                    :
				case null                                  :
					if (options.ConfigurationString?.ContainsEx("Hana") == true)
						goto case ProviderName.SapHana;
					break;
				case ProviderName.SapHana                  :
					if (options.ConfigurationString?.IndexOf("ODBC", StringComparison.OrdinalIgnoreCase) >= 0)
						return _hanaOdbcDataProvider.Value;

					return GetDataProvider();
			}

			return null;
		}

		[Obsolete("Use SapHanaOptions.Default.BulkCopyType instead.")]
		public static BulkCopyType DefaultBulkCopyType
		{
			get => SapHanaOptions.Default.BulkCopyType;
			set => SapHanaOptions.Default = SapHanaOptions.Default with { BulkCopyType = value };
		}
	}
}
