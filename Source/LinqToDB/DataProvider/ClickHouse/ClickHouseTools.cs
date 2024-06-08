﻿using System;
using System.Data.Common;

namespace LinqToDB.DataProvider.ClickHouse
{
	using Data;

	public static partial class ClickHouseTools
	{
		internal static ClickHouseProviderDetector ProviderDetector = new();

		public static bool AutoDetectProvider
		{
			get => ProviderDetector.AutoDetectProvider;
			set => ProviderDetector.AutoDetectProvider = value;
		}

		public static IDataProvider GetDataProvider(ClickHouseProvider provider = ClickHouseProvider.AutoDetect, string? connectionString = null)
		{
			return ProviderDetector.GetDataProvider(new ConnectionOptions(ConnectionString: connectionString), provider, default);
		}

		public static DataConnection CreateDataConnection(string connectionString, ClickHouseProvider provider = ClickHouseProvider.AutoDetect)
		{
			return new DataConnection(ProviderDetector.GetDataProvider(new ConnectionOptions(ConnectionString: connectionString), provider, default), connectionString);
		}

		public static DataConnection CreateDataConnection(DbConnection connection, ClickHouseProvider provider = ClickHouseProvider.AutoDetect)
		{
			return new DataConnection(ProviderDetector.GetDataProvider(new ConnectionOptions(DbConnection: connection), provider, default), connection);
		}

		public static DataConnection CreateDataConnection(DbTransaction transaction, ClickHouseProvider provider = ClickHouseProvider.AutoDetect)
		{
			return new DataConnection(ProviderDetector.GetDataProvider(new ConnectionOptions(DbTransaction: transaction), provider, default), transaction);
		}
	}
}
