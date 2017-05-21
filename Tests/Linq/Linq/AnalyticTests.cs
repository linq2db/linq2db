namespace Tests.Linq
{
	using System.Linq;

	using LinqToDB;
	using NUnit.Framework;

	[TestFixture]
	public class AnalyticTests : TestBase
	{
		[Test, IncludeDataContextSource(true, ProviderName.Oracle, ProviderName.OracleManaged, ProviderName.OracleNative, 
			ProviderName.SqlServer2012, ProviderName.SqlServer2014, ProviderName.PostgreSQL, ProviderName.PostgreSQL92, ProviderName.PostgreSQL93)]
		public void Test(string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = 
					from p in db.Parent
					join c in db.Child on p.ParentID equals c.ParentID 
					select new
					{
						Rank1     = Sql.Ext.Rank().Over().PartitionBy(p.Value1, c.ChildID).OrderBy(p.Value1).ThenBy(c.ChildID).ThenBy(c.ParentID).ToValue(),
						RowNumber = Sql.Ext.RowNumber().Over().PartitionBy(p.Value1, c.ChildID).OrderByDesc(p.Value1).ThenBy(c.ChildID).ThenByDesc(c.ParentID).ToValue(),
						DenseRank = Sql.Ext.DenseRank().Over().PartitionBy(p.Value1, c.ChildID).OrderBy(p.Value1).ToValue(),
						Sum       = Sql.Ext.Sum(p.Value1).Over().PartitionBy(p.Value1, c.ChildID).OrderBy(p.Value1).ToValue(),
						Avg       = Sql.Ext.Average<double>(p.Value1).Over().PartitionBy(p.Value1, c.ChildID).OrderBy(p.Value1).ToValue(),

						Count1     = Sql.Ext.Count(p.ParentID, Sql.AggregateModifier.All).Over().PartitionBy(p.Value1).OrderBy(p.Value1).Range.Between.UnboundedPreceding.And.CurrentRow.ToValue(),
						Count2     = Sql.Ext.Count(p.ParentID, Sql.AggregateModifier.All).Over().PartitionBy(p.Value1).OrderBy(p.Value1).ThenBy(c.ChildID).Range.Between.UnboundedPreceding.And.CurrentRow.ToValue(),
						Count4     = Sql.Ext.Count(p.ParentID, Sql.AggregateModifier.All).Over().PartitionBy(p.Value1).OrderBy(p.Value1).ThenByDesc(c.ChildID).Range.Between.UnboundedPreceding.And.CurrentRow.ToValue(),
						Count6     = Sql.Ext.Count(p.ParentID, Sql.AggregateModifier.All).Over().PartitionBy(p.Value1).OrderBy(p.Value1).Rows.Between.UnboundedPreceding.And.ValuePreceding(3).ToValue(),
						Count7     = Sql.Ext.Count(p.ParentID, Sql.AggregateModifier.All).Over().PartitionBy(p.Value1).OrderBy(p.Value1).Range.Between.CurrentRow.And.UnboundedFollowing.ToValue(),
						Count8     = Sql.Ext.Count(p.ParentID, Sql.AggregateModifier.All).Over().PartitionBy(p.Value1).OrderBy(p.Value1, Sql.NullsPosition.None).Rows.Between.UnboundedPreceding.And.CurrentRow.ToValue(),
						Count9     = Sql.Ext.Count(p.ParentID, Sql.AggregateModifier.All).Over().PartitionBy(p.Value1).OrderBy(p.Value1).Range.UnboundedPreceding.ToValue(),
						Count10    = Sql.Ext.Count(p.ParentID, Sql.AggregateModifier.All).Over().PartitionBy(p.Value1).OrderBy(p.Value1).Range.CurrentRow.ToValue(),
						Count11    = Sql.Ext.Count(p.ParentID, Sql.AggregateModifier.All).Over().PartitionBy(p.Value1).OrderBy(p.Value1).Rows.ValuePreceding(1).ToValue(),
						Count12    = Sql.Ext.Count(p.ParentID, Sql.AggregateModifier.All).Over().PartitionBy(p.Value1).OrderByDesc(p.Value1).Range.Between.UnboundedPreceding.And.CurrentRow.ToValue(),
						Count14    = Sql.Ext.Count().Over().ToValue(),

						Combination = Sql.Ext.Rank().Over().PartitionBy(p.Value1, c.ChildID).OrderBy(p.Value1).ThenBy(c.ChildID).ToValue() + 
									  Sql.Sqrt(Sql.Ext.DenseRank().Over().PartitionBy(p.Value1, c.ChildID).OrderBy(p.Value1).ToValue()) +
									  Sql.Ext.Count(p.ParentID, Sql.AggregateModifier.All).Over().PartitionBy(p.Value1).OrderBy(p.Value1).Range.Between.UnboundedPreceding.And.CurrentRow.ToValue() +
									  Sql.Ext.Count().Over().ToValue(),
					};
				var res = q.ToArray();
				Assert.IsNotEmpty(res);
			}
		}

		[Test, IncludeDataContextSource(ProviderName.Oracle, ProviderName.OracleManaged, ProviderName.OracleNative)]
		public void TestExtensionsOracle(string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = 
					from p in db.Parent
					join c in db.Child on p.ParentID equals c.ParentID 
					select new
					{
						Rank1     = Sql.Ext.Rank().Over().PartitionBy(p.Value1, c.ChildID).OrderBy(p.Value1).ThenBy(c.ChildID).ThenBy(c.ParentID, Sql.NullsPosition.First).ToValue(),
						Rank2     = Sql.Ext.Rank().Over().PartitionBy(p.Value1, c.ChildID).OrderBy(p.Value1, Sql.NullsPosition.Last).ThenBy(c.ChildID).ThenBy(c.ParentID, Sql.NullsPosition.First).ToValue(),
						RowNumber = Sql.Ext.RowNumber().Over().PartitionBy(p.Value1, c.ChildID).OrderByDesc(p.Value1, Sql.NullsPosition.First).ThenBy(c.ChildID).ThenByDesc(c.ParentID, Sql.NullsPosition.First).ToValue(),
						DenseRank = Sql.Ext.DenseRank().Over().PartitionBy(p.Value1, c.ChildID).OrderBy(p.Value1).ToValue(),

						Count1     = Sql.Ext.Count(p.ParentID, Sql.AggregateModifier.All).Over().PartitionBy(p.Value1).OrderBy(p.Value1).Range.Between.UnboundedPreceding.And.CurrentRow.ToValue(),
						Count2     = Sql.Ext.Count(p.ParentID, Sql.AggregateModifier.All).Over().PartitionBy(p.Value1).OrderBy(p.Value1).ThenBy(c.ChildID).Range.Between.UnboundedPreceding.And.CurrentRow.ToValue(),
						Count3     = Sql.Ext.Count(p.ParentID, Sql.AggregateModifier.All).Over().PartitionBy(p.Value1).OrderBy(p.Value1).ThenBy(c.ChildID, Sql.NullsPosition.First).Range.Between.UnboundedPreceding.And.CurrentRow.ToValue(),
						Count4     = Sql.Ext.Count(p.ParentID, Sql.AggregateModifier.All).Over().PartitionBy(p.Value1).OrderBy(p.Value1).ThenByDesc(c.ChildID).Range.Between.UnboundedPreceding.And.CurrentRow.ToValue(),
						Count5     = Sql.Ext.Count(p.ParentID, Sql.AggregateModifier.All).Over().PartitionBy(p.Value1).OrderBy(p.Value1).ThenByDesc(c.ChildID, Sql.NullsPosition.Last).Range.Between.UnboundedPreceding.And.CurrentRow.ToValue(),
						Count6     = Sql.Ext.Count(p.ParentID, Sql.AggregateModifier.All).Over().PartitionBy(p.Value1).OrderBy(p.Value1).Range.Between.UnboundedPreceding.And.ValuePreceding(3).ToValue(),
						Count7     = Sql.Ext.Count(p.ParentID, Sql.AggregateModifier.All).Over().PartitionBy(p.Value1).OrderBy(p.Value1).Range.Between.CurrentRow.And.UnboundedFollowing.ToValue(),
						Count8     = Sql.Ext.Count(p.ParentID, Sql.AggregateModifier.All).Over().PartitionBy(p.Value1).OrderBy(p.Value1, Sql.NullsPosition.None).Rows.Between.UnboundedPreceding.And.CurrentRow.ToValue(),
						Count9     = Sql.Ext.Count(p.ParentID, Sql.AggregateModifier.All).Over().PartitionBy(p.Value1).OrderBy(p.Value1, Sql.NullsPosition.First).Range.UnboundedPreceding.ToValue(),
						Count10    = Sql.Ext.Count(p.ParentID, Sql.AggregateModifier.All).Over().PartitionBy(p.Value1).OrderBy(p.Value1, Sql.NullsPosition.First).Range.CurrentRow.ToValue(),
						Count11    = Sql.Ext.Count(p.ParentID, Sql.AggregateModifier.All).Over().PartitionBy(p.Value1).OrderBy(p.Value1, Sql.NullsPosition.First).Range.ValuePreceding(1).ToValue(),
						Count12    = Sql.Ext.Count(p.ParentID, Sql.AggregateModifier.All).Over().PartitionBy(p.Value1).OrderByDesc(p.Value1).Range.Between.UnboundedPreceding.And.CurrentRow.ToValue(),
						Count13    = Sql.Ext.Count(p.ParentID, Sql.AggregateModifier.All).Over().PartitionBy(p.Value1).OrderByDesc(p.Value1, Sql.NullsPosition.First).Range.Between.UnboundedPreceding.And.CurrentRow.ToValue(),
						Count14    = Sql.Ext.Count().Over().ToValue(),

						Combination = Sql.Ext.Rank().Over().PartitionBy(p.Value1, c.ChildID).OrderBy(p.Value1).ThenBy(c.ChildID).ToValue() + 
									  Sql.Sqrt(Sql.Ext.DenseRank().Over().PartitionBy(p.Value1, c.ChildID).OrderBy(p.Value1).ToValue()) +
									  Sql.Ext.Count(p.ParentID, Sql.AggregateModifier.All).Over().PartitionBy(p.Value1).OrderBy(p.Value1).Range.Between.UnboundedPreceding.And.CurrentRow.ToValue() +
									  Sql.Ext.Count().Over().ToValue(),
					};
				var res = q.ToArray();
				Assert.IsNotEmpty(res);
			}
		}

		[Test, IncludeDataContextSource(ProviderName.Oracle, ProviderName.OracleManaged, ProviderName.OracleNative)]
		public void TestAvgOracle(string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = 
					from p in db.Parent
					join c in db.Child on p.ParentID equals c.ParentID 
					select new
					{
						AvgNoOrder          = Sql.Ext.Average<double>(p.Value1, Sql.AggregateModifier.None).Over().ToValue(),
						AvgRange            = Sql.Ext.Average<double>(p.Value1, Sql.AggregateModifier.None).Over().PartitionBy(p.Value1, c.ChildID).OrderBy(p.Value1).Range.Between.ValuePreceding(0).And.ValueFollowing(1).ToValue(),
						AvgRangeNoModifier  = Sql.Ext.Average<double>(p.Value1).Over().PartitionBy(p.Value1, c.ChildID).OrderBy(p.Value1).Range.Between.ValuePreceding(0).And.ValueFollowing(1).ToValue(),

						// Testing conversion. Average may fail with Overflow error

						AvgD1     = Sql.Ext.Average<decimal>(p.Value1, Sql.AggregateModifier.All).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						AvgD2     = Sql.Ext.Average<decimal>(p.Value1, Sql.AggregateModifier.Distinct).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						AvgD3     = Sql.Ext.Average<decimal>(p.Value1, Sql.AggregateModifier.None).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						AvgD4     = Sql.Ext.Average<decimal>(p.Value1, Sql.AggregateModifier.All).Over().ToValue(),

						AvgDN1    = Sql.Ext.Average<decimal?>(p.Value1, Sql.AggregateModifier.All).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						AvgDN2    = Sql.Ext.Average<decimal?>(p.Value1, Sql.AggregateModifier.Distinct).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						AvgDN3    = Sql.Ext.Average<decimal?>(p.Value1, Sql.AggregateModifier.None).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						AvgDN4    = Sql.Ext.Average<decimal?>(p.Value1, Sql.AggregateModifier.None).Over().ToValue(),

						AvgIN1    = Sql.Ext.Average<int?>(p.Value1, Sql.AggregateModifier.All).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						AvgIN2    = Sql.Ext.Average<int?>(p.Value1, Sql.AggregateModifier.Distinct).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						AvgIN3    = Sql.Ext.Average<int?>(p.Value1, Sql.AggregateModifier.None).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						AvgIN4    = Sql.Ext.Average<int?>(p.Value1, Sql.AggregateModifier.None).Over().ToValue(),

						AvgI1     = Sql.Ext.Average<int>(p.Value1, Sql.AggregateModifier.All).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						AvgI2     = Sql.Ext.Average<int>(p.Value1, Sql.AggregateModifier.Distinct).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						AvgI3     = Sql.Ext.Average<int>(p.Value1, Sql.AggregateModifier.None).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						AvgI4     = Sql.Ext.Average<int>(p.Value1, Sql.AggregateModifier.None).Over().ToValue(),

						AvgL1     = Sql.Ext.Average<long>(p.Value1, Sql.AggregateModifier.All).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						AvgL2     = Sql.Ext.Average<long>(p.Value1, Sql.AggregateModifier.Distinct).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						AvgL3     = Sql.Ext.Average<long>(p.Value1, Sql.AggregateModifier.None).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						AvgL4     = Sql.Ext.Average<long>(p.Value1, Sql.AggregateModifier.None).Over().ToValue(),

						AvgLN1     = Sql.Ext.Average<long?>(p.Value1, Sql.AggregateModifier.All).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						AvgLN2     = Sql.Ext.Average<long?>(p.Value1, Sql.AggregateModifier.Distinct).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						AvgLN3     = Sql.Ext.Average<long?>(p.Value1, Sql.AggregateModifier.None).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						AvgLN4     = Sql.Ext.Average<long?>(p.Value1, Sql.AggregateModifier.None).Over().ToValue(),

						AvgF1     = Sql.Ext.Average<float>(p.Value1, Sql.AggregateModifier.All).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						AvgF2     = Sql.Ext.Average<float>(p.Value1, Sql.AggregateModifier.Distinct).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						AvgF3     = Sql.Ext.Average<float>(p.Value1, Sql.AggregateModifier.None).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						AvgF4     = Sql.Ext.Average<float>(p.Value1, Sql.AggregateModifier.None).Over().ToValue(),

						AvgFN1    = Sql.Ext.Average<float?>(p.Value1, Sql.AggregateModifier.All).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						AvgFN2    = Sql.Ext.Average<float?>(p.Value1, Sql.AggregateModifier.Distinct).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						AvgFN3    = Sql.Ext.Average<float?>(p.Value1, Sql.AggregateModifier.None).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						AvgFN4    = Sql.Ext.Average<float?>(p.Value1, Sql.AggregateModifier.None).Over().ToValue(),

						AvgDO1    = Sql.Ext.Average<double>(p.Value1, Sql.AggregateModifier.All).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						AvgDO2    = Sql.Ext.Average<double>(p.Value1, Sql.AggregateModifier.Distinct).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						AvgDO3    = Sql.Ext.Average<double>(p.Value1, Sql.AggregateModifier.None).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						AvgDO4    = Sql.Ext.Average<double>(p.Value1, Sql.AggregateModifier.None).Over().ToValue(),

						AvgDON1   = Sql.Ext.Average<double?>(p.Value1, Sql.AggregateModifier.All).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						AvgDON2   = Sql.Ext.Average<double?>(p.Value1, Sql.AggregateModifier.Distinct).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						AvgDON3   = Sql.Ext.Average<double?>(p.Value1, Sql.AggregateModifier.None).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						AvgDON4   = Sql.Ext.Average<double?>(p.Value1, Sql.AggregateModifier.None).Over().ToValue(),

						// modifications

						AvgAll       = Sql.Ext.Average<long?>(p.Value1, Sql.AggregateModifier.All).Over().PartitionBy(p.Value1, c.ChildID).OrderBy(p.Value1).Range.Between.UnboundedPreceding.And.CurrentRow.ToValue(),
						AvgNone      = Sql.Ext.Average<long?>(p.Value1, Sql.AggregateModifier.None).Over().PartitionBy(p.Value1, c.ChildID).OrderBy(p.Value1).Range.Between.UnboundedPreceding.And.ValueFollowing(1).ToValue(),
						AvgDistinct1 = Sql.Ext.Average<long?>(p.Value1, Sql.AggregateModifier.Distinct).Over().ToValue(),
						AvgDistinct2 = Sql.Ext.Average<long?>(p.Value1, Sql.AggregateModifier.Distinct).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),

					};
				var res = q.ToArray();
				Assert.IsNotEmpty(res);
			}
		}

		[Test, IncludeDataContextSource(ProviderName.Oracle, ProviderName.OracleManaged, ProviderName.OracleNative)]
		public void TestCorrOracle(string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = 
					from p in db.Parent
					join c in db.Child on p.ParentID equals c.ParentID 
					select new
					{
						// type conversion tests
						Corr1    = Sql.Ext.Corr<decimal>(p.Value1, c.ChildID).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						Corr2    = Sql.Ext.Corr<decimal?>(p.Value1, c.ChildID).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						Corr3    = Sql.Ext.Corr<float>(p.Value1, c.ChildID).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						Corr4    = Sql.Ext.Corr<int>(p.Value1, c.ChildID).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),

						// variations
						CorrSimple           = Sql.Ext.Corr<decimal>(p.Value1, c.ChildID).Over().ToValue(),
						CorrWithOrder        = Sql.Ext.Corr<decimal>(p.Value1, c.ChildID).Over().PartitionBy(p.Value1, c.ChildID).OrderBy(p.Value1).ToValue(),
						CorrPartitionNoOrder = Sql.Ext.Corr<decimal>(p.Value1, c.ChildID).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						CorrWithoutPartition = Sql.Ext.Corr<decimal>(p.Value1, c.ChildID).Over().OrderBy(p.Value1).ToValue(),
					};
				var res = q.ToArray();
				Assert.IsNotEmpty(res);
			}
		}

		[Test, IncludeDataContextSource(true, ProviderName.Oracle, ProviderName.OracleManaged, ProviderName.OracleNative)]
		public void TestCountOracle(string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = 
					from p in db.Parent
					join c in db.Child on p.ParentID equals c.ParentID 
					select new
					{
						Count1    = Sql.Ext.Count().Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						Count2    = Sql.Ext.Count(p.Value1).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						Count3    = Sql.Ext.Count(p.Value1, Sql.AggregateModifier.All).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						Count4    = Sql.Ext.Count(p.Value1, Sql.AggregateModifier.Distinct).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						Count5    = Sql.Ext.Count(p.Value1, Sql.AggregateModifier.None).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),

						Count11   = Sql.Ext.Count().Over().ToValue(),
						Count12   = Sql.Ext.Count(p.Value1).Over().ToValue(),
						Count13   = Sql.Ext.Count(p.Value1, Sql.AggregateModifier.All).Over().ToValue(),
						Count14   = Sql.Ext.Count(p.Value1, Sql.AggregateModifier.Distinct).Over().ToValue(),
						Count15   = Sql.Ext.Count(p.Value1, Sql.AggregateModifier.None).Over().ToValue(),

						Count21   = Sql.Ext.Count(p.Value1, Sql.AggregateModifier.All).Over().PartitionBy(c.ChildID).OrderBy(p.Value1).Range.Between.UnboundedPreceding.And.CurrentRow.ToValue(),
					};
				var res = q.ToArray();
				Assert.IsNotEmpty(res);
			}
		}

		[Test, IncludeDataContextSource(ProviderName.Oracle, ProviderName.OracleManaged, ProviderName.OracleNative)]
		public void TestCovarPopOracle(string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = 
					from p in db.Parent
					join c in db.Child on p.ParentID equals c.ParentID 
					select new
					{
						CovarPop1    = Sql.Ext.CovarPop(p.Value1, c.ChildID).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						CovarPop2    = Sql.Ext.CovarPop(p.Value1, c.ChildID).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						CovarPop3    = Sql.Ext.CovarPop(p.Value1, c.ChildID).Over().ToValue(),

						CovarPop4   = Sql.Ext.CovarPop(p.Value1, c.ChildID).Over().PartitionBy(c.ChildID).OrderBy(p.Value1).Range.Between.UnboundedPreceding.And.CurrentRow.ToValue(),
					};
				var res = q.ToArray();
				Assert.IsNotEmpty(res);
			}
		}

		[Test, IncludeDataContextSource(ProviderName.Oracle, ProviderName.OracleManaged, ProviderName.OracleNative)]
		public void TestCovarSampOracle(string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = 
					from p in db.Parent
					join c in db.Child on p.ParentID equals c.ParentID 
					select new
					{
						CovarSamp1    = Sql.Ext.CovarSamp(p.Value1, c.ChildID).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						CovarSamp2    = Sql.Ext.CovarSamp(p.Value1, c.ChildID).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						CovarSamp3    = Sql.Ext.CovarSamp(p.Value1, c.ChildID).Over().ToValue(),

						CovarSamp4   = Sql.Ext.CovarSamp(p.Value1, c.ChildID).Over().PartitionBy(c.ChildID).OrderBy(p.Value1).Range.Between.UnboundedPreceding.And.CurrentRow.ToValue(),
					};
				var res = q.ToArray();
				Assert.IsNotEmpty(res);
			}
		}

		[Test, IncludeDataContextSource(ProviderName.Oracle, ProviderName.OracleManaged, ProviderName.OracleNative)]
		public void TestCumeDistOracle(string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = 
					from p in db.Parent
					join c in db.Child on p.ParentID equals c.ParentID 
					select new
					{
						CumeDist1     = Sql.Ext.CumeDist<decimal>().Over().PartitionBy(p.Value1, c.ChildID).OrderBy(p.Value1).ToValue(),
						CumeDist2     = Sql.Ext.CumeDist<decimal>().Over().OrderBy(p.Value1).ThenByDesc(c.ChildID).ToValue(),
					};
				Assert.IsNotEmpty(q.ToArray());

				var q2 = 
					from p in db.Parent
					join c in db.Child on p.ParentID equals c.ParentID 
					select new
					{
						CumeDist1     = Sql.Ext.CumeDist<decimal>(1, 2).WithinGroup.OrderBy(p.Value1).ThenByDesc(c.ChildID).ToValue(),
						CumeDist2     = Sql.Ext.CumeDist<decimal>(2, 3).WithinGroup.OrderByDesc(p.Value1).ThenBy(c.ChildID).ToValue(),
					};
				Assert.IsNotEmpty(q2.ToArray());
			}
		}

		[Test, IncludeDataContextSource(true, ProviderName.Oracle, ProviderName.OracleManaged, ProviderName.OracleNative)]
		public void TestDenseRankOracle(string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = 
					from p in db.Parent
					join c in db.Child on p.ParentID equals c.ParentID 
					select new
					{
						DenseRank1     = Sql.Ext.DenseRank().Over().PartitionBy(p.Value1, c.ChildID).OrderBy(p.Value1).ToValue(),
						DenseRank2     = Sql.Ext.DenseRank().Over().OrderBy(p.Value1).ThenByDesc(c.ChildID).ToValue(),
					};
				Assert.IsNotEmpty(q.ToArray());

				var q2 = 
					from p in db.Parent
					join c in db.Child on p.ParentID equals c.ParentID 
					select new
					{
						DenseRank1     = Sql.Ext.DenseRank(1, 2).WithinGroup.OrderBy(p.Value1).ThenByDesc(c.ChildID).ToValue(),
					};
				Assert.IsNotEmpty(q2.ToArray());
			}
		}

		[Test, IncludeDataContextSource(true, ProviderName.Oracle, ProviderName.OracleManaged, ProviderName.OracleNative)]
		public void TestFirstValueOracle(string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = 
					from p in db.Parent
					join c in db.Child on p.ParentID equals c.ParentID 
					select new
					{
						FirstValue1     = Sql.Ext.FirstValue(p.Value1, Sql.Nulls.Ignore).Over().ToValue(),
						FirstValue2     = Sql.Ext.FirstValue(p.Value1, Sql.Nulls.None).Over().PartitionBy(p.Value1, c.ChildID).OrderBy(p.Value1).ToValue(),
						FirstValue3     = Sql.Ext.FirstValue(p.Value1, Sql.Nulls.Respect).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						FirstValue4     = Sql.Ext.FirstValue(p.Value1, Sql.Nulls.Respect).Over().OrderBy(p.Value1).ToValue(),
						FirstValue5     = Sql.Ext.FirstValue(p.Value1, Sql.Nulls.Respect).Over().OrderBy(p.Value1).ThenByDesc(c.ChildID).ToValue(),
					};
				Assert.IsNotEmpty(q.ToArray());
			}
		}

		[Test, IncludeDataContextSource(true, ProviderName.Oracle, ProviderName.OracleManaged, ProviderName.OracleNative)]
		public void TestLastValueOracle(string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = 
					from p in db.Parent
					join c in db.Child on p.ParentID equals c.ParentID 
					select new
					{
						LastValue1     = Sql.Ext.LastValue(p.Value1, Sql.Nulls.Ignore).Over().ToValue(),
						LastValue2     = Sql.Ext.LastValue(p.Value1, Sql.Nulls.None).Over().PartitionBy(p.Value1, c.ChildID).OrderBy(p.Value1).ToValue(),
						LastValue3     = Sql.Ext.LastValue(p.Value1, Sql.Nulls.Respect).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						LastValue4     = Sql.Ext.LastValue(p.Value1, Sql.Nulls.Respect).Over().OrderBy(p.Value1).ToValue(),
						LastValue5     = Sql.Ext.LastValue(p.Value1, Sql.Nulls.Respect).Over().OrderBy(p.Value1).ThenByDesc(c.ChildID).ToValue(),
					};
				Assert.IsNotEmpty(q.ToArray());
			}
		}

		[Test, IncludeDataContextSource(true, ProviderName.Oracle, ProviderName.OracleManaged, ProviderName.OracleNative)]
		public void TestLagOracle(string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = 
					from p in db.Parent
					join c in db.Child on p.ParentID equals c.ParentID 
					select new
					{
						Lag1     = Sql.Ext.Lag(p.Value1, Sql.Nulls.Respect, 1, 0).Over().PartitionBy(p.Value1, c.ChildID).OrderBy(p.Value1).ToValue(),
						Lag2     = Sql.Ext.Lag(p.Value1, Sql.Nulls.Ignore, 1, 0).Over().OrderBy(p.Value1).ThenByDesc(c.ChildID).ToValue(),
						Lag3     = Sql.Ext.Lag(p.Value1, Sql.Nulls.None, 1, 0).Over().OrderBy(p.Value1).ThenByDesc(c.ChildID).ToValue(),
						Lag4     = Sql.Ext.Lag(p.Value1, Sql.Nulls.Ignore).Over().PartitionBy(p.Value1, c.ChildID).OrderBy(p.Value1).ToValue(),
					};
				Assert.IsNotEmpty(q.ToArray());
			}
		}

		[Test, IncludeDataContextSource(true, ProviderName.Oracle, ProviderName.OracleManaged, ProviderName.OracleNative)]
		public void TestLeadOracle(string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = 
					from p in db.Parent
					join c in db.Child on p.ParentID equals c.ParentID 
					select new
					{
						Lead1     = Sql.Ext.Lead(p.Value1, Sql.Nulls.Ignore).Over().PartitionBy(p.Value1, c.ChildID).OrderBy(p.Value1).ToValue(),
						Lead2     = Sql.Ext.Lead(p.Value1, Sql.Nulls.Respect).Over().OrderBy(p.Value1).ThenByDesc(c.ChildID).ToValue(),
						Lead3     = Sql.Ext.Lead(p.Value1, Sql.Nulls.None).Over().OrderBy(p.Value1).ThenByDesc(c.ChildID).ToValue(),
						Lead4     = Sql.Ext.Lead(p.Value1, Sql.Nulls.Respect, 1, null).Over().OrderBy(p.Value1).ThenByDesc(c.ChildID).ToValue(),
						Lead5     = Sql.Ext.Lead(p.Value1, Sql.Nulls.Respect, 1, c.ChildID).Over().OrderBy(p.Value1).ThenByDesc(c.ChildID).ToValue(),
					};
				Assert.IsNotEmpty(q.ToArray());
			}
		}

		[Test, IncludeDataContextSource(true, ProviderName.Oracle, ProviderName.OracleManaged, ProviderName.OracleNative)]
		public void TestListAggOracle(string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = 
					from p in db.Parent
					join c in db.Child on p.ParentID equals c.ParentID 
					select new
					{
						ListAgg1  = Sql.Ext.ListAgg(c.ChildID).WithinGroup.OrderBy(p.Value1).ThenBy(p.ParentTest.Value1).ThenByDesc(c.ParentID).ToValue(),
						ListAgg2  = Sql.Ext.ListAgg(c.ChildID).WithinGroup.OrderBy(p.Value1, Sql.NullsPosition.Last).ThenByDesc(c.ParentID, Sql.NullsPosition.First).ToValue(),
						ListAgg3  = Sql.Ext.ListAgg(c.ChildID).WithinGroup.OrderBy(p.Value1, Sql.NullsPosition.First).ThenBy(c.ParentID).ThenBy(c.ParentID, Sql.NullsPosition.First).ToValue(),
						ListAgg4  = Sql.Ext.ListAgg(c.ChildID).WithinGroup.OrderByDesc(p.Value1).ThenBy(p.ParentTest.Value1).ThenByDesc(c.ParentID).ToValue(),
						ListAgg5  = Sql.Ext.ListAgg(c.ChildID).WithinGroup.OrderByDesc(p.Value1, Sql.NullsPosition.None).ThenBy(p.ParentTest.Value1).ThenByDesc(c.ParentID).ToValue(),

						ListAgg6  = Sql.Ext.ListAgg(c.ChildID, "..").WithinGroup.OrderByDesc(p.Value1, Sql.NullsPosition.None).ThenBy(p.ParentTest.Value1).ThenByDesc(c.ParentID).ToValue(),
					};

				var res = q.ToArray();
				Assert.IsNotEmpty(res);
			}
		}

		[Test, IncludeDataContextSource(true, ProviderName.Oracle, ProviderName.OracleManaged, ProviderName.OracleNative)]
		public void TestMaxOracle(string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = 
					from p in db.Parent
					join c in db.Child on p.ParentID equals c.ParentID 
					select new
					{
						Max1   = Sql.Ext.Max(p.Value1).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						Max2   = Sql.Ext.Max(p.Value1, Sql.AggregateModifier.All).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						Max3   = Sql.Ext.Max(p.Value1, Sql.AggregateModifier.Distinct).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						Max4   = Sql.Ext.Max(p.Value1, Sql.AggregateModifier.None).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),

						Max5   = Sql.Ext.Max(p.Value1).Over().ToValue(),
						Max6   = Sql.Ext.Max(p.Value1, Sql.AggregateModifier.All).Over().ToValue(),
						Max7   = Sql.Ext.Max(p.Value1, Sql.AggregateModifier.Distinct).Over().ToValue(),
						Max8   = Sql.Ext.Max(p.Value1, Sql.AggregateModifier.None).Over().ToValue(),
						Max9   = Sql.Ext.Max(p.Value1, Sql.AggregateModifier.None).Over().OrderBy(p.Value1).ToValue(),

						Max10  = Sql.Ext.Max(p.Value1, Sql.AggregateModifier.All).Over().PartitionBy(c.ChildID).OrderBy(p.Value1).Range.Between.UnboundedPreceding.And.CurrentRow.ToValue(),
					};
				var res = q.ToArray();
				Assert.IsNotEmpty(res);

				var q2 = 
					from p in db.Parent
					join c in db.Child on p.ParentID equals c.ParentID 
					select new
					{
						Max11  = Sql.Ext.Max(p.Value1, Sql.AggregateModifier.All).ToValue(),
					};
				var res2 = q2.ToArray();
				Assert.IsNotEmpty(res2);
			}
		}


		[Test, IncludeDataContextSource(true, ProviderName.Oracle, ProviderName.OracleManaged, ProviderName.OracleNative)]
		public void TestMedianOracle(string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = 
					from p in db.Parent
					join c in db.Child on p.ParentID equals c.ParentID 
					select new
					{
						Median1   = Sql.Ext.Median(p.Value1).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						Median2   = Sql.Ext.Median(p.Value1).Over().ToValue(),
					};
				var res = q.ToArray();
				Assert.IsNotEmpty(res);
			}
		}

		[Test, IncludeDataContextSource(true, ProviderName.Oracle, ProviderName.OracleManaged, ProviderName.OracleNative)]
		public void TestMinOracle(string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = 
					from p in db.Parent
					join c in db.Child on p.ParentID equals c.ParentID 
					select new
					{
						Min1   = Sql.Ext.Min(p.Value1).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						Min2   = Sql.Ext.Min(p.Value1, Sql.AggregateModifier.All).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						Min3   = Sql.Ext.Min(p.Value1, Sql.AggregateModifier.Distinct).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						Min4   = Sql.Ext.Min(p.Value1, Sql.AggregateModifier.None).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),

						Min5   = Sql.Ext.Min(p.Value1).Over().ToValue(),
						Min6   = Sql.Ext.Min(p.Value1, Sql.AggregateModifier.All).Over().ToValue(),
						Min7   = Sql.Ext.Min(p.Value1, Sql.AggregateModifier.Distinct).Over().ToValue(),
						Min8   = Sql.Ext.Min(p.Value1, Sql.AggregateModifier.None).Over().ToValue(),
						Min9   = Sql.Ext.Min(p.Value1, Sql.AggregateModifier.None).Over().OrderBy(p.Value1).ToValue(),

						Min10  = Sql.Ext.Min(p.Value1, Sql.AggregateModifier.All).Over().PartitionBy(c.ChildID).OrderBy(p.Value1).Range.Between.UnboundedPreceding.And.CurrentRow.ToValue(),
					};
				var res = q.ToArray();
				Assert.IsNotEmpty(res);
			}
		}

		[Test, IncludeDataContextSource(true, ProviderName.Oracle, ProviderName.OracleManaged, ProviderName.OracleNative)]
		public void TestNthValueOracle(string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = 
					from p in db.Parent
					join c in db.Child on p.ParentID equals c.ParentID 
					select new
					{
						NthValue1   = Sql.Ext.NthValue(c.ChildID, 1).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						NthValue2   = Sql.Ext.NthValue(c.ChildID, p.ParentID).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),

						NthValue21  = Sql.Ext.NthValue(c.ChildID, 1, Sql.From.First, Sql.Nulls.Respect).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						NthValue22  = Sql.Ext.NthValue(c.ChildID, p.ParentID, Sql.From.Last, Sql.Nulls.Ignore).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						NthValue23  = Sql.Ext.NthValue(c.ChildID, p.ParentID, Sql.From.None, Sql.Nulls.None).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),

						NthValue5   = Sql.Ext.NthValue(c.ChildID, 1).Over().ToValue(),
						NthValue9   = Sql.Ext.NthValue(c.ChildID, 1).Over().OrderBy(p.Value1).ToValue(),

						NthValue10  = Sql.Ext.NthValue(c.ChildID, 1).Over().PartitionBy(c.ChildID).OrderBy(p.Value1).Range.Between.UnboundedPreceding.And.CurrentRow.ToValue(),
					};
				var res = q.ToArray();
				Assert.IsNotEmpty(res);
			}
		}

		[Test, IncludeDataContextSource(true, ProviderName.Oracle, ProviderName.OracleManaged, ProviderName.OracleNative)]
		public void TestNTileOracle(string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = 
					from p in db.Parent
					join c in db.Child on p.ParentID equals c.ParentID 
					select new
					{
						NTile1     = Sql.Ext.NTile(p.Value1.Value).Over().PartitionBy(p.Value1, c.ChildID).OrderBy(p.Value1).ToValue(),
						NTile2     = Sql.Ext.NTile(1).Over().OrderBy(p.Value1).ThenByDesc(c.ChildID).ToValue(),

					};
				Assert.IsNotEmpty(q.ToArray());
			}
		}

		[Test, IncludeDataContextSource(true, ProviderName.Oracle, ProviderName.OracleManaged, ProviderName.OracleNative)]
		public void TestPercentileContOracle(string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = 
					from p in db.Parent
					join c in db.Child on p.ParentID equals c.ParentID 
					select new
					{
						PercentileCont1  = Sql.Ext.PercentileCont<double>(0.5).WithinGroup.OrderBy(p.Value1).Over().PartitionBy(p.Value1, p.ParentID).ToValue(),
					};

				var res = q.ToArray();
				Assert.IsNotEmpty(res);

				var q2 = 
					from p in db.Parent
					join c in db.Child on p.ParentID equals c.ParentID 
					select new
					{
						PercentileCont1  = Sql.Ext.PercentileCont<double>(0.5).WithinGroup.OrderByDesc(p.Value1).ToValue(),
						PercentileCont2  = Sql.Ext.PercentileCont<double>(1).WithinGroup.OrderByDesc(p.Value1).ToValue(),
					};

				var res2 = q2.ToArray();
				Assert.IsNotEmpty(res2);
			}
		}

		[Test, IncludeDataContextSource(true, ProviderName.Oracle, ProviderName.OracleManaged, ProviderName.OracleNative)]
		public void TestPercentileDiscOracle(string Discext)
		{
			using (var db = GetDataContext(Discext))
			{
				var q = 
					from p in db.Parent
					join c in db.Child on p.ParentID equals c.ParentID 
					select new
					{
						PercentileDisc1  = Sql.Ext.PercentileDisc<double>(0.5).WithinGroup.OrderBy(p.Value1).Over().PartitionBy(p.Value1, p.ParentID).ToValue(),
					};

				var res = q.ToArray();
				Assert.IsNotEmpty(res);

				var q2 = 
					from p in db.Parent
					join c in db.Child on p.ParentID equals c.ParentID 
					select new
					{
						PercentileDisc1  = Sql.Ext.PercentileDisc<double>(0.5).WithinGroup.OrderByDesc(p.Value1).ToValue(),
						PercentileDisc2  = Sql.Ext.PercentileDisc<double>(1).WithinGroup.OrderByDesc(p.Value1).ToValue(),
					};

				var res2 = q2.ToArray();
				Assert.IsNotEmpty(res2);
			}
		}

		[Test, IncludeDataContextSource(false, ProviderName.Oracle, ProviderName.OracleManaged, ProviderName.OracleNative)]
		public void TestPercentRankOracle(string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = 
					from p in db.Parent
					join c in db.Child on p.ParentID equals c.ParentID 
					select new
					{
						PercentRank1     = Sql.Ext.PercentRank<double>().Over().PartitionBy(p.Value1, c.ChildID).OrderBy(p.Value1).ToValue(),
						PercentRank2     = Sql.Ext.PercentRank<double>().Over().OrderBy(p.Value1).ThenByDesc(c.ChildID).ToValue(),
					};
				Assert.IsNotEmpty(q.ToArray());

				var q2 = 
					from p in db.Parent
					join c in db.Child on p.ParentID equals c.ParentID 
					select new
					{
						PercentRank3     = Sql.Ext.PercentRank<double>(2, 3).WithinGroup.OrderBy(p.Value1).ThenByDesc(c.ChildID).ToValue(),
					};
				Assert.IsNotEmpty(q2.ToArray());
			}
		}

		[Test, IncludeDataContextSource(ProviderName.Oracle, ProviderName.OracleManaged, ProviderName.OracleNative)]
		public void TestPercentRatioToReportOracle(string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = 
					from p in db.Parent
					join c in db.Child on p.ParentID equals c.ParentID 
					select new
					{
						RatioToReport1     = Sql.Ext.RatioToReport<double>(1).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						RatioToReport2     = Sql.Ext.RatioToReport<double>(c.ChildID).Over().ToValue(),
					};
				Assert.IsNotEmpty(q.ToArray());
			}
		}

		[Test, IncludeDataContextSource(true, ProviderName.Oracle, ProviderName.OracleManaged, ProviderName.OracleNative)]
		public void TestRowNumberOracle(string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = 
					from p in db.Parent
					join c in db.Child on p.ParentID equals c.ParentID 
					select new
					{
						RowNumber1     = Sql.Ext.RowNumber().Over().PartitionBy(p.Value1, c.ChildID).OrderBy(p.Value1).ToValue(),
						RowNumber2     = Sql.Ext.RowNumber().Over().OrderBy(p.Value1).ThenByDesc(c.ChildID).ToValue(),
					};
				Assert.IsNotEmpty(q.ToArray());
			}
		}

		[Test, IncludeDataContextSource(true, ProviderName.Oracle, ProviderName.OracleManaged, ProviderName.OracleNative)]
		public void TestRankOracle(string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = 
					from p in db.Parent
					join c in db.Child on p.ParentID equals c.ParentID 
					select new
					{
						Rank1     = Sql.Ext.Rank().Over().PartitionBy(p.Value1, c.ChildID).OrderBy(p.Value1).ToValue(),
						Rank2     = Sql.Ext.Rank().Over().OrderBy(p.Value1).ThenByDesc(c.ChildID).ToValue(),
					};
				Assert.IsNotEmpty(q.ToArray());

				var q2 = 
					from p in db.Parent
					join c in db.Child on p.ParentID equals c.ParentID 
					select new
					{
						Rank1     = Sql.Ext.Rank(1000).WithinGroup.OrderBy(p.Value1).ToValue(),
						Rank2     = Sql.Ext.Rank(0, 0.1).WithinGroup.OrderBy(p.Value1).ThenByDesc(c.ChildID).ToValue(),
					};
				Assert.IsNotEmpty(q2.ToArray());
			}
		}

		[Test, IncludeDataContextSource(ProviderName.Oracle, ProviderName.OracleManaged, ProviderName.OracleNative)]
		public void TestRegrOracle(string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = 
					from p in db.Parent
					join c in db.Child on p.ParentID equals c.ParentID 
					select new
					{
						// type conversion tests
						AvgX      = Sql.Ext.RegrAvgX<decimal>(p.Value1, c.ChildID).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						AvgY      = Sql.Ext.RegrAvgY<decimal>(p.Value1, c.ChildID).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						Count     = Sql.Ext.RegrCount(p.Value1, c.ChildID).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						Intercept = Sql.Ext.RegrIntercept<decimal>(p.Value1, c.ChildID).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						R2        = Sql.Ext.RegrR2<decimal>(p.Value1, c.ChildID).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						SXX       = Sql.Ext.RegrSXX<decimal>(p.Value1, c.ChildID).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						SXY       = Sql.Ext.RegrSXY<decimal>(p.Value1, c.ChildID).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						XYY       = Sql.Ext.RegrSYY<decimal>(p.Value1, c.ChildID).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						Slope     = Sql.Ext.RegrSlope<decimal>(p.Value1, c.ChildID).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
					};
				var res = q.ToArray();
				Assert.IsNotEmpty(res);
			}
		}

		[Test, IncludeDataContextSource(ProviderName.Oracle, ProviderName.OracleManaged, ProviderName.OracleNative)]
		public void TestStdDevOracle(string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = 
					from p in db.Parent
					join c in db.Child on p.ParentID equals c.ParentID 
					select new
					{
						StdDev1    = Sql.Ext.StdDev<double>(p.Value1).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						StdDev2    = Sql.Ext.StdDev<double>(p.Value1, Sql.AggregateModifier.All).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						StdDev3    = Sql.Ext.StdDev<double>(p.Value1, Sql.AggregateModifier.Distinct).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						StdDev4    = Sql.Ext.StdDev<double>(p.Value1, Sql.AggregateModifier.None).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),

						StdDev11   = Sql.Ext.StdDev<double>(p.Value1).Over().ToValue(),
						StdDev12   = Sql.Ext.StdDev<double>(p.Value1, Sql.AggregateModifier.All).Over().ToValue(),
						StdDev13   = Sql.Ext.StdDev<double>(p.Value1, Sql.AggregateModifier.Distinct).Over().ToValue(),
						StdDev14   = Sql.Ext.StdDev<double>(p.Value1, Sql.AggregateModifier.None).Over().ToValue(),

						StdDev21   = Sql.Ext.StdDev<double>(p.Value1, Sql.AggregateModifier.All).Over().PartitionBy(c.ChildID).OrderBy(p.Value1).Range.Between.UnboundedPreceding.And.CurrentRow.ToValue(),
					};

				var res = q.ToArray();
				Assert.IsNotEmpty(res);

				//TODO: support of aggreagate functions
//				var qg =
//					from p in db.Parent
//					join c in db.Child on p.ParentID equals c.ParentID
//					group c by p.ParentID
//					into g
//					select new
//					{
//						StdDev = g.StdDev(a => a.ChildID)
//					};
//
//				var res2 = qg.ToArray();
//				Assert.IsNotEmpty(res2);
			}
		}

		[Test, IncludeDataContextSource(ProviderName.Oracle, ProviderName.OracleManaged, ProviderName.OracleNative)]
		public void TestStdDevPopOracle(string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = 
					from p in db.Parent
					join c in db.Child on p.ParentID equals c.ParentID 
					select new
					{
						StdDevPop1   = Sql.Ext.StdDevPop<double>(p.Value1).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						StdDevPop2   = Sql.Ext.StdDevPop<double>(p.Value1).Over().OrderBy(p.Value1).ToValue(),
						StdDevPop3   = Sql.Ext.StdDevPop<double>(p.Value1).Over().ToValue(),
						StdDevPop4   = Sql.Ext.StdDevPop<double>(p.Value1).Over().PartitionBy(c.ChildID).OrderBy(p.Value1).Range.Between.UnboundedPreceding.And.CurrentRow.ToValue(),
					};
				var res = q.ToArray();
				Assert.IsNotEmpty(res);
			}
		}

		[Test, IncludeDataContextSource(ProviderName.Oracle, ProviderName.OracleManaged, ProviderName.OracleNative)]
		public void TestStdDevSampOracle(string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = 
					from p in db.Parent
					join c in db.Child on p.ParentID equals c.ParentID 
					select new
					{
						StdDevSamp1   = Sql.Ext.StdDevSamp<double>(p.Value1).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						StdDevSamp2   = Sql.Ext.StdDevSamp<double>(p.Value1).Over().OrderBy(p.Value1).ToValue(),
						StdDevSamp3   = Sql.Ext.StdDevSamp<double>(p.Value1).Over().ToValue(),
						StdDevSamp4   = Sql.Ext.StdDevSamp<double>(p.Value1).Over().PartitionBy(c.ChildID).OrderBy(p.Value1).Range.Between.UnboundedPreceding.And.CurrentRow.ToValue(),
					};
				var res = q.ToArray();
				Assert.IsNotEmpty(res);
			}
		}

		[Test, IncludeDataContextSource(true, ProviderName.Oracle, ProviderName.OracleManaged, ProviderName.OracleNative)]
		public void TestSumOracle(string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = 
					from p in db.Parent
					join c in db.Child on p.ParentID equals c.ParentID 
					select new
					{
						Sum1   = Sql.Ext.Sum(p.Value1).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						Sum2   = Sql.Ext.Sum(p.Value1, Sql.AggregateModifier.All).Over().OrderBy(p.Value1).ToValue(),
						Sum3   = Sql.Ext.Sum(p.Value1, Sql.AggregateModifier.Distinct).Over().ToValue(),
						Sum4   = Sql.Ext.Sum(p.Value1, Sql.AggregateModifier.None).Over().PartitionBy(c.ChildID).OrderBy(p.Value1).Range.Between.UnboundedPreceding.And.CurrentRow.ToValue(),
					};
				var res = q.ToArray();
				Assert.IsNotEmpty(res);

				var q2 = 
					from p in db.Parent
					join c in db.Child on p.ParentID equals c.ParentID 
					select new
					{
						Sum1   = Sql.Ext.Sum(p.Value1).ToValue(),
					};
				var res2 = q2.ToArray();
				Assert.IsNotEmpty(res2);
			}
		}

		[Test, IncludeDataContextSource(ProviderName.Oracle, ProviderName.OracleManaged, ProviderName.OracleNative)]
		public void TestVarPopOracle(string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = 
					from p in db.Parent
					join c in db.Child on p.ParentID equals c.ParentID 
					select new
					{
						VarPop1   = Sql.Ext.VarPop<double>(p.Value1).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						VarPop2   = Sql.Ext.VarPop<double>(p.Value1).Over().OrderBy(p.Value1).ToValue(),
						VarPop3   = Sql.Ext.VarPop<double>(p.Value1).Over().ToValue(),
						VarPop4   = Sql.Ext.VarPop<double>(p.Value1).Over().PartitionBy(c.ChildID).OrderBy(p.Value1).Range.Between.UnboundedPreceding.And.CurrentRow.ToValue(),
					};
				var res = q.ToArray();
				Assert.IsNotEmpty(res);
			}
		}

		[Test, IncludeDataContextSource(ProviderName.Oracle, ProviderName.OracleManaged, ProviderName.OracleNative)]
		public void TestVarSampOracle(string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = 
					from p in db.Parent
					join c in db.Child on p.ParentID equals c.ParentID 
					select new
					{
						VarSamp1   = Sql.Ext.VarSamp<double>(p.Value1).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						VarSamp2   = Sql.Ext.VarSamp<double>(p.Value1).Over().OrderBy(p.Value1).ToValue(),
						VarSamp3   = Sql.Ext.VarSamp<double>(p.Value1).Over().ToValue(),
						VarSamp4   = Sql.Ext.VarSamp<double>(p.Value1).Over().PartitionBy(c.ChildID).OrderBy(p.Value1).Range.Between.UnboundedPreceding.And.CurrentRow.ToValue(),
					};
				var res = q.ToArray();
				Assert.IsNotEmpty(res);
			}
		}


		[Test, IncludeDataContextSource(ProviderName.Oracle, ProviderName.OracleManaged, ProviderName.OracleNative)]
		public void TestVarianceOracle(string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = 
					from p in db.Parent
					join c in db.Child on p.ParentID equals c.ParentID 
					select new
					{
						Variance1    = Sql.Ext.Variance<double>(p.Value1).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						Variance2    = Sql.Ext.Variance<double>(p.Value1, Sql.AggregateModifier.All).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						Variance3    = Sql.Ext.Variance<double>(p.Value1, Sql.AggregateModifier.Distinct).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						Variance4    = Sql.Ext.Variance<double>(p.Value1, Sql.AggregateModifier.None).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),

						Variance11   = Sql.Ext.Variance<double>(p.Value1).Over().ToValue(),
						Variance12   = Sql.Ext.Variance<double>(p.Value1, Sql.AggregateModifier.All).Over().ToValue(),
						Variance13   = Sql.Ext.Variance<double>(p.Value1, Sql.AggregateModifier.Distinct).Over().ToValue(),
						Variance14   = Sql.Ext.Variance<double>(p.Value1, Sql.AggregateModifier.None).Over().ToValue(),

						Variance21   = Sql.Ext.Variance<double>(p.Value1, Sql.AggregateModifier.All).Over().PartitionBy(c.ChildID).OrderBy(p.Value1).Range.Between.UnboundedPreceding.And.CurrentRow.ToValue(),
					};
				var res = q.ToArray();
				Assert.IsNotEmpty(res);
			}
		}

		[Test, IncludeDataContextSource(ProviderName.Oracle, ProviderName.OracleManaged, ProviderName.OracleNative)]
		public void TestKeepFirstOracle(string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = 
					from p in db.Parent
					join c in db.Child on p.ParentID equals c.ParentID 
					select new
					{
						Min         = Sql.Ext.Min(p.Value1).KeepFirst().OrderBy(p.Value1).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						Max         = Sql.Ext.Max(p.Value1).KeepFirst().OrderBy(p.Value1).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						Sum         = Sql.Ext.Sum(p.Value1).KeepFirst().OrderBy(p.Value1).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						Variance    = Sql.Ext.Variance<double>(p.Value1).KeepFirst().OrderBy(p.Value1).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
					};
				var res = q.ToArray();
				Assert.IsNotEmpty(res);
			}
		}

		[Test, IncludeDataContextSource(ProviderName.Oracle, ProviderName.OracleManaged, ProviderName.OracleNative)]
		public void TestKeepLastOracle(string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = 
					from p in db.Parent
					join c in db.Child on p.ParentID equals c.ParentID 
					select new
					{
						Min         = Sql.Ext.Min(p.Value1).KeepLast().OrderBy(p.Value1).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						Max         = Sql.Ext.Max(p.Value1).KeepLast().OrderBy(p.Value1).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						Sum         = Sql.Ext.Sum(p.Value1).KeepLast().OrderBy(p.Value1).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						Variance    = Sql.Ext.Variance<double>(p.Value1).KeepLast().OrderBy(p.Value1).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
					};
				var res = q.ToArray();
				Assert.IsNotEmpty(res);
			}
		}

	}
}