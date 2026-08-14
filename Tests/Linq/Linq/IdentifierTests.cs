using System.Collections.Generic;
using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

namespace Tests.Linq
{
	[TestFixture]
	public class IdentifierTests : TestBase
	{
		#region Parameter Name Testing

		// to set parameter name we use two tricks:
		// - for parameter used against column linq2db use property/field name
		// - for parameter used against nested column linq2db use column name
		[Table("testparams")]
		public sealed class Table
		{
			[PrimaryKey] public int Id { get; set; }
			public Nested Component { get; set; } = null!;

			public sealed class Nested
			{
				public int Column1 { get; set; }
			}

			// long ascii identifier (60 chars, we trim parameters to <= 50)
			public int A123456789b123456789c123456789d123456789e123456789f123456789 { get; set; }
		}

		private static readonly IReadOnlyCollection<string> _names = new[]
		{
			// test '-'
			"Test-Name",
			// test non-ascii characters
			"Test名前",
			// test keywords
			"from",
			// test _
			"p_p",
			// test leading _
			"_p",
			// test leading number
			"1p",
		};

		[Test]
		public void TestParameterCharactersTrimming([DataSources] string context, [ValueSource(nameof(_names))] string identifier)
		{
			var ms = new MappingSchema();
			new FluentMappingBuilder(ms)
				.Entity<Table>()
					.Property(t => t.Component.Column1).HasColumnName(identifier)
					.Build();

			using var db = GetDataContext(context, ms);

			if (context.IsAnyOf(TestProvName.AllYdb) && identifier.Any(c => c > 127))
				Assert.Ignore("YDB does not support non-ASCII column identifiers.");

			using var tb = db.CreateLocalTable<Table>();

			tb
				.Where(t => t.Component.Column1 == 1)
				.Set(t => t.Component.Column1, 2)
				.Update();
		}

		[Test]
		public void TestParameterLength([DataSources] string context)
		{
			var ms = new MappingSchema();
			new FluentMappingBuilder(ms)
				.Entity<Table>()
					.Property(t => t.Component.Column1)
					.Property(t => t.A123456789b123456789c123456789d123456789e123456789f123456789)
						.HasColumnName("col1")
					.Build();

			using var db = GetDataContext(context, ms);
			using var tb = db.CreateLocalTable<Table>();

			tb
				.Where(t => t.Component.Column1 == 1)
				.Set(t => t.A123456789b123456789c123456789d123456789e123456789f123456789, 2)
				.Update();
		}
		#endregion

		#region Length Tests
		/*
		 * 1. we use identifiers as base name for alias/parameter
		 * 2. .NET metadata limits identifier length to 1023 bytes in UTF-8
		 * 1+2 means => there is no reason for us to test longer names for now till users find a way to generate even longer names :)
		 * 3. we test both latin and non-latin based names as db behavior could differ for them
		 * 4. extended plane characters (surrogate pairs) not tested as Roslyn currently cannot handle them in idetifiers despite being allowed by language spec
		 * https://github.com/dotnet/roslyn/issues/13474 and referenced issues
		 */
		[Test]
		public void TestParameterLengthName_Latin([DataSources] string context)
		{
			var  abcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabc = 1;
			using var db = GetDataContext(context);
			db.Person
				.Where(r => r.ID == abcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabc)
				.ToArray();
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/2798")]
		public void TestAliasLengthName_Latin([DataSources] string context)
		{
			using var db = GetDataContext(context);
			db.Person
				.Where(abcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabc => abcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabc.ID == 1)
				.ToArray();
		}

		[Test]
		public void TestParameterLengthName_Localized([DataSources] string context)
		{
			var あいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこz = 1;
			using var db = GetDataContext(context);
			db.Person
				.Where(r => r.ID == あいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこz)
				.ToArray();
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/2798")]
		public void TestAliasLengthName_Localized([DataSources] string context)
		{
			using var db = GetDataContext(context);
			db.Person
				.Where(あいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこz => あいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかきくけこz.ID == 1)
				.ToArray();
		}

		/* The tests above use a single long name, so truncating it can never produce a duplicate.
		 * The ones below use several names that differ only past the truncation point, which is what
		 * actually breaks: a server that silently truncates (PostgreSQL, at 63 bytes) collapses them
		 * into one and rejects the query. Aliases have to be uniquified *after* truncation.
		 */
		[Test(Description = "https://github.com/linq2db/linq2db/pull/5772")]
		public void LongAliasNameTest([DataSources] string context)
		{
			using var db = GetDataContext(context);
			var parentId = Parent.First().ParentID;

			var parentsQry =
					from longLongLongLongVeryLongVeryVeryLongVeryVeryLongLongAliasNameTestParent in db.Parent
					from longLongLongLongVeryLongVeryVeryLongVeryVeryLongLongAliasNameTestChild in db.Child.InnerJoin(_ => _.ParentID == longLongLongLongVeryLongVeryVeryLongVeryVeryLongLongAliasNameTestParent.ParentID)
					where longLongLongLongVeryLongVeryVeryLongVeryVeryLongLongAliasNameTestParent.ParentID == parentId
					select longLongLongLongVeryLongVeryVeryLongVeryVeryLongLongAliasNameTestParent;

			var parents =
					from parent in Parent
					join child in Child on parent.ParentID equals child.ParentID
					where parent.ParentID == parentId
					select parent;

			AreEqual(parents, parentsQry);
		}

		/* C# accepts any Unicode character in an identifier, so the same long name can carry non-ASCII
		 * characters well before the truncation point rather than only in the tail. CorrectAlias keeps
		 * letters and digits whatever the script, so those characters reach the length check and are
		 * measured in the unit the provider actually counts - three UTF-8 bytes each here, which is
		 * what makes these names collide at a byte limit that their character count would clear.
		 */
		[Test(Description = "https://github.com/linq2db/linq2db/pull/5772")]
		public void LongAliasNameTest_Localized([DataSources] string context)
		{
			using var db = GetDataContext(context);
			var parentId = Parent.First().ParentID;

			var parentsQry =
					from long親Long子Long孫LongVeryLongVeryVeryLongVeryVeryLongLongAliasNameTestParent in db.Parent
					from long親Long子Long孫LongVeryLongVeryVeryLongVeryVeryLongLongAliasNameTestChild in db.Child.InnerJoin(_ => _.ParentID == long親Long子Long孫LongVeryLongVeryVeryLongVeryVeryLongLongAliasNameTestParent.ParentID)
					where long親Long子Long孫LongVeryLongVeryVeryLongVeryVeryLongLongAliasNameTestParent.ParentID == parentId
					select long親Long子Long孫LongVeryLongVeryVeryLongVeryVeryLongLongAliasNameTestParent;

			var parents =
					from parent in Parent
					join child in Child on parent.ParentID equals child.ParentID
					where parent.ParentID == parentId
					select parent;

			AreEqual(parents, parentsQry);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/pull/5772")]
		public void TestAliasLengthName_ThreeWayCollision([DataSources] string context)
		{
			using var db = GetDataContext(context);
			var parentId = Parent.First().ParentID;

			// three sources sharing a prefix longer than the shortest provider limit, so the
			// uniquifier has to hand out more than one suffix
			var actual =
					from longLongLongLongVeryLongVeryVeryLongVeryVeryLongLongAliasNameTestParent in db.Parent
					from longLongLongLongVeryLongVeryVeryLongVeryVeryLongLongAliasNameTestChild in db.Child.InnerJoin(_ => _.ParentID == longLongLongLongVeryLongVeryVeryLongVeryVeryLongLongAliasNameTestParent.ParentID)
					from longLongLongLongVeryLongVeryVeryLongVeryVeryLongLongAliasNameTestGrand in db.GrandChild.InnerJoin(_ => _.ChildID == longLongLongLongVeryLongVeryVeryLongVeryVeryLongLongAliasNameTestChild.ChildID)
					where longLongLongLongVeryLongVeryVeryLongVeryVeryLongLongAliasNameTestParent.ParentID == parentId
					select longLongLongLongVeryLongVeryVeryLongVeryVeryLongLongAliasNameTestParent;

			var expected =
					from parent in Parent
					join child in Child on parent.ParentID equals child.ParentID
					join grand in GrandChild on child.ChildID equals grand.ChildID
					where parent.ParentID == parentId
					select parent;

			AreEqual(expected, actual);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/pull/5772")]
		public void TestAliasLengthName_ColumnAliasCollision([DataSources] string context)
		{
			using var db = GetDataContext(context);

			// Same shape as above but on column aliases, which AliasesHelper uniquifies separately.
			// PostgreSQL tolerates duplicate output column names, so unlike the two tests above this
			// one does not go red on the 63-byte limit; it guards the column uniquifier for providers
			// that do reject duplicate root columns (SqlCe, YDB).
			var actual =
					from p in db.Parent
					select new
					{
						longLongLongLongVeryLongVeryVeryLongVeryVeryLongLongAliasNameTestParentId = p.ParentID,
						longLongLongLongVeryLongVeryVeryLongVeryVeryLongLongAliasNameTestValueOne = p.Value1,
					};

			var expected =
					from p in Parent
					select new
					{
						longLongLongLongVeryLongVeryVeryLongVeryVeryLongLongAliasNameTestParentId = p.ParentID,
						longLongLongLongVeryLongVeryVeryLongVeryVeryLongLongAliasNameTestValueOne = p.Value1,
					};

			AreEqual(expected, actual);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/pull/5772")]
		public void TestTableName_QualifiedRendersAsTwoParts([IncludeDataSources(false, TestProvName.AllSQLite, TestProvName.AllSqlCe, TestProvName.AllSybase)] string context)
		{
			// A dotted name is two parts, and each has to be delimited separately. Delimiting the joined
			// string instead lets the delimiter escape the separator, collapsing schema.table into one
			// name - which stays valid SQL and fails later as "no such table". Only these three split a
			// dotted name; SQL Server carries the parts in SqlObjectName and treats a dot as literal.
			using var db = GetDataContext(context);

			var sql = db.GetTable<Table>().TableName("a.b").ToSqlQuery().Sql;

			sql.ShouldContain("[a].[b]");
		}

		// Oracle and Access cannot represent such a name at all - Oracle's quoted identifiers accept
		// every character except the quote itself, and Access has no escape inside brackets - so they
		// are asserted to refuse it rather than being excluded, which keeps this red if that changes.
		[Test(Description = "https://github.com/linq2db/linq2db/pull/5772")]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllOracle, TestProvName.AllAccess, ErrorMessage = "cannot represent the identifier")]
		public void TestColumnName_WithDelimiterCharacter([DataSources] string context)
		{
			// A name carrying the provider's own delimiter has to be escaped rather than closing the
			// delimited name early - SAP HANA, Db2, Firebird and Informix used to emit it raw, and
			// SQLite bracket-quoted it with no way to escape the bracket.
			var ms = new MappingSchema();
			new FluentMappingBuilder(ms)
				.Entity<Table>()
					.Property(t => t.Component.Column1).HasColumnName("qu\"ote`d]name")
					.Build();

			using var db = GetDataContext(context, ms);

			if (context.IsAnyOf(TestProvName.AllYdb))
				Assert.Ignore("YDB does not support non-ASCII or delimiter characters in column identifiers.");

			using var tb = db.CreateLocalTable<Table>();

			tb.Where(t => t.Component.Column1 == 1).ToArray();
		}

		[Test(Description = "https://github.com/linq2db/linq2db/pull/5772")]
		public void TestAliasName_NonAsciiOnly([DataSources] string context)
		{
			using var db = GetDataContext(context);
			var parentId = Parent.First().ParentID;

			// A name with no ASCII at all is the shape that decides whether a provider quotes the
			// alias: a mixed name like long親Long carries lowercase ASCII, which some builders use
			// as their quoting trigger, so it would pass even where a bare 顧客 does not.
			var actual =
					from 親 in db.Parent
					from 子 in db.Child.InnerJoin(_ => _.ParentID == 親.ParentID)
					where 親.ParentID == parentId
					select 親;

			var expected =
					from parent in Parent
					join child in Child on parent.ParentID equals child.ParentID
					where parent.ParentID == parentId
					select parent;

			AreEqual(expected, actual);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/pull/5772")]
		public void TestAliasLengthName_ThreeWayCollision_Localized([DataSources] string context)
		{
			using var db = GetDataContext(context);
			var parentId = Parent.First().ParentID;

			var actual =
					from long親Long子Long孫LongVeryLongVeryVeryLongVeryVeryLongLongAliasNameTestParent in db.Parent
					from long親Long子Long孫LongVeryLongVeryVeryLongVeryVeryLongLongAliasNameTestChild in db.Child.InnerJoin(_ => _.ParentID == long親Long子Long孫LongVeryLongVeryVeryLongVeryVeryLongLongAliasNameTestParent.ParentID)
					from long親Long子Long孫LongVeryLongVeryVeryLongVeryVeryLongLongAliasNameTestGrand in db.GrandChild.InnerJoin(_ => _.ChildID == long親Long子Long孫LongVeryLongVeryVeryLongVeryVeryLongLongAliasNameTestChild.ChildID)
					where long親Long子Long孫LongVeryLongVeryVeryLongVeryVeryLongLongAliasNameTestParent.ParentID == parentId
					select long親Long子Long孫LongVeryLongVeryVeryLongVeryVeryLongLongAliasNameTestParent;

			var expected =
					from parent in Parent
					join child in Child on parent.ParentID equals child.ParentID
					join grand in GrandChild on child.ChildID equals grand.ChildID
					where parent.ParentID == parentId
					select parent;

			AreEqual(expected, actual);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/pull/5772")]
		public void TestAliasLengthName_ColumnAliasCollision_Localized([DataSources] string context)
		{
			using var db = GetDataContext(context);

			// the Japanese characters sit ahead of the truncation point in both member names, so the
			// column uniquifier sees them in the seed it truncates
			var actual =
					from p in db.Parent
					select new
					{
						long親Long子Long孫LongVeryLongVeryVeryLongVeryVeryLongLongAliasNameTestParentId = p.ParentID,
						long親Long子Long孫LongVeryLongVeryVeryLongVeryVeryLongLongAliasNameTestValueOne = p.Value1,
					};

			var expected =
					from p in Parent
					select new
					{
						long親Long子Long孫LongVeryLongVeryVeryLongVeryVeryLongLongAliasNameTestParentId = p.ParentID,
						long親Long子Long孫LongVeryLongVeryVeryLongVeryVeryLongLongAliasNameTestValueOne = p.Value1,
					};

			AreEqual(expected, actual);
		}
		#endregion
	}
}
