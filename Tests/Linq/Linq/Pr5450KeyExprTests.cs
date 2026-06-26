using System.Linq;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

namespace Tests.Linq
{
	// Regression coverage for the >7-member (Rest-nested ValueTuple) composite eager-loading key path.
	// Two distinct defects, both reachable only with a composite correlation key of 8+ members:
	//  1. GenerateKeyExpression nested the Rest tuple from startIndex+count instead of startIndex+count-1,
	//     dropping one member and producing a ValueTuple`1 Rest that AccessValueTupleField then read with
	//     Item2 -> ArgumentException at query-build time. Fixed in ExpressionBuilder.EagerLoad.cs.
	//  2. The KeyedQuery key-carry SELECT projects only the 7 top-level ValueTuple Item fields and does not
	//     recurse into Rest, so the grouping key carried back to the client is truncated; children fail to
	//     match their parent. Fix lives in the (cross-cutting) nested-ValueTuple SELECT-projection path.
	[TestFixture]
	public class Pr5450KeyExprTests : TestBase
	{
		[Table]
		sealed class KParent
		{
			[Column, PrimaryKey] public int Id { get; set; }
			[Column] public int K1 { get; set; }
			[Column] public int K2 { get; set; }
			[Column] public int K3 { get; set; }
			[Column] public int K4 { get; set; }
			[Column] public int K5 { get; set; }
			[Column] public int K6 { get; set; }
			[Column] public int K7 { get; set; }
			[Column] public int K8 { get; set; }
			[Column] public int K9 { get; set; }
		}

		[Table]
		sealed class KChild
		{
			[Column, PrimaryKey] public int     Id  { get; set; }
			[Column] public int F1 { get; set; }
			[Column] public int F2 { get; set; }
			[Column] public int F3 { get; set; }
			[Column] public int F4 { get; set; }
			[Column] public int F5 { get; set; }
			[Column] public int F6 { get; set; }
			[Column] public int F7 { get; set; }
			[Column] public int F8 { get; set; }
			[Column] public int F9 { get; set; }
			[Column] public string? Tag { get; set; }
		}

		static (KParent[] parents, KChild[] children) Seed()
		{
			// Both parents share K1..K7 and K9, differing only in K8 (the 8th correlation member,
			// which falls into the nested Rest tuple). Each has exactly one matching child.
			var parents = new[]
			{
				new KParent { Id = 1, K1 = 1, K2 = 1, K3 = 1, K4 = 1, K5 = 1, K6 = 1, K7 = 1, K8 = 1, K9 = 1 },
				new KParent { Id = 2, K1 = 1, K2 = 1, K3 = 1, K4 = 1, K5 = 1, K6 = 1, K7 = 1, K8 = 2, K9 = 1 },
			};
			var children = new[]
			{
				new KChild { Id = 1, F1 = 1, F2 = 1, F3 = 1, F4 = 1, F5 = 1, F6 = 1, F7 = 1, F8 = 1, F9 = 1, Tag = "p1" },
				new KChild { Id = 2, F1 = 1, F2 = 1, F3 = 1, F4 = 1, F5 = 1, F6 = 1, F7 = 1, F8 = 2, F9 = 1, Tag = "p2" },
			};
			return (parents, children);
		}

		// Defect #1 regression: a 9-member composite KeyedQuery key built the Rest tuple incorrectly and
		// threw ArgumentException ("Item2 is not defined for ValueTuple`1") at build time. With the fix the
		// query builds and executes (returning every parent); see the gated test below for the grouping bug.
		[Test]
		public void NineMemberCompositeKey_KeyedQuery_NoCrash([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataConnection(context);
			using var tP = db.CreateLocalTable<KParent>();
			using var tC = db.CreateLocalTable<KChild>();
			var (parents, children) = Seed();
			db.BulkCopy(parents);
			db.BulkCopy(children);

			// Pre-fix this query threw ArgumentException while building the key tuple; the fix lets it execute.
			var result = (
				from p in tP
				orderby p.Id
				select new
				{
					p.Id, p.K8,
					Children = tC.Where(c =>
							c.F1 == p.K1 && c.F2 == p.K2 && c.F3 == p.K3 && c.F4 == p.K4 &&
							c.F5 == p.K5 && c.F6 == p.K6 && c.F7 == p.K7 && c.F8 == p.K8 && c.F9 == p.K9)
						.OrderBy(c => c.Id).ToList(),
				})
				.WithKeyedLoadStrategy().ToList();

			result.Count.ShouldBe(2);
		}

		// Defect #2 regression (gated): the key-carry SELECT truncates the nested key to its 7 top-level
		// fields, so children differing only in members >=8 are mis-grouped. Each parent must get its own child.
		[ActiveIssue("PR #5450 review: KeyedQuery key-carry projection of a Rest-nested (>7-member) ValueTuple omits the Rest_* columns, truncating the client grouping key to 7 members.")]
		[Test]
		public void NineMemberCompositeKey_KeyedQuery_NestedKeyGrouping([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataConnection(context);
			using var tP = db.CreateLocalTable<KParent>();
			using var tC = db.CreateLocalTable<KChild>();
			var (parents, children) = Seed();
			db.BulkCopy(parents);
			db.BulkCopy(children);

			var result = (
				from p in tP
				orderby p.Id
				select new
				{
					p.Id, p.K8,
					Children = tC.Where(c =>
							c.F1 == p.K1 && c.F2 == p.K2 && c.F3 == p.K3 && c.F4 == p.K4 &&
							c.F5 == p.K5 && c.F6 == p.K6 && c.F7 == p.K7 && c.F8 == p.K8 && c.F9 == p.K9)
						.OrderBy(c => c.Id).ToList(),
				})
				.WithKeyedLoadStrategy().ToList();

			result.Count.ShouldBe(2);
			result[0].Children.Select(c => c.Tag).ShouldBe(new[] { "p1" });
			result[1].Children.Select(c => c.Tag).ShouldBe(new[] { "p2" });
		}

		// Baseline: the same 9-member composite key under the Default strategy groups correctly,
		// proving the data and the correlation are sound independent of KeyedQuery.
		[Test]
		public void NineMemberCompositeKey_Default([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataConnection(context);
			using var tP = db.CreateLocalTable<KParent>();
			using var tC = db.CreateLocalTable<KChild>();
			var (parents, children) = Seed();
			db.BulkCopy(parents);
			db.BulkCopy(children);

			var result = (
				from p in tP
				orderby p.Id
				select new
				{
					p.Id, p.K8,
					Children = tC.Where(c =>
							c.F1 == p.K1 && c.F2 == p.K2 && c.F3 == p.K3 && c.F4 == p.K4 &&
							c.F5 == p.K5 && c.F6 == p.K6 && c.F7 == p.K7 && c.F8 == p.K8 && c.F9 == p.K9)
						.OrderBy(c => c.Id).ToList(),
				})
				.WithSeparateLoadStrategy().ToList();

			result.Count.ShouldBe(2);
			result[0].Children.Select(c => c.Tag).ShouldBe(new[] { "p1" });
			result[1].Children.Select(c => c.Tag).ShouldBe(new[] { "p2" });
		}

		// Baseline: a 7-member composite key (no Rest nesting) groups correctly under KeyedQuery.
		[Test]
		public void SevenMemberCompositeKey_KeyedQuery([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataConnection(context);
			using var tP = db.CreateLocalTable<KParent>();
			using var tC = db.CreateLocalTable<KChild>();
			var (parents, children) = Seed();
			db.BulkCopy(parents);
			db.BulkCopy(children);

			var result = (
				from p in tP
				orderby p.Id
				select new
				{
					p.Id, p.K7,
					Children = tC.Where(c =>
							c.F1 == p.K1 && c.F2 == p.K2 && c.F3 == p.K3 && c.F4 == p.K4 &&
							c.F5 == p.K5 && c.F6 == p.K6 && c.F7 == p.K7)
						.OrderBy(c => c.Id).ToList(),
				})
				.WithKeyedLoadStrategy().ToList();

			// All rows share K1..K7, so every parent matches both children here.
			result.Count.ShouldBe(2);
			result[0].Children.Select(c => c.Tag).OrderBy(t => t).ShouldBe(new[] { "p1", "p2" });
			result[1].Children.Select(c => c.Tag).OrderBy(t => t).ShouldBe(new[] { "p1", "p2" });
		}
	}
}
