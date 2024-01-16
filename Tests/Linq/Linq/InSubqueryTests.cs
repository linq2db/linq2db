using System;
using System.Collections.Generic;
using System.Linq;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Tools;

using NUnit.Framework;

using Tests.Model;

namespace Tests.Linq
{
	[TestFixture]
	public class InSubqueryTests : TestBase
	{
		void AssertTest<T>(
			ITestDataContext db,
			bool             preferExists,
			bool             compareNullsAsValues,
			IEnumerable<T>   expected,
			IQueryable<T>    actual)
		{
			if (compareNullsAsValues)
				AreEqual(expected, actual);
			else
				_ = actual.ToList();

			if (compareNullsAsValues == false)
				Assert.That(LastQuery, Is.Not.Contains(" IS NULL").And.Not.Contains("IS NOT NULL"));

			if ((preferExists || db.SqlProviderFlags.IsExistsPreferableForContains) && !db.SqlProviderFlags.DoesNotSupportCorrelatedSubquery)
				Assert.That(LastQuery, Is.Not.Contains(" IN ").And.Contains("EXISTS"));
			else
				Assert.That(LastQuery, Is.Not.Contains("EXISTS").And.Contains(" IN "));
		}

		[Test]
		public void InTest([DataSources] string context, [Values(true, false)] bool preferExists, [Values(true, false)] bool compareNullsAsValues)
		{
			using var db = GetDataContext(context, o => o.WithOptions<LinqOptions>(lo => lo with { PreferExistsForScalar = preferExists, CompareNullsAsValues = compareNullsAsValues }));

			AssertTest(db, preferExists, true,
				from c in Child
				where c.ParentID.In(Parent.Select(p => p.ParentID))
				select c
				,
				from c in db.Child
				where c.ParentID.In(db.Parent.Select(p => p.ParentID))
				select c);
		}

		[Test]
		public void InTest2([DataSources] string context, [Values(true, false)] bool preferExists, [Values(true, false)] bool compareNullsAsValues)
		{
			using var db = GetDataContext(context, o => o.WithOptions<LinqOptions>(lo => lo with { PreferExistsForScalar = preferExists, CompareNullsAsValues = compareNullsAsValues }));

			AssertTest(db, preferExists, true,
				from c in GrandChild
				where c.ParentID.In(Parent.Select(p => p.Value1))
				select c
				,
				from c in db.GrandChild
				where c.ParentID.In(db.Parent.Select(p => p.Value1))
				select c);
		}

		[Test]
		public void InConstTest([DataSources] string context, [Values(true, false)] bool preferExists, [Values(true, false)] bool compareNullsAsValues)
		{
			using var db = GetDataContext(context, o => o.WithOptions<LinqOptions>(lo => lo with { PreferExistsForScalar = preferExists, CompareNullsAsValues = compareNullsAsValues }));

			AssertTest(db, preferExists, true,
				from c in Parent
				where Parent.Select(p => p.Value1).Contains(1)
				select c
				,
				from c in db.Parent
				where db.Parent.Select(p => p.Value1).Contains(1)
				select c);
		}

		[Test]
		public void InWithTakeTest([DataSources] string context, [Values(true, false)] bool preferExists, [Values(true, false)] bool compareNullsAsValues)
		{
			using var db = GetDataContext(context, o => o.WithOptions<LinqOptions>(lo => lo with { PreferExistsForScalar = preferExists, CompareNullsAsValues = compareNullsAsValues }));

			AssertTest(db, preferExists, true,
				from c in Child
				where c.ParentID.In(Parent.Select(p => p.ParentID).Take(100))
				select c
				,
				from c in db.Child
				where c.ParentID.In(db.Parent.Select(p => p.ParentID).Take(100))
				select c);
		}

		[Test]
		public void ObjectInTest([DataSources(TestProvName.AllClickHouse)] string context, [Values(true, false)] bool preferExists, [Values(true, false)] bool compareNullsAsValues)
		{
			using var db = GetDataContext(context, o => o.WithOptions<LinqOptions>(lo => lo with { PreferExistsForScalar = preferExists, CompareNullsAsValues = compareNullsAsValues }));

			AssertTest(db, preferExists, true,
				from c in Child
				where new { c.ParentID, Value = c.ParentID }.In(Parent.Select(p => new { p.ParentID, p.Value1!.Value }))
				select c
				,
				from c in db.Child
				where new { c.ParentID, Value = c.ParentID }.In(db.Parent.Select(p => new { p.ParentID, p.Value1!.Value }))
				select c);
		}

		[Test]
		public void ObjectInWithTakeTest([DataSources(TestProvName.AllClickHouse)] string context, [Values(true, false)] bool preferExists, [Values(true, false)] bool compareNullsAsValues)
		{
			using var db = GetDataContext(context, o => o.WithOptions<LinqOptions>(lo => lo with { PreferExistsForScalar = preferExists, CompareNullsAsValues = compareNullsAsValues }));

			AssertTest(db, preferExists, true,
				from c in Child
				where new { c.ParentID, Value = c.ParentID }.In(Parent.Select(p => new { p.ParentID, p.Value1!.Value }).Take(100))
				select c
				,
				from c in db.Child
				where new { c.ParentID, Value = c.ParentID }.In(db.Parent.Select(p => new { p.ParentID, p.Value1!.Value }).Take(100))
				select c);
		}

		[Test]
		public void ContainsTest([DataSources] string context, [Values(true, false)] bool preferExists, [Values(true, false)] bool compareNullsAsValues)
		{
			using var db = GetDataContext(context, o => o.WithOptions<LinqOptions>(lo => lo with { PreferExistsForScalar = preferExists, CompareNullsAsValues = compareNullsAsValues }));

			var res = db.Child.Select(c => c.ParentID).Contains(1);

			Assert.IsTrue(res);

			if (compareNullsAsValues == false)
				Assert.That(LastQuery, Is.Not.Contains(" IS NULL").And.Not.Contains("IS NOT NULL"));

			if ((preferExists || db.SqlProviderFlags.IsExistsPreferableForContains) && !db.SqlProviderFlags.DoesNotSupportCorrelatedSubquery)
				Assert.That(LastQuery, Is.Not.Contains(" IN ").And.Contains("EXISTS"));
			else
				Assert.That(LastQuery, Is.Not.Contains("EXISTS").And.Contains(" IN "));
		}

		[Test]
		public void ContainsExprTest([DataSources] string context, [Values(true, false)] bool preferExists, [Values(true, false)] bool compareNullsAsValues)
		{
			using var db = GetDataContext(context, o => o.WithOptions<LinqOptions>(lo => lo with { PreferExistsForScalar = preferExists, CompareNullsAsValues = compareNullsAsValues }));

			var n = 1;

			AssertTest(db, preferExists, true,
				from p in    Parent where    Child.Select(c => c.ParentID).Contains(p.ParentID + n) select p,
				from p in db.Parent where db.Child.Select(c => c.ParentID).Contains(p.ParentID + n) select p);
		}

		[Test]
		public void ContainsNullTest([DataSources] string context, [Values(true, false)] bool preferExists)
		{
			using var db = GetDataContext(context, o => o.WithOptions<LinqOptions>(lo => lo.WithPreferExistsForScalar(preferExists)));

			_ = db.Parent.Select(c => c.Value1).Contains(null);
		}

		[Test]
		public void NotNull_In_NotNull_Test([DataSources] string context, [Values(true, false)] bool preferExists, [Values(true, false)] bool compareNullsAsValues)
		{
			using var db = GetDataContext(context, o => o.UsePreferExistsForScalar(preferExists).UseCompareNullsAsValues(compareNullsAsValues));

			using var t1 = db.CreateLocalTable("test_in_1", new[] { 1, 2, 4 }.Select(i => new { ID = i }));
			using var t2 = db.CreateLocalTable("test_in_2", new[] { 1, 2, 3 }.Select(i => new { ID = i }));

			AssertTest(db, preferExists, true,
				t1.ToList().Where(t => t.ID.In(t2.ToList().Select(p => p.ID))),
				t1.         Where(t => t.ID.In(t2.         Select(p => p.ID))));
		}

		[Test]
		public void NotNull_NotIn_NotNull_Test([DataSources] string context, [Values(true, false)] bool preferExists, [Values(true, false)] bool compareNullsAsValues)
		{
			using var db = GetDataContext(context, o => o.UsePreferExistsForScalar(preferExists).UseCompareNullsAsValues(compareNullsAsValues));

			using var t1 = db.CreateLocalTable("test_in_1", new[] { 1, 3 }.Select(i => new { ID = i }));
			using var t2 = db.CreateLocalTable("test_in_2", new[] { 1, 2 }.Select(i => new { ID = i }));

			AssertTest(db, preferExists, true,
				t1.ToList().Where(t => t.ID.NotIn(t2.ToList().Select(p => p.ID))).OrderBy(i => i),
				t1.         Where(t => t.ID.NotIn(t2.         Select(p => p.ID))).OrderBy(i => i));
		}

		[Test]
		public void Null_In_NotNull_Test([DataSources] string context, [Values(true, false)] bool preferExists, [Values(true, false)] bool compareNullsAsValues)
		{
			using var db = GetDataContext(context, o => o.UsePreferExistsForScalar(preferExists).UseCompareNullsAsValues(compareNullsAsValues));

			using var t1 = db.CreateLocalTable("test_in_1", new[] { (int?)1, 3, null }.Select(i => new { ID = i }));
			using var t2 = db.CreateLocalTable("test_in_2", new[] {       1, 2       }.Select(i => new { ID = i }));

			AssertTest(db, preferExists, true,
				t1.ToList().Where(t => t.ID.In(t2.ToList().Select(p => (int?)p.ID))),
				t1.         Where(t => t.ID.In(t2.         Select(p => (int?)p.ID))));
		}

		[Test]
		public void Null_NotIn_NotNull_Test1([DataSources] string context, [Values(true, false)] bool preferExists, [Values(true, false)] bool compareNullsAsValues)
		{
			using var db = GetDataContext(context, o => o.UsePreferExistsForScalar(preferExists).UseCompareNullsAsValues(compareNullsAsValues));

			using var t1 = db.CreateLocalTable("test_in_1", new[] { (int?)1, 3, null }.Select(i => new { ID = i }));
			using var t2 = db.CreateLocalTable("test_in_2", new[] {       1, 2       }.Select(i => new { ID = i }));

			AssertTest(db, preferExists, compareNullsAsValues,
				t1.ToList().Where(t => t.ID.NotIn(t2.ToList().Select(p => (int?)p.ID))),
				t1.         Where(t => t.ID.NotIn(t2.         Select(p => (int?)p.ID))));
		}

		[Test]
		public void Null_NotIn_NotNull_Test2([DataSources] string context, [Values(true, false)] bool preferExists, [Values(true, false)] bool compareNullsAsValues)
		{
			using var db = GetDataContext(context, o => o.UsePreferExistsForScalar(preferExists).UseCompareNullsAsValues(compareNullsAsValues));

			using var t1 = db.CreateLocalTable("test_in_1", new[] { (int?)1, 3 }.Select(i => new { ID = i }));
			using var t2 = db.CreateLocalTable("test_in_2", new[] {       1, 2 }.Select(i => new { ID = i }));

			AssertTest(db, preferExists, true,
				t1.ToList().Where(t => t.ID.NotIn(t2.ToList().Select(p => (int?)p.ID))),
				t1.         Where(t => t.ID.NotIn(t2.         Select(p => (int?)p.ID))));
		}

		[Test]
		public void NotNull_In_Null_Test([DataSources] string context, [Values(true, false)] bool preferExists, [Values(true, false)] bool compareNullsAsValues)
		{
			using var db = GetDataContext(context, o => o.UsePreferExistsForScalar(preferExists).UseCompareNullsAsValues(compareNullsAsValues));

			using var t1 = db.CreateLocalTable("test_in_1", new[] {       1, 3       }.Select(i => new { ID = i }));
			using var t2 = db.CreateLocalTable("test_in_2", new[] { (int?)1, 2, null }.Select(i => new { ID = i }));

			AssertTest(db, preferExists, true,
				t1.ToList().Where(t => ((int?)t.ID).In(t2.ToList().Select(p => p.ID))),
				t1.         Where(t => ((int?)t.ID).In(t2.         Select(p => p.ID))));
		}

		[Test]
		public void Null_In_Null_Test1([DataSources] string context, [Values(true, false)] bool preferExists, [Values(true, false)] bool compareNullsAsValues)
		{
			using var db = GetDataContext(context, o => o.UsePreferExistsForScalar(preferExists).UseCompareNullsAsValues(compareNullsAsValues)
				.UseBulkCopyType(BulkCopyType.MultipleRows));

			using var t1 = db.CreateLocalTable("test_in_1", new[] { (int?)1, 3, null }.Select(i => new { ID = i }));
			using var t2 = db.CreateLocalTable("test_in_2", new[] { (int?)1, 2, null }.Select(i => new { ID = i }));

			AssertTest(db, preferExists, compareNullsAsValues,
				t1.ToList().Where(t => t.ID.In(t2.ToList().Select(p => p.ID))),
				t1.         Where(t => t.ID.In(t2.         Select(p => p.ID))));
		}

		[Test]
		public void Null_In_Null_Test2([DataSources] string context, [Values(true, false)] bool preferExists, [Values(true, false)] bool compareNullsAsValues)
		{
			using var db = GetDataContext(context, o => o.UsePreferExistsForScalar(preferExists).UseCompareNullsAsValues(compareNullsAsValues));

			using var t1 = db.CreateLocalTable("test_in_1", new[] { (int?)1, 3, null }.Select(i => new { ID = i }));
			using var t2 = db.CreateLocalTable("test_in_2", new[] { (int?)1, 2       }.Select(i => new { ID = i }));

			AssertTest(db, preferExists, true,
				t1.ToList().Where(t => t.ID.In(t2.ToList().Select(p => p.ID))),
				t1.         Where(t => t.ID.In(t2.         Select(p => p.ID))));
		}

		[Test]
		public void Null_In_Null_Test3([DataSources] string context, [Values(true, false)] bool preferExists, [Values(true, false)] bool compareNullsAsValues)
		{
			using var db = GetDataContext(context, o => o.UsePreferExistsForScalar(preferExists).UseCompareNullsAsValues(compareNullsAsValues));

			using var t1 = db.CreateLocalTable("test_in_1", new[] { (int?)1, 3,      }.Select(i => new { ID = i }));
			using var t2 = db.CreateLocalTable("test_in_2", new[] { (int?)1, 2, null }.Select(i => new { ID = i }));

			AssertTest(db, preferExists, true,
				t1.ToList().Where(t => t.ID.In(t2.ToList().Select(p => p.ID))),
				t1.         Where(t => t.ID.In(t2.         Select(p => p.ID))));
		}

		[Test]
		public void Null_NotIn_Null_Test1([DataSources] string context, [Values(true, false)] bool preferExists, [Values(true, false)] bool compareNullsAsValues)
		{
			using var db = GetDataContext(context, o => o.UsePreferExistsForScalar(preferExists).UseCompareNullsAsValues(compareNullsAsValues));

			using var t1 = db.CreateLocalTable("test_in_1", new[] { (int?)1, 3, 4, 5, null }.Select(i => new { ID = i }));
			using var t2 = db.CreateLocalTable("test_in_2", new[] { (int?)1, 2, 4, 6, null }.Select(i => new { ID = i }));

			AssertTest(db, preferExists, true,
				t1.ToList().Where(t => t.ID.NotIn(t2.ToList().Select(p => p.ID))),
				t1.         Where(t => t.ID.NotIn(t2.         Select(p => p.ID))));
		}

		[Test]
		public void Null_NotIn_Null_Test2([DataSources] string context, [Values(true, false)] bool preferExists, [Values(true, false)] bool compareNullsAsValues)
		{
			using var db = GetDataContext(context, o => o.UsePreferExistsForScalar(preferExists).UseCompareNullsAsValues(compareNullsAsValues));

			using var t1 = db.CreateLocalTable("test_in_1", new[] { (int?)1, 3, 4, 5       }.Select(i => new { ID = i }));
			using var t2 = db.CreateLocalTable("test_in_2", new[] { (int?)1, 2, 4, 6, null }.Select(i => new { ID = i }));

			AssertTest(db, preferExists, true,
				t1.ToList().Where(t => t.ID.NotIn(t2.ToList().Select(p => p.ID))),
				t1.         Where(t => t.ID.NotIn(t2.         Select(p => p.ID))));
		}

		[Test]
		public void Null_NotIn_Null_Test3([DataSources] string context, [Values(true, false)] bool preferExists, [Values(true, false)] bool compareNullsAsValues)
		{
			using var db = GetDataContext(context, o => o.UsePreferExistsForScalar(preferExists).UseCompareNullsAsValues(compareNullsAsValues));

			using var t1 = db.CreateLocalTable("test_in_1", new[] { (int?)1, 3, 4, 5, null }.Select(i => new { ID = i }));
			using var t2 = db.CreateLocalTable("test_in_2", new[] { (int?)1, 2, 4, 6       }.Select(i => new { ID = i }));

			AssertTest(db, preferExists, true,
				t1.ToList().Where(t => t.ID.NotIn(t2.ToList().Select(p => p.ID))),
				t1.         Where(t => t.ID.NotIn(t2.         Select(p => p.ID))));
		}
	}
}
