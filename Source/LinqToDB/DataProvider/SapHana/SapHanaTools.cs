﻿using System.Data.Common;
using System.Reflection;

using JetBrains.Annotations;

using LinqToDB.Data;

namespace LinqToDB.DataProvider.SapHana
{
	[PublicAPI]
	public static class SapHanaTools
	{
		internal static SapHanaProviderDetector ProviderDetector = new();

		public static bool AutoDetectProvider
		{
			get => ProviderDetector.AutoDetectProvider;
			set => ProviderDetector.AutoDetectProvider = value;
		}

		public static void ResolveSapHana(string path, string? assemblyName = null)
		{
			_ = new AssemblyResolver(path, assemblyName ?? OdbcProviderAdapter.AssemblyName);
		}

		public static void ResolveSapHana(Assembly assembly)
		{
			_ = new AssemblyResolver(assembly, assembly.FullName!);
		}

		public static IDataProvider GetDataProvider(SapHanaProvider provider = SapHanaProvider.AutoDetect, string? connectionString = null)
		{
			return ProviderDetector.GetDataProvider(new ConnectionOptions(ConnectionString: connectionString), provider, default);
		}

		#region CreateDataConnection

		public static DataConnection CreateDataConnection(string connectionString, SapHanaProvider provider = SapHanaProvider.AutoDetect)
		{
			return new DataConnection(ProviderDetector.GetDataProvider(new ConnectionOptions(ConnectionString: connectionString), provider, default), connectionString);
		}

		public static DataConnection CreateDataConnection(DbConnection connection, SapHanaProvider provider = SapHanaProvider.AutoDetect)
		{
			return new DataConnection(ProviderDetector.GetDataProvider(new ConnectionOptions(DbConnection: connection), provider, default), connection);
		}

		public static DataConnection CreateDataConnection(DbTransaction transaction, SapHanaProvider provider = SapHanaProvider.AutoDetect)
		{
			return new DataConnection(ProviderDetector.GetDataProvider(new ConnectionOptions(DbTransaction: transaction), provider, default), transaction);
		}

		#endregion
	}
}
