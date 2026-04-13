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
		public void AggregateWithFilter([DataSources(TestProvName.AllOracleNative, TestProvName.AllAccess, TestProvName.AllSapHana)] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				select new
				{
					SumFiltered             = Sql.Window.Sum(t.IntValue, w => w.Filter(t.CategoryId == 1).PartitionBy(t.CategoryId).OrderBy(t.Id)),
					SumFilteredWithFrame    = Sql.Window.Sum(t.IntValue, w => w.Filter(t.IntValue > 20).OrderBy(t.Id).RowsBetween.Unbounded.And.CurrentRow),
					SumFilterPartitionFrame = Sql.Window.Sum(t.IntValue, w => w.Filter(t.IntValue > 10).PartitionBy(t.CategoryId).OrderBy(t.Id).RowsBetween.Value(1).And.Value(1)),
					AvgFilteredRange        = Sql.Window.Average(t.DoubleValue, w => w.Filter(t.DoubleValue > 15.0).OrderBy(t.Id).RangeBetween.Unbounded.And.CurrentRow),
					MinFiltered             = Sql.Window.Min(t.IntValue, w => w.Filter(t.IntValue > 10).PartitionBy(t.CategoryId).OrderBy(t.Id)),
					MaxFilteredFrame        = Sql.Window.Max(t.IntValue, w => w.Filter(t.IntValue < 80).OrderBy(t.Id).RowsBetween.Unbounded.And.CurrentRow),
					CountFiltered           = Sql.Window.Count(w => w.Filter(t.IntValue > 20).PartitionBy(t.CategoryId).OrderBy(t.Id)),
				};

			_ = query.ToList();
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllMySql57, TestProvName.AllAccess, TestProvName.AllSqlCe, TestProvName.AllSybase, TestProvName.AllFirebirdLess3, TestProvName.AllSqlServer2008Minus, ErrorMessage = ErrorHelper.Error_WindowFunction_NotSupported)]
		public void AggregateWithFrame([DataSources(TestProvName.AllOracleNative, TestProvName.AllAccess, TestProvName.AllSapHana)] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				select new
				{
					SumFrame    = Sql.Window.Sum(t.IntValue, w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).RowsBetween.Unbounded.And.CurrentRow),
					SumFrameVal = Sql.Window.Sum(t.IntValue, w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).RowsBetween.Value(2).And.Value(2)),
					SumRange    = Sql.Window.Sum(t.IntValue, w => w.OrderBy(t.Id).RangeBetween.Unbounded.And.CurrentRow),
					AvgFrame    = Sql.Window.Average(t.DoubleValue, w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).RowsBetween.Value(1).And.CurrentRow),
					CountFrame  = Sql.Window.Count(w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).RowsBetween.Unbounded.And.CurrentRow),
				};

			_ = query.ToList();
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllMySql57, TestProvName.AllAccess, TestProvName.AllSqlCe, TestProvName.AllSybase, TestProvName.AllFirebirdLess3, TestProvName.AllSqlServer2008Minus, ErrorMessage = ErrorHelper.Error_WindowFunction_NotSupported)]
		public void CountArgWithFrame([DataSources(
			TestProvName.AllOracleNative,
			TestProvName.AllMySql57,
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
					CountArgFrame = Sql.Window.Count(w => w.Argument(t.NullableIntValue).OrderBy(t.Id).RowsBetween.Value(2).And.Value(2)),
				};

			_ = query.ToList();
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllMySql57, TestProvName.AllAccess, TestProvName.AllSqlCe, TestProvName.AllSybase, TestProvName.AllFirebirdLess3, TestProvName.AllSqlServer2008Minus, ErrorMessage = ErrorHelper.Error_WindowFunction_NotSupported)]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSqlServer2012Plus, TestProvName.AllClickHouse, TestProvName.AllMySql80, TestProvName.AllMariaDB, TestProvName.AllFirebird3Plus, TestProvName.AllDB2, TestProvName.AllSapHana, TestProvName.AllInformix, ProviderName.Ydb, TestProvName.AllOracle, ErrorMessage = ErrorHelper.Error_WindowFunction_FrameExclude)]
		public void AggregateWithFrameExclude([DataSources(TestProvName.AllOracleNative, TestProvName.AllAccess, TestProvName.AllSapHana)] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				select new
				{
					SumFrameExclude      = Sql.Window.Sum(t.IntValue, w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).RowsBetween.Unbounded.And.Unbounded.ExcludeCurrentRow()),
					AvgGroupsExcludeTies = Sql.Window.Average(t.DoubleValue, w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).GroupsBetween.Unbounded.And.Unbounded.ExcludeTies()),
				};

			_ = query.ToList();
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllMySql57, TestProvName.AllAccess, TestProvName.AllSqlCe, TestProvName.AllSybase, TestProvName.AllFirebirdLess3, TestProvName.AllSqlServer2008Minus, ErrorMessage = ErrorHelper.Error_WindowFunction_NotSupported)]
		public void MixedFunctionsInOneSelect([DataSources(TestProvName.AllOracleNative, TestProvName.AllAccess, TestProvName.AllSapHana)] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				select new
				{
					RowNum    = Sql.Window.RowNumber(w => w.PartitionBy(t.CategoryId).OrderBy(t.Id)),
					Rank      = Sql.Window.Rank(w => w.PartitionBy(t.CategoryId).OrderBy(t.IntValue)),
					DenseRank = Sql.Window.DenseRank(w => w.PartitionBy(t.CategoryId).OrderBy(t.IntValue)),
					Sum       = Sql.Window.Sum(t.IntValue, w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).RowsBetween.Unbounded.And.CurrentRow),
					LeadVal   = Sql.Window.Lead(t.IntValue, 1, 0, w => w.PartitionBy(t.CategoryId).OrderBy(t.Id)),
					LagVal    = Sql.Window.Lag(t.IntValue, 1, 0, w => w.PartitionBy(t.CategoryId).OrderBy(t.Id)),
					First     = Sql.Window.FirstValue(t.IntValue, w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).RowsBetween.Unbounded.And.CurrentRow),
					Last      = Sql.Window.LastValue(t.IntValue, w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).RowsBetween.Unbounded.And.Unbounded),
				};

			_ = query.ToList();
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllMySql57, TestProvName.AllAccess, TestProvName.AllSqlCe, TestProvName.AllSybase, TestProvName.AllFirebirdLess3, TestProvName.AllSqlServer2008Minus, ErrorMessage = ErrorHelper.Error_WindowFunction_NotSupported)]
		public void ValueFunctionWithFrames([DataSources(TestProvName.AllOracleNative, TestProvName.AllAccess, TestProvName.AllSapHana)] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				select new
				{
					FirstRows       = Sql.Window.FirstValue(t.IntValue, w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).RowsBetween.Unbounded.And.CurrentRow),
					FirstRange      = Sql.Window.FirstValue(t.IntValue, w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).RangeBetween.Unbounded.And.CurrentRow),
					LastRowsAll     = Sql.Window.LastValue(t.IntValue, w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).RowsBetween.Unbounded.And.Unbounded),
					LastRowsCurrent = Sql.Window.LastValue(t.IntValue, w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).RowsBetween.CurrentRow.And.Unbounded),
				};

			_ = query.ToList();
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllMySql57, TestProvName.AllAccess, TestProvName.AllSqlCe, TestProvName.AllSybase, TestProvName.AllFirebirdLess3, TestProvName.AllSqlServer2008Minus, ErrorMessage = ErrorHelper.Error_WindowFunction_NotSupported)]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSqlServer2012Plus, TestProvName.AllInformix, ErrorMessage = ErrorHelper.Error_WindowFunction_NthValue)]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllClickHouse, TestProvName.AllMySql80, TestProvName.AllMariaDB, TestProvName.AllFirebird3Plus, TestProvName.AllDB2, TestProvName.AllSapHana, ProviderName.Ydb, TestProvName.AllOracle, ErrorMessage = ErrorHelper.Error_WindowFunction_FrameExclude)]
		public void ValueFunctionWithFrameExclude([DataSources(TestProvName.AllOracleNative, TestProvName.AllAccess, TestProvName.AllSapHana)] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				select new
				{
					NthRowsAll   = Sql.Window.NthValue(t.IntValue, 2L, w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).RowsBetween.Unbounded.And.Unbounded),
					FirstExclude = Sql.Window.FirstValue(t.IntValue, w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).RowsBetween.Unbounded.And.Unbounded.ExcludeCurrentRow()),
				};

			_ = query.ToList();
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllMySql57, TestProvName.AllAccess, TestProvName.AllSqlCe, TestProvName.AllSybase, TestProvName.AllFirebirdLess3, TestProvName.AllSqlServer2008Minus, ErrorMessage = ErrorHelper.Error_WindowFunction_NotSupported)]
		public void RowsFrameAllBoundaries([DataSources(TestProvName.AllOracleNative, TestProvName.AllAccess, TestProvName.AllSapHana)] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				select new
				{
					RowsUnbCurr  = Sql.Window.Sum(t.IntValue, w => w.OrderBy(t.Id).RowsBetween.Unbounded.And.CurrentRow),
					RowsUnbUnb   = Sql.Window.Sum(t.IntValue, w => w.OrderBy(t.Id).RowsBetween.Unbounded.And.Unbounded),
					RowsUnbVal   = Sql.Window.Sum(t.IntValue, w => w.OrderBy(t.Id).RowsBetween.Unbounded.And.Value(3)),
					RowsCurrCurr = Sql.Window.Sum(t.IntValue, w => w.OrderBy(t.Id).RowsBetween.CurrentRow.And.CurrentRow),
					RowsCurrUnb  = Sql.Window.Sum(t.IntValue, w => w.OrderBy(t.Id).RowsBetween.CurrentRow.And.Unbounded),
					RowsCurrVal  = Sql.Window.Sum(t.IntValue, w => w.OrderBy(t.Id).RowsBetween.CurrentRow.And.Value(3)),
					RowsValCurr  = Sql.Window.Sum(t.IntValue, w => w.OrderBy(t.Id).RowsBetween.Value(2).And.CurrentRow),
					RowsValUnb   = Sql.Window.Sum(t.IntValue, w => w.OrderBy(t.Id).RowsBetween.Value(2).And.Unbounded),
					RowsValVal   = Sql.Window.Sum(t.IntValue, w => w.OrderBy(t.Id).RowsBetween.Value(2).And.Value(3)),
				};

			_ = query.ToList();
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllMySql57, TestProvName.AllAccess, TestProvName.AllSqlCe, TestProvName.AllSybase, TestProvName.AllFirebirdLess3, TestProvName.AllSqlServer2008Minus, ErrorMessage = ErrorHelper.Error_WindowFunction_NotSupported)]
		public void RangeFrameBoundaries([DataSources(TestProvName.AllOracleNative, TestProvName.AllAccess, TestProvName.AllSapHana)] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				select new
				{
					RangeUnbCurr = Sql.Window.Sum(t.IntValue, w => w.OrderBy(t.Id).RangeBetween.Unbounded.And.CurrentRow),
					RangeUnbUnb  = Sql.Window.Sum(t.IntValue, w => w.OrderBy(t.Id).RangeBetween.Unbounded.And.Unbounded),
					RangeCurrUnb = Sql.Window.Sum(t.IntValue, w => w.OrderBy(t.Id).RangeBetween.CurrentRow.And.Unbounded),
				};

			_ = query.ToList();
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllMySql57, TestProvName.AllAccess, TestProvName.AllSqlCe, TestProvName.AllSybase, TestProvName.AllFirebirdLess3, TestProvName.AllSqlServer2008Minus, ErrorMessage = ErrorHelper.Error_WindowFunction_NotSupported)]
		public void DefineWindowReuseRanking([DataSources(TestProvName.AllOracleNative, TestProvName.AllAccess, TestProvName.AllSapHana)] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				let wnd = Sql.Window.DefineWindow(w => w.PartitionBy(t.CategoryId).OrderBy(t.Id))
				select new
				{
					RowNum = Sql.Window.RowNumber(w => w.UseWindow(wnd)),
					Rank   = Sql.Window.Rank(w => w.UseWindow(wnd)),
					Sum    = Sql.Window.Sum(t.IntValue, w => w.UseWindow(wnd)),
					Avg    = Sql.Window.Average(t.DoubleValue, w => w.UseWindow(wnd)),
					Min    = Sql.Window.Min(t.IntValue, w => w.UseWindow(wnd)),
					Max    = Sql.Window.Max(t.IntValue, w => w.UseWindow(wnd)),
					Count  = Sql.Window.Count(w => w.UseWindow(wnd)),
					Lead   = Sql.Window.Lead(t.IntValue, w => w.UseWindow(wnd)),
					Lag    = Sql.Window.Lag(t.IntValue, w => w.UseWindow(wnd)),
				};

			_ = query.ToList();
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllMySql57, TestProvName.AllAccess, TestProvName.AllSqlCe, TestProvName.AllSybase, TestProvName.AllFirebirdLess3, TestProvName.AllSqlServer2008Minus, ErrorMessage = ErrorHelper.Error_WindowFunction_NotSupported)]
		public void DefineWindowReuseValueFunctions([DataSources(TestProvName.AllOracleNative, TestProvName.AllAccess, TestProvName.AllSapHana)] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				let wnd = Sql.Window.DefineWindow(w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).RowsBetween.Unbounded.And.Unbounded)
				select new
				{
					First = Sql.Window.FirstValue(t.IntValue, w => w.UseWindow(wnd)),
					Last  = Sql.Window.LastValue(t.IntValue, w => w.UseWindow(wnd)),
				};

			_ = query.ToList();
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllMySql57, TestProvName.AllAccess, TestProvName.AllSqlCe, TestProvName.AllSybase, TestProvName.AllFirebirdLess3, TestProvName.AllSqlServer2008Minus, ErrorMessage = ErrorHelper.Error_WindowFunction_NotSupported)]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSqlServer2012Plus, TestProvName.AllClickHouse, TestProvName.AllMySql80, TestProvName.AllMariaDB, TestProvName.AllFirebird3Plus, TestProvName.AllDB2, TestProvName.AllSapHana, TestProvName.AllInformix, ProviderName.Ydb, TestProvName.AllOracle, ErrorMessage = ErrorHelper.Error_WindowFunction_FrameExclude)]
		public void FrameExclusionRows([DataSources(TestProvName.AllOracleNative, TestProvName.AllAccess, TestProvName.AllSapHana)] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				select new
				{
					ExclCurrRow = Sql.Window.Sum(t.IntValue, w => w.OrderBy(t.Id).RowsBetween.Unbounded.And.Unbounded.ExcludeCurrentRow()),
					ExclGroup   = Sql.Window.Sum(t.IntValue, w => w.OrderBy(t.Id).RowsBetween.Unbounded.And.Unbounded.ExcludeGroup()),
					ExclTies    = Sql.Window.Sum(t.IntValue, w => w.OrderBy(t.Id).RowsBetween.Unbounded.And.Unbounded.ExcludeTies()),
					ValExcl     = Sql.Window.Sum(t.IntValue, w => w.OrderBy(t.Id).RowsBetween.Value(2).And.Value(2).ExcludeCurrentRow()),
				};

			_ = query.ToList();
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllMySql57, TestProvName.AllAccess, TestProvName.AllSqlCe, TestProvName.AllSybase, TestProvName.AllFirebirdLess3, TestProvName.AllSqlServer2008Minus, ErrorMessage = ErrorHelper.Error_WindowFunction_NotSupported)]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSqlServer2012Plus, TestProvName.AllClickHouse, TestProvName.AllMySql80, TestProvName.AllMariaDB, TestProvName.AllFirebird3Plus, TestProvName.AllDB2, TestProvName.AllSapHana, TestProvName.AllInformix, ProviderName.Ydb, TestProvName.AllOracle, ErrorMessage = ErrorHelper.Error_WindowFunction_FrameExclude)]
		public void FrameExclusionRange([DataSources(TestProvName.AllOracleNative, TestProvName.AllAccess, TestProvName.AllSapHana)] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				select new
				{
					RangeExclCurrRow = Sql.Window.Sum(t.IntValue, w => w.OrderBy(t.Id).RangeBetween.Unbounded.And.Unbounded.ExcludeCurrentRow()),
				};

			_ = query.ToList();
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllMySql57, TestProvName.AllAccess, TestProvName.AllSqlCe, TestProvName.AllSybase, TestProvName.AllFirebirdLess3, TestProvName.AllSqlServer2008Minus, ErrorMessage = ErrorHelper.Error_WindowFunction_NotSupported)]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSqlServer2012Plus, TestProvName.AllClickHouse, TestProvName.AllMySql80, TestProvName.AllMariaDB, TestProvName.AllFirebird3Plus, TestProvName.AllDB2, TestProvName.AllSapHana, TestProvName.AllInformix, ProviderName.Ydb, TestProvName.AllOracle, ErrorMessage = ErrorHelper.Error_WindowFunction_FrameGroups)]
		public void FrameExclusionGroups([DataSources(TestProvName.AllOracleNative, TestProvName.AllAccess, TestProvName.AllSapHana)] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				select new
				{
					GroupsExclTies = Sql.Window.Sum(t.IntValue, w => w.OrderBy(t.Id).GroupsBetween.Unbounded.And.Unbounded.ExcludeTies()),
				};

			_ = query.ToList();
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllMySql57, TestProvName.AllAccess, TestProvName.AllSqlCe, TestProvName.AllSybase, TestProvName.AllFirebirdLess3, TestProvName.AllSqlServer2008Minus, ErrorMessage = ErrorHelper.Error_WindowFunction_NotSupported)]
		public void LeadLagAllOverloads([DataSources(TestProvName.AllOracleNative, TestProvName.AllAccess, TestProvName.AllSapHana)] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				select new
				{
					Lead1 = Sql.Window.Lead(t.IntValue,       w => w.PartitionBy(t.CategoryId).OrderBy(t.Id)),
					Lead2 = Sql.Window.Lead(t.IntValue, 2,    w => w.PartitionBy(t.CategoryId).OrderBy(t.Id)),
					Lead3 = Sql.Window.Lead(t.IntValue, 2, 0, w => w.PartitionBy(t.CategoryId).OrderBy(t.Id)),
					Lag1  = Sql.Window.Lag(t.IntValue,        w => w.PartitionBy(t.CategoryId).OrderBy(t.Id)),
					Lag2  = Sql.Window.Lag(t.IntValue, 2,     w => w.PartitionBy(t.CategoryId).OrderBy(t.Id)),
					Lag3  = Sql.Window.Lag(t.IntValue, 2, 0,  w => w.PartitionBy(t.CategoryId).OrderBy(t.Id)),
				};

			_ = query.ToList();
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllMySql57, TestProvName.AllAccess, TestProvName.AllSqlCe, TestProvName.AllSybase, TestProvName.AllFirebirdLess3, TestProvName.AllSqlServer2008Minus, ErrorMessage = ErrorHelper.Error_WindowFunction_NotSupported)]
		public void LeadLagDifferentTypes([DataSources(TestProvName.AllOracleNative, TestProvName.AllAccess, TestProvName.AllSapHana)] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				select new
				{
					LeadDouble   = Sql.Window.Lead(t.DoubleValue,      w => w.OrderBy(t.Id)),
					LagDecimal   = Sql.Window.Lag(t.DecimalValue,      w => w.OrderBy(t.Id)),
					LeadNullable = Sql.Window.Lead(t.NullableIntValue, w => w.OrderBy(t.Id)),
					LeadMultiOrd = Sql.Window.Lead(t.IntValue,         w => w.OrderBy(t.CategoryId).ThenBy(t.Id)),
				};

			_ = query.ToList();
		}
	}
}
