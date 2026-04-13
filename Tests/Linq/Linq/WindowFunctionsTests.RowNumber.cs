using System.Linq;

using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Internal.Common;

using NUnit.Framework;

namespace Tests.Linq
{
	public partial class WindowFunctionsTests
	{
		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllMySql57, TestProvName.AllAccess, TestProvName.AllSqlCe, TestProvName.AllSybase, TestProvName.AllFirebirdLess3, TestProvName.AllSqlServer2008Minus, ErrorMessage = ErrorHelper.Error_WindowFunction_NotSupported)]
		public void RowNumberWithMultiplePartitions([DataSources(TestProvName.AllOracleNative, TestProvName.AllAccess, TestProvName.AllSapHana)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(WindowFunctionTestEntity.Seed());

			var query = table
				.Select(x => new
				{
					Entity = x,
					rn1    = Sql.Window.RowNumber(f => f.PartitionBy(x.CategoryId, x.Name).OrderBy(x.Timestamp)),
					rn2    = Sql.Window.RowNumber(f => f.PartitionBy(x.CategoryId, x.Name).OrderBy(x.Value)),
					rn3    = Sql.Window.RowNumber(f => f.PartitionBy(x.CategoryId, x.Name).OrderByDesc(x.Timestamp)),
					rn4    = Sql.Window.RowNumber(f => f.PartitionBy(x.CategoryId, x.Name).OrderByDesc(x.Value)),
					rn5    = Sql.Window.RowNumber(f => f.PartitionBy(x.CategoryId, x.Name).OrderBy(x.Timestamp).ThenBy(x.Value)),
					rn6    = Sql.Window.RowNumber(f => f.PartitionBy(x.CategoryId, x.Name).OrderByDesc(x.Timestamp).ThenByDesc(x.Value))
				})
				.OrderBy(x => x.Entity.Id);

				_ = query.ToList();
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllMySql57, TestProvName.AllAccess, TestProvName.AllSqlCe, TestProvName.AllSybase, TestProvName.AllFirebirdLess3, TestProvName.AllSqlServer2008Minus, ErrorMessage = ErrorHelper.Error_WindowFunction_NotSupported)]
		public void RowNumberWithMultiplePartitionsWithDefineWindow([DataSources(TestProvName.AllOracleNative, TestProvName.AllAccess, TestProvName.AllSapHana)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(WindowFunctionTestEntity.Seed());

			var query =
				from x in table
				let wnd1 = Sql.Window.DefineWindow(f => f.PartitionBy(x.CategoryId).OrderBy(x.Timestamp))
				let wnd2 = Sql.Window.DefineWindow(f => f.PartitionBy(x.CategoryId, x.Name).OrderBy(x.Value))
				let wnd3 = Sql.Window.DefineWindow(f => f.PartitionBy(x.CategoryId, x.Name).OrderByDesc(x.Timestamp))
				let wnd4 = Sql.Window.DefineWindow(f => f.PartitionBy(x.CategoryId, x.Name).OrderByDesc(x.Value))
				let wnd5 = Sql.Window.DefineWindow(f => f.PartitionBy(x.CategoryId, x.Name).OrderBy(x.Timestamp).ThenBy(x.Value))
				let wnd6 = Sql.Window.DefineWindow(f => f.PartitionBy(x.CategoryId, x.Name).OrderByDesc(x.Timestamp).ThenByDesc(x.Value))
				select new
				{
					Entity = x,
					rn1   = Sql.Window.RowNumber(f => f.UseWindow(wnd1)),
					rn2   = Sql.Window.RowNumber(f => f.UseWindow(wnd2)),
					rn3   = Sql.Window.RowNumber(f => f.UseWindow(wnd3)),
					rn4   = Sql.Window.RowNumber(f => f.UseWindow(wnd4)),
					rn5   = Sql.Window.RowNumber(f => f.UseWindow(wnd5)),
					rn6   = Sql.Window.RowNumber(f => f.UseWindow(wnd6))
				}
				into s
				orderby s.Entity.Id
				select s;

				_ = query.ToList();
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllMySql57, TestProvName.AllAccess, TestProvName.AllSqlCe, TestProvName.AllSybase, TestProvName.AllFirebirdLess3, TestProvName.AllSqlServer2008Minus, ErrorMessage = ErrorHelper.Error_WindowFunction_NotSupported)]
		//TODO: we can emulate it for other providers by using additional order by with CASE:
		//ROW_NUMBER() OVER(ORDER BY WHEN x.Value IS NULL THEN 1 ELSE 0 END, x.Value)
		public void RowNumberWithNulls([IncludeDataSources(
			true,
			TestProvName.AllOracle12Plus)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(WindowFunctionTestEntity.Seed());

			var query = table
				.Select(x => new
				{
					Entity = x,
					rn7    = Sql.Window.RowNumber(f => f.PartitionBy(x.CategoryId).OrderBy(x.Timestamp, Sql.NullsPosition.First)),
					rn8    = Sql.Window.RowNumber(f => f.PartitionBy(x.CategoryId).OrderByDesc(x.Timestamp, Sql.NullsPosition.Last))
				})
				.OrderBy(x => x.Entity.Id);

				_ = query.ToList();
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllMySql57, TestProvName.AllAccess, TestProvName.AllSqlCe, TestProvName.AllSybase, TestProvName.AllFirebirdLess3, TestProvName.AllSqlServer2008Minus, ErrorMessage = ErrorHelper.Error_WindowFunction_NotSupported)]
		public void RowNumberWithoutPartition([DataSources(TestProvName.AllOracleNative, TestProvName.AllAccess, TestProvName.AllSapHana)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(WindowFunctionTestEntity.Seed());

			var query = table
				.Select(x => new
				{
					Entity = x,
					rn1    = Sql.Window.RowNumber(f => f.OrderBy(x.Timestamp)),
					rn2    = Sql.Window.RowNumber(f => f.OrderBy(x.Value)),
					rn3    = Sql.Window.RowNumber(f => f.OrderByDesc(x.Timestamp)),
					rn4    = Sql.Window.RowNumber(f => f.OrderByDesc(x.Value)),
					rn5    = Sql.Window.RowNumber(f => f.OrderBy(x.Timestamp).ThenBy(x.Value)),
					rn6    = Sql.Window.RowNumber(f => f.OrderByDesc(x.Timestamp).ThenByDesc(x.Value))
				})
				.OrderBy(x => x.Entity.Id);

				_ = query.ToList();
		}
	}
}
