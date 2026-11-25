using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue5683Tests : TestBase
	{
		sealed class Part
		{
			[PrimaryKey] public int     Id   { get; set; }
			public              string? Name { get; set; }
		}

		sealed class Reference
		{
			[PrimaryKey] public int Id          { get; set; }
			public              int ParentId    { get; set; }
			public              int ReferenceId { get; set; }
		}

		class Hierarchy
		{
			public object? RootPartSortField { get; set; }
			public int     RootPartId        { get; set; }
			public int     PartId            { get; set; }
			public int     HierarchyLevel    { get; set; }
		}

		sealed class DerivedHierarchy : Hierarchy;

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5683 - recursive CTE drops columns when the recursive term projects a differing-but-assignable type")]
		public void RecursiveCteWithDerivedRecursiveTerm([RecursiveCteContextSource] string context)
		{
			using var db    = GetDataContext(context);
			using var parts = db.CreateLocalTable(new[]
			{
				new Part { Id = 1, Name = "A" },
				new Part { Id = 2, Name = "B" },
				new Part { Id = 3, Name = "C" },
			});
			using var refs = db.CreateLocalTable(new[]
			{
				new Reference { Id = 1, ParentId = 1, ReferenceId = 2 },
				new Reference { Id = 2, ParentId = 2, ReferenceId = 3 },
			});

			// Anchor term paginated with OrderBy/Skip/Take (wraps it in a subquery).
			var anchor = parts
				.Select(x => new Hierarchy
				{
					RootPartSortField = x.Name,
					RootPartId        = x.Id,
					HierarchyLevel    = 0,
					PartId            = x.Id
				})
				.OrderBy(x => x.RootPartSortField)
				.Skip(0)
				.Take(20);

			var partCte = db.GetCte<Hierarchy>(partHierarchy =>
				anchor.Concat(
					partHierarchy.InnerJoin(
						refs,
						(cte, reference) => reference.ParentId == cte.PartId,
						// the recursive term projects a DERIVED type cast to the CTE type, so the merged
						// set-operation projection is `test ? Hierarchy : Convert(DerivedHierarchy)`
						(cte, reference) => (Hierarchy)new DerivedHierarchy
						{
							RootPartSortField = cte.RootPartSortField,
							RootPartId        = cte.RootPartId,
							PartId            = reference.ReferenceId,
							HierarchyLevel    = cte.HierarchyLevel + 1
						})));

			// Outer query joins on cte.PartId but only projects RootPartId/RootPartSortField.
			var allRelevant = parts
				.InnerJoin(
					partCte,
					(me, cte) => me.Id == cte.PartId,
					(me, id) => new { id.RootPartId, id.RootPartSortField, id.HierarchyLevel, me });

			var result = allRelevant.ToList();

			// CTE columns PartId/RootPartId/RootPartSortField must survive the cast so the recursion and the
			// outer join resolve correctly, and each root Part's Name must round-trip.
			// Expected (RootPartId, joined Part.Id, RootPartSortField, HierarchyLevel) tuples for the hierarchy:
			//   roots: (1,1,"A",0) (2,2,"B",0) (3,3,"C",0); 1->2: (1,2,"A",1); 2->3: (2,3,"B",1); 1->2->3: (1,3,"A",2)
			result
				.Select(x => (x.RootPartId, x.me.Id, SortField: (string)x.RootPartSortField!, x.HierarchyLevel))
				.OrderBy(x => (x.RootPartId, x.Id))
				.ShouldBe(new[] { (1, 1, "A", 0), (1, 2, "A", 1), (1, 3, "A", 2), (2, 2, "B", 0), (2, 3, "B", 1), (3, 3, "C", 0) });
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5683 - the issue's own shape, spelled without the cast")]
		public void RecursiveCteWithDerivedRecursiveTermWithoutCast([RecursiveCteContextSource] string context)
		{
			using var db    = GetDataContext(context);
			using var parts = db.CreateLocalTable(new[]
			{
				new Part { Id = 1, Name = "A" },
				new Part { Id = 2, Name = "B" },
				new Part { Id = 3, Name = "C" },
			});
			using var refs = db.CreateLocalTable(new[]
			{
				new Reference { Id = 1, ParentId = 1, ReferenceId = 2 },
				new Reference { Id = 2, ParentId = 2, ReferenceId = 3 },
			});

			var anchor = parts
				.Select(x => new Hierarchy
				{
					RootPartSortField = x.Name,
					RootPartId        = x.Id,
					HierarchyLevel    = 0,
					PartId            = x.Id
				})
				.OrderBy(x => x.RootPartSortField)
				.Skip(0)
				.Take(20);

			var partCte = db.GetCte<Hierarchy>(partHierarchy =>
				anchor.Concat(
					partHierarchy.InnerJoin(
						refs,
						(cte, reference) => reference.ParentId == cte.PartId,
						// no `(Hierarchy)` cast - covariance alone makes the lambda return IQueryable<Hierarchy>,
						// so nothing in the tree says the branch is read as the CTE's element type
						(cte, reference) => new DerivedHierarchy
						{
							RootPartSortField = cte.RootPartSortField,
							RootPartId        = cte.RootPartId,
							PartId            = reference.ReferenceId,
							HierarchyLevel    = cte.HierarchyLevel + 1
						})));

			var result = parts
				.InnerJoin(partCte, (me, cte) => me.Id == cte.PartId, (me, id) => new { id.RootPartId, id.RootPartSortField, id.HierarchyLevel, me })
				.ToList();

			result
				.Select(x => (x.RootPartId, x.me.Id, SortField: (string)x.RootPartSortField!, x.HierarchyLevel))
				.OrderBy(x => (x.RootPartId, x.Id))
				.ToArray()
				.ShouldBe(new[] { (1, 1, "A", 0), (1, 2, "A", 1), (1, 3, "A", 2), (2, 2, "B", 0), (2, 3, "B", 1), (3, 3, "C", 0) });
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5683 - the anchor projects a differing-but-assignable type")]
		public void RecursiveCteWithDerivedAnchor([RecursiveCteContextSource] string context)
		{
			using var db    = GetDataContext(context);
			using var parts = db.CreateLocalTable(new[]
			{
				new Part { Id = 1, Name = "A" },
				new Part { Id = 2, Name = "B" },
				new Part { Id = 3, Name = "C" },
			});
			using var refs = db.CreateLocalTable(new[]
			{
				new Reference { Id = 1, ParentId = 1, ReferenceId = 2 },
				new Reference { Id = 2, ParentId = 2, ReferenceId = 3 },
			});

			var anchor = parts
				.Select(x => (Hierarchy)new DerivedHierarchy
				{
					RootPartSortField = x.Name,
					RootPartId        = x.Id,
					HierarchyLevel    = 0,
					PartId            = x.Id
				});

			var partCte = db.GetCte<Hierarchy>(partHierarchy =>
				anchor.Concat(
					partHierarchy.InnerJoin(
						refs,
						(cte, reference) => reference.ParentId == cte.PartId,
						(cte, reference) => new Hierarchy
						{
							RootPartSortField = cte.RootPartSortField,
							RootPartId        = cte.RootPartId,
							PartId            = reference.ReferenceId,
							HierarchyLevel    = cte.HierarchyLevel + 1
						})));

			var result = parts
				.InnerJoin(partCte, (me, cte) => me.Id == cte.PartId, (me, id) => new { id.RootPartId, id.RootPartSortField, id.HierarchyLevel, me })
				.ToList();

			result
				.Select(x => (x.RootPartId, x.me.Id, SortField: (string)x.RootPartSortField!, x.HierarchyLevel))
				.OrderBy(x => (x.RootPartId, x.Id))
				.ToArray()
				.ShouldBe(new[] { (1, 1, "A", 0), (1, 2, "A", 1), (1, 3, "A", 2), (2, 2, "B", 0), (2, 3, "B", 1), (3, 3, "C", 0) });
		}
	}
}
