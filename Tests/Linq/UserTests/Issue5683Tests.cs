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

		sealed class DerivedHierarchy : Hierarchy
		{
		}

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

		class Projection
		{
			public int  Id    { get; set; }
			public int? Value { get; set; }
		}

		sealed class DerivedProjection : Projection
		{
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5683 - branches projecting differing-but-assignable types must meet in the same columns")]
		public void ConcatBranchProjectingDerivedType([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var query =
				(from p in db.Parent where p.ParentID > 3 select new Projection { Id = p.ParentID, Value = p.Value1 })
				.Concat(
					// cast to the projected type, so this branch reaches the set operation Convert-wrapped
					from p in db.Parent where p.ParentID <= 3 select (Projection)new DerivedProjection { Id = p.ParentID, Value = p.Value1 });

			var result = AssertQuery(query);

			// the branch-discriminator column exists so each branch is read with its own constructor
			result.Where(x => x.Id <= 3).ShouldAllBe(x => x.GetType() == typeof(DerivedProjection));
			result.Where(x => x.Id >  3).ShouldAllBe(x => x.GetType() == typeof(Projection));

			// A member is one column for both branches, and one more column tells which branch a row came from -
			// the branches construct different types, so the reader has to know which constructor to run. Keyed by
			// the constructed type rather than the type the projection is read as, the two branches would occupy
			// places of their own instead and each member would be emitted twice, once filled and once NULL-padded.
			query.GetSelectQuery()!.Select.Columns.Count.ShouldBe(3);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5683 - a constructed object read through `as` projects through the conversion")]
		public void SelectConstructedObjectThroughAs([DataSources] string context)
		{
			using var db = GetDataContext(context);

			// `as` reaches the projection as a TypeAs the builder projects *through*, so the constructor is asked
			// for as a whole rather than member by member - which used to throw NotImplementedException.
			var query = from p in db.Parent select new DerivedProjection { Id = p.ParentID, Value = p.Value1 } as Projection;

			var result = AssertQuery(query);

			result.ShouldAllBe(x => x.GetType() == typeof(DerivedProjection));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5683 - `as`-spelled branches projecting differing-but-assignable types must meet in the same columns")]
		public void ConcatBranchProjectingDerivedTypeThroughAs([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var query =
				(from p in db.Parent where p.ParentID > 3 select new Projection { Id = p.ParentID, Value = p.Value1 })
				.Concat(
					// the `as` spelling of the same cast the test above writes as `(Projection)`
					from p in db.Parent where p.ParentID <= 3 select new DerivedProjection { Id = p.ParentID, Value = p.Value1 } as Projection);

			var result = AssertQuery(query);

			// Same per-branch assertions as the `(Projection)`-cast twin: AssertQuery compares member-wise, and the
			// column count is symmetric, so neither can see the branches read with each other's constructor.
			result.Where(x => x.Id <= 3).ShouldAllBe(x => x.GetType() == typeof(DerivedProjection));
			result.Where(x => x.Id >  3).ShouldAllBe(x => x.GetType() == typeof(Projection));

			query.GetSelectQuery()!.Select.Columns.Count.ShouldBe(3);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5683 - branches pair by the element type even with no cast written anywhere")]
		public void ConcatBranchProjectingDerivedTypeWithoutCast([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var baseBranch    = from p in db.Parent where p.ParentID >  3 select new Projection        { Id = p.ParentID, Value = p.Value1 };
			// no cast: Concat binds TSource from the first operand and takes the second through IEnumerable<T>
			// covariance, so this branch reaches the set operation as a bare constructor typed DerivedProjection
			var derivedBranch = from p in db.Parent where p.ParentID <= 3 select new DerivedProjection { Id = p.ParentID, Value = p.Value1 };

			var query = baseBranch.Concat(derivedBranch);

			var result = AssertQuery(query);

			result.Where(x => x.Id <= 3).ShouldAllBe(x => x.GetType() == typeof(DerivedProjection));
			result.Where(x => x.Id >  3).ShouldAllBe(x => x.GetType() == typeof(Projection));

			query.GetSelectQuery()!.Select.Columns.Count.ShouldBe(3);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5683 - a distinct set operation cannot pair differing construction types and must say so")]
		public void UnionBranchProjectingDerivedTypeIsRefused([DataSources] string context)
		{
			using var db = GetDataContext(context);

			// Pairing the branches removes the NULL padding that used to double as the branch discriminator, and
			// GetDifferencePredicate only creates the __projection__set_id__ anchor for UNION ALL. So a distinct
			// set operation has no way to tell the branches apart, and refuses rather than comparing rows that
			// would need a discriminator column - which would itself stop equal rows from de-duplicating.
			// Before this was refused it silently returned wrong rows (9 where 7 were due).
			var query =
				(from p in db.Parent where p.ParentID > 2 select new Projection { Id = p.ParentID, Value = p.Value1 })
				.Union(
					from p in db.Parent where p.ParentID <= 4 select (Projection)new DerivedProjection { Id = p.ParentID, Value = p.Value1 });

			Shouldly.Should.Throw<LinqToDBException>(() => query.ToList())
				.Message.ShouldContain("Could not decide which construction type to use");
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5683 - Except needs no discriminator, so pairing makes it correct")]
		public void ExceptBranchProjectingDerivedType([DataSources] string context)
		{
			using var db = GetDataContext(context);

			// Except/ExceptAll read both sides through the left projection (InitializeProjections assigns
			// _projection2 = _projection1), so they never need to tell a row's branch apart and reach none of the
			// refusal above. Unpaired, the padded branches shared no column and nothing was ever removed.
			var query =
				(from p in db.Parent where p.ParentID > 2 select new Projection { Id = p.ParentID, Value = p.Value1 })
				.Except(
					from p in db.Parent where p.ParentID <= 4 select (Projection)new DerivedProjection { Id = p.ParentID, Value = p.Value1 });

			query.ToList().Select(x => x.Id).OrderBy(x => x).ToArray().ShouldBe(new[] { 5, 6, 7 });
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
	}
}
