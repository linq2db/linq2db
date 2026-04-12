using System.Linq;

using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Internal.Common;

using NUnit.Framework;

namespace Tests.Linq
{
	partial class WindowFunctionsTests
	{
		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllMySql57, TestProvName.AllAccess, TestProvName.AllSqlCe, TestProvName.AllSybase, ErrorMessage = ErrorHelper.Error_WindowFunction_NotSupported)]
		public void LagSimple([DataSources(TestProvName.AllOracleNative)] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				select new
				{
					Id  = t.Id,
					Lag = Sql.Window.Lag(t.IntValue, w => w.OrderBy(t.Id)),
				};

				_ = query.ToList();
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllMySql57, TestProvName.AllAccess, TestProvName.AllSqlCe, TestProvName.AllSybase, ErrorMessage = ErrorHelper.Error_WindowFunction_NotSupported)]
		public void LagWithOffset([DataSources(TestProvName.AllOracleNative)] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				select new
				{
					Id  = t.Id,
					Lag = Sql.Window.Lag(t.IntValue, 2, w => w.OrderBy(t.Id)),
				};

				_ = query.ToList();
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllMySql57, TestProvName.AllAccess, TestProvName.AllSqlCe, TestProvName.AllSybase, ErrorMessage = ErrorHelper.Error_WindowFunction_NotSupported)]
		public void LagWithOffsetAndDefault([DataSources(TestProvName.AllOracleNative)] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				select new
				{
					Id  = t.Id,
					Lag = Sql.Window.Lag(t.IntValue, 2, 0, w => w.OrderBy(t.Id)),
				};

				_ = query.ToList();
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllMySql57, TestProvName.AllAccess, TestProvName.AllSqlCe, TestProvName.AllSybase, ErrorMessage = ErrorHelper.Error_WindowFunction_NotSupported)]
		public void LagWithPartition([DataSources(TestProvName.AllOracleNative)] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				select new
				{
					Id  = t.Id,
					Lag = Sql.Window.Lag(t.IntValue, w => w.PartitionBy(t.CategoryId).OrderBy(t.Id)),
				};

				_ = query.ToList();
		}
	}
}
