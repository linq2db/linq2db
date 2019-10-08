namespace LinqToDB.DataProvider.SapHana
{
	using System;
	using System.Data;
	using System.Reflection;

	using Data;
	using LinqToDB.Configuration;

	public static class SapHanaTools
	{
#if NET45 || NET46
		public static string AssemblyName = "Sap.Data.Hana.v4.5";
#endif

#if NETCOREAPP2_1
		public static string AssemblyName = "Sap.Data.Hana.Core.v2.1";
#endif

#if !NETSTANDARD2_0
		static readonly SapHanaDataProvider _hanaDataProvider = new SapHanaDataProvider();
#endif

		static readonly SapHanaOdbcDataProvider _hanaOdbcDataProvider = new SapHanaOdbcDataProvider();

		static readonly IDataProvider DefaultProvider;

		public static BulkCopyType DefaultBulkCopyType { get; set; } = BulkCopyType.MultipleRows;

		static SapHanaTools()
		{
#if !NETSTANDARD2_0
			DataConnection.AddDataProvider(_hanaDataProvider);
			DataConnection.AddProviderDetector(ProviderDetector);
			DefaultProvider = _hanaDataProvider;
#else
			DefaultProvider = _hanaOdbcDataProvider;
#endif
			DataConnection.AddDataProvider(_hanaOdbcDataProvider);
		}

#if !NETSTANDARD2_0
		public static void ResolveSapHana(string path)
		{
			new AssemblyResolver(path, AssemblyName);
		}

		public static void ResolveSapHana(Assembly assembly)
		{
			new AssemblyResolver(assembly, AssemblyName);
		}
#endif

		public static IDataProvider GetDataProvider()
		{
			return DefaultProvider;
		}

#region CreateDataConnection

		public static DataConnection CreateDataConnection(string connectionString)
		{
			return new DataConnection(DefaultProvider, connectionString);
		}

		public static DataConnection CreateDataConnection(IDbConnection connection)
		{
			return new DataConnection(DefaultProvider, connection);
		}

		public static DataConnection CreateDataConnection(IDbTransaction transaction)
		{
			return new DataConnection(DefaultProvider, transaction);
		}

#endregion

		static string _detectedProviderName;

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

		static IDataProvider? ProviderDetector(IConnectionStringSettings css, string connectionString)
		{

			switch (css.ProviderName)
			{
				case "":
				case null:

					if (css.Name.Contains("Hana"))
						goto case "SapHana";
					break;

				case "SapHana.Odbc"      : return _hanaOdbcDataProvider;
				case "Sap.Data.Hana"     :
				case "Sap.Data.Hana.Core":
#if !NETSTANDARD2_0
					return _hanaDataProvider;
#else
					return _hanaOdbcDataProvider;
#endif
				case "SapHana":

					if (css.Name.Contains("Odbc"))
						return _hanaOdbcDataProvider;

					return DefaultProvider;
			}

			return null;
		}
	}
}
