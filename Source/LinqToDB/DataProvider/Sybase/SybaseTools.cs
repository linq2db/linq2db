using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Reflection;
using LinqToDB.Common;
using LinqToDB.Extensions;

namespace LinqToDB.DataProvider.Sybase
{
	using Data;
	using LinqToDB.Configuration;

	public static class SybaseTools
	{
		public static string AssemblyName;
		public static string NativeAssemblyName;

		static readonly SybaseDataProvider _sybaseNativeDataProvider  = new SybaseDataProvider(ProviderName.Sybase);
		static readonly SybaseDataProvider _sybaseManagedDataProvider = new SybaseDataProvider(ProviderName.SybaseManaged);

#pragma warning disable 3015, 219
		static SybaseTools()
		{
			try
			{
				var path = typeof(SybaseTools).AssemblyEx().GetPath();

				var _ =
					File.Exists(Path.Combine(path, (NativeAssemblyName = "Sybase.AdoNet45.AseClient") + ".dll")) ||
					File.Exists(Path.Combine(path, (NativeAssemblyName = "Sybase.AdoNet4.AseClient") + ".dll")) ||
					File.Exists(Path.Combine(path, (NativeAssemblyName = "Sybase.AdoNet35.AseClient") + ".dll")) ||
					File.Exists(Path.Combine(path, (NativeAssemblyName = "Sybase.AdoNet2.AseClient") + ".dll"));
			}
			catch
			{
			}

			AssemblyName = DetectedProviderName == ProviderName.SybaseManaged ? "AdoNetCore.AseClient" : NativeAssemblyName;

			DataConnection.AddDataProvider(ProviderName.Sybase, DetectedProvider);
			DataConnection.AddDataProvider(_sybaseNativeDataProvider);
			DataConnection.AddDataProvider(_sybaseManagedDataProvider);

			DataConnection.AddProviderDetector(ProviderDetector);
		}
#pragma warning restore 3015, 219

		private static IDataProvider ProviderDetector(IConnectionStringSettings css, string connectionString)
		{
			switch (css.ProviderName)
			{
				case "":
				case null:

					if (css.Name.Contains("Sybase"))
						goto case "Sybase";
					break;

				case "Sybase.Native":
				case "Sybase.Data.AseClient": return _sybaseNativeDataProvider;
				case "Sybase.Managed":
				case "AdoNetCore.AseClient" : return _sybaseManagedDataProvider;
				case "Sybase":

					if (css.Name.Contains("Managed"))
						return _sybaseManagedDataProvider;

					if (css.Name.Contains("Native"))
						return _sybaseNativeDataProvider;

					return DetectedProvider;
			}

			return null;
		}

		private static string _detectedProviderName;

		public static string DetectedProviderName =>
			_detectedProviderName ?? (_detectedProviderName = DetectProviderName());

		private static SybaseDataProvider DetectedProvider =>
			DetectedProviderName == ProviderName.Sybase ? _sybaseNativeDataProvider : _sybaseManagedDataProvider;

		private static string DetectProviderName()
		{
			var path = typeof(SybaseTools).AssemblyEx().GetPath();

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

		public static BulkCopyType DefaultBulkCopyType { get; set; } = BulkCopyType.MultipleRows;

		public static BulkCopyRowsCopied MultipleRowsCopy<T>(
			DataConnection             dataConnection,
			IEnumerable<T>             source,
			int                        maxBatchSize       = 1000,
			Action<BulkCopyRowsCopied> rowsCopiedCallback = null)
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
