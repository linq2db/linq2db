using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Tools;

using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	public class InSubqueryTests : TestBase
	{
		[Test]
		public void InTest([DataSources] string context, [Values(true, false)] bool preferExists)
		{
			using var db = GetDataContext(context, o => o.WithOptions<LinqOptions>(lo => lo with { PreferExistsForScalar = preferExists }));

			var q =
				from c in db.Child
				where c.ParentID.In(db.Parent.Select(p => p.ParentID))
				select c;

			_ = q.ToList();

			if (!preferExists && db is DataConnection dc)
				Assert.That(dc.LastQuery, Contains.Substring(" IN (").And.Not.Contains("EXISTS("));
		}

		[Test]
		public void InNullTest([DataSources] string context, [Values(true, false)] bool preferExists)
		{
			using var db = GetDataContext(context, o => o.WithOptions<LinqOptions>(lo => lo with { PreferExistsForScalar = preferExists }));

			var q =
				from c in db.Parent
				where db.Parent.Select(p => p.Value1).Contains(null)
				select c;

			_ = q.ToList();

			if (!preferExists && db is DataConnection dc)
				Assert.That(dc.LastQuery, Contains.Substring(" IN (").And.Not.Contains("EXISTS("));
		}

		[Test]
		public void InWithTakeTest([DataSources] string context, [Values(true, false)] bool preferExists)
		{
			using var db = GetDataContext(context, o => o.WithOptions<LinqOptions>(lo => lo with { PreferExistsForScalar = preferExists }));

			var q =
				from c in db.Child
				where c.ParentID.In(db.Parent.Select(p => p.ParentID).Take(100))
				select c;

			_ = q.ToList();

			if (!preferExists && db is DataConnection dc)
				Assert.That(dc.LastQuery, Contains.Substring(" IN (").And.Not.Contains("EXISTS("));
		}

		[Test]
		public void ObjectInTest([DataSources(TestProvName.AllClickHouse)] string context, [Values(true, false)] bool preferExists)
		{
			using var db = GetDataContext(context, o => o.WithOptions<LinqOptions>(lo => lo with { PreferExistsForScalar = preferExists }));

			var q =
				from c in db.Child
				where new { c.ParentID, Value = c.ParentID }.In(db.Parent.Select(p => new { p.ParentID, p.Value1!.Value }))
				select c;

			_ = q.ToList();
		}

		[Test]
		public void ObjectInWithTakeTest([DataSources(TestProvName.AllClickHouse)] string context, [Values(true, false)] bool preferExists)
		{
			using var db = GetDataContext(context, o => o.WithOptions<LinqOptions>(lo => lo with { PreferExistsForScalar = preferExists }));

			var q =
				from c in db.Child
				where new { c.ParentID, Value = c.ParentID }.In(db.Parent.Select(p => new { p.ParentID, p.Value1!.Value }).Take(100))
				select c;

			_ = q.ToList();
		}

		[Test]
		public void ContainsTest([DataSources] string context, [Values(true, false)] bool preferExists)
		{
			using var db = GetDataContext(context, o => o.WithOptions<LinqOptions>(lo => lo with { PreferExistsForScalar = preferExists }));

			var res = db.Child.Select(c => c.ParentID).Contains(1);

			Assert.IsTrue(res);
		}

		[Test]
		public void ContainsExprTest([DataSources] string context, [Values(true, false)] bool preferExists)
		{
			using var db = GetDataContext(context, o => o.WithOptions<LinqOptions>(lo => lo with { PreferExistsForScalar = preferExists }));

			var n = 1;
			var q = from p in db.Parent where db.Child.Select(c => c.ParentID).Contains(p.ParentID + n) select p;

			_ = q.ToList();
		}

		[Test]
		public void ContainsNullTest([DataSources] string context, [Values(true, false)] bool preferExists)
		{
			using var db = GetDataContext(context, o => o.WithOptions<LinqOptions>(lo => lo with { PreferExistsForScalar = preferExists }));

			_ = db.Parent.Select(c => c.Value1).Contains(null);
		}
	}
}
