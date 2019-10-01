using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;

namespace LinqToDB.DataProvider.Informix
{
	using System.IO;
	using Data;
	using LinqToDB.Common;
	using LinqToDB.Configuration;

	public static class InformixTools
	{
		public static string? AssemblyName;
#if !NETCOREAPP2_0
		public static bool             IsCore;
#else
		public static readonly bool    IsCore = true;
#endif


		static readonly InformixDataProvider _informixDataProvider = new InformixDataProvider();

		public static bool AutoDetectProvider { get; set; }

		static InformixTools()
		{
			try
			{
				var path = typeof(InformixTools).Assembly.GetPath();

				AssemblyName = "IBM.Data.DB2.Core";

#if !NETCOREAPP2_0
				IsCore = File.Exists(Path.Combine(path, (AssemblyName = "IBM.Data.DB2.Core") + ".dll"));

				if (!IsCore)
					AssemblyName = "IBM.Data.Informix";
#endif
			}
			catch (Exception)
			{
			}

			AutoDetectProvider = true;

			DataConnection.AddDataProvider(_informixDataProvider);

#if !NETCOREAPP2_0
			DataConnection.AddProviderDetector(ProviderDetector);
#endif
		}

#if !NETCOREAPP2_0
		static IDataProvider? ProviderDetector(IConnectionStringSettings css, string connectionString)
		{
			// IBM.Data.DB2.Core already mapped to DB2 provider...
			switch (css.ProviderName)
			{
				case "":
				case null:

					if (css.Name == "Informix.Core")
						goto case "IBM.Data.Informix.Core";
					if (css.Name == "Informix")
						goto case "IBM.Data.Informix";
					break;

				case "Informix.Core":
				case "IBM.Data.Informix.Core":
					IsCore       = true;
					AssemblyName = "IBM.Data.DB2.Core";

					break;

				case "IBM.Data.Informix":

					IsCore       = false;
					AssemblyName = "IBM.Data.Informix";

					break;
			}

			return null;
		}
#endif

		public static IDataProvider GetDataProvider()
		{
			return _informixDataProvider;
		}

		public static void ResolveInformix(string path)
		{
			new AssemblyResolver(path, AssemblyName);
		}

		public static void ResolveInformix(Assembly assembly)
		{
			new AssemblyResolver(assembly, AssemblyName);
		}

#region CreateDataConnection

		public static DataConnection CreateDataConnection(string connectionString)
		{
			return new DataConnection(_informixDataProvider, connectionString);
		}

		public static DataConnection CreateDataConnection(IDbConnection connection)
		{
			return new DataConnection(_informixDataProvider, connection);
		}

		public static DataConnection CreateDataConnection(IDbTransaction transaction)
		{
			return new DataConnection(_informixDataProvider, transaction);
		}

#endregion

#region BulkCopy

		public  static BulkCopyType  DefaultBulkCopyType { get; set; } = BulkCopyType.MultipleRows;

		public static BulkCopyRowsCopied MultipleRowsCopy<T>(
			DataConnection              dataConnection,
			IEnumerable<T>              source,
			int                         maxBatchSize       = 1000,
			Action<BulkCopyRowsCopied>? rowsCopiedCallback = null)
			where T : class
		{
			return dataConnection.BulkCopy(
				new BulkCopyOptions
				{
					BulkCopyType       = BulkCopyType.MultipleRows,
					MaxBatchSize       = maxBatchSize,
					RowsCopiedCallback = rowsCopiedCallback,
				}, source);
		}

#endregion
	}
}
