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
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllMySql57, TestProvName.AllAccess, TestProvName.AllSqlCe, TestProvName.AllSybase, TestProvName.AllFirebirdLess3, ErrorMessage = ErrorHelper.Error_WindowFunction_NotSupported)]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSqlServer2008Minus, TestProvName.AllSqlServer2012Plus, TestProvName.AllInformix, ProviderName.Firebird3, ProviderName.Firebird4, ErrorMessage = ErrorHelper.Error_WindowFunction_NthValue)]
		public void NthValueBasic([DataSources] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				select new
				{
					Id       = t.Id,
					NthValue = Sql.Window.NthValue(t.IntValue, 2L, w => w.PartitionBy(t.CategoryId).OrderBy(t.Id)),
				};

				_ = query.ToList();
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllMySql57, TestProvName.AllAccess, TestProvName.AllSqlCe, TestProvName.AllSybase, TestProvName.AllFirebirdLess3, ErrorMessage = ErrorHelper.Error_WindowFunction_NotSupported)]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSqlServer2008Minus, TestProvName.AllSqlServer2012Plus, TestProvName.AllInformix, ProviderName.Firebird3, ProviderName.Firebird4, ErrorMessage = ErrorHelper.Error_WindowFunction_NthValue)]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSapHana, ErrorMessage = ErrorHelper.Error_WindowFunction_FrameRows)]
		public void NthValueWithFrame([DataSources] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				select new
				{
					Id       = t.Id,
					NthValue = Sql.Window.NthValue(t.IntValue, 2L, w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).RowsBetween.Unbounded.And.Unbounded),
				};

				_ = query.ToList();
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllMySql57, TestProvName.AllAccess, TestProvName.AllSqlCe, TestProvName.AllSybase, TestProvName.AllFirebirdLess3, ErrorMessage = ErrorHelper.Error_WindowFunction_NotSupported)]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSqlServer2008Minus, TestProvName.AllSqlServer2012Plus, TestProvName.AllInformix, ProviderName.Firebird3, ProviderName.Firebird4, ErrorMessage = ErrorHelper.Error_WindowFunction_NthValue)]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSapHana, ErrorMessage = ErrorHelper.Error_WindowFunction_FrameRows)]
		public void NthValueWithDefineWindow([DataSources] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				let wnd = Sql.Window.DefineWindow(w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).RowsBetween.Unbounded.And.Unbounded)
				select new
				{
					Id       = t.Id,
					NthValue = Sql.Window.NthValue(t.IntValue, 2L, w => w.UseWindow(wnd)),
				};

				_ = query.ToList();
		}
	}
}
