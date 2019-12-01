using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Reflection;

namespace LinqToDB.DataProvider.Sybase
{
	using Data;
	using Common;
	using Configuration;

	public static class SybaseTools
	{
		public static string AssemblyName;
		public static string NativeAssemblyName = "Sybase.AdoNet45.AseClient.dll";

#if NET45 || NET46
		static readonly SybaseDataProvider _sybaseNativeDataProvider  = new SybaseDataProvider(ProviderName.Sybase);
#endif
		static readonly SybaseDataProvider _sybaseManagedDataProvider = new SybaseDataProvider(ProviderName.SybaseManaged);

#pragma warning disable 3015, 219
		static SybaseTools()
		{
			AssemblyName = DetectedProviderName == ProviderName.SybaseManaged ? "AdoNetCore.AseClient" : NativeAssemblyName;

			DataConnection.AddDataProvider(ProviderName.Sybase, DetectedProvider);
#if NET45 || NET46
			DataConnection.AddDataProvider(_sybaseNativeDataProvider);
#endif
			DataConnection.AddDataProvider(_sybaseManagedDataProvider);

			DataConnection.AddProviderDetector(ProviderDetector);
		}
#pragma warning restore 3015, 219

		private static IDataProvider? ProviderDetector(IConnectionStringSettings css, string connectionString)
		{
			switch (css.ProviderName)
			{
				case "":
				case null:

					if (css.Name.Contains("Sybase"))
						goto case "Sybase";
					break;

				case "Sybase.Native":
				case "Sybase.Data.AseClient":
#if NET45 || NET46
					return _sybaseNativeDataProvider;
#endif
				case "Sybase.Managed":
				case "AdoNetCore.AseClient" : return _sybaseManagedDataProvider;
				case "Sybase":

					if (css.Name.Contains("Managed"))
						return _sybaseManagedDataProvider;

#if NET45 || NET46
					if (css.Name.Contains("Native"))
						return _sybaseNativeDataProvider;
#endif

					return DetectedProvider;
			}

			return null;
		}

		private static string? _detectedProviderName;
		public  static string  DetectedProviderName =>
			_detectedProviderName ?? (_detectedProviderName = DetectProviderName());

		private static SybaseDataProvider DetectedProvider =>
#if NET45 || NET46
			DetectedProviderName == ProviderName.Sybase
				? _sybaseNativeDataProvider :
#endif
				_sybaseManagedDataProvider;

		private static string DetectProviderName()
		{
			var path = typeof(SybaseTools).Assembly.GetPath();

			if (File.Exists(Path.Combine(path, "AdoNetCore.AseClient.dll")))
				return ProviderName.SybaseManaged;

			return ProviderName.Sybase;
		}

		public static IDataProvider GetDataProvider()
		{
			return DetectedProvider;
		}

		public static void ResolveSybase(string path)
		{
			new AssemblyResolver(path, AssemblyName);
		}

		public static void ResolveSybase(Assembly assembly)
		{
			new AssemblyResolver(assembly, AssemblyName);
		}

		#region CreateDataConnection

		public static DataConnection CreateDataConnection(string connectionString)
		{
			return new DataConnection(DetectedProvider, connectionString);
		}

		public static DataConnection CreateDataConnection(IDbConnection connection)
		{
			return new DataConnection(DetectedProvider, connection);
		}

		public static DataConnection CreateDataConnection(IDbTransaction transaction)
		{
			return new DataConnection(DetectedProvider, transaction);
		}

		#endregion

		#region BulkCopy
		// don't set ProviderSpecific as default type while SAP not fix incorrect bit field value
		// insert for first record
		/// <summary>
		/// Using <see cref="BulkCopyType.ProviderSpecific"/> mode with bit and identity fields could lead to following errors:
		/// - bit: <c>false</c> inserted into bit field for first record even if <c>true</c> provided;
		/// - identity: bulk copy operation fail with exception: "Bulk insert failed. Null value is not allowed in not null column.".
		/// Those are provider bugs and could be fixed in latest versions.
		/// </summary>
		public static BulkCopyType DefaultBulkCopyType { get; set; } = BulkCopyType.MultipleRows;

		public static BulkCopyRowsCopied MultipleRowsCopy<T>(
			DataConnection             dataConnection,
			IEnumerable<T>             source,
			int                        maxBatchSize        = 1000,
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
