using System.Data;
using System.Reflection;

namespace LinqToDB.DataProvider.SapHana
{
	using Data;
	using Configuration;
	using System;
	using LinqToDB.DataProvider.Wrappers;

	public static class SapHanaTools
	{
#if !NETSTANDARD2_0
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
#if !NETSTANDARD2_0
			DetectedProviderName == ProviderName.SapHanaNative
						? SapHanaWrappers.AssemblyName :
#endif
						Mappers.ODBC.AssemblyName);
		}

		public static void ResolveSapHana(Assembly assembly)
		{
			new AssemblyResolver(assembly, assembly.FullName);
		}

		public static IDataProvider GetDataProvider(string? providerName = null, string? assemblyName = null)
		{
#if !NETSTANDARD2_0
			if (assemblyName == SapHanaWrappers.AssemblyName) return _hanaDataProvider.Value;
#endif
			if (assemblyName == Mappers.ODBC.AssemblyName)    return _hanaOdbcDataProvider.Value;


			switch (providerName)
			{
				case ProviderName.SapHanaOdbc  : return _hanaOdbcDataProvider.Value;
#if !NETSTANDARD2_0
				case ProviderName.SapHanaNative: return _hanaDataProvider.Value;
#endif
			}

#if !NETSTANDARD2_0
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

		static string? _detectedProviderName;

		public static string  DetectedProviderName =>
			_detectedProviderName ?? (_detectedProviderName = DetectProviderName());

		static string DetectProviderName()
		{
#if !NETSTANDARD2_0
			return ProviderName.SapHanaNative;
#else
			return ProviderName.SapHanaOdbc;
#endif
		}

		internal static IDataProvider? ProviderDetector(IConnectionStringSettings css, string connectionString)
		{

			switch (css.ProviderName)
			{
				case "":
				case null:

					if (css.Name.Contains("Hana"))
						goto case "SapHana";
					break;

				case "SapHana.Odbc"      : return _hanaOdbcDataProvider.Value;
				case "Sap.Data.Hana"     :
				case "Sap.Data.Hana.Core":
#if !NETSTANDARD2_0
					return _hanaDataProvider.Value;
#else
					return _hanaOdbcDataProvider.Value;
#endif
				case "SapHana":

					if (css.Name.Contains("Odbc"))
						return _hanaOdbcDataProvider.Value;

					return GetDataProvider();
			}

			return null;
		}

		public static BulkCopyType DefaultBulkCopyType { get; set; } = BulkCopyType.MultipleRows;
	}
}
