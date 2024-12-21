using System.Data.Common;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider;
using LinqToDB.DataProvider.SQLite;

using StackExchange.Profiling;
using StackExchange.Profiling.Data;

namespace Tests
{
	public class SQLiteMiniprofilerProvider : SQLiteDataProvider
	{
		private readonly bool _mapped;

		public SQLiteMiniprofilerProvider(bool mapped)
			: base(ProviderName.SQLiteClassic, SQLiteProvider.System)
		{
			_mapped = mapped;
		}

		public override void InitContext(IDataContext dataContext)
		{
			if (_mapped)
				dataContext.AddInterceptor(UnwrapProfilerInterceptor.Instance);
		}

		protected override DbConnection CreateConnectionInternal(string connectionString)
		{
			return new ProfiledDbConnection(base.CreateConnectionInternal(connectionString), MiniProfiler.Current);
		}

		public static void Init()
		{
			// initialize miniprofiler or it will not wrap non-connection objects
			MiniProfiler.DefaultOptions.StartProfiler();

			var mpm = new SQLiteMiniprofilerProvider(true);
			var mpu = new SQLiteMiniprofilerProvider(false);

			DataConnection.InsertProviderDetector(ProviderDetector);

			IDataProvider? ProviderDetector(ConnectionOptions options)
			{
				return options.ConfigurationString switch
				{
					TestProvName.SQLiteClassicMiniProfilerMapped   => mpm,
					TestProvName.SQLiteClassicMiniProfilerUnmapped => mpu,
					_ => null
				};
			}
		}
	}
}
