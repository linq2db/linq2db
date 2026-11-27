using System;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Internal.Linq;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

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
			throw new ServerSideOnlyException(nameof(UseInQuery));
		}

		[ExpressionMethod(nameof(UseInQueryImpl))]
		static bool UseInQuery<T>(T value, bool compareNullsAsValues)
			where T: struct
		{
			throw new ServerSideOnlyException(nameof(UseInQuery));
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
		public void InWithSkipTest([DataSources(TestProvName.AllSybase, TestProvName.AllNativeAccess, TestProvName.AllSQLite, TestProvName.AllYdb)] string context, [Values] bool preferExists, [Values] bool compareNullsAsValues)
		{
			using var db = GetDataContext(context, preferExists, compareNullsAsValues);

			var query =
				from c in db.Child
				where UseInQuery(c.ParentID, compareNullsAsValues) && c.ParentID.In(db.Parent.Select(p => p.ParentID).Where(v => UseInQuery(v, compareNullsAsValues)).Skip(100))
				select c;

			AssertTest(query, preferExists);
		}

		[Test]
		public void InWithSkipTakeTest([DataSources(TestProvName.AllSybase, TestProvName.AllNativeAccess)] string context, [Values] bool preferExists, [Values] bool compareNullsAsValues)
		{
			using var db = GetDataContext(context, preferExists, compareNullsAsValues);

			var query =
				from c in db.Child
				where UseInQuery(c.ParentID, compareNullsAsValues) && c.ParentID.In(db.Parent.Select(p => p.ParentID).Where(v => UseInQuery(v, compareNullsAsValues)).Skip(100).Take(100))
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
				var act = () => AssertQuery(query);
				act.ShouldThrow<LinqToDBException>();
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
				var act = () => AssertQuery(query);
				act.ShouldThrow<LinqToDBException>();
			}
			else
			{
				AssertQuery(query);
			}
		}

		[Test]
		public void ContainsTest([DataSources(TestProvName.AllNativeAccess)] string context, [Values] bool preferExists, [Values] bool compareNullsAsValues)
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

			AssertTest(query, preferExists);
		}

		[Test]
		public void ContainsNullTest([DataSources] string context, [Values] bool preferExists)
		{
			using var db = GetDataContext(context, o => o.UsePreferExistsForScalar(preferExists));

			_ = db.Parent.Select(c => c.Value1).Contains(null);
		}

		[Test]
		public void ContainsWithUnionKeepsProjectionTest([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var __ = db.UseOptions(o => o.UsePreferExistsForScalar(true));

			var union = db.Parent
				.Where(p => p.ParentID <= 2)
				.Select(p => p.ParentID)
				.Union(
					db.Child
						.Where(c => c.ParentID >= 3)
						.Select(c => c.ParentID));

			var query = db.Parent.Where(p => union.Contains(p.ParentID));

			var actual = query.OrderBy(p => p.ParentID).ToArray();

			var expected = Parent
				.Where(p =>
					Parent.Where(p1 => p1.ParentID <= 2).Select(p1 => p1.ParentID)
						.Union(Child.Where(c => c.ParentID >= 3).Select(c => c.ParentID))
						.Contains(p.ParentID))
				.OrderBy(p => p.ParentID)
				.ToArray();

			AreEqual(expected, actual);
		}

		[Test]
		public void ContainsWithGroupByKeepsProjectionTest([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var __ = db.UseOptions(o => o.UsePreferExistsForScalar(true));

			var grouped = db.Child
				.GroupBy(c => c.ParentID)
				.Select(g => g.Key);

			var query = db.Parent.Where(p => grouped.Contains(p.ParentID));

			var actual = query.OrderBy(p => p.ParentID).ToArray();

			var groupedKeys = Child
				.GroupBy(c => c.ParentID)
				.Select(g => g.Key);

			var expected = Parent
				.Where(p => groupedKeys.Contains(p.ParentID))
				.OrderBy(p => p.ParentID)
				.ToArray();

			AreEqual(expected, actual);
		}

		sealed class TestRecord
		{
			[PrimaryKey]
			public int PK { get; set; }
			public int ID { get; set; }
		}

		sealed class TestRecordNullable
		{
			[PrimaryKey]
			public int PK { get; set; }
			public int? ID { get; set; }
			public int? GV { get; set; }
		}

		[Test]
		public void NotNull_In_NotNull_Test([DataSources] string context, [Values] bool preferExists, [Values] bool compareNullsAsValues)
		{
			using var db = GetDataContext(context, preferExists, compareNullsAsValues);

			using var t1 = db.CreateLocalTable("test_in_1", new[] { 1, 2, 4 }.Select((i, idx) => new TestRecord { PK = idx, ID = i }));
			using var t2 = db.CreateLocalTable("test_in_2", new[] { 1, 2, 3 }.Select((i, idx) => new TestRecord { PK = idx, ID = i }));

			var query = t1.Where(t => t.ID.In(t2.Select(p => p.ID)));

			AssertTest(query, preferExists);
		}

		[Test]
		public void NotNull_NotIn_NotNull_Test([DataSources] string context, [Values] bool preferExists, [Values] bool compareNullsAsValues)
		{
			using var db = GetDataContext(context, preferExists, compareNullsAsValues);

			using var t1 = db.CreateLocalTable("test_in_1", new[] { 1, 3 }.Select((i, idx) => new TestRecord { PK = idx, ID = i }));
			using var t2 = db.CreateLocalTable("test_in_2", new[] { 1, 2 }.Select((i, idx) => new TestRecord { PK = idx, ID = i }));

			var query = t1.Where(t => t.ID.NotIn(t2.Select(p => p.ID))).OrderBy(i => i);

			AssertTest(query, preferExists);
		}

		[Test]
		public void Null_In_NotNull_Test([DataSources] string context, [Values] bool preferExists, [Values] bool compareNullsAsValues)
		{
			using var db = GetDataContext(context, preferExists, compareNullsAsValues);

			using var t1 = db.CreateLocalTable("test_in_1", new[] { (int?)1, 3, null }.Select((i, idx) => new TestRecordNullable { PK = idx, ID = i }));
			using var t2 = db.CreateLocalTable("test_in_2", new[] {       1, 2       }.Select((i, idx) => new TestRecord { PK = idx, ID = i }));

			var query = t1.Where(t => t.ID.In(t2.Select(p => (int?)p.ID)));

			AssertTest(query, preferExists);
		}

		[Test]
		public void Null_NotIn_NotNull_Test1([DataSources] string context, [Values] bool preferExists, [Values] bool compareNullsAsValues)
		{
			using var db = GetDataContext(context, preferExists, compareNullsAsValues);

			using var t1 = db.CreateLocalTable("test_in_1", new[] { (int?)1, 3, null }.Select((i, idx) => new TestRecordNullable { PK = idx, ID = i }));
			using var t2 = db.CreateLocalTable("test_in_2", new[] {       1, 2       }.Select((i, idx) => new TestRecord { PK = idx, ID = i }));

			var query = t1.Where(t => (compareNullsAsValues ? t.ID == null : false) || t.ID.NotIn(t2.Select(p => (int?)p.ID)));

			AssertTest(query, preferExists);
		}

		[Test]
		public void Null_NotIn_NotNull_Test2([DataSources] string context, [Values] bool preferExists, [Values] bool compareNullsAsValues)
		{
			using var db = GetDataContext(context, preferExists, compareNullsAsValues);

			using var t1 = db.CreateLocalTable("test_in_1", new[] { (int?)1, 3 }.Select((i, idx) => new TestRecordNullable { PK = idx, ID = i }));
			using var t2 = db.CreateLocalTable("test_in_2", new[] {       1, 2 }.Select((i, idx) => new TestRecord { PK = idx, ID = i }));

			var query = t1.Where(t => UseInQuery(t.ID, compareNullsAsValues) && t.ID.NotIn(t2.Select(p => (int?)p.ID).Where(v => UseInQuery(v, compareNullsAsValues))));

			AssertTest(query, preferExists);
		}

		[Test]
		public void NotNull_In_Null_Test([DataSources] string context, [Values] bool preferExists, [Values] bool compareNullsAsValues)
		{
			using var db = GetDataContext(context, preferExists, compareNullsAsValues);

			using var t1 = db.CreateLocalTable("test_in_1", new[] {       1, 3       }.Select((i, idx) => new TestRecord { PK = idx, ID = i }));
			using var t2 = db.CreateLocalTable("test_in_2", new[] { (int?)1, 2, null }.Select((i, idx) => new TestRecordNullable { PK = idx, ID = i }));

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

			using var t1 = db.CreateLocalTable("test_in_1", new[] { (int?)1, 3, null }.Select((i, idx) => new TestRecordNullable { PK = idx, ID = i }));
			using var t2 = db.CreateLocalTable("test_in_2", new[] { (int?)1, 2, null }.Select((i, idx) => new TestRecordNullable { PK = idx, ID = i }));

			var query = t1.Where(t => UseInQuery(t.ID, compareNullsAsValues) && t.ID.In(t2.Select(p => p.ID).Where(v => UseInQuery(v, compareNullsAsValues))));

			AssertTest(query, true);
		}

		[Test]
		public void Null_In_Null_Test2([DataSources] string context, [Values] bool preferExists, [Values] bool compareNullsAsValues)
		{
			using var db = GetDataContext(context, preferExists, compareNullsAsValues);

			using var t1 = db.CreateLocalTable("test_in_1", new[] { (int?)1, 3, null }.Select((i, idx) => new TestRecordNullable { PK = idx, ID = i }));
			using var t2 = db.CreateLocalTable("test_in_2", new[] { (int?)1, 2       }.Select((i, idx) => new TestRecordNullable { PK = idx, ID = i }));

			var query = t1.Where(t => UseInQuery(t.ID, compareNullsAsValues) && t.ID.In(t2.Select(p => p.ID).Where(v => UseInQuery(v, compareNullsAsValues))));

			AssertTest(query, true);
		}

		[Test]
		public void Null_In_Null_Test3([DataSources] string context, [Values] bool preferExists, [Values] bool compareNullsAsValues)
		{
			using var db = GetDataContext(context, preferExists, compareNullsAsValues);

			using var t1 = db.CreateLocalTable("test_in_1", new[] { (int?)1, 3,      }.Select((i, idx) => new TestRecordNullable { PK = idx, ID = i }));
			using var t2 = db.CreateLocalTable("test_in_2", new[] { (int?)1, 2, null }.Select((i, idx) => new TestRecordNullable { PK = idx, ID = i }));

			var query = t1.Where(t => t.ID.In(t2.Select(p => p.ID)));

			AssertTest(query, true);
		}

		[Test]
		public void Null_In_Null_Aggregation([DataSources] string context)
		{
			using var db = GetDataContext(context);

			using var t1 = db.CreateLocalTable("test_in_1", new[] { (int?)1, 3,      }.Select((i, idx) => new TestRecordNullable { PK = idx, ID = i }));
			using var t2 = db.CreateLocalTable("test_in_2", new[] { (int?)1, 2, null }.Select((i, idx) => new TestRecordNullable { PK = idx, ID = i, GV = i % 2 }));

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

			using var t1 = db.CreateLocalTable("test_in_1", new[] { (int?)1, 3, 4, 5, null }.Select((i, idx) => new TestRecordNullable { PK = idx, ID = i }));
			using var t2 = db.CreateLocalTable("test_in_2", new[] { (int?)1, 2, 4, 6, null }.Select((i, idx) => new TestRecordNullable { PK = idx, ID = i }));

			var query = t1.Where(t => UseInQuery(t.ID, compareNullsAsValues) && t.ID.NotIn(t2.Select(p => p.ID).Where(v => UseInQuery(v, compareNullsAsValues))));

			AssertTest(query, true);
		}

		[Test]
		public void Null_NotIn_Null_Test2([DataSources] string context, [Values] bool preferExists, [Values] bool compareNullsAsValues)
		{
			using var db = GetDataContext(context, preferExists, compareNullsAsValues);

			using var t1 = db.CreateLocalTable("test_in_1", ((int?[])[ 1, 3, 4, 5       ]).Select((i, idx) => new TestRecordNullable { PK = idx, ID = i }));
			using var t2 = db.CreateLocalTable("test_in_2", ((int?[])[ 1, 2, 4, 6, null ]).Select((i, idx) => new TestRecordNullable { PK = idx, ID = i }));

			var query = t1.Where(t => t.ID.NotIn(t2.Select(p => p.ID)));

			AssertTest(query, true);
		}

		[Test]
		public void Null_NotIn_Null_Test3([DataSources] string context, [Values] bool preferExists, [Values] bool compareNullsAsValues)
		{
			using var db = GetDataContext(context, preferExists, compareNullsAsValues);

			using var t1 = db.CreateLocalTable("test_in_1", new[] { (int?)1, 3, 4, 5, null }.Select((i, idx) => new TestRecordNullable { PK = idx, ID = i }));
			using var t2 = db.CreateLocalTable("test_in_2", new[] { (int?)1, 2, 4, 6       }.Select((i, idx) => new TestRecordNullable { PK = idx, ID = i }));

			var query = t1.Where(t => t.ID.NotIn(t2.Select(p => p.ID)));

			AssertTest(query, true);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4415")]
		public void InSubqueryOverGroupedMaxNullableColumn([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllPostgreSQL, TestProvName.AllAccess, TestProvName.AllOracle)] string configuration)
		{
			var ms = new FluentMappingBuilder()
					.Entity<GroupedMaxLanguage>()
						.HasTableName("Common_Language")
						.Property(e => e.LanguageID).IsNullable()
					.Build()
					.MappingSchema;

			using var db = GetDataContext(configuration, ms);

			using var tbl = db.CreateLocalTable(new[]
				{
					new GroupedMaxLanguage
					{
						LanguageID = "de",
						Name = "deutsch"
					},
					new GroupedMaxLanguage
					{
						Name = "english"
					}
				});

			var p = db.GetTable<GroupedMaxLanguage>();
			var qry = p.GroupBy(x => x.Name).Select(x => x.Max(y => y.LanguageID));
			var qry2 = p.Where(x => qry.Contains(x.LanguageID));
			var lst = qry2.ToList();

			Assert.That(lst, Has.Count.EqualTo(2));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4415")]
		public void InSubqueryOverGroupedMaxNotNullColumn([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllPostgreSQL, TestProvName.AllAccess, TestProvName.AllOracle)] string configuration)
		{
			var ms = new FluentMappingBuilder()
					.Entity<GroupedMaxLanguage>()
						.HasTableName("Common_Language")
						.Property(e => e.LanguageID).IsNotNull()
					.Build()
					.MappingSchema;

			using var db = GetDataContext(configuration, ms);

			using var tbl = db.CreateLocalTable(new[]
				{
					new GroupedMaxLanguage
					{
						LanguageID = "de",
						Name = "deutsch"
					},
				});

			var p = db.GetTable<GroupedMaxLanguage>();
			var qry = p.GroupBy(x => x.Name).Select(x => x.Max(y => y.LanguageID));
			var qry2 = p.Where(x => qry.Contains(x.LanguageID));
			var lst = qry2.ToList();

			Assert.That(lst, Has.Count.EqualTo(1));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4415")]
		public void InSubqueryOverGroupedMaxWithAsNotNull([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllPostgreSQL, TestProvName.AllAccess, TestProvName.AllOracle)] string configuration)
		{
			var ms = new FluentMappingBuilder()
					.Entity<GroupedMaxLanguage>()
						.HasTableName("Common_Language")
						.Property(e => e.LanguageID)
					.Build()
					.MappingSchema;

			using var db = GetDataContext(configuration, ms);

			using var tbl = db.CreateLocalTable(new[]
				{
					new GroupedMaxLanguage
					{
						LanguageID = "de",
						Name = "deutsch"
					},
					new GroupedMaxLanguage
					{
						Name = "english"
					}
				});

			var p = db.GetTable<GroupedMaxLanguage>();
			var qry = p.GroupBy(x => x.Name).Select(x => x.Max(y => y.LanguageID));
			var qry2 = p.Where(x => qry.Contains(Sql.AsNotNull(x.LanguageID)));
			var lst = qry2.ToList();

			Assert.That(lst, Has.Count.EqualTo(1));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4415")]
		public void InSubqueryOverGroupedMaxConcatenatedColumn([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllPostgreSQL, TestProvName.AllAccess)] string configuration)
		{
			var ms = new FluentMappingBuilder()
					.Entity<GroupedMaxLanguage>()
						.HasTableName("Common_Language")
						.Property(e => e.LanguageID).IsNullable()
					.Build()
					.MappingSchema;

			using var db = GetDataContext(configuration, ms);

			using var tbl = db.CreateLocalTable(new[]
				{
					new GroupedMaxLanguage
					{
						LanguageID = "de",
						Name = "deutsch"
					},
					new GroupedMaxLanguage
					{
						Name = "english"
					}
				});

			var p = db.GetTable<GroupedMaxLanguage>();
			// use complex column. `string + string` translation now wraps each operand in
			// `Coalesce(..., '')` (C# null-as-empty); `Max(null) + "test"` evaluates to
			// `"test"` rather than NULL, so the null-LanguageID row no longer matches the
			// IN-list (was Count=1 under SQL null-propagation).
			var qry = p.GroupBy(x => x.Name).Select(x => x.Max(y => y.LanguageID) + "test");
			var qry2 = p.Where(x => qry.Contains(x.LanguageID));
			var lst = qry2.ToList();

			Assert.That(lst, Has.Count.EqualTo(0));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4415")]
		public void InSubqueryOverGroupedMaxCoalescedConcatMatchesClientEvaluation([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllPostgreSQL, TestProvName.AllAccess, TestProvName.AllOracle)] string configuration)
		{
			var ms = new FluentMappingBuilder()
					.Entity<GroupedMaxLanguage>()
						.HasTableName("Common_Language")
						.Property(e => e.LanguageID).IsNullable()
					.Build()
					.MappingSchema;

			using var db = GetDataContext(configuration, ms);

			using var tbl = db.CreateLocalTable(new[]
				{
					new GroupedMaxLanguage
					{
						LanguageID = "de",
						Name = "deutsch"
					},
					new GroupedMaxLanguage
					{
						Name = "english"
					}
				});

			var p_2 = db.GetTable<GroupedMaxLanguage>().ToList();
			var qry_2 = p_2.GroupBy(x => x.Name).Select(x => (x.Max(y => y.LanguageID) ?? "") + "test");
			var qry2_2 = p_2.Where(x => qry_2.Contains(x.LanguageID));
			var expected = qry2_2.ToList();

			var p = db.GetTable<GroupedMaxLanguage>();
			// use complex column
			var qry = p.GroupBy(x => x.Name).Select(x => (x.Max(y => y.LanguageID) ?? "") + "test");
			var qry2 = p.Where(x => qry.Contains(x.LanguageID));
			var lst = qry2.ToList();

			Assert.That(lst, Has.Count.EqualTo(expected.Count));
		}

		public class GroupedMaxLanguage
		{
			public string? LanguageID { get; set; }

			public string? Name { get; set; }
		}
	}
}
