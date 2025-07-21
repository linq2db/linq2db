using System.Data.Common;

using LinqToDB.Data;
using LinqToDB.Internal.DataProvider.Oracle;

namespace LinqToDB.DataProvider.Oracle
{
	public static partial class OracleTools
	{
		internal static OracleProviderDetector ProviderDetector = new();

		public static OracleVersion DefaultVersion
		{
			get => ProviderDetector.DefaultVersion;
			set => ProviderDetector.DefaultVersion = value;
		}

		public static bool AutoDetectProvider
		{
			get => ProviderDetector.AutoDetectProvider;
			set => ProviderDetector.AutoDetectProvider = value;
		}

		public static IDataProvider GetDataProvider(
			OracleVersion  version          = OracleVersion.AutoDetect,
			OracleProvider provider         = OracleProvider.AutoDetect,
			string?        connectionString = null,
			DbConnection? connection        = null)
		{
			return ProviderDetector.GetDataProvider(new ConnectionOptions(ConnectionString : connectionString, DbConnection: connection), provider, version);
		}

		#region CreateDataConnection

		public static DataConnection CreateDataConnection(
			string connectionString,
			OracleVersion version   = OracleVersion.AutoDetect,
			OracleProvider provider = OracleProvider.AutoDetect)
		{
			return new DataConnection(new DataOptions()
				.UseConnectionString(ProviderDetector.GetDataProvider(new ConnectionOptions(ConnectionString: connectionString), provider, version), connectionString));
		}

		public static DataConnection CreateDataConnection(
			DbConnection connection,
			OracleVersion version   = OracleVersion.AutoDetect,
			OracleProvider provider = OracleProvider.AutoDetect)
		{
			return new DataConnection(new DataOptions()
				.UseConnection(ProviderDetector.GetDataProvider(new ConnectionOptions(DbConnection: connection), provider, version), connection));
		}

		public static DataConnection CreateDataConnection(
			DbTransaction transaction,
			OracleVersion version   = OracleVersion.AutoDetect,
			OracleProvider provider = OracleProvider.AutoDetect)
		{
			return new DataConnection(new DataOptions()
				.UseTransaction(ProviderDetector.GetDataProvider(new ConnectionOptions(DbTransaction: transaction), provider, version), transaction));
		}

		#endregion
	}
}
