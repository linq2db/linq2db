﻿using System.Data;
using System.Reflection;

namespace LinqToDB.DataProvider.SapHana
{
	using Data;
	using Configuration;
	using System;

	public static class SapHanaTools
	{
#if !NETSTANDARD2_0 && !NETSTANDARD2_1
		private static readonly Lazy<IDataProvider> _hanaDataProvider = new Lazy<IDataProvider>(() =>
		{
			var provider = new SapHanaDataProvider(ProviderName.SapHanaNative);

			DataConnection.AddDataProvider(provider);

			return provider;
		}, true);
#endif

		private static readonly Lazy<IDataProvider> _hanaOdbcDataProvider = new Lazy<IDataProvider>(() =>
		{
			var provider = new SapHanaOdbcDataProvider();

			DataConnection.AddDataProvider(provider);

			return provider;
		}, true);

		public static void ResolveSapHana(string path)
		{
			new AssemblyResolver(
				path,
#if !NETSTANDARD2_0 && !NETSTANDARD2_1
			DetectedProviderName == ProviderName.SapHanaNative
						? SapHanaProviderAdapter.AssemblyName :
#endif
						OdbcProviderAdapter.AssemblyName);
		}

		public static void ResolveSapHana(Assembly assembly)
		{
			new AssemblyResolver(assembly, assembly.FullName!);
		}

		public static IDataProvider GetDataProvider(string? providerName = null, string? assemblyName = null)
		{
#if !NETSTANDARD2_0 && !NETSTANDARD2_1
			if (assemblyName == SapHanaProviderAdapter.AssemblyName) return _hanaDataProvider.Value;
#endif
			if (assemblyName == OdbcProviderAdapter.AssemblyName)    return _hanaOdbcDataProvider.Value;


			switch (providerName)
			{
				case ProviderName.SapHanaOdbc  : return _hanaOdbcDataProvider.Value;
#if !NETSTANDARD2_0 && !NETSTANDARD2_1
				case ProviderName.SapHanaNative: return _hanaDataProvider.Value;
#endif
			}

#if !NETSTANDARD2_0 && !NETSTANDARD2_1
			if (DetectedProviderName == ProviderName.SapHanaNative)
				return _hanaDataProvider.Value;
#endif

			return _hanaOdbcDataProvider.Value;
		}

		#region CreateDataConnection

		public static DataConnection CreateDataConnection(string connectionString, string? providerName = null)
		{
			return new DataConnection(GetDataProvider(providerName), connectionString);
		}

		public static DataConnection CreateDataConnection(IDbConnection connection, string? providerName = null)
		{
			return new DataConnection(GetDataProvider(providerName), connection);
		}

		public static DataConnection CreateDataConnection(IDbTransaction transaction, string? providerName = null)
		{
			return new DataConnection(GetDataProvider(providerName), transaction);
		}

#endregion

		private static string? _detectedProviderName;
		public  static string  DetectedProviderName =>
			_detectedProviderName ??= DetectProviderName();

		static string DetectProviderName()
		{
#if !NETSTANDARD2_0 && !NETSTANDARD2_1
			return ProviderName.SapHanaNative;
#else
			return ProviderName.SapHanaOdbc;
#endif
		}

		internal static IDataProvider? ProviderDetector(IConnectionStringSettings css, string connectionString)
		{
			if (connectionString.IndexOf("HDBODBC", StringComparison.InvariantCultureIgnoreCase) >= 0)
				return _hanaOdbcDataProvider.Value;

			switch (css.ProviderName)
			{
#if !NETSTANDARD2_0 && !NETSTANDARD2_1
				case SapHanaProviderAdapter.ClientNamespace:
				case "Sap.Data.Hana.v4.5"                  :
				case "Sap.Data.Hana.Core"                  :
				case "Sap.Data.Hana.Core.v2.1"             :
				case ProviderName.SapHanaNative            : return _hanaDataProvider.Value;
#endif
				case ProviderName.SapHanaOdbc              : return _hanaOdbcDataProvider.Value;
				case ""                                    :
				case null                                  :
					if (css.Name.Contains("Hana"))
						goto case ProviderName.SapHana;
					break;
				case ProviderName.SapHana                  :
					if (css.Name.IndexOf("ODBC", StringComparison.InvariantCultureIgnoreCase) >= 0)
						return _hanaOdbcDataProvider.Value;

					return GetDataProvider();
			}

			return null;
		}

		public static BulkCopyType DefaultBulkCopyType { get; set; } = BulkCopyType.MultipleRows;
	}
}
