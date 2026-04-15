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
		public void FirstValueBasic([DataSources] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				select new
				{
					Id         = t.Id,
					FirstValue = Sql.Window.FirstValue(t.IntValue, w => w.PartitionBy(t.CategoryId).OrderBy(t.Id)),
				};

				_ = query.ToList();
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllMySql57, TestProvName.AllAccess, TestProvName.AllSqlCe, TestProvName.AllSybase, TestProvName.AllFirebirdLess3, TestProvName.AllSqlServer2008Minus, ErrorMessage = ErrorHelper.Error_WindowFunction_NotSupported)]
		[ThrowsForProvider(typeof(LinqToDBException), ProviderName.Firebird3, TestProvName.AllSapHana, ErrorMessage = ErrorHelper.Error_WindowFunction_FrameRows)]
		public void FirstValueWithFrame([DataSources] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				select new
				{
					Id         = t.Id,
					FirstValue = Sql.Window.FirstValue(t.IntValue, w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).RowsBetween.Unbounded.And.CurrentRow),
				};

				_ = query.ToList();
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllMySql57, TestProvName.AllAccess, TestProvName.AllSqlCe, TestProvName.AllSybase, TestProvName.AllFirebirdLess3, TestProvName.AllSqlServer2008Minus, ErrorMessage = ErrorHelper.Error_WindowFunction_NotSupported)]
		public void FirstValueWithDefineWindow([DataSources] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				let wnd = Sql.Window.DefineWindow(w => w.PartitionBy(t.CategoryId).OrderBy(t.Id))
				select new
				{
					Id         = t.Id,
					FirstValue = Sql.Window.FirstValue(t.IntValue, w => w.UseWindow(wnd)),
				};

				_ = query.ToList();
		}
	}
}
