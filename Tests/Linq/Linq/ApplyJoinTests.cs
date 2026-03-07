using System;
using System.Data;
using System.Linq;

using LinqToDB;
using LinqToDB.Internal.SqlQuery;

using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	public class ApplyJoinTests : TestBase
	{
		/// <summary>
		/// Issue 3311 Test 3 pattern: OUTER APPLY with inner LEFT JOIN referencing outer source.
		/// This should be decorrelated into a standard LEFT JOIN.
		/// </summary>
		[Test]
		public void OuterApplyWithCorrelatedLeftJoin([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			var result = (
				from u in db.Person
				from x in (
					from r in db.SelectQuery(() => 1)
					from l in db.Patient.LeftJoin(l => l.PersonID == u.ID)
					select l.PersonID
				).AsSubQuery()
				select new { u.ID, x }
			).ToList();

			Assert.That(result, Is.Not.Null);
			Assert.That(result.Count(), Is.GreaterThan(0));
		}

		/// <summary>
		/// CROSS APPLY with correlated WHERE (simpler case, already should work).
		/// </summary>
		[Test]
		public void CrossApplyWithCorrelatedWhere([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			var result = (
				from p in db.Person
				from c in db.Patient.Where(c => c.PersonID == p.ID)
				select new { p.FirstName, c.Diagnosis }
			).ToList();

			Assert.That(result, Is.Not.Null);
		}

		/// <summary>
		/// OUTER APPLY with correlated subquery (DefaultIfEmpty → LEFT JOIN pattern).
		/// </summary>
		[Test]
		public void OuterApplyWithCorrelatedWhere([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			var result = (
				from p in db.Person
				from c in db.Patient.Where(c => c.PersonID == p.ID).DefaultIfEmpty()
				select new { p.FirstName, Diagnosis = c != null ? c.Diagnosis : null }
			).ToList();

			Assert.That(result, Is.Not.Null);
			Assert.That(result.Count(), Is.GreaterThan(0));
		}

		/// <summary>
		/// CROSS APPLY with inner join referencing outer table.
		/// </summary>
		[Test]
		public void CrossApplyWithCorrelatedInnerJoin([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			var result = (
				from p in db.Person
				from x in (
					from c in db.Patient
					where c.PersonID == p.ID
					join p2 in db.Person on c.PersonID equals p2.ID
					select new { c.Diagnosis, p2.FirstName }
				)
				select new { p.ID, x.Diagnosis, x.FirstName }
			).ToList();

			Assert.That(result, Is.Not.Null);
		}

		/// <summary>
		/// Simple non-correlated APPLY (should already be optimized to regular JOIN).
		/// </summary>
		[Test]
		public void NonCorrelatedApply([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			var result = (
				from p in db.Person
				from c in db.Patient
				select new { p.FirstName, c.Diagnosis }
			).ToList();

			Assert.That(result, Is.Not.Null);
		}

		/// <summary>
		/// DefaultIfEmpty scalar subquery pattern.
		/// </summary>
		[Test]
		public void OuterApplyScalar([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			var result = (
				from x in db.Person
				from apply in db.SelectQuery(() => Sql.AsSql(x.ID + 1)).DefaultIfEmpty()
				select apply
			).ToList();

			Assert.That(result, Is.Not.Null);
			Assert.That(result.Count(), Is.GreaterThan(0));
		}

		/// <summary>
		/// Multiple columns from correlated OUTER APPLY with LEFT JOIN.
		/// </summary>
		[Test]
		public void OuterApplyMultiColumnWithCorrelatedLeftJoin([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			var result = (
				from u in db.Person
				from x in (
					from r in db.SelectQuery(() => 1)
					from l in db.Patient.LeftJoin(l => l.PersonID == u.ID)
					select new { l.PersonID, l.Diagnosis }
				).AsSubQuery()
				select new { u.ID, x.PersonID, x.Diagnosis }
			).ToList();

			Assert.That(result, Is.Not.Null);
			Assert.That(result.Count(), Is.GreaterThan(0));
		}

		// =====================================================================
		// Aggregate APPLY patterns (GROUP BY decorrelation)
		// =====================================================================

		/// <summary>
		/// CROSS APPLY with COUNT(*) aggregate — tests GROUP BY decorrelation.
		/// For each parent, count children.
		/// </summary>
		[Test]
		public void CrossApplyWithCount([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			var query =
				from p in db.Parent
				from c in (
					from c in db.Child
					where c.ParentID == p.ParentID
					group c by c.ParentID into g
					select new { Count = g.Count() }
				)
				select new { p.ParentID, c.Count };

			AssertQuery(query);
		}

		/// <summary>
		/// CROSS APPLY with SUM aggregate — sums child IDs per parent.
		/// </summary>
		[Test]
		public void CrossApplyWithSum([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			var query =
				from p in db.Parent
				from c in (
					from c in db.Child
					where c.ParentID == p.ParentID
					group c by c.ParentID into g
					select new { Sum = g.Sum(x => x.ChildID) }
				)
				select new { p.ParentID, c.Sum };

			AssertQuery(query);
		}

		/// <summary>
		/// CROSS APPLY with MAX aggregate per parent.
		/// </summary>
		[Test]
		public void CrossApplyWithMax([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			var query =
				from p in db.Parent
				from c in (
					from c in db.Child
					where c.ParentID == p.ParentID
					group c by c.ParentID into g
					select new { MaxChildID = g.Max(x => x.ChildID) }
				)
				select new { p.ParentID, c.MaxChildID };

			AssertQuery(query);
		}

		// =====================================================================
		// Multi-level APPLY patterns (3-level hierarchy)
		// =====================================================================

		/// <summary>
		/// Three-level APPLY: Parent → Child → GrandChild.
		/// Two nested correlated subqueries.
		/// </summary>
		[Test]
		public void MultiLevelApply([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			var query =
				from p in db.Parent
				from c in db.Child.Where(c => c.ParentID == p.ParentID)
				from g in db.GrandChild.Where(g => g.ParentID == c.ParentID && g.ChildID == c.ChildID)
				select new { p.ParentID, c.ChildID, g.GrandChildID };

			AssertQuery(query);
		}

		/// <summary>
		/// Three-level APPLY with OUTER at the last level.
		/// Parent → CROSS APPLY Child → OUTER APPLY GrandChild.
		/// </summary>
		[Test]
		public void MultiLevelMixedApply([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			var query =
				from p in db.Parent
				from c in db.Child.Where(c => c.ParentID == p.ParentID)
				from g in db.GrandChild.Where(g => g.ParentID == c.ParentID && g.ChildID == c.ChildID).DefaultIfEmpty()
				select new { p.ParentID, c.ChildID, GrandChildID = g.GrandChildID };

			AssertQuery(query);
		}

		// =====================================================================
		// Multiple simultaneous APPLY from same outer source
		// =====================================================================

		/// <summary>
		/// Two independent CROSS APPLYs from the same parent — children and grandchildren separately.
		/// </summary>
		[Test]
		public void DualCrossApply([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			var query =
				from p in db.Parent
				from c in db.Child.Where(c => c.ParentID == p.ParentID)
				from g in db.GrandChild.Where(g => g.ParentID == p.ParentID)
				select new { p.ParentID, c.ChildID, g.GrandChildID };

			AssertQuery(query);
		}

		/// <summary>
		/// CROSS APPLY + OUTER APPLY from same parent.
		/// First subquery filters strictly, second uses DefaultIfEmpty.
		/// </summary>
		[Test]
		public void MixedCrossAndOuterApply([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			var query =
				from p in db.Parent
				from c in db.Child.Where(c => c.ParentID == p.ParentID)
				from pat in db.Patient.Where(pat => pat.PersonID == p.ParentID).DefaultIfEmpty()
				select new { p.ParentID, c.ChildID, Diagnosis = pat == null ? null : pat.Diagnosis };

			AssertQuery(query);
		}

		// =====================================================================
		// APPLY with Take (TOP N per group)
		// =====================================================================

		/// <summary>
		/// CROSS APPLY with Take(1) — get the first child per parent.
		/// Generates TOP 1 / LIMIT 1 in correlated subquery.
		/// </summary>
		[Test]
		public void CrossApplyWithTake([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			var query =
				from p in db.Parent
				from c in db.Child
					.Where(c => c.ParentID == p.ParentID)
					.OrderBy(c => c.ChildID)
					.Take(1)
				select new { p.ParentID, c.ChildID };

			AssertQuery(query);
		}

		/// <summary>
		/// CROSS APPLY with Take(2) — get top 2 children per parent.
		/// </summary>
		[Test]
		public void CrossApplyWithTake2([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			var query =
				from p in db.Parent
				from c in db.Child
					.Where(c => c.ParentID == p.ParentID)
					.OrderBy(c => c.ChildID)
					.Take(2)
				select new { p.ParentID, c.ChildID };

			AssertQuery(query);
		}

		// =====================================================================
		// APPLY with OrderBy (sorted correlated subquery)
		// =====================================================================

		/// <summary>
		/// CROSS APPLY with OrderByDescending inside the subquery.
		/// </summary>
		[Test]
		public void CrossApplyWithOrderBy([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			var query =
				from p in db.Parent
				from c in db.Child
					.Where(c => c.ParentID == p.ParentID)
					.OrderByDescending(c => c.ChildID)
				select new { p.ParentID, p.Value1, c.ChildID };

			AssertQuery(query);
		}

		// =====================================================================
		// APPLY with multiple correlation predicates
		// =====================================================================

		/// <summary>
		/// CROSS APPLY with two correlation predicates (ParentID + ChildID).
		/// </summary>
		[Test]
		public void CrossApplyWithMultipleCorrelation([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			var query =
				from c in db.Child
				from g in db.GrandChild.Where(g => g.ParentID == c.ParentID && g.ChildID == c.ChildID)
				select new { c.ParentID, c.ChildID, g.GrandChildID };

			AssertQuery(query);
		}

		/// <summary>
		/// OUTER APPLY with two correlation predicates and DefaultIfEmpty.
		/// </summary>
		[Test]
		public void OuterApplyWithMultipleCorrelation([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			var query =
				from c in db.Child
				from g in db.GrandChild
					.Where(g => g.ParentID == c.ParentID && g.ChildID == c.ChildID)
					.DefaultIfEmpty()
				select new { c.ParentID, c.ChildID, GrandChildID = g.GrandChildID };

			AssertQuery(query);
		}

		// =====================================================================
		// APPLY combined with regular JOINs
		// =====================================================================

		/// <summary>
		/// Regular JOIN followed by correlated APPLY.
		/// </summary>
		[Test]
		public void JoinThenApply([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			var query =
				from p in db.Parent
				join c in db.Child on p.ParentID equals c.ParentID
				from g in db.GrandChild
					.Where(g => g.ParentID == c.ParentID && g.ChildID == c.ChildID)
				select new { p.ParentID, c.ChildID, g.GrandChildID };

			AssertQuery(query);
		}

		/// <summary>
		/// Correlated APPLY followed by regular JOIN.
		/// </summary>
		[Test]
		public void ApplyThenJoin([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			var query =
				from p in db.Parent
				from c in db.Child.Where(c => c.ParentID == p.ParentID)
				join g in db.GrandChild on new { c.ParentID, c.ChildID } equals new { ParentID = g.ParentID!.Value, ChildID = g.ChildID!.Value }
				select new { p.ParentID, c.ChildID, g.GrandChildID };

			AssertQuery(query);
		}

		// =====================================================================
		// APPLY with subquery projections (computed columns)
		// =====================================================================

		/// <summary>
		/// APPLY returning computed values (aggregates + expressions).
		/// </summary>
		[Test]
		public void ApplyWithComputedColumns([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			var query =
				from p in db.Parent
				from stats in (
					from c in db.Child
					where c.ParentID == p.ParentID
					group c by c.ParentID into g
					select new
					{
						ChildCount = g.Count(),
						MaxChildID = g.Max(x => x.ChildID),
						MinChildID = g.Min(x => x.ChildID),
					}
				)
				select new { p.ParentID, stats.ChildCount, stats.MaxChildID, stats.MinChildID };

			AssertQuery(query);
		}

		// =====================================================================
		// APPLY with filtered inner query (WHERE + correlation)
		// =====================================================================

		/// <summary>
		/// APPLY with both correlated and non-correlated WHERE predicates.
		/// </summary>
		[Test]
		public void ApplyWithMixedPredicates([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			var query =
				from p in db.Parent
				from c in db.Child
					.Where(c => c.ParentID == p.ParentID && c.ChildID > 2)
				select new { p.ParentID, c.ChildID };

			AssertQuery(query);
		}

		/// <summary>
		/// CROSS APPLY with additional non-correlated filter reducing results.
		/// </summary>
		[Test]
		public void CrossApplyWithAdditionalFilter([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			var query =
				from p in db.Parent
				from c in db.Child
					.Where(c => c.ParentID == p.ParentID && c.ChildID > 20)
				select new { p.ParentID, c.ChildID };

			AssertQuery(query);
		}

		// =====================================================================
		// APPLY with AsSubQuery() forcing subquery materialization
		// =====================================================================

		/// <summary>
		/// APPLY with explicit AsSubQuery to force subquery form.
		/// </summary>
		[Test]
		public void ApplyWithAsSubQuery([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			var query =
				from p in db.Parent
				from c in db.Child
					.Where(c => c.ParentID == p.ParentID)
					.OrderBy(c => c.ChildID)
					.Select(c => new { c.ChildID, c.ParentID })
					.AsSubQuery()
				select new { p.ParentID, p.Value1, c.ChildID };

			AssertQuery(query);
		}

		/// <summary>
		/// Nested subquery APPLY: inner subquery contains another subquery.
		/// </summary>
		[Test]
		public void NestedSubqueryApply([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			var query =
				from p in db.Parent
				from x in (
					from c in db.Child
					where c.ParentID == p.ParentID
					select new
					{
						c.ChildID,
						GrandChildCount = db.GrandChild.Count(g => g.ParentID == c.ParentID && g.ChildID == c.ChildID)
					}
				)
				select new { p.ParentID, x.ChildID, x.GrandChildCount };

			AssertQuery(query);
		}
	}
}
