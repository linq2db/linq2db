using System;
using System.Linq;
using System.Linq.Expressions;

using FluentAssertions;

using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.Internal.Linq;
using LinqToDB.Tools;

using NUnit.Framework;

using Tests.Model;

namespace Tests.Linq
{
	[TestFixture]
	public class InSubqueryTests : TestBase
	{
		#region Helper functions

		[ExpressionMethod(nameof(UseInQueryNullableImpl))]
		static bool UseInQuery<T>(T? value, bool compareNullsAsValues)
			where T: struct
		{
			throw new NotImplementedException();
		}

		[ExpressionMethod(nameof(UseInQueryImpl))]
		static bool UseInQuery<T>(T value, bool compareNullsAsValues)
			where T: struct
		{
			throw new NotImplementedException();
		}

		static Expression<Func<T?, bool, bool>> UseInQueryNullableImpl<T>()
			where T: struct
		{
			return (value, compareNullsAsValues) => compareNullsAsValues || value != null;
		}

		static Expression<Func<T, bool, bool>> UseInQueryImpl<T>()
			where T: struct
		{
			return (value, compareNullsAsValues) => compareNullsAsValues || Sql.ToNullable(value) != null;
		}

		ITestDataContext GetDataContext(string context, bool preferExists, bool compareNullsAsValues)
		{
			return GetDataContext(context, o => o.UsePreferExistsForScalar(preferExists).UseCompareNulls(compareNullsAsValues ? CompareNulls.LikeClr : CompareNulls.LikeSql));
		}

		void AssertTest<T>(IQueryable<T> query, bool preferExists)
		{
			var db = Internals.GetDataContext(query) ?? throw new InvalidOperationException("Could not retrieve data context");

			AssertQuery(query);

			var sqlStr = query.ToSqlQuery().Sql;

			preferExists = preferExists || db.SqlProviderFlags.IsExistsPreferableForContains;

			if (preferExists && db.SqlProviderFlags.SupportedCorrelatedSubqueriesLevel != 0)
				Assert.That(sqlStr, Is.Not.Contains(" IN ").And.Contains("EXISTS"));
			else
				Assert.That(sqlStr, Is.Not.Contains("EXISTS").And.Contains(" IN "));
		}

		#endregion

		[Test]
		public void InTest([DataSources] string context, [Values] bool preferExists, [Values] bool compareNullsAsValues)
		{
			using var db = GetDataContext(context, preferExists, compareNullsAsValues);

			var query =
				from c in db.Child
				where c.ParentID.In(db.Parent.Select(p => p.ParentID).Where(v => UseInQuery(v, compareNullsAsValues)))
				select c;

			AssertTest(query, preferExists);
		}

		[Test]
		public void InTest2([DataSources] string context, [Values] bool preferExists, [Values] bool compareNullsAsValues)
		{
			using var db = GetDataContext(context, preferExists, compareNullsAsValues);

			var query =
				from c in db.GrandChild
				where UseInQuery(c.ParentID, compareNullsAsValues)
					&& c.ParentID.In(db.Parent.Select(p => p.Value1).Where(v => UseInQuery(v, compareNullsAsValues)))
				select c;

			AssertTest(query, true);
		}

		[Test]
		public void InConstTest([DataSources] string context, [Values] bool preferExists, [Values] bool compareNullsAsValues)
		{
			using var db = GetDataContext(context, preferExists, compareNullsAsValues);

			var query =
				from c in db.Parent
				where db.Parent.Select(p => p.Value1).Contains(1)
				select c;

			AssertTest(query, preferExists);
		}

		[Test]
		public void InWithTakeTest([DataSources] string context, [Values] bool preferExists, [Values] bool compareNullsAsValues)
		{
			using var db = GetDataContext(context, preferExists, compareNullsAsValues);

			var query =
				from c in db.Child
				where UseInQuery(c.ParentID, compareNullsAsValues) && c.ParentID.In(db.Parent.Select(p => p.ParentID).Where(v => UseInQuery(v, compareNullsAsValues)).Take(100))
				select c;

			AssertTest(query, preferExists);
		}

		[Test]
		public void ObjectInTest([DataSources] string context, [Values] bool preferExists, [Values] bool compareNullsAsValues)
		{
			using var db = GetDataContext(context, preferExists, compareNullsAsValues);

			var query =
				from c in db.Child
				where new { c.ParentID, Value = c.ParentID }.In(db.Parent.Select(p => new { p.ParentID, Value = p.Value1 ?? -1 }))
				select c;

			if (db.SqlProviderFlags.SupportedCorrelatedSubqueriesLevel == 0)
			{
				FluentActions.Invoking(() => AssertQuery(query)).Should().Throw<LinqToDBException>();
			}
			else
			{
				AssertQuery(query);
			}
		}

		[Test]
		public void ObjectInWithTakeTest([DataSources] string context, [Values] bool preferExists, [Values] bool compareNullsAsValues)
		{
			using var db = GetDataContext(context, preferExists, compareNullsAsValues);

			var query =
				from c in db.Child
				where new { c.ParentID, Value = c.ParentID }.In(db.Parent.Select(p => new { p.ParentID, Value = p.Value1!.Value }).Take(100))
				select c;

			if (db.SqlProviderFlags.SupportedCorrelatedSubqueriesLevel == 0)
			{
				FluentActions.Invoking(() => AssertQuery(query)).Should().Throw<LinqToDBException>();
			}
			else
			{
				AssertQuery(query);
			}
		}

		[Test]
		public void ContainsTest([DataSources(TestProvName.AllAccess)] string context, [Values] bool preferExists, [Values] bool compareNullsAsValues)
		{
			using var db = GetDataContext(context, preferExists, compareNullsAsValues);

			var res = db.Child.Select(c => c.ParentID).Contains(1);

			Assert.That(res, Is.True);

			if (compareNullsAsValues == false)
				Assert.That(LastQuery, Is.Not.Contains(" IS NULL").And.Not.Contains("IS NOT NULL"));

			if ((preferExists || db.SqlProviderFlags.IsExistsPreferableForContains) && db.SqlProviderFlags.SupportedCorrelatedSubqueriesLevel != 0)
				Assert.That(LastQuery, Is.Not.Contains(" IN ").And.Contains("EXISTS"));
			else
				Assert.That(LastQuery, Is.Not.Contains("EXISTS").And.Contains(" IN "));
		}

		[Test]
		public void ContainsExprTest([DataSources] string context, [Values] bool preferExists, [Values] bool compareNullsAsValues)
		{
			using var db = GetDataContext(context, preferExists, compareNullsAsValues);

			var n = 1;

			var query =
				from p in db.Parent
				where db.Child.Select(c => c.ParentID).Contains(p.ParentID + n)
				select p;

			AssertTest(query, preferExists);;
		}

		[Test]
		public void ContainsNullTest([DataSources] string context, [Values] bool preferExists)
		{
			using var db = GetDataContext(context, o => o.UsePreferExistsForScalar(preferExists));

			_ = db.Parent.Select(c => c.Value1).Contains(null);
		}

		[Test]
		public void NotNull_In_NotNull_Test([DataSources] string context, [Values] bool preferExists, [Values] bool compareNullsAsValues)
		{
			using var db = GetDataContext(context, preferExists, compareNullsAsValues);

			using var t1 = db.CreateLocalTable("test_in_1", new[] { 1, 2, 4 }.Select(i => new { ID = i }));
			using var t2 = db.CreateLocalTable("test_in_2", new[] { 1, 2, 3 }.Select(i => new { ID = i }));

			var query = t1.Where(t => t.ID.In(t2.Select(p => p.ID)));

			AssertTest(query, preferExists);
		}

		[Test]
		public void NotNull_NotIn_NotNull_Test([DataSources] string context, [Values] bool preferExists, [Values] bool compareNullsAsValues)
		{
			using var db = GetDataContext(context, preferExists, compareNullsAsValues);

			using var t1 = db.CreateLocalTable("test_in_1", new[] { 1, 3 }.Select(i => new { ID = i }));
			using var t2 = db.CreateLocalTable("test_in_2", new[] { 1, 2 }.Select(i => new { ID = i }));

			var query = t1.Where(t => t.ID.NotIn(t2.Select(p => p.ID))).OrderBy(i => i);

			AssertTest(query, preferExists);
		}

		[Test]
		public void Null_In_NotNull_Test([DataSources] string context, [Values] bool preferExists, [Values] bool compareNullsAsValues)
		{
			using var db = GetDataContext(context, preferExists, compareNullsAsValues);

			using var t1 = db.CreateLocalTable("test_in_1", new[] { (int?)1, 3, null }.Select(i => new { ID = i }));
			using var t2 = db.CreateLocalTable("test_in_2", new[] {       1, 2       }.Select(i => new { ID = i }));

			var query = t1.Where(t => t.ID.In(t2.Select(p => (int?)p.ID)));

			AssertTest(query, preferExists);
		}

		[Test]
		public void Null_NotIn_NotNull_Test1([DataSources] string context, [Values] bool preferExists, [Values] bool compareNullsAsValues)
		{
			using var db = GetDataContext(context, preferExists, compareNullsAsValues);

			using var t1 = db.CreateLocalTable("test_in_1", new[] { (int?)1, 3, null }.Select(i => new { ID = i }));
			using var t2 = db.CreateLocalTable("test_in_2", new[] {       1, 2       }.Select(i => new { ID = i }));

			var query = t1.Where(t => (compareNullsAsValues ? t.ID == null : false) || t.ID.NotIn(t2.Select(p => (int?)p.ID)));

			AssertTest(query, preferExists);
		}

		[Test]
		public void Null_NotIn_NotNull_Test2([DataSources] string context, [Values] bool preferExists, [Values] bool compareNullsAsValues)
		{
			using var db = GetDataContext(context, preferExists, compareNullsAsValues);

			using var t1 = db.CreateLocalTable("test_in_1", new[] { (int?)1, 3 }.Select(i => new { ID = i }));
			using var t2 = db.CreateLocalTable("test_in_2", new[] {       1, 2 }.Select(i => new { ID = i }));

			var query = t1.Where(t => UseInQuery(t.ID, compareNullsAsValues) && t.ID.NotIn(t2.Select(p => (int?)p.ID).Where(v => UseInQuery(v, compareNullsAsValues))));

			AssertTest(query, preferExists);
		}

		[Test]
		public void NotNull_In_Null_Test([DataSources] string context, [Values] bool preferExists, [Values] bool compareNullsAsValues)
		{
			using var db = GetDataContext(context, preferExists, compareNullsAsValues);

			using var t1 = db.CreateLocalTable("test_in_1", new[] {       1, 3       }.Select(i => new { ID = i }));
			using var t2 = db.CreateLocalTable("test_in_2", new[] { (int?)1, 2, null }.Select(i => new { ID = i }));

			var query = t1.Where(t => ((int?)t.ID).In(t2.Select(p => p.ID)));

			AssertTest(query, preferExists);
		}

		[Test]
		public void Null_In_Null_Test1([DataSources] string context, [Values] bool preferExists, [Values] bool compareNullsAsValues)
		{
			using var db = GetDataContext(context, o => o
				.UsePreferExistsForScalar(preferExists)
				.UseCompareNulls(compareNullsAsValues ? CompareNulls.LikeClr : CompareNulls.LikeSql)
				.UseBulkCopyType(BulkCopyType.MultipleRows));

			using var t1 = db.CreateLocalTable("test_in_1", new[] { (int?)1, 3, null }.Select(i => new { ID = i }));
			using var t2 = db.CreateLocalTable("test_in_2", new[] { (int?)1, 2, null }.Select(i => new { ID = i }));

			var query = t1.Where(t => UseInQuery(t.ID, compareNullsAsValues) && t.ID.In(t2.Select(p => p.ID).Where(v => UseInQuery(v, compareNullsAsValues))));

			AssertTest(query, true);
		}

		[Test]
		public void Null_In_Null_Test2([DataSources] string context, [Values] bool preferExists, [Values] bool compareNullsAsValues)
		{
			using var db = GetDataContext(context, preferExists, compareNullsAsValues);

			using var t1 = db.CreateLocalTable("test_in_1", new[] { (int?)1, 3, null }.Select(i => new { ID = i }));
			using var t2 = db.CreateLocalTable("test_in_2", new[] { (int?)1, 2       }.Select(i => new { ID = i }));

			var query = t1.Where(t => UseInQuery(t.ID, compareNullsAsValues) && t.ID.In(t2.Select(p => p.ID).Where(v => UseInQuery(v, compareNullsAsValues))));

			AssertTest(query, true);
		}

		[Test]
		public void Null_In_Null_Test3([DataSources] string context, [Values] bool preferExists, [Values] bool compareNullsAsValues)
		{
			using var db = GetDataContext(context, preferExists, compareNullsAsValues);

			using var t1 = db.CreateLocalTable("test_in_1", new[] { (int?)1, 3,      }.Select(i => new { ID = i }));
			using var t2 = db.CreateLocalTable("test_in_2", new[] { (int?)1, 2, null }.Select(i => new { ID = i }));

			var query = t1.Where(t => t.ID.In(t2.Select(p => p.ID)));

			AssertTest(query, true);
		}

		[Test]
		public void Null_In_Null_Aggregation([DataSources] string context)
		{
			using var db = GetDataContext(context);

			using var t1 = db.CreateLocalTable("test_in_1", new[] { (int?)1, 3,      }.Select(i => new { ID = i }));
			using var t2 = db.CreateLocalTable("test_in_2", new[] { (int?)1, 2, null }.Select(i => new { ID = i, GV = i % 2 }));

			var subQuery =
				from t in t2
				group t by t.GV
				into g
				select g.Min(x => x.ID);

			var query = t1.Where(t => t.ID.In(subQuery));

			AssertQuery(query);
		}

		[Test]
		public void Null_NotIn_Null_Test1([DataSources] string context, [Values] bool preferExists, [Values] bool compareNullsAsValues)
		{
			using var db = GetDataContext(context, preferExists, compareNullsAsValues);

			using var t1 = db.CreateLocalTable("test_in_1", new[] { (int?)1, 3, 4, 5, null }.Select(i => new { ID = i }));
			using var t2 = db.CreateLocalTable("test_in_2", new[] { (int?)1, 2, 4, 6, null }.Select(i => new { ID = i }));

			var query = t1.Where(t => UseInQuery(t.ID, compareNullsAsValues) && t.ID.NotIn(t2.Select(p => p.ID).Where(v => UseInQuery(v, compareNullsAsValues))));

			AssertTest(query, true);
		}

		[Test]
		public void Null_NotIn_Null_Test2([DataSources] string context, [Values] bool preferExists, [Values] bool compareNullsAsValues)
		{
			using var db = GetDataContext(context, preferExists, compareNullsAsValues);

			using var t1 = db.CreateLocalTable("test_in_1", ((int?[])[ 1, 3, 4, 5       ]).Select(i => new { ID = i }));
			using var t2 = db.CreateLocalTable("test_in_2", ((int?[])[ 1, 2, 4, 6, null ]).Select(i => new { ID = i }));

			var query = t1.Where(t => t.ID.NotIn(t2.Select(p => p.ID)));

			AssertTest(query, true);
		}

		[Test]
		public void Null_NotIn_Null_Test3([DataSources] string context, [Values] bool preferExists, [Values] bool compareNullsAsValues)
		{
			using var db = GetDataContext(context, preferExists, compareNullsAsValues);

			using var t1 = db.CreateLocalTable("test_in_1", new[] { (int?)1, 3, 4, 5, null }.Select(i => new { ID = i }));
			using var t2 = db.CreateLocalTable("test_in_2", new[] { (int?)1, 2, 4, 6       }.Select(i => new { ID = i }));

			var query = t1.Where(t => t.ID.NotIn(t2.Select(p => p.ID)));

			AssertTest(query, true);
		}
	}
}
