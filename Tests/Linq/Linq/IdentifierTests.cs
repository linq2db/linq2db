using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

using Tests.Model;

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
			"TestИмя",
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
			var абвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиаz = 1;
			using var db = GetDataContext(context);
			db.Person
				.Where(r => r.ID == абвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиаz)
				.ToArray();
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/2798")]
		public void TestAliasLengthName_Localized([DataSources] string context)
		{
			using var db = GetDataContext(context);
			db.Person
				.Where(абвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиаz => абвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиаz.ID == 1)
				.ToArray();
		}
		#endregion

		[Test(Description = "https://github.com/linq2db/linq2db/pull/5772")]
		public void TestTableName_QualifiedRendersAsTwoParts([IncludeDataSources(false, TestProvName.AllSQLite, TestProvName.AllSqlCe, TestProvName.AllSybase)] string context)
		{
			// A dotted name is two parts, and each has to be delimited separately. Delimiting the joined
			// string instead lets the delimiter escape the separator, collapsing schema.table into one
			// name - which stays valid SQL and fails later as "no such table". Only these three split a
			// dotted name; SQL Server carries the parts in SqlObjectName and treats a dot as literal.
			using var db = GetDataContext(context);

			var sql = db
				.GetTable<Table>()
				.TableName("a.b")
				.ToSqlQuery()
				.Sql;

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

			if (context.IsAnyOf(TestProvName.AllYdb))
				Assert.Ignore("YDB does not support non-ASCII or delimiter characters in column identifiers.");

			using var db = GetDataContext(context, ms);
			using var tb = db.CreateLocalTable<Table>();

			tb
				.Where(t => t.Component.Column1 == 1)
				.ToArray();
		}

		// ACE OLE DB and LibRed accept a double quote inside a bracketed name in DDL and DML; the ACE ODBC
		// driver rewrites it before parsing ([Say "Hi"] arrives as [Say [Hi]]), so only ODBC refuses it.
		[Test(Description = "https://github.com/linq2db/linq2db/pull/5772")]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllAccessOdbc, ErrorMessage = "cannot represent the identifier")]
		public void TestColumnName_WithDoubleQuote([IncludeDataSources(TestProvName.AllAccess)] string context)
		{
			var ms = new MappingSchema();
			new FluentMappingBuilder(ms)
				.Entity<Table>()
					.Property(t => t.Component.Column1).HasColumnName("Say \"Hi\"")
					.Build();

			using var db = GetDataContext(context, ms);
			using var tb = db.CreateLocalTable<Table>();

			db.Insert(new Table { Id = 1, Component = new Table.Nested { Column1 = 2 } });

			tb
				.Where(t => t.Component.Column1 == 2)
				.Select(t => t.Id)
				.ToArray()
				.ShouldBe([1]);
		}

		// An empty name part cannot be rendered; it used to vanish from the SQL (".Parent") and fail on the
		// server with a syntax error that does not name the cause. Only the providers whose Convert routes
		// through BuildIdentifier are listed: the others delimit unconditionally (see the control below).
		[Test(Description = "https://github.com/linq2db/linq2db/pull/5772")]
		public void TestSchemaName_Empty([IncludeDataSources(TestProvName.AllPostgreSQL, ProviderName.DuckDB, TestProvName.AllOracle, TestProvName.AllDB2, TestProvName.AllInformix)] string context)
		{
			using var db = GetDataContext(context);

			var query = db
				.GetTable<Parent>()
				.SchemaName("");

			Assert.That(() => query.ToSqlQuery(), Throws.TypeOf<LinqToDBException>().With.Message.EqualTo("Empty identifier"));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/pull/5772")]
		public void TestDatabaseName_Empty([IncludeDataSources(TestProvName.AllClickHouse, ProviderName.DuckDB, TestProvName.AllInformix)] string context)
		{
			using var db = GetDataContext(context);

			var query = db
				.GetTable<Parent>()
				.DatabaseName("");

			Assert.That(() => query.ToSqlQuery(), Throws.TypeOf<LinqToDBException>().With.Message.EqualTo("Empty identifier"));
		}

		[Sql.TableFunction("TEST_TABLE_FUNCTION", argIndices: new[] { 1 }, Package = "")]
		static ITable<Parent> EmptyPackageFunction(IDataContext db, int param1)
		{
			return db.GetTable<Parent>(null, (MethodInfo)MethodBase.GetCurrentMethod()!, db, param1);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/pull/5772")]
		public void TestPackageName_Empty([IncludeDataSources(TestProvName.AllFirebird)] string context)
		{
			using var db = GetDataContext(context);

			var query = EmptyPackageFunction(db, 1);

			Assert.That(() => query.ToSqlQuery(), Throws.TypeOf<LinqToDBException>().With.Message.EqualTo("Empty identifier"));
		}

		// Control: providers that delimit every name unconditionally keep rendering an empty part as an
		// empty delimited name.
		[Test(Description = "https://github.com/linq2db/linq2db/pull/5772")]
		public void TestDatabaseName_Empty_UnconditionalDelimiter([IncludeDataSources(ProviderName.SQLiteMS, TestProvName.AllSqlServer2022)] string context)
		{
			using var db = GetDataContext(context);

			var sql = db
				.GetTable<Parent>()
				.DatabaseName("")
				.ToSqlQuery()
				.Sql;

			sql.ShouldContain("[].");
		}
	}
}
