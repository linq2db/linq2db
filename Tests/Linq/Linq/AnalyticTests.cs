using System;
using System.Diagnostics;

namespace Tests.Linq
{
	using System;
	using System.Linq;

	using LinqToDB;
	using LinqToDB.Mapping;
	using NUnit.Framework;

	[TestFixture]
	public class AnalyticTests : TestBase
	{
		[Test]
		public void Test(
			[IncludeDataSources(
				true,
				TestProvName.AllOracle,
				TestProvName.AllSqlServer2012Plus,
				TestProvName.AllPostgreSQL)]
			string context)
		{
			using (var db = GetDataContext(context))
			{
				var q =
					from p in db.Parent
					join c in db.Child on p.ParentID equals c.ParentID
					select new
					{
						Rank1       = Sql.Ext.Rank().Over().PartitionBy(p.Value1, c.ChildID).OrderBy(p.Value1).ThenBy(c.ChildID).ThenBy(c.ParentID).ToValue(),
						RowNumber   = Sql.Ext.RowNumber().Over().PartitionBy(p.Value1, c.ChildID).OrderByDesc(p.Value1).ThenBy(c.ChildID).ThenByDesc(c.ParentID).ToValue(),
						DenseRank   = Sql.Ext.DenseRank().Over().PartitionBy(p.Value1, c.ChildID).OrderBy(p.Value1).ToValue(),
						Sum         = Sql.Ext.Sum(p.Value1).Over().PartitionBy(p.Value1, c.ChildID).OrderBy(p.Value1).ToValue(),
						Avg         = Sql.Ext.Average<double>(p.Value1).Over().PartitionBy(p.Value1, c.ChildID).OrderBy(p.Value1).ToValue(),

						Count1      = Sql.Ext.Count(p.ParentID, Sql.AggregateModifier.All).Over().PartitionBy(p.Value1).OrderBy(p.Value1).Range.Between.UnboundedPreceding.And.CurrentRow.ToValue(),
						Count2      = Sql.Ext.Count(p.ParentID, Sql.AggregateModifier.All).Over().PartitionBy(p.Value1).OrderBy(p.Value1).ThenBy(c.ChildID).Range.Between.UnboundedPreceding.And.CurrentRow.ToValue(),
						Count4      = Sql.Ext.Count(p.ParentID, Sql.AggregateModifier.All).Over().PartitionBy(p.Value1).OrderBy(p.Value1).ThenByDesc(c.ChildID).Range.Between.UnboundedPreceding.And.CurrentRow.ToValue(),
						Count6      = Sql.Ext.Count(p.ParentID, Sql.AggregateModifier.All).Over().PartitionBy(p.Value1).OrderBy(p.Value1).Rows.Between.UnboundedPreceding.And.ValuePreceding(3).ToValue(),
						Count7      = Sql.Ext.Count(p.ParentID, Sql.AggregateModifier.All).Over().PartitionBy(p.Value1).OrderBy(p.Value1).Range.Between.CurrentRow.And.UnboundedFollowing.ToValue(),
						Count8      = Sql.Ext.Count(p.ParentID, Sql.AggregateModifier.All).Over().PartitionBy(p.Value1).OrderBy(p.Value1, Sql.NullsPosition.None).Rows.Between.UnboundedPreceding.And.CurrentRow.ToValue(),
						Count9      = Sql.Ext.Count(p.ParentID, Sql.AggregateModifier.All).Over().PartitionBy(p.Value1).OrderBy(p.Value1).Range.UnboundedPreceding.ToValue(),
						Count10     = Sql.Ext.Count(p.ParentID, Sql.AggregateModifier.All).Over().PartitionBy(p.Value1).OrderBy(p.Value1).Range.CurrentRow.ToValue(),
						Count11     = Sql.Ext.Count(p.ParentID, Sql.AggregateModifier.All).Over().PartitionBy(p.Value1).OrderBy(p.Value1).Rows.ValuePreceding(1).ToValue(),
						Count12     = Sql.Ext.Count(p.ParentID, Sql.AggregateModifier.All).Over().PartitionBy(p.Value1).OrderByDesc(p.Value1).Range.Between.UnboundedPreceding.And.CurrentRow.ToValue(),
						Count14     = Sql.Ext.Count().Over().ToValue(),

						Combination = Sql.Ext.Rank().Over().PartitionBy(p.Value1, c.ChildID).OrderBy(p.Value1).ThenBy(c.ChildID).ToValue() +
									  Sql.Sqrt(Sql.Ext.DenseRank().Over().PartitionBy(p.Value1, c.ChildID).OrderBy(p.Value1).ToValue()) +
									  Sql.Ext.Count(p.ParentID, Sql.AggregateModifier.All).Over().PartitionBy(p.Value1).OrderBy(p.Value1).Range.Between.UnboundedPreceding.And.CurrentRow.ToValue() +
									  Sql.Ext.Count().Over().ToValue(),
					};
				var res = q.ToArray();
				Assert.IsNotEmpty(res);
			}
		}

		[Test]
		public void TestSubqueryOptimization(
			[IncludeDataSources(
				true,
				TestProvName.AllOracle,
				TestProvName.AllSqlServer2012Plus,
				TestProvName.AllPostgreSQL)]
			string context)
		{
			using (var db = GetDataContext(context))
			{
				var subq =
					from p in db.Parent
					join c in db.Child on p.ParentID equals c.ParentID
					select new
					{
						Rank      = Sql.Ext.Rank().Over().PartitionBy(p.Value1, c.ChildID).OrderBy(p.Value1).ThenBy(c.ChildID).ThenBy(c.ParentID).ToValue(),
						RowNumber = Sql.Ext.RowNumber().Over().PartitionBy(p.Value1, c.ChildID).OrderByDesc(p.Value1).ThenBy(c.ChildID).ThenByDesc(c.ParentID).ToValue(),
						DenseRank = Sql.Ext.DenseRank().Over().PartitionBy(p.Value1, c.ChildID).OrderBy(p.Value1).ToValue(),
					};

				var q = from sq in subq
					where sq.Rank > 0
					select sq;

				var res = q.ToList();
				Assert.IsNotEmpty(res);
			}
		}

		[Test]
		public void TestExtensionsOracle([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q =
					from p in db.Parent
					join c in db.Child on p.ParentID equals c.ParentID
					select new
					{
						Rank1       = Sql.Ext.Rank().Over().PartitionBy(p.Value1, c.ChildID).OrderBy(p.Value1).ThenBy(c.ChildID).ThenBy(c.ParentID, Sql.NullsPosition.First).ToValue(),
						Rank2       = Sql.Ext.Rank().Over().PartitionBy(p.Value1, c.ChildID).OrderBy(p.Value1, Sql.NullsPosition.Last).ThenBy(c.ChildID).ThenBy(c.ParentID, Sql.NullsPosition.First).ToValue(),
						RowNumber   = Sql.Ext.RowNumber().Over().PartitionBy(p.Value1, c.ChildID).OrderByDesc(p.Value1, Sql.NullsPosition.First).ThenBy(c.ChildID).ThenByDesc(c.ParentID, Sql.NullsPosition.First).ToValue(),
						DenseRank   = Sql.Ext.DenseRank().Over().PartitionBy(p.Value1, c.ChildID).OrderBy(p.Value1).ToValue(),

						Count1      = Sql.Ext.Count(p.ParentID, Sql.AggregateModifier.All).Over().PartitionBy(p.Value1).OrderBy(p.Value1).Range.Between.UnboundedPreceding.And.CurrentRow.ToValue(),
						Count2      = Sql.Ext.Count(p.ParentID, Sql.AggregateModifier.All).Over().PartitionBy(p.Value1).OrderBy(p.Value1).ThenBy(c.ChildID).Range.Between.UnboundedPreceding.And.CurrentRow.ToValue(),
						Count3      = Sql.Ext.Count(p.ParentID, Sql.AggregateModifier.All).Over().PartitionBy(p.Value1).OrderBy(p.Value1).ThenBy(c.ChildID, Sql.NullsPosition.First).Range.Between.UnboundedPreceding.And.CurrentRow.ToValue(),
						Count4      = Sql.Ext.Count(p.ParentID, Sql.AggregateModifier.All).Over().PartitionBy(p.Value1).OrderBy(p.Value1).ThenByDesc(c.ChildID).Range.Between.UnboundedPreceding.And.CurrentRow.ToValue(),
						Count5      = Sql.Ext.Count(p.ParentID, Sql.AggregateModifier.All).Over().PartitionBy(p.Value1).OrderBy(p.Value1).ThenByDesc(c.ChildID, Sql.NullsPosition.Last).Range.Between.UnboundedPreceding.And.CurrentRow.ToValue(),
						Count6      = Sql.Ext.Count(p.ParentID, Sql.AggregateModifier.All).Over().PartitionBy(p.Value1).OrderBy(p.Value1).Range.Between.UnboundedPreceding.And.ValuePreceding(3).ToValue(),
						Count7      = Sql.Ext.Count(p.ParentID, Sql.AggregateModifier.All).Over().PartitionBy(p.Value1).OrderBy(p.Value1).Range.Between.CurrentRow.And.UnboundedFollowing.ToValue(),
						Count8      = Sql.Ext.Count(p.ParentID, Sql.AggregateModifier.All).Over().PartitionBy(p.Value1).OrderBy(p.Value1, Sql.NullsPosition.None).Rows.Between.UnboundedPreceding.And.CurrentRow.ToValue(),
						Count9      = Sql.Ext.Count(p.ParentID, Sql.AggregateModifier.All).Over().PartitionBy(p.Value1).OrderBy(p.Value1, Sql.NullsPosition.First).Range.UnboundedPreceding.ToValue(),
						Count10     = Sql.Ext.Count(p.ParentID, Sql.AggregateModifier.All).Over().PartitionBy(p.Value1).OrderBy(p.Value1, Sql.NullsPosition.First).Range.CurrentRow.ToValue(),
						Count11     = Sql.Ext.Count(p.ParentID, Sql.AggregateModifier.All).Over().PartitionBy(p.Value1).OrderBy(p.Value1, Sql.NullsPosition.First).Range.ValuePreceding(1).ToValue(),
						Count12     = Sql.Ext.Count(p.ParentID, Sql.AggregateModifier.All).Over().PartitionBy(p.Value1).OrderByDesc(p.Value1).Range.Between.UnboundedPreceding.And.CurrentRow.ToValue(),
						Count13     = Sql.Ext.Count(p.ParentID, Sql.AggregateModifier.All).Over().PartitionBy(p.Value1).OrderByDesc(p.Value1, Sql.NullsPosition.First).Range.Between.UnboundedPreceding.And.CurrentRow.ToValue(),
						Count14     = Sql.Ext.Count().Over().ToValue(),

						Combination = Sql.Ext.Rank().Over().PartitionBy(p.Value1, c.ChildID).OrderBy(p.Value1).ThenBy(c.ChildID).ToValue() +
									  Sql.Sqrt(Sql.Ext.DenseRank().Over().PartitionBy(p.Value1, c.ChildID).OrderBy(p.Value1).ToValue()) +
									  Sql.Ext.Count(p.ParentID, Sql.AggregateModifier.All).Over().PartitionBy(p.Value1).OrderBy(p.Value1).Range.Between.UnboundedPreceding.And.CurrentRow.ToValue() +
									  Sql.Ext.Count().Over().ToValue(),
					};
				var res = q.ToArray();
				Assert.IsNotEmpty(res);
			}
		}

		[ActiveIssue(
			Configuration         = TestProvName.AllOracle,
			Details               = "Specified cast is not valid.",
			SkipForNonLinqService = true)]
		[Test]
		public void TestAvg([IncludeDataSources(true, TestProvName.AllSqlServer, TestProvName.AllOracle)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var qg =
					from p in db.Parent
					join c in db.Child on p.ParentID equals c.ParentID
					group c by p.ParentID
					into g
					select new
					{
						Average         = g.Average(a => a.ChildID),
						AverageNone     = g.Average(a => a.ChildID, Sql.AggregateModifier.None),
						AverageAll      = g.Average(a => a.ChildID, Sql.AggregateModifier.All),
						AverageDistinct = g.Average(a => a.ChildID, Sql.AggregateModifier.Distinct)
					};

				var res = qg.ToArray();
				Assert.IsNotEmpty(res);

				db.Child.Average(c => c.ParentID);
				db.Child.Average(c => c.ParentID, Sql.AggregateModifier.All);
				db.Child.Average(c => c.ParentID, Sql.AggregateModifier.Distinct);
			}
		}

		[Test]
		public void TestAvgOracle([IncludeDataSources(TestProvName.AllOracle)]
			string context)
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

						AvgD1      = Sql.Ext.Average<decimal>(p.Value1, Sql.AggregateModifier.All).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						AvgD2      = Sql.Ext.Average<decimal>(p.Value1, Sql.AggregateModifier.Distinct).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						AvgD3      = Sql.Ext.Average<decimal>(p.Value1, Sql.AggregateModifier.None).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						AvgD4      = Sql.Ext.Average<decimal>(p.Value1, Sql.AggregateModifier.All).Over().ToValue(),

						AvgDN1     = Sql.Ext.Average<decimal?>(p.Value1, Sql.AggregateModifier.All).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						AvgDN2     = Sql.Ext.Average<decimal?>(p.Value1, Sql.AggregateModifier.Distinct).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						AvgDN3     = Sql.Ext.Average<decimal?>(p.Value1, Sql.AggregateModifier.None).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						AvgDN4     = Sql.Ext.Average<decimal?>(p.Value1, Sql.AggregateModifier.None).Over().ToValue(),

						AvgIN1     = Sql.Ext.Average<int?>(p.Value1, Sql.AggregateModifier.All).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						AvgIN2     = Sql.Ext.Average<int?>(p.Value1, Sql.AggregateModifier.Distinct).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						AvgIN3     = Sql.Ext.Average<int?>(p.Value1, Sql.AggregateModifier.None).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						AvgIN4     = Sql.Ext.Average<int?>(p.Value1, Sql.AggregateModifier.None).Over().ToValue(),

						AvgI1      = Sql.Ext.Average<int>(p.Value1, Sql.AggregateModifier.All).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						AvgI2      = Sql.Ext.Average<int>(p.Value1, Sql.AggregateModifier.Distinct).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						AvgI3      = Sql.Ext.Average<int>(p.Value1, Sql.AggregateModifier.None).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						AvgI4      = Sql.Ext.Average<int>(p.Value1, Sql.AggregateModifier.None).Over().ToValue(),

						AvgL1      = Sql.Ext.Average<long>(p.Value1, Sql.AggregateModifier.All).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						AvgL2      = Sql.Ext.Average<long>(p.Value1, Sql.AggregateModifier.Distinct).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						AvgL3      = Sql.Ext.Average<long>(p.Value1, Sql.AggregateModifier.None).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						AvgL4      = Sql.Ext.Average<long>(p.Value1, Sql.AggregateModifier.None).Over().ToValue(),

						AvgLN1     = Sql.Ext.Average<long?>(p.Value1, Sql.AggregateModifier.All).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						AvgLN2     = Sql.Ext.Average<long?>(p.Value1, Sql.AggregateModifier.Distinct).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						AvgLN3     = Sql.Ext.Average<long?>(p.Value1, Sql.AggregateModifier.None).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						AvgLN4     = Sql.Ext.Average<long?>(p.Value1, Sql.AggregateModifier.None).Over().ToValue(),

						AvgF1      = Sql.Ext.Average<float>(p.Value1, Sql.AggregateModifier.All).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						AvgF2      = Sql.Ext.Average<float>(p.Value1, Sql.AggregateModifier.Distinct).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						AvgF3      = Sql.Ext.Average<float>(p.Value1, Sql.AggregateModifier.None).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						AvgF4      = Sql.Ext.Average<float>(p.Value1, Sql.AggregateModifier.None).Over().ToValue(),

						AvgFN1     = Sql.Ext.Average<float?>(p.Value1, Sql.AggregateModifier.All).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						AvgFN2     = Sql.Ext.Average<float?>(p.Value1, Sql.AggregateModifier.Distinct).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						AvgFN3     = Sql.Ext.Average<float?>(p.Value1, Sql.AggregateModifier.None).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						AvgFN4     = Sql.Ext.Average<float?>(p.Value1, Sql.AggregateModifier.None).Over().ToValue(),

						AvgDO1     = Sql.Ext.Average<double>(p.Value1, Sql.AggregateModifier.All).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						AvgDO2     = Sql.Ext.Average<double>(p.Value1, Sql.AggregateModifier.Distinct).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						AvgDO3     = Sql.Ext.Average<double>(p.Value1, Sql.AggregateModifier.None).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						AvgDO4     = Sql.Ext.Average<double>(p.Value1, Sql.AggregateModifier.None).Over().ToValue(),

						AvgDON1    = Sql.Ext.Average<double?>(p.Value1, Sql.AggregateModifier.All).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						AvgDON2    = Sql.Ext.Average<double?>(p.Value1, Sql.AggregateModifier.Distinct).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						AvgDON3    = Sql.Ext.Average<double?>(p.Value1, Sql.AggregateModifier.None).Over().PartitionBy(p.Value1, c.ChildID).ToValue(),
						AvgDON4    = Sql.Ext.Average<double?>(p.Value1, Sql.AggregateModifier.None).Over().ToValue(),

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

		[Test]
		public void TestCorrOracle([IncludeDataSources(TestProvName.AllOracle)]
			string context)
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

				var qg =
					from p in db.Parent
					join c in db.Child on p.ParentID equals c.ParentID
					group c by p.ParentID
					into g
					select new
					{
						Corr = g.Corr(c => c.ParentID, c => c.ChildID),
					};

				var resg = qg.ToArray();
				Assert.IsNotEmpty(resg);

				db.Child.Corr(c => c.ParentID, c => c.ChildID);
			}
		}

		[Test]
		public void TestCountOracle([IncludeDataSources(true, TestProvName.AllOracle)] string context)
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

		[Test]
		public void TestCount([IncludeDataSources(true, TestProvName.AllSqlServer, TestProvName.AllOracle)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var qg =
					from p in db.Parent
					join c in db.Child on p.ParentID equals c.ParentID
					group c by p.ParentID
					into g
					select new
					{
						Count         = g.Count(),
						CountMember   = g.CountExt(a => a.ChildID),
						CountNone     = g.CountExt(a => a.ChildID, Sql.AggregateModifier.None),
						CountAll      = g.CountExt(a => a.ChildID, Sql.AggregateModifier.All),
						CountDistinct = g.CountExt(a => a.ChildID, Sql.AggregateModifier.Distinct)
					};

				var res = qg.ToArray();
				Assert.IsNotEmpty(res);

				db.Child.Count();
				db.Child.CountExt(c => c.ParentID);
				db.Child.CountExt(c => c.ParentID, Sql.AggregateModifier.All);
				db.Child.CountExt(c => c.ParentID, Sql.AggregateModifier.Distinct);
			}
		}

		[Test]
		public void TestCovarPopOracle([IncludeDataSources(TestProvName.AllOracle)]
			string context)
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

						CovarPop4    = Sql.Ext.CovarPop(p.Value1, c.ChildID).Over().PartitionBy(c.ChildID).OrderBy(p.Value1).Range.Between.UnboundedPreceding.And.CurrentRow.ToValue(),
					};
				var res = q.ToArray();
				Assert.IsNotEmpty(res);

				var qg =
					from p in db.Parent
					join c in db.Child on p.ParentID equals c.ParentID
					group c by p.ParentID
					into g
					select new
					{
						CovarPop = g.CovarPop(c => c.ParentID, c => c.ChildID),
					};

				var resg = qg.ToArray();
				Assert.IsNotEmpty(resg);

				db.Child.CovarPop(c => c.ParentID, c => c.ChildID);
			}
		}

		[Test]
		public void TestCovarSampOracle([IncludeDataSources(TestProvName.AllOracle)]
			string context)
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

				var qg =
					from p in db.Parent
					join c in db.Child on p.ParentID equals c.ParentID
					group c by p.ParentID
					into g
					select new
					{
						CovarSamp = g.CovarSamp(c => c.ParentID, c => c.ChildID),
					};

				var resg = qg.ToArray();
				Assert.IsNotEmpty(resg);

				db.Child.CovarSamp(c => c.ParentID, c => c.ChildID);
			}
		}

		[Test]
		public void TestCumeDistOracle([IncludeDataSources(TestProvName.AllOracle)]
			string context)
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

		[Test]
		public void TestDenseRankOracle([IncludeDataSources(true, TestProvName.AllOracle)] string context)
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

		[Test]
		public void TestFirstValueOracle([IncludeDataSources(true, TestProvName.AllOracle)] string context)
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

		[Test]
		public void TestLastValueOracle([IncludeDataSources(true, TestProvName.AllOracle)] string context)
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

		[Test]
		public void TestLagOracle([IncludeDataSources(true, TestProvName.AllOracle)] string context)
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

		[Test]
		public void TestLeadOracle([IncludeDataSources(true, TestProvName.AllOracle)] string context)
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

		[Test]
		public void TestListAggOracle([IncludeDataSources(true, TestProvName.AllOracle)] string context)
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

		[Test]
		public void TestMaxOracle([IncludeDataSources(true, TestProvName.AllOracle)] string context)
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

		[Test]
		public void TestMax([IncludeDataSources(true, TestProvName.AllOracle, TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var qg =
					from p in db.Parent
					join c in db.Child on p.ParentID equals c.ParentID
					group c by p.ParentID
					into g
					select new
					{
						Max         = g.Max(a => a.ChildID),
						MaxNone     = g.Max(a => a.ChildID, Sql.AggregateModifier.None),
						MaxAll      = g.Max(a => a.ChildID, Sql.AggregateModifier.All),
						MaxDistinct = g.Max(a => a.ChildID, Sql.AggregateModifier.Distinct)
					};

				var res = qg.ToArray();
				Assert.IsNotEmpty(res);

				db.Child.Max(c => c.ParentID);
				db.Child.Max(c => c.ParentID, Sql.AggregateModifier.All);
				db.Child.Max(c => c.ParentID, Sql.AggregateModifier.Distinct);
			}
		}

		[Test]
		public void TestMedianOracle([IncludeDataSources(true, TestProvName.AllOracle)] string context)
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

				var qg =
					from p in db.Parent
					join c in db.Child on p.ParentID equals c.ParentID
					group c by p.ParentID
					into g
					select new
					{
						Median = g.Median(c => c.ParentID),
					};

				var resg = qg.ToArray();
				Assert.IsNotEmpty(resg);

				db.Child.Median(c => c.ParentID);
			}
		}

		[Test]
		public void TestMinOracle([IncludeDataSources(true, TestProvName.AllOracle)] string context)
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

		[Test]
		public void TestMin([IncludeDataSources(true, TestProvName.AllOracle, TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var qg =
					from p in db.Parent
					join c in db.Child on p.ParentID equals c.ParentID
					group c by p.ParentID
					into g
					select new
					{
						Min         = g.Min(a => a.ChildID),
						MinNone     = g.Min(a => a.ChildID, Sql.AggregateModifier.None),
						MinAll      = g.Min(a => a.ChildID, Sql.AggregateModifier.All),
						MinDistinct = g.Min(a => a.ChildID, Sql.AggregateModifier.Distinct)
					};

				var res = qg.ToArray();
				Assert.IsNotEmpty(res);

				db.Child.Min(c => c.ParentID);
				db.Child.Min(c => c.ParentID, Sql.AggregateModifier.All);
				db.Child.Min(c => c.ParentID, Sql.AggregateModifier.Distinct);
			}
		}

		[Test]
		public void TestNthValueOracle([IncludeDataSources(true, TestProvName.AllOracle)] string context)
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

		[Test]
		public void TestNTileOracle([IncludeDataSources(true, TestProvName.AllOracle)]
			string context)
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

		[Test]
		public void TestPercentileContOracle([IncludeDataSources(true, TestProvName.AllOracle)] string context)
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

		[Test]
		public void TestPercentileDiscOracle([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using (var db = GetDataContext(context))
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

		[Test]
		public void TestPercentRankOracle([IncludeDataSources(TestProvName.AllOracle)]
			string context)
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

		[Test]
		public void TestPercentRatioToReportOracle([IncludeDataSources(TestProvName.AllOracle)]
			string context)
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

		[Test]
		public void TestRowNumberOracle([IncludeDataSources(true, TestProvName.AllOracle)]
			string context)
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

		[Test]
		public void TestRankOracle([IncludeDataSources(true,
			ProviderName.Oracle, ProviderName.OracleManaged, ProviderName.OracleNative)]
			string context)
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

		[Test]
		public void TestRegrOracle([IncludeDataSources(
			true, ProviderName.Oracle, ProviderName.OracleManaged, ProviderName.OracleNative)]
			string context)
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

		[Test]
		public void TestStdDevOracle([IncludeDataSources(TestProvName.AllOracle)]
			string context)
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
			}
		}

		[Test]
		public void TestStdDev([IncludeDataSources(TestProvName.AllSqlServer, TestProvName.AllOracle)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var qg =
					from p in db.Parent
					join c in db.Child on p.ParentID equals c.ParentID
					group c by p.ParentID
					into g
					select new
					{
						StdDev         = g.StdDev(a => a.ChildID),
						StdDevNone     = g.StdDev(a => a.ChildID, Sql.AggregateModifier.None),
						StdDevAll      = g.StdDev(a => a.ChildID, Sql.AggregateModifier.All),
						StdDevDistinct = g.StdDev(a => a.ChildID, Sql.AggregateModifier.Distinct)
					};

				var res = qg.ToArray();
				Assert.IsNotEmpty(res);

				db.Child.StdDev(c => c.ParentID);
				db.Child.StdDev(c => c.ParentID, Sql.AggregateModifier.All);
				db.Child.StdDev(c => c.ParentID, Sql.AggregateModifier.Distinct);
			}
		}

		[Test]
		public void TestStdDevPopOracle([IncludeDataSources(TestProvName.AllOracle)]
			string context)
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

				var qg =
					from p in db.Parent
					join c in db.Child on p.ParentID equals c.ParentID
					group c by p.ParentID
					into g
					select new
					{
						StdDevPop = g.StdDevPop(c => c.ParentID),
					};

				var resg = qg.ToArray();
				Assert.IsNotEmpty(resg);

				db.Child.StdDevPop(c => c.ParentID);
			}
		}

		[Test]
		public void TestStdDevSampOracle([IncludeDataSources(TestProvName.AllOracle)]
			string context)
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

				var qg =
					from p in db.Parent
					join c in db.Child on p.ParentID equals c.ParentID
					group c by p.ParentID
					into g
					select new
					{
						StdDevSamp = g.StdDevSamp(c => c.ParentID),
					};

				var resg = qg.ToArray();
				Assert.IsNotEmpty(resg);

				db.Child.StdDevSamp(c => c.ParentID);
			}
		}

		[Test]
		public void TestSumOracle([IncludeDataSources(true,
			ProviderName.Oracle, ProviderName.OracleManaged, ProviderName.OracleNative)]
			string context)
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

		[Test]
		public void TestVarPopOracle([IncludeDataSources(TestProvName.AllOracle)]
			string context)
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

				var qg =
					from p in db.Parent
					join c in db.Child on p.ParentID equals c.ParentID
					group c by p.ParentID
					into g
					select new
					{
						VarPop = g.VarPop(c => c.ParentID),
					};

				var resg = qg.ToArray();
				Assert.IsNotEmpty(resg);

				db.Child.VarPop(c => c.ParentID);
			}
		}

		[Test]
		public void TestVarSampOracle([IncludeDataSources(TestProvName.AllOracle)]
			string context)
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

				var qg =
					from p in db.Parent
					join c in db.Child on p.ParentID equals c.ParentID
					group c by p.ParentID
					into g
					select new
					{
						VarSamp = g.VarSamp(c => c.ParentID),
					};

				var resg = qg.ToArray();
				Assert.IsNotEmpty(resg);

				db.Child.VarSamp(c => c.ParentID);
			}
		}


		[Test]
		public void TestVarianceOracle([IncludeDataSources(TestProvName.AllOracle)] string context)
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

				var qg =
					from p in db.Parent
					join c in db.Child on p.ParentID equals c.ParentID
					group c by p.ParentID
					into g
					select new
					{
						Variance         = g.Variance(a => a.ChildID),
						VarianceNone     = g.Variance(a => a.ChildID, Sql.AggregateModifier.None),
						VarianceAll      = g.Variance(a => a.ChildID, Sql.AggregateModifier.All),
						VarianceDistinct = g.Variance(a => a.ChildID, Sql.AggregateModifier.Distinct)
					};

				var resg = qg.ToArray();
				Assert.IsNotEmpty(resg);

				db.Child.Variance(c => c.ParentID);
				db.Child.Variance(c => c.ParentID, Sql.AggregateModifier.All);
				db.Child.Variance(c => c.ParentID, Sql.AggregateModifier.Distinct);
			}
		}

		[Test]
		public void TestKeepFirstOracle([IncludeDataSources(true, TestProvName.AllOracle)] string context)
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

		[Test]
		public void TestKeepLastOracle([IncludeDataSources(true, TestProvName.AllOracle)] string context)
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

		[Test]
		public void NestedQueries([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus, ProviderName.Oracle)]string context)
		{
			using (var db = GetDataContext(context))
			{
				var q1 =
					from p in db.Parent.Where(p => p.ParentID > 0).AsSubQuery()
					select new
					{
						p.ParentID,
						MaxValue = Sql.Ext.Max(p.Value1).Over().PartitionBy(p.ParentID).ToValue(),
					};

				var q2 = from q in q1.AsSubQuery()
					select new
					{
						q.ParentID,
						MaxValue = Sql.Ext.Min(q.MaxValue).Over().PartitionBy(q.ParentID).ToValue(),
					};

				Console.WriteLine(q1.ToString());
				Console.WriteLine(q2.ToString());

				Assert.AreEqual(2, q1.EnumQueries().Count());
				Assert.AreEqual(3, q2.EnumQueries().Count());
			}
		}

		[Table]
		class Position
		{
			[Column] public int  Group { get; set; }
			[Column] public int  Order { get; set; }
			[Column] public int? Id    { get; set; }

			public static Position[] TestData = new []
			{
				new Position() { Id = 5,    Group = 7, Order = 10 },
				new Position() { Id = 6,    Group = 7, Order = 20 },
				new Position() { Id = null, Group = 7, Order = 30 },
				new Position() { Id = null, Group = 7, Order = 40 }
			};
		}

		// Tests that default LAG behavior matches RESPECT NULLS as per spec, so we don't need to generate RESPECT NULLS token
		// TODO: needs sqllite 3.25
		// TODO: needs mysql 8.0/mariadb 10.2
		// also syntax should be altered (mariadb doesn't support defaults, mysql supports Sql.Nulls.Ignore in other place)
		[ActiveIssue(Configurations = new[] { TestProvName.AllSQLite, TestProvName.AllMySql })]
		[Test]
		public void Issue1732Lag([DataSources(
			TestProvName.AllSqlServer2008Minus,
			TestProvName.AllSybase,
			ProviderName.SqlCe,
			ProviderName.Access,
			ProviderName.Firebird)] string context)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(Position.TestData))
			{
				var group = 7;

				var q =
					from p in db.GetTable<Position>()
					where p.Group == @group
					select new
					{
						Id         = p.Id,
						PreviousId = (int?)Sql.Ext.Lag(p.Id, Sql.Nulls.Respect, 1, -1).Over().OrderBy(p.Order).ToValue(),

					};

				var res = q.ToArray();

				Assert.AreEqual(4, res.Length);

				// BTW, order from original query behaves differently for
				// Oracle, PostgreSQL, DB2 vs Informix, SQL Server
				Assert.AreEqual(5, res[0].Id);
				Assert.AreEqual(-1, res[0].PreviousId);
				Assert.AreEqual(6, res[1].Id);
				Assert.AreEqual(5, res[1].PreviousId);
				Assert.IsNull(res[2].Id);
				Assert.AreEqual(6, res[2].PreviousId);
				Assert.IsNull(res[3].Id);
				Assert.IsNull(res[3].PreviousId);
			}
		}

		// TODO: needs sqllite 3.25
		// TODO: needs mysql 8.0/mariadb 10.2
		[ActiveIssue(Configurations = new[] { TestProvName.AllSQLite, TestProvName.AllMySql })]
		[Test]
		public void Issue1732Lead([DataSources(
			TestProvName.AllSqlServer2008Minus,
			TestProvName.AllSybase,
			ProviderName.SqlCe,
			ProviderName.Access,
			ProviderName.Firebird)] string context)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(Position.TestData))
			{
				var group = 7;

				var q =
					from p in db.GetTable<Position>()
					where p.Group == @group
					select new
					{
						Id         = p.Id,
						PreviousId = (int?)Sql.Ext.Lead(p.Id, Sql.Nulls.Respect, 1, -1).Over().OrderBy(p.Order).ToValue(),

					};

				var res = q.ToArray();

				Assert.AreEqual(4, res.Length);

				Assert.AreEqual(5, res[0].Id);
				Assert.AreEqual(6, res[0].PreviousId);
				Assert.AreEqual(6, res[1].Id);
				Assert.IsNull(res[1].PreviousId);
				Assert.IsNull(res[2].Id);
				Assert.IsNull(res[2].PreviousId);
				Assert.IsNull(res[3].Id);
				Assert.AreEqual(-1, res[3].PreviousId);
			}
		}

		[ActiveIssue(Configurations = new[] { TestProvName.AllSQLite, TestProvName.AllMySql })]
		[Test]
		public void Issue1732FirstValue([DataSources(
			TestProvName.AllSqlServer2008Minus,
			TestProvName.AllSybase,
			ProviderName.SqlCe,
			ProviderName.Access,
			ProviderName.Firebird)] string context)
		{
			using (var db    = GetDataContext(context))
			using (var table = db.CreateLocalTable(Position.TestData))
			{
				var group = 7;

				var q =
					from p in db.GetTable<Position>()
					where p.Group == @group
					select new
					{
						Id         = p.Id,
						PreviousId = (int?)Sql.Ext.FirstValue(p.Id, Sql.Nulls.Respect).Over().OrderByDesc(p.Order).ToValue(),

					};

				var res = q.ToArray();

				Assert.AreEqual(4, res.Length);

				Assert.IsNull(res[0].Id);
				Assert.IsNull(res[0].PreviousId);
				Assert.IsNull(res[1].Id);
				Assert.IsNull(res[1].PreviousId);
				Assert.AreEqual(6, res[2].Id);
				Assert.IsNull(res[2].PreviousId);
				Assert.AreEqual(5, res[3].Id);
				Assert.IsNull(res[3].PreviousId);
			}
		}

		[ActiveIssue(Configurations = new[] { TestProvName.AllSQLite, TestProvName.AllMySql })]
		[Test]
		public void Issue1732LastValue([DataSources(
			TestProvName.AllSqlServer2008Minus,
			TestProvName.AllSybase,
			ProviderName.SqlCe,
			ProviderName.Access,
			ProviderName.Firebird)] string context)
		{
			using (var db    = GetDataContext(context))
			using (var table = db.CreateLocalTable(Position.TestData))
			{
				var group = 7;

				var q =
					from p in db.GetTable<Position>()
					where p.Group == @group
					select new
					{
						Id         = p.Id,
						PreviousId = (int?)Sql.Ext.LastValue(p.Id, Sql.Nulls.Respect).Over().OrderBy(p.Order).ToValue(),

					};

				var res = q.ToArray();

				Assert.AreEqual(4, res.Length);

				Assert.AreEqual(5, res[0].Id);
				Assert.AreEqual(5, res[0].PreviousId);
				Assert.AreEqual(6, res[1].Id);
				Assert.AreEqual(6, res[1].PreviousId);
				Assert.IsNull(res[2].Id);
				Assert.IsNull(res[2].PreviousId);
				Assert.IsNull(res[3].Id);
				Assert.IsNull(res[3].PreviousId);
			}
		}

		// TODO: pgsql/SAP HANA doesn't support FROM clause
		[ActiveIssue(Configurations = new[] { TestProvName.AllSQLite, TestProvName.AllMySql, TestProvName.AllPostgreSQL, ProviderName.SapHana })]
		[Test]
		public void Issue1732NthValue([DataSources(
			TestProvName.AllSqlServer,
			TestProvName.AllSybase,
			TestProvName.AllPostgreSQL,
			ProviderName.Informix,
			ProviderName.SqlCe,
			ProviderName.Access,
			ProviderName.Firebird)] string context)
		{
			using (var db    = GetDataContext(context))
			using (var table = db.CreateLocalTable(Position.TestData))
			{
				var group = 7;

				var q =
					from p in db.GetTable<Position>()
					where p.Group == @group
					select new
					{
						Id         = p.Id,
						PreviousId = (int?)Sql.Ext.NthValue(p.Id, 2, Sql.From.First, Sql.Nulls.Respect).Over().OrderByDesc(p.Order).ToValue(),

					};

				var res = q.ToArray();

				Assert.AreEqual(4, res.Length);

				Assert.IsNull(res[0].Id);
				Assert.IsNull(res[0].PreviousId);
				Assert.IsNull(res[1].Id);
				Assert.IsNull(res[1].PreviousId);
				Assert.AreEqual(6, res[2].Id);
				Assert.IsNull(res[2].PreviousId);
				Assert.AreEqual(5, res[3].Id);
				Assert.IsNull(res[3].PreviousId);
			}
		}

		[Table]
		class Issue1799Table1
		{
			[Column] public int      EventUser { get; set; }
			[Column] public int      ProcessID { get; set; }
			[Column] public DateTime EventTime { get; set; }
		}

		[Table]
		class Issue1799Table2
		{
			[Column] public int UserId        { get; set; }
			[Column] public string UserGroups { get; set; }
		}

		[Table]
		class Issue1799Table3
		{
			[Column] public int    ProcessID   { get; set; }
			[Column] public string ProcessName { get; set; }
		}

		// TODO: various issues like old db version, minute datepart translation
		[ActiveIssue(Configurations = new[] { TestProvName.AllSQLite, TestProvName.AllMySql, TestProvName.AllPostgreSQL, ProviderName.Informix, TestProvName.AllOracle })]
		[Test]
		public void Issue1799Test1([DataSources(
			TestProvName.AllSqlServer2008Minus,
			TestProvName.AllSybase,
			ProviderName.SqlCe,
			ProviderName.Access,
			ProviderName.Firebird)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<Issue1799Table1>())
			using (db.CreateLocalTable<Issue1799Table2>())
			using (db.CreateLocalTable<Issue1799Table3>())
			{
				var query =
					from x in db.GetTable<Issue1799Table1>()
					select new
					{
						User = x.EventUser,
						Proc = x.ProcessID,
						Diff = Sql.DateDiff(
							Sql.DateParts.Minute,
							Sql.Ext
								.Lag(x.EventTime, Sql.Nulls.None)
								.Over()
								.PartitionBy(x.EventUser, x.ProcessID)
								.OrderBy(x.EventTime)
								.ToValue(),
							x.EventTime),
					};

				query = query.Where(q => q.Diff > 0 && q.Diff <= 5);

				var finalQuery = from q in query
								 from u in db.GetTable<Issue1799Table2>().InnerJoin(u => u.UserId == q.User)
								 from p in db.GetTable<Issue1799Table3>().InnerJoin(p => p.ProcessID == q.Proc)
								 group q by new { q.User, u.UserGroups, p.ProcessName }
								 into g
								 select new
								 {
									g.Key.User,
									g.Key.ProcessName,
									g.Key.UserGroups,
									Sum = g.Sum(e => e.Diff) / 60
								 };

				finalQuery
					.Take(10)
					.ToList();
			}
		}

		// TODO: various issues like old db version, minute datepart translation
		[ActiveIssue(Configurations = new[] { TestProvName.AllSQLite, TestProvName.AllMySql, TestProvName.AllPostgreSQL, ProviderName.Informix, TestProvName.AllOracle })]
		[Test]
		public void Issue1799Test2([DataSources(
			TestProvName.AllSqlServer2008Minus,
			TestProvName.AllSybase,
			ProviderName.SqlCe,
			ProviderName.Access,
			ProviderName.Firebird)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<Issue1799Table1>())
			using (db.CreateLocalTable<Issue1799Table2>())
			using (db.CreateLocalTable<Issue1799Table3>())
			{
				var query =
					from x in db.GetTable<Issue1799Table1>()
					select new
					{
						User = x.EventUser,
						Proc = x.ProcessID,
						Diff = Sql.DateDiff(
							Sql.DateParts.Minute,
							Sql.Ext
								.Lag(x.EventTime, Sql.Nulls.None)
								.Over()
								.PartitionBy(x.EventUser, x.ProcessID)
								.OrderBy(x.EventTime)
								.ToValue(),
							x.EventTime),
					};

				// this part removed
				//query = query.Where(q => q.Diff > 0 && q.Diff <= 5);

				var finalQuery = from q in query
								 from u in db.GetTable<Issue1799Table2>().InnerJoin(u => u.UserId == q.User)
								 from p in db.GetTable<Issue1799Table3>().InnerJoin(p => p.ProcessID == q.Proc)
								 group q by new { q.User, u.UserGroups, p.ProcessName }
								 into g
								 select new
								 {
									 g.Key.User,
									 g.Key.ProcessName,
									 g.Key.UserGroups,
									 Sum = g.Sum(e => e.Diff) / 60
								 };

				finalQuery
					.Take(10)
					.ToList();
			}
		}
	}
}
