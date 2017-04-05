namespace Tests.Linq
{
	using System;
	using System.Linq;

	using LinqToDB;
	using NUnit.Framework;

	[TestFixture]
	public class AnalyticTests : TestBase
	{
		[Test, DataContextSource(ProviderName.Access, ProviderName.SQLite, ProviderName.SapHana, ProviderName.MySql, ProviderName.SqlCe,
			TestProvName.MySql57, TestProvName.SQLiteMs)]
		public void Test(string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = 
					from p in db.Parent
					join c in db.Child on p.ParentID equals c.ParentID 
					select new
					{
						Rank      = Sql.Over.PartitionBy(p.Value1, c.ChildID).OrderBy(p.Value1).Rank(),
						DenseRank = Sql.Over.PartitionBy(p.Value1, c.ChildID).OrderBy(p.Value1).DenseRank(),
						Count1    = Sql.Over.PartitionBy(p.Value1).OrderBy(p.Value1).Range.Between.UnboundedPreceding.And.CurrentRow.Count(p.ParentID, Sql.AggregateModifier.All),
						Count2    = Sql.Over.Count()
					};
				 Assert.IsNotEmpty(q.ToArray());
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
						AvgNoOrder  = Sql.Over.Average<double>(p.Value1, Sql.AggregateModifier.None),
						AvgRange    = Sql.Over.PartitionBy(p.Value1, c.ChildID).OrderBy(p.Value1).Range.Between.Value(0).And.Value(1).Following.Average<double>(p.Value1, Sql.AggregateModifier.None),

						// Testing conversion. Average may fail with Overflow error

						AvgD1     = Sql.Over.PartitionBy(p.Value1, c.ChildID).Average<decimal>(p.Value1, Sql.AggregateModifier.All),
						AvgD2     = Sql.Over.PartitionBy(p.Value1, c.ChildID).Average<decimal>(p.Value1, Sql.AggregateModifier.Distinct),
						AvgD3     = Sql.Over.PartitionBy(p.Value1, c.ChildID).Average<decimal>(p.Value1, Sql.AggregateModifier.None),
						AvgD4     = Sql.Over.Average<decimal>(p.Value1, Sql.AggregateModifier.None),

						AvgDN1    = Sql.Over.PartitionBy(p.Value1, c.ChildID).Average<decimal?>(p.Value1, Sql.AggregateModifier.All),
						AvgDN2    = Sql.Over.PartitionBy(p.Value1, c.ChildID).Average<decimal?>(p.Value1, Sql.AggregateModifier.Distinct),
						AvgDN3    = Sql.Over.PartitionBy(p.Value1, c.ChildID).Average<decimal?>(p.Value1, Sql.AggregateModifier.None),
						AvgDN4    = Sql.Over.Average<decimal?>(p.Value1, Sql.AggregateModifier.None),

						AvgIN1    = Sql.Over.PartitionBy(p.Value1, c.ChildID).Average<int?>(p.Value1, Sql.AggregateModifier.All),
						AvgIN2    = Sql.Over.PartitionBy(p.Value1, c.ChildID).Average<int?>(p.Value1, Sql.AggregateModifier.Distinct),
						AvgIN3    = Sql.Over.PartitionBy(p.Value1, c.ChildID).Average<int?>(p.Value1, Sql.AggregateModifier.None),
						AvgIN4    = Sql.Over.Average<int?>(p.Value1, Sql.AggregateModifier.None),

						AvgI1     = Sql.Over.PartitionBy(p.Value1, c.ChildID).Average<int>(p.Value1, Sql.AggregateModifier.All),
						AvgI2     = Sql.Over.PartitionBy(p.Value1, c.ChildID).Average<int>(p.Value1, Sql.AggregateModifier.Distinct),
						AvgI3     = Sql.Over.PartitionBy(p.Value1, c.ChildID).Average<int>(p.Value1, Sql.AggregateModifier.None),
						AvgI4     = Sql.Over.Average<int>(p.Value1, Sql.AggregateModifier.None),

						AvgL1     = Sql.Over.PartitionBy(p.Value1, c.ChildID).Average<long>(p.Value1, Sql.AggregateModifier.All),
						AvgL2     = Sql.Over.PartitionBy(p.Value1, c.ChildID).Average<long>(p.Value1, Sql.AggregateModifier.Distinct),
						AvgL3     = Sql.Over.PartitionBy(p.Value1, c.ChildID).Average<long>(p.Value1, Sql.AggregateModifier.None),
						AvgL4     = Sql.Over.Average<long>(p.Value1, Sql.AggregateModifier.None),

						AvgLN1     = Sql.Over.PartitionBy(p.Value1, c.ChildID).Average<long?>(p.Value1, Sql.AggregateModifier.All),
						AvgLN2     = Sql.Over.PartitionBy(p.Value1, c.ChildID).Average<long?>(p.Value1, Sql.AggregateModifier.Distinct),
						AvgLN3     = Sql.Over.PartitionBy(p.Value1, c.ChildID).Average<long?>(p.Value1, Sql.AggregateModifier.None),
						AvgLN4     = Sql.Over.Average<long?>(p.Value1, Sql.AggregateModifier.None),

						AvgF1     = Sql.Over.PartitionBy(p.Value1, c.ChildID).Average<float>(p.Value1, Sql.AggregateModifier.All),
						AvgF2     = Sql.Over.PartitionBy(p.Value1, c.ChildID).Average<float>(p.Value1, Sql.AggregateModifier.Distinct),
						AvgF3     = Sql.Over.PartitionBy(p.Value1, c.ChildID).Average<float>(p.Value1, Sql.AggregateModifier.None),
						AvgF4     = Sql.Over.Average<float>(p.Value1, Sql.AggregateModifier.None),

						AvgFN1    = Sql.Over.PartitionBy(p.Value1, c.ChildID).Average<float?>(p.Value1, Sql.AggregateModifier.All),
						AvgFN2    = Sql.Over.PartitionBy(p.Value1, c.ChildID).Average<float?>(p.Value1, Sql.AggregateModifier.Distinct),
						AvgFN3    = Sql.Over.PartitionBy(p.Value1, c.ChildID).Average<float?>(p.Value1, Sql.AggregateModifier.None),
						AvgFN4    = Sql.Over.Average<float?>(p.Value1, Sql.AggregateModifier.None),

						AvgDO1    = Sql.Over.PartitionBy(p.Value1, c.ChildID).Average<double>(p.Value1, Sql.AggregateModifier.All),
						AvgDO2    = Sql.Over.PartitionBy(p.Value1, c.ChildID).Average<double>(p.Value1, Sql.AggregateModifier.Distinct),
						AvgDO3    = Sql.Over.PartitionBy(p.Value1, c.ChildID).Average<double>(p.Value1, Sql.AggregateModifier.None),
						AvgDO4    = Sql.Over.Average<double>(p.Value1, Sql.AggregateModifier.None),

						AvgDON1   = Sql.Over.PartitionBy(p.Value1, c.ChildID).Average<double?>(p.Value1, Sql.AggregateModifier.All),
						AvgDON2   = Sql.Over.PartitionBy(p.Value1, c.ChildID).Average<double?>(p.Value1, Sql.AggregateModifier.Distinct),
						AvgDON3   = Sql.Over.PartitionBy(p.Value1, c.ChildID).Average<double?>(p.Value1, Sql.AggregateModifier.None),
						AvgDON4   = Sql.Over.Average<double?>(p.Value1, Sql.AggregateModifier.None),

						// modifications

						AvgAll       = Sql.Over.PartitionBy(p.Value1, c.ChildID).OrderBy(p.Value1).Range.Between.UnboundedPreceding.And.CurrentRow.Average<long?>(p.Value1, Sql.AggregateModifier.All),
						AvgNone      = Sql.Over.PartitionBy(p.Value1, c.ChildID).OrderBy(p.Value1).Range.Between.UnboundedPreceding.And.Value(1).Following.Average<long?>(p.Value1, Sql.AggregateModifier.None),
						AvgDistinct1 = Sql.Over.Average<long?>(p.Value1, Sql.AggregateModifier.Distinct),
						AvgDistinct2 = Sql.Over.PartitionBy(p.Value1, c.ChildID).Average<long?>(p.Value1, Sql.AggregateModifier.Distinct),

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
						Corr1    = Sql.Over.PartitionBy(p.Value1, c.ChildID).Corr<decimal>(p.Value1, c.ChildID),
						Corr2    = Sql.Over.PartitionBy(p.Value1, c.ChildID).Corr<decimal?>(p.Value1, c.ChildID),
						Corr3    = Sql.Over.PartitionBy(p.Value1, c.ChildID).Corr<float>(p.Value1, c.ChildID),
						Corr4    = Sql.Over.PartitionBy(p.Value1, c.ChildID).Corr<int>(p.Value1, c.ChildID),

						// variations
						CorrSimple           = Sql.Over.Corr<decimal>(p.Value1, c.ChildID),
						CorrWithOrder        = Sql.Over.PartitionBy(p.Value1, c.ChildID).OrderBy(p.Value1).Corr<decimal>(p.Value1, c.ChildID),
						CorrPartitionNoOrder = Sql.Over.PartitionBy(p.Value1, c.ChildID).Corr<decimal>(p.Value1, c.ChildID),
						CorrWithoutPartition = Sql.Over.OrderBy(p.Value1).Corr<decimal>(p.Value1, c.ChildID),
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
						Count1    = Sql.Over.PartitionBy(p.Value1, c.ChildID).Count(),
						Count2    = Sql.Over.PartitionBy(p.Value1, c.ChildID).Count(p.Value1),
						Count3    = Sql.Over.PartitionBy(p.Value1, c.ChildID).Count(p.Value1, Sql.AggregateModifier.All),
						Count4    = Sql.Over.PartitionBy(p.Value1, c.ChildID).Count(p.Value1, Sql.AggregateModifier.Distinct),
						Count5    = Sql.Over.PartitionBy(p.Value1, c.ChildID).Count(p.Value1, Sql.AggregateModifier.None),

						Count11   = Sql.Over.Count(),
						Count12   = Sql.Over.Count(p.Value1),
						Count13   = Sql.Over.Count(p.Value1, Sql.AggregateModifier.All),
						Count14   = Sql.Over.Count(p.Value1, Sql.AggregateModifier.Distinct),
						Count15   = Sql.Over.Count(p.Value1, Sql.AggregateModifier.None),

						Count21   = Sql.Over.PartitionBy(c.ChildID).OrderBy(p.Value1).Range.Between.UnboundedPreceding.And.CurrentRow.Count(p.Value1, Sql.AggregateModifier.All),
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
						CovarPop1    = Sql.Over.PartitionBy(p.Value1, c.ChildID).CovarPop(p.Value1, c.ChildID),
						CovarPop2    = Sql.Over.PartitionBy(p.Value1, c.ChildID).CovarPop(p.Value1, c.ChildID),
						CovarPop3    = Sql.Over.CovarPop(p.Value1, c.ChildID),

						CovarPop4   = Sql.Over.PartitionBy(c.ChildID).OrderBy(p.Value1).Range.Between.UnboundedPreceding.And.CurrentRow.CovarPop(p.Value1, c.ChildID),
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
						CovarSamp1    = Sql.Over.PartitionBy(p.Value1, c.ChildID).CovarSamp(p.Value1, c.ChildID),
						CovarSamp2    = Sql.Over.PartitionBy(p.Value1, c.ChildID).CovarSamp(p.Value1, c.ChildID),
						CovarSamp3    = Sql.Over.CovarSamp(p.Value1, c.ChildID),

						CovarSamp4   = Sql.Over.PartitionBy(c.ChildID).OrderBy(p.Value1).Range.Between.UnboundedPreceding.And.CurrentRow.CovarSamp(p.Value1, c.ChildID),
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
						CumeDist1     = Sql.Over.PartitionBy(p.Value1, c.ChildID).OrderBy(p.Value1).CumeDist(),
						CumeDist2     = Sql.Over.OrderBy(p.Value1).ThenByDesc(c.ChildID).CumeDist(),
					};
				Assert.IsNotEmpty(q.ToArray());
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
						DenseRank1     = Sql.Over.PartitionBy(p.Value1, c.ChildID).OrderBy(p.Value1).DenseRank(),
						DenseRank2     = Sql.Over.OrderBy(p.Value1).ThenByDesc(c.ChildID).DenseRank(),
					};
				Assert.IsNotEmpty(q.ToArray());
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
						FirstValue1     = Sql.Over.FirstValue(p.Value1, Sql.Nulls.Ignore),
						FirstValue2     = Sql.Over.PartitionBy(p.Value1, c.ChildID).OrderBy(p.Value1).FirstValue(p.Value1, Sql.Nulls.None),
						FirstValue3     = Sql.Over.PartitionBy(p.Value1, c.ChildID).FirstValue(p.Value1, Sql.Nulls.Respect),
						FirstValue4     = Sql.Over.OrderBy(p.Value1).FirstValue(p.Value1, Sql.Nulls.Respect),
						FirstValue5     = Sql.Over.OrderBy(p.Value1).ThenByDesc(c.ChildID).FirstValue(p.Value1, Sql.Nulls.Respect),
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
						LastValue1     = Sql.Over.LastValue(p.Value1, Sql.Nulls.Ignore),
						LastValue2     = Sql.Over.PartitionBy(p.Value1, c.ChildID).OrderBy(p.Value1).LastValue(p.Value1, Sql.Nulls.None),
						LastValue3     = Sql.Over.PartitionBy(p.Value1, c.ChildID).LastValue(p.Value1, Sql.Nulls.Respect),
						LastValue4     = Sql.Over.OrderBy(p.Value1).LastValue(p.Value1, Sql.Nulls.Respect),
						LastValue5     = Sql.Over.OrderBy(p.Value1).ThenByDesc(c.ChildID).LastValue(p.Value1, Sql.Nulls.Respect),
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
						Lead1     = Sql.Over.PartitionBy(p.Value1, c.ChildID).OrderBy(p.Value1).Lead(p.Value1, Sql.Nulls.Ignore),
						Lead2     = Sql.Over.OrderBy(p.Value1).ThenByDesc(c.ChildID).Lead(p.Value1, Sql.Nulls.Respect),
						Lead3     = Sql.Over.OrderBy(p.Value1).ThenByDesc(c.ChildID).Lead(p.Value1, Sql.Nulls.None),
						Lead4     = Sql.Over.OrderBy(p.Value1).ThenByDesc(c.ChildID).Lead(p.Value1, Sql.Nulls.Respect, 1, null),
						Lead5     = Sql.Over.OrderBy(p.Value1).ThenByDesc(c.ChildID).Lead(p.Value1, Sql.Nulls.Respect, 1, c.ChildID),
					};
				Assert.IsNotEmpty(q.ToArray());
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
						Max1   = Sql.Over.PartitionBy(p.Value1, c.ChildID).Max(p.Value1),
						Max2   = Sql.Over.PartitionBy(p.Value1, c.ChildID).Max(p.Value1, Sql.AggregateModifier.All),
						Max3   = Sql.Over.PartitionBy(p.Value1, c.ChildID).Max(p.Value1, Sql.AggregateModifier.Distinct),
						Max4   = Sql.Over.PartitionBy(p.Value1, c.ChildID).Max(p.Value1, Sql.AggregateModifier.None),

						Max5   = Sql.Over.Max(p.Value1),
						Max6   = Sql.Over.Max(p.Value1, Sql.AggregateModifier.All),
						Max7   = Sql.Over.Max(p.Value1, Sql.AggregateModifier.Distinct),
						Max8   = Sql.Over.Max(p.Value1, Sql.AggregateModifier.None),
						Max9   = Sql.Over.OrderBy(p.Value1).Max(p.Value1, Sql.AggregateModifier.None),

						Max10  = Sql.Over.PartitionBy(c.ChildID).OrderBy(p.Value1).Range.Between.UnboundedPreceding.And.CurrentRow.Max(p.Value1, Sql.AggregateModifier.All),
					};
				var res = q.ToArray();
				Assert.IsNotEmpty(res);
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
						Median1   = Sql.Over.PartitionBy(p.Value1, c.ChildID).Median(p.Value1),
						Median2   = Sql.Over.Median(p.Value1),
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
						Min1   = Sql.Over.PartitionBy(p.Value1, c.ChildID).Min(p.Value1),
						Min2   = Sql.Over.PartitionBy(p.Value1, c.ChildID).Min(p.Value1, Sql.AggregateModifier.All),
						Min3   = Sql.Over.PartitionBy(p.Value1, c.ChildID).Min(p.Value1, Sql.AggregateModifier.Distinct),
						Min4   = Sql.Over.PartitionBy(p.Value1, c.ChildID).Min(p.Value1, Sql.AggregateModifier.None),

						Min5   = Sql.Over.Min(p.Value1),
						Min6   = Sql.Over.Min(p.Value1, Sql.AggregateModifier.All),
						Min7   = Sql.Over.Min(p.Value1, Sql.AggregateModifier.Distinct),
						Min8   = Sql.Over.Min(p.Value1, Sql.AggregateModifier.None),
						Min9   = Sql.Over.OrderBy(p.Value1).Min(p.Value1, Sql.AggregateModifier.None),

						Min10  = Sql.Over.PartitionBy(c.ChildID).OrderBy(p.Value1).Range.Between.UnboundedPreceding.And.CurrentRow.Min(p.Value1, Sql.AggregateModifier.All),
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
						NthValue1   = Sql.Over.PartitionBy(p.Value1, c.ChildID).NthValue(c.ChildID, 1),
						NthValue2   = Sql.Over.PartitionBy(p.Value1, c.ChildID).NthValue(c.ChildID, p.ParentID),

						NthValue5   = Sql.Over.NthValue(c.ChildID, 1),
						NthValue9   = Sql.Over.OrderBy(p.Value1).NthValue(c.ChildID, 1),

						NthValue10  = Sql.Over.PartitionBy(c.ChildID).OrderBy(p.Value1).Range.Between.UnboundedPreceding.And.CurrentRow.NthValue(c.ChildID, 1),
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
						NTile1     = Sql.Over.PartitionBy(p.Value1, c.ChildID).OrderBy(p.Value1).NTile(p.Value1.Value),
						NTile2     = Sql.Over.OrderBy(p.Value1).ThenByDesc(c.ChildID).NTile(1),
					};
				Assert.IsNotEmpty(q.ToArray());
			}
		}

		[Test, IncludeDataContextSource(true, ProviderName.Oracle, ProviderName.OracleManaged, ProviderName.OracleNative)]
		public void TestPercentRankOracle(string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = 
					from p in db.Parent
					join c in db.Child on p.ParentID equals c.ParentID 
					select new
					{
						PercentRank1     = Sql.Over.PartitionBy(p.Value1, c.ChildID).OrderBy(p.Value1).PercentRank(),
						PercentRank2     = Sql.Over.OrderBy(p.Value1).ThenByDesc(c.ChildID).PercentRank(),
					};
				Assert.IsNotEmpty(q.ToArray());
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
						RatioToReport1     = Sql.Over.PartitionBy(p.Value1, c.ChildID).RatioToReport(1),
						RatioToReport2     = Sql.Over.RatioToReport(c.ChildID),
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
						RowNumber1     = Sql.Over.PartitionBy(p.Value1, c.ChildID).OrderBy(p.Value1).RowNumber(),
						RowNumber2     = Sql.Over.OrderBy(p.Value1).ThenByDesc(c.ChildID).RowNumber(),
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
						Rank1     = Sql.Over.PartitionBy(p.Value1, c.ChildID).OrderBy(p.Value1).Rank(),
						Rank2     = Sql.Over.OrderBy(p.Value1).ThenByDesc(c.ChildID).Rank(),
					};
				Assert.IsNotEmpty(q.ToArray());
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
						StdDev1    = Sql.Over.PartitionBy(p.Value1, c.ChildID).StdDev(p.Value1),
						StdDev2    = Sql.Over.PartitionBy(p.Value1, c.ChildID).StdDev(p.Value1, Sql.AggregateModifier.All),
						StdDev3    = Sql.Over.PartitionBy(p.Value1, c.ChildID).StdDev(p.Value1, Sql.AggregateModifier.Distinct),
						StdDev4    = Sql.Over.PartitionBy(p.Value1, c.ChildID).StdDev(p.Value1, Sql.AggregateModifier.None),

						StdDev11   = Sql.Over.StdDev(p.Value1),
						StdDev12   = Sql.Over.StdDev(p.Value1, Sql.AggregateModifier.All),
						StdDev13   = Sql.Over.StdDev(p.Value1, Sql.AggregateModifier.Distinct),
						StdDev14   = Sql.Over.StdDev(p.Value1, Sql.AggregateModifier.None),

						StdDev21   = Sql.Over.PartitionBy(c.ChildID).OrderBy(p.Value1).Range.Between.UnboundedPreceding.And.CurrentRow.StdDev(p.Value1, Sql.AggregateModifier.All),
					};
				var res = q.ToArray();
				Assert.IsNotEmpty(res);
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
						StdDevPop1   = Sql.Over.PartitionBy(p.Value1, c.ChildID).StdDevPop(p.Value1),
						StdDevPop2   = Sql.Over.StdDevPop(p.Value1),
						StdDevPop3   = Sql.Over.PartitionBy(c.ChildID).OrderBy(p.Value1).Range.Between.UnboundedPreceding.And.CurrentRow.StdDevPop(p.Value1),
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
						StdDevSamp1   = Sql.Over.PartitionBy(p.Value1, c.ChildID).StdDevSamp(p.Value1),
						StdDevSamp2   = Sql.Over.StdDevSamp(p.Value1),
						StdDevSamp3   = Sql.Over.PartitionBy(c.ChildID).OrderBy(p.Value1).Range.Between.UnboundedPreceding.And.CurrentRow.StdDevSamp(p.Value1),
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
						Sum1   = Sql.Over.PartitionBy(p.Value1, c.ChildID).Sum(p.Value1),
						Sum2   = Sql.Over.Sum(p.Value1),
						Sum3   = Sql.Over.PartitionBy(c.ChildID).OrderBy(p.Value1).Range.Between.UnboundedPreceding.And.CurrentRow.Sum(p.Value1),
					};
				var res = q.ToArray();
				Assert.IsNotEmpty(res);
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
						VarPop1   = Sql.Over.PartitionBy(p.Value1, c.ChildID).VarPop(p.Value1),
						VarPop2   = Sql.Over.VarPop(p.Value1),
						VarPop3   = Sql.Over.PartitionBy(c.ChildID).OrderBy(p.Value1).Range.Between.UnboundedPreceding.And.CurrentRow.VarPop(p.Value1),
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
						VarSamp1   = Sql.Over.PartitionBy(p.Value1, c.ChildID).VarSamp(p.Value1),
						VarSamp2   = Sql.Over.VarSamp(p.Value1),
						VarSamp3   = Sql.Over.PartitionBy(c.ChildID).OrderBy(p.Value1).Range.Between.UnboundedPreceding.And.CurrentRow.VarSamp(p.Value1),
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
						Variance1    = Sql.Over.PartitionBy(p.Value1, c.ChildID).Variance(p.Value1),
						Variance2    = Sql.Over.PartitionBy(p.Value1, c.ChildID).Variance(p.Value1, Sql.AggregateModifier.All),
						Variance3    = Sql.Over.PartitionBy(p.Value1, c.ChildID).Variance(p.Value1, Sql.AggregateModifier.Distinct),
						Variance4    = Sql.Over.PartitionBy(p.Value1, c.ChildID).Variance(p.Value1, Sql.AggregateModifier.None),

						Variance11   = Sql.Over.Variance(p.Value1),
						Variance12   = Sql.Over.Variance(p.Value1, Sql.AggregateModifier.All),
						Variance13   = Sql.Over.Variance(p.Value1, Sql.AggregateModifier.Distinct),
						Variance14   = Sql.Over.Variance(p.Value1, Sql.AggregateModifier.None),

						Variance21   = Sql.Over.PartitionBy(c.ChildID).OrderBy(p.Value1).Range.Between.UnboundedPreceding.And.CurrentRow.Variance(p.Value1, Sql.AggregateModifier.All),
					};
				var res = q.ToArray();
				Assert.IsNotEmpty(res);
			}
		}
		}
}