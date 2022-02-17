using System;
using System.Data.Common;

using LinqToDB;
using LinqToDB.Configuration;
using LinqToDB.Data;
using LinqToDB.DataProvider;
using LinqToDB.DataProvider.SQLite;

using StackExchange.Profiling;
using StackExchange.Profiling.Data;

namespace Tests
{
	using Data;

	internal class SQLiteMiniprofilerProvider : SQLiteDataProvider
	{
		private readonly bool _mapped;

		public SQLiteMiniprofilerProvider(bool mapped)
			: base(ProviderName.SQLiteClassic)
		{
			_mapped = mapped;
		}

		public override void InitContext(IDataContext dataContext)
		{
			if (_mapped)
				dataContext.AddInterceptor(new MiniProfilerTests.UnwrapProfilerInterceptor());
		}

		protected override DbConnection CreateConnectionInternal(string connectionString)
		{
			return new ProfiledDbConnection(base.CreateConnectionInternal(connectionString), MiniProfiler.Current);
		}

		public static void Init()
		{
			// initialize miniprofiler or it will not wrap non-connection objects
#if NET472
			MiniProfiler.Settings.ProfilerProvider = new SingletonProfilerProvider();
			MiniProfiler.Start();
#else
			MiniProfiler.DefaultOptions.StartProfiler();
#endif

			var mpm = new SQLiteMiniprofilerProvider(true);
			var mpu = new SQLiteMiniprofilerProvider(false);

			DataConnection.InsertProviderDetector(ProviderDetector);

			IDataProvider? ProviderDetector(IConnectionStringSettings css, string connectionString)
			{
				return css.Name switch
				{
					TestProvName.SQLiteClassicMiniProfilerMapped   => mpm,
					TestProvName.SQLiteClassicMiniProfilerUnmapped => mpu,
					_ => null
				};
			}
		}
	}
}
