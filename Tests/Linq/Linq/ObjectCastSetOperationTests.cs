using System.Collections.Generic;
using System.Linq;

using LinqToDB;

using NUnit.Framework;

using Shouldly;

namespace Tests.Linq
{
	[TestFixture]
	public class ObjectCastSetOperationTests : TestBase
	{
		// AssertQuery's default comparer walks the members of `object`, finds none, and folds the empty set
		// into an always-true equality - leaving only the row count, which is already right when members are
		// dropped. Every object-typed query below is asserted with structural equality instead.
		static IEqualityComparer<object> Structural => EqualityComparer<object>.Default;

		// https://github.com/linq2db/linq2db/issues/5916 - an (object)-cast set-operation branch must keep members evaluated client-side
		[Test(Description = "https://github.com/linq2db/linq2db/issues/5916")]
		public void ConcatObjectCastKeepsClientSideMember([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var query =
				(from p in db.Person where p.ID > 2 select (object)new { id = "p_" + p.ID.ToString("X"), name = p.FirstName })
				.Concat(
				 from p in db.Person where p.ID <= 2 select (object)new { id = "q_" + p.ID.ToString("X"), name = p.FirstName });

			var result = AssertQuery(query, Structural);

			// the whole defect in one line: the projection loses `id`, and the row is materialized as the
			// single surviving column's value
			result.ShouldAllBe(x => x.GetType() != typeof(string));
		}

		// https://github.com/linq2db/linq2db/issues/5916 - the (object)-cast form must read the same columns as the uncast form
		[Test(Description = "https://github.com/linq2db/linq2db/issues/5916")]
		public void ConcatObjectCastSelectsSameColumnsAsUncast([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var uncast =
				(from p in db.Person where p.ID > 2 select new { id = "p_" + p.ID.ToString("X"), name = p.FirstName })
				.Concat(
				 from p in db.Person where p.ID <= 2 select new { id = "q_" + p.ID.ToString("X"), name = p.FirstName });

			var cast =
				(from p in db.Person where p.ID > 2 select (object)new { id = "p_" + p.ID.ToString("X"), name = p.FirstName })
				.Concat(
				 from p in db.Person where p.ID <= 2 select (object)new { id = "q_" + p.ID.ToString("X"), name = p.FirstName });

			// compared against the uncast form rather than a literal, so the expectation cannot be tuned to
			// whatever the run happens to produce
			cast.GetSelectQuery()!.Select.Columns.Count.ShouldBe(uncast.GetSelectQuery()!.Select.Columns.Count);
		}

		// https://github.com/linq2db/linq2db/issues/5916 - differently shaped (object)-cast branches keep their own members
		[Test(Description = "https://github.com/linq2db/linq2db/issues/5916")]
		public void ConcatObjectCastDifferentShapes([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var query =
				(from p in db.Person where p.ID > 2 select (object)new { id = "p_" + p.ID.ToString("X"), name = p.FirstName })
				.Concat(
				 from p in db.Person where p.ID <= 2 select (object)new { key = "c_" + p.ID.ToString("X"), first = p.FirstName, last = p.LastName });

			var result = AssertQuery(query, Structural);

			result.ShouldAllBe(x => x.GetType() != typeof(string));
		}

		// https://github.com/linq2db/linq2db/issues/5916 - UnionAll behaves as Concat does
		[Test(Description = "https://github.com/linq2db/linq2db/issues/5916")]
		public void UnionAllObjectCastKeepsClientSideMember([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var query =
				(from p in db.Person where p.ID > 2 select (object)new { id = "p_" + p.ID.ToString("X"), name = p.FirstName })
				.UnionAll(
				 from p in db.Person where p.ID <= 2 select (object)new { id = "q_" + p.ID.ToString("X"), name = p.FirstName });

			var result = AssertQuery(query, Structural);

			result.ShouldAllBe(x => x.GetType() != typeof(string));
		}

		// https://github.com/linq2db/linq2db/issues/5916 - the dropped member is not specific to a formatted key column: one computed from a translated expression (Length) is dropped too
		[Test(Description = "https://github.com/linq2db/linq2db/issues/5916")]
		public void ConcatObjectCastStringLengthClientSideMember([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var query =
				(from p in db.Person where p.ID > 2 select (object)new { id = p.FirstName.Length.ToString("X"), name = p.FirstName })
				.Concat(
				 from p in db.Person where p.ID <= 2 select (object)new { id = p.LastName.Length.ToString("X"), name = p.FirstName });

			var result = AssertQuery(query, Structural);

			result.ShouldAllBe(x => x.GetType() != typeof(string));
		}

		// https://github.com/linq2db/linq2db/issues/5916 - `as object` is rewritten into the same conversion and reaches the same site
		[Test(Description = "https://github.com/linq2db/linq2db/issues/5916")]
		public void ConcatObjectCastThroughAs([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var query =
				(from p in db.Person where p.ID > 2 select new { id = "p_" + p.ID.ToString("X"), name = p.FirstName } as object)
				.Concat(
				 from p in db.Person where p.ID <= 2 select new { id = "q_" + p.ID.ToString("X"), name = p.FirstName } as object);

			var result = AssertQuery(query, Structural);

			result.ShouldAllBe(x => x.GetType() != typeof(string));
		}

		// https://github.com/linq2db/linq2db/issues/5916 - the collapse is order-independent: a client-side member on the second operand is dropped too (the issue's case 6, recorded there as passing)
		[Test(Description = "https://github.com/linq2db/linq2db/issues/5916")]
		public void ConcatObjectCastClientSideMemberOnSecondOperand([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var query =
				(from p in db.Person where p.ID > 2 select (object)new { key = "c_" + p.ID.ToString("X"), first = p.FirstName, last = p.LastName })
				.Concat(
				 from p in db.Person where p.ID <= 2 select (object)new { id = "p_" + p.ID.ToString("X"), name = p.FirstName });

			var result = AssertQuery(query, Structural);

			result.ShouldAllBe(x => x.GetType() != typeof(string));
		}

		// https://github.com/linq2db/linq2db/issues/5916 - control: every member translates, so nothing is at risk
		[Test(Description = "https://github.com/linq2db/linq2db/issues/5916")]
		public void ConcatObjectCastTranslatableMembers([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var query =
				(from p in db.Person where p.ID > 2 select (object)new { id = p.ID, name = p.FirstName })
				.Concat(
				 from p in db.Person where p.ID <= 2 select (object)new { id = p.ID, name = p.LastName });

			var result = AssertQuery(query, Structural);

			result.ShouldAllBe(x => x.GetType() != typeof(string));
		}

		// https://github.com/linq2db/linq2db/issues/5916 - control: a third member leaves more than one column standing
		[Test(Description = "https://github.com/linq2db/linq2db/issues/5916")]
		public void ConcatObjectCastThreeMembers([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var query =
				(from p in db.Person where p.ID > 2 select (object)new { id = "p_" + p.ID.ToString("X"), name = p.FirstName, last = p.LastName })
				.Concat(
				 from p in db.Person where p.ID <= 2 select (object)new { id = "q_" + p.ID.ToString("X"), name = p.FirstName, last = p.LastName });

			var result = AssertQuery(query, Structural);

			result.ShouldAllBe(x => x.GetType() != typeof(string));
		}

		// https://github.com/linq2db/linq2db/issues/5916 - control: the same projection outside a set operation
		[Test(Description = "https://github.com/linq2db/linq2db/issues/5916")]
		public void SelectObjectCastClientSideMember([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var query = from p in db.Person select (object)new { id = "p_" + p.ID.ToString("X"), name = p.FirstName };

			var result = AssertQuery(query, Structural);

			result.ShouldAllBe(x => x.GetType() != typeof(string));
		}

		// https://github.com/linq2db/linq2db/issues/5916 - control: a bare column cast to object stays a single column
		[Test(Description = "https://github.com/linq2db/linq2db/issues/5916")]
		public void ConcatObjectCastScalarMember([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var query =
				(from p in db.Person where p.ID > 2 select (object)p.FirstName)
				.Concat(
				 from p in db.Person where p.ID <= 2 select (object)p.LastName);

			// the operand here IS the placeholder, so the conversion still collapses to that one column -
			// this is the shape the collapse exists for and the one the fix must leave alone
			AssertQuery(query, Structural).ShouldAllBe(x => x.GetType() == typeof(string));

			query.GetSelectQuery()!.Select.Columns.Count.ShouldBe(1);
		}
	}
}
