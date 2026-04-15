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
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllMySql57, TestProvName.AllAccess, TestProvName.AllSqlCe, TestProvName.AllSybase, TestProvName.AllFirebirdLess3, TestProvName.AllSqlServer2008Minus, ErrorMessage = ErrorHelper.Error_WindowFunction_NotSupported)]
		public void CountNoArgs([DataSources] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				select new
				{
					CountAll            = Sql.Window.Count(w => w.PartitionBy(t.CategoryId).OrderBy(t.Id)),
					CountAllNoPartition = Sql.Window.Count(w => w.OrderBy(t.Id)),
				};

				_ = query.ToList();
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllMySql57, TestProvName.AllAccess, TestProvName.AllSqlCe, TestProvName.AllSybase, TestProvName.AllFirebirdLess3, TestProvName.AllSqlServer2008Minus, ErrorMessage = ErrorHelper.Error_WindowFunction_NotSupported)]
		public void CountWithArg([DataSources(
			TestProvName.AllAccess,
			ProviderName.Firebird3,
			// ClickHouse has DB bug with COUNT(nullable_column) in window context
			TestProvName.AllClickHouse)] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				select new
				{
					CountArg = Sql.Window.Count(w => w.Argument(t.NullableIntValue).PartitionBy(t.CategoryId).OrderBy(t.Id)),
				};

				_ = query.ToList();
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllMySql57, TestProvName.AllAccess, TestProvName.AllSqlCe, TestProvName.AllSybase, TestProvName.AllFirebirdLess3, TestProvName.AllSqlServer2008Minus, ErrorMessage = ErrorHelper.Error_WindowFunction_NotSupported)]
		public void CountWithFilter([DataSources] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				select new
				{
					CountFiltered = Sql.Window.Count(w => w.Filter(t.IntValue > 20).PartitionBy(t.CategoryId).OrderBy(t.Id)),
				};

				_ = query.ToList();
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllMySql57, TestProvName.AllAccess, TestProvName.AllSqlCe, TestProvName.AllSybase, TestProvName.AllFirebirdLess3, TestProvName.AllSqlServer2008Minus, ErrorMessage = ErrorHelper.Error_WindowFunction_NotSupported)]
		public void CountWithDefineWindow([DataSources] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				let wnd = Sql.Window.DefineWindow(w => w.PartitionBy(t.CategoryId).OrderBy(t.Id))
				select new
				{
					CountAll = Sql.Window.Count(w => w.UseWindow(wnd)),
				};

				_ = query.ToList();
		}
	}
}
