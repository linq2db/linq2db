using System.Linq;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

namespace Tests.Linq
{
	// Eager loading with a wide composite correlation key (8+ members), which the key machinery
	// represents as a Rest-nested ValueTuple. These cases span multiple load strategies (KeyedQuery
	// and the Default/separate strategy), so they live on their own rather than under a single
	// strategy's fixture.
	[TestFixture]
	public class EagerLoadingWideKeyTests : TestBase
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

		// A 9-member composite key under KeyedQuery builds and executes, returning every parent.
		[Test]
		public void NineMemberCompositeKey_KeyedQuery_NoCrash([IncludeDataSources(TestProvName.AllSQLite)] string context)
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
		}

		// A 9-member composite key under KeyedQuery: each parent gets only the child whose full key matches,
		// including members >=8 (the Rest-nested part of the key).
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

		// A 9-member composite key under the Default strategy: each parent gets its own matching child.
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

		// A 7-member composite key (no Rest nesting) under KeyedQuery: all rows share K1..K7, so every
		// parent matches both children.
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

			result.Count.ShouldBe(2);
			result[0].Children.Select(c => c.Tag).OrderBy(t => t).ShouldBe(new[] { "p1", "p2" });
			result[1].Children.Select(c => c.Tag).OrderBy(t => t).ShouldBe(new[] { "p1", "p2" });
		}
	}
}
