using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Reflection;
using System.Linq;
using LinqToDB.Common;
using LinqToDB.Extensions;

namespace LinqToDB.DataProvider.Oracle
{
	using Configuration;

	using Data;

	public static partial class OracleTools
	{
		public static string AssemblyName;

		static readonly OracleDataProvider _oracleNativeDataProvider  = new OracleDataProvider(ProviderName.OracleNative);
		static readonly OracleDataProvider _oracleManagedDataProvider = new OracleDataProvider(ProviderName.OracleManaged);

		static OracleTools()
		{
			AssemblyName = DetectedProviderName == ProviderName.OracleNative ? "Oracle.DataAccess" : "Oracle.ManagedDataAccess";

			DataConnection.AddDataProvider(ProviderName.Oracle, DetectedProvider);
			DataConnection.AddDataProvider(_oracleNativeDataProvider);
			DataConnection.AddDataProvider(_oracleManagedDataProvider);

			DataConnection.AddProviderDetector(ProviderDetector);

			foreach (var method in typeof(OracleTools).GetMethodsEx().Where(_ => _.Name == "OracleXmlTable" && _.IsGenericMethod))
			{
				var parameters = method.GetParameters();

				if (parameters[1].ParameterType == typeof(string))
					OracleXmlTableString = method;
				else if (parameters[1].ParameterType == typeof(Func<string>))
					OracleXmlTableFuncString = method;
				else if (parameters[1].ParameterType.IsGenericTypeEx() &&
				         parameters[1].ParameterType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
					OracleXmlTableIEnumerableT = method;
				else
					throw new InvalidOperationException("Overload method for OracleXmlTable is unknown");
			}
		}

		static IDataProvider ProviderDetector(IConnectionStringSettings css, string connectionString)
		{
			if (css.IsGlobal /* DataConnection.IsMachineConfig(css)*/)
				return null;

			switch (css.ProviderName)
			{
				case ""                                :
				case null                              :

					if (css.Name.Contains("Oracle"))
						goto case "Oracle";
					break;

				case "Oracle.Native"                   :
				case "Oracle.DataAccess.Client"        : return _oracleNativeDataProvider;
				case "Oracle.Managed"                  :
				case "Oracle.ManagedDataAccess.Client" : return _oracleManagedDataProvider;
				case "Oracle"                          :

					if (css.Name.Contains("Managed"))
						return _oracleManagedDataProvider;

					if (css.Name.Contains("Native"))
						return _oracleNativeDataProvider;

					return DetectedProvider;
			}

			return null;
		}

		static string _detectedProviderName;

		public static string  DetectedProviderName
		{
			get { return _detectedProviderName ?? (_detectedProviderName = DetectProviderName()); }
		}

		static OracleDataProvider DetectedProvider
		{
			get { return DetectedProviderName == ProviderName.OracleNative ? _oracleNativeDataProvider : _oracleManagedDataProvider; }
		}

		static string DetectProviderName()
		{
			try
			{
				var path = typeof(OracleTools).AssemblyEx().GetPath();

				if (!File.Exists(Path.Combine(path, "Oracle.DataAccess.dll")))
					if (File.Exists(Path.Combine(path, "Oracle.ManagedDataAccess.dll")))
						return ProviderName.OracleManaged;;
			}
			catch (Exception)
			{
			}

			return ProviderName.OracleNative;
		}

		public static IDataProvider GetDataProvider()
		{
			return DetectedProvider;
		}

		public static void ResolveOracle(string path)
		{
			new AssemblyResolver(path, AssemblyName);
		}

		public static void ResolveOracle(Assembly assembly)
		{
			new AssemblyResolver(assembly, AssemblyName);
		}

		public static bool IsXmlTypeSupported
		{
			get
			{
				return DetectedProvider.IsXmlTypeSupported;
			}
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

		private static BulkCopyType _defaultBulkCopyType = BulkCopyType.MultipleRows;
		public  static BulkCopyType  DefaultBulkCopyType
		{
			get { return _defaultBulkCopyType;  }
			set { _defaultBulkCopyType = value; }
		}

		public static BulkCopyRowsCopied MultipleRowsCopy<T>(
			this DataConnection        dataConnection,
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

		public static BulkCopyRowsCopied ProviderSpecificBulkCopy<T>(
			DataConnection             dataConnection,
			IEnumerable<T>             source,
			int?                       maxBatchSize       = null,
			int?                       bulkCopyTimeout    = null,
			int                        notifyAfter        = 0,
			Action<BulkCopyRowsCopied> rowsCopiedCallback = null)
		{
			return dataConnection.BulkCopy(
				new BulkCopyOptions
				{
					BulkCopyType       = BulkCopyType.ProviderSpecific,
					BulkCopyTimeout    = bulkCopyTimeout,
					NotifyAfter        = notifyAfter,
					RowsCopiedCallback = rowsCopiedCallback,
				}, source);
		}

		#endregion

		public static bool UseAlternativeBulkCopy = false;

		public static Func<IDataReader,int,decimal> DataReaderGetDecimal = (dr, i) => dr.GetDecimal(i);
	}
}
