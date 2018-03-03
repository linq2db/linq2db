﻿using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Reflection;
using LinqToDB.Common;
using LinqToDB.Extensions;

namespace LinqToDB.DataProvider.Sybase
{
	using Data;

	public static class SybaseTools
	{
		public static string AssemblyName;

		static readonly SybaseDataProvider _sybaseDataProvider = new SybaseDataProvider();

#pragma warning disable 3015, 219
		static SybaseTools()
		{
			try
			{
				var path = typeof(SybaseTools).AssemblyEx().GetPath();

				var _ =
					File.Exists(Path.Combine(path, (AssemblyName = "Sybase.AdoNet45.AseClient") + ".dll")) ||
					File.Exists(Path.Combine(path, (AssemblyName = "Sybase.AdoNet4.AseClient")  + ".dll"))  ||
					File.Exists(Path.Combine(path, (AssemblyName = "Sybase.AdoNet35.AseClient") + ".dll")) ||
					File.Exists(Path.Combine(path, (AssemblyName = "Sybase.AdoNet2.AseClient")  + ".dll"));
			}
			catch (Exception)
			{
			}

			DataConnection.AddDataProvider(_sybaseDataProvider);
		}
#pragma warning restore 3015, 219

		public static IDataProvider GetDataProvider()
		{
			return _sybaseDataProvider;
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
			return new DataConnection(_sybaseDataProvider, connectionString);
		}

		public static DataConnection CreateDataConnection(IDbConnection connection)
		{
			return new DataConnection(_sybaseDataProvider, connection);
		}

		public static DataConnection CreateDataConnection(IDbTransaction transaction)
		{
			return new DataConnection(_sybaseDataProvider, transaction);
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
