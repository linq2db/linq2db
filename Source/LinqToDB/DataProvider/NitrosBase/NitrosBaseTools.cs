using System;
using System.Data;

namespace LinqToDB.DataProvider.NitrosBase
{
	using Data;
	using LinqToDB.Configuration;

	// stores provider instance and provides:
	// - APIs to get provider instance
	// - default bulk copy options
	// also could contain other helpers if needed.
	public static class NitrosBaseTools
	{
		// single instance of provider implementation
		// self-registers in DataConnection on creation
		private static readonly Lazy<IDataProvider> _nitrosBaseDataProvider = new (() =>
		{
			var provider = new NitrosBaseDataProvider();

			DataConnection.AddDataProvider(provider);

			return provider;
		}, true);

		// this is important method that inspects connection settings and if they look like settings for current provider
		// returns provider instance
		// used by DataConnection to find corresponding provider
		internal static IDataProvider? ProviderDetector(IConnectionStringSettings css, string connectionString)
		{
			//if (/* TODO: add implementation */)
			//	return _nitrosBaseDataProvider.Value;

			return null;
		}

		/// <summary>
		/// Gets instance of NitrosBase provider.
		/// </summary>
		/// <returns>Returns provider instance.</returns>
		public static IDataProvider GetDataProvider() => _nitrosBaseDataProvider.Value;

		#region CreateDataConnection

		/// <summary>
		/// Creates <see cref="DataConnection"/> instance using NitrosBase provider and <paramref name="connectionString"/>.
		/// </summary>
		/// <param name="connectionString">Database connection string.</param>
		/// <returns><see cref="DataConnection"/> instance.</returns>
		public static DataConnection CreateDataConnection(string connectionString)
		{
			return new DataConnection(_nitrosBaseDataProvider.Value, connectionString);
		}

		/// <summary>
		/// Creates <see cref="DataConnection"/> instance using NitrosBase provider and attaches it to existing connection object <paramref name="connection"/>.
		/// </summary>
		/// <param name="connection">Database connection.</param>
		/// <returns><see cref="DataConnection"/> instance.</returns>
		public static DataConnection CreateDataConnection(IDbConnection connection)
		{
			return new DataConnection(_nitrosBaseDataProvider.Value, connection);
		}

		/// <summary>
		/// Creates <see cref="DataConnection"/> instance using NitrosBase provider and attaches it to existing connection object <paramref name="transaction"/>.
		/// </summary>
		/// <param name="transaction">Database transaction.</param>
		/// <returns><see cref="DataConnection"/> instance.</returns>
		public static DataConnection CreateDataConnection(IDbTransaction transaction)
		{
			return new DataConnection(_nitrosBaseDataProvider.Value, transaction);
		}
		#endregion

		#region BulkCopy
		/// <summary>
		/// Default bulk copy mode, used when there is no mode specified explicitly on API call.
		/// If specified mode doesn't supported by provider, it will be silently downgraded to supported mode.
		/// </summary>
		public  static BulkCopyType  DefaultBulkCopyType { get; set; } = BulkCopyType.ProviderSpecific;
		#endregion
	}
}
