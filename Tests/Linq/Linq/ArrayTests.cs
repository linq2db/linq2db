#if SUPPORTS_DATEONLY
using System;
#endif
using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;
using LinqToDB.SchemaProvider;

using NUnit.Framework;

using Tests.Model;

namespace Tests.Linq
{
	[TestFixture]
	public class ArrayTests : TestBase
	{
		enum SimpleEnum
		{
			Value1,
			Value2,
			Value3,
		}

		[Table]
		sealed class ArrayTable
		{
			[Column] public int[]?        Numbers     { get; set; }
			[Column] public Gender[]?     StringEnums { get; set; }
			[Column] public SimpleEnum[]? IntEnums    { get; set; }
		}

		[Table]
		sealed class ArrayTTable<T>
		{
			[Column] public int Id { get; set; }
			[Column] public T[]? Value { get; set; }
		}

		// PostgreSQL gets its own attribute: it maps the int array natively and stops at the enum one instead, so
		// the type named in the refusal differs there.
		[ActiveIssueNew(ErrorTypeName = "LinqToDB.LinqToDBException",
			ErrorMessage = "Database column type cannot be determined automatically and must be specified explicitly for system type System.Int32[]",
			Details = "no-issue: an array column has no automatic database type. Nothing on the tracker covers it.")]
		[ActiveIssueNew(Configuration = TestProvName.AllPostgreSQL, ErrorTypeName = "LinqToDB.LinqToDBException",
			ErrorMessage = "Database column type cannot be determined automatically and must be specified explicitly for system type Tests.Model.Gender[]",
			Details = "no-issue: as above, but PostgreSQL only reaches the enum array.")]
		[Test]
		public void CreateTable([DataSources(false)] string context)
		{
			using var db = GetDataConnection(context);
			using var tb = db.CreateLocalTable<ArrayTable>();

			var schema = db.DataProvider.GetSchemaProvider().GetSchema(db, new GetSchemaOptions()
			{
				LoadTable = t => t.Name == nameof(ArrayTable)
			});

			var tableSchema = schema.Tables.Single();

			Assert.That(tableSchema.Columns, Has.Count.EqualTo(3));

			var column = tableSchema.Columns.Single(c => c.ColumnName == nameof(ArrayTable.Numbers));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(column.SystemType, Is.EqualTo(typeof(int[])));
				Assert.That(column.IsNullable, Is.True);
			}

			column = tableSchema.Columns.Single(c => c.ColumnName == nameof(ArrayTable.StringEnums));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(column.SystemType, Is.EqualTo(typeof(string[])));
				Assert.That(column.IsNullable, Is.True);
			}

			column = tableSchema.Columns.Single(c => c.ColumnName == nameof(ArrayTable.IntEnums));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(column.SystemType, Is.EqualTo(typeof(int[])));
				Assert.That(column.IsNullable, Is.True);
			}
		}

		// One cause seen through three transports - direct, the Grpc wrapper and the WCF FaultException - all of
		// which carry the inner type name, which is what Matches looks for on a remote case.
		[ActiveIssueNew(ErrorTypeName = "LinqToDB.LinqToDBException",
			ErrorMessage = "Database column type cannot be determined automatically and must be specified explicitly for system type System.Int32[]",
			Details = "no-issue: an array column has no automatic database type, as in ArrayTests.CreateTable.")]
		[Test]
		public void InsertArray([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<ArrayTable>();

			var record = new ArrayTable()
			{
				Numbers     = [-1, 0, 5],
				StringEnums = [Gender.Unknown, Gender.Male, Gender.Female],
				IntEnums    = [SimpleEnum.Value2, SimpleEnum.Value3, SimpleEnum.Value1],
			};

			db.Insert(record);

			var result = tb.Single();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.Numbers, Is.EqualTo(record.Numbers));
				Assert.That(result.StringEnums, Is.EqualTo(record.StringEnums));
				Assert.That(result.IntEnums, Is.EqualTo(record.IntEnums));
			}
		}

		[ActiveIssueNew(ErrorTypeName = "LinqToDB.LinqToDBException",
			ErrorMessage = "Database column type cannot be determined automatically and must be specified explicitly for system type System.Int32[]",
			Details = "no-issue: as InsertArray.")]
		[Test]
		public void UpdateArray([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<ArrayTable>();

			var record = new ArrayTable()
			{
				Numbers     = [-1, 0, 5],
				StringEnums = [Gender.Unknown, Gender.Male, Gender.Female],
				IntEnums    = [SimpleEnum.Value2, SimpleEnum.Value3, SimpleEnum.Value1],
			};

			db.Insert(record);

			tb
				.Set(r => r.Numbers, [4, 5, 6])
				.Set(r => r.StringEnums, r => null)
				.Set(r => r.IntEnums, r => new[] { SimpleEnum.Value2, SimpleEnum.Value1, SimpleEnum.Value1 })
				.Update();

			var result = tb.Single();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.Numbers, Is.EqualTo(new[] { 4, 5, 6 }));
				Assert.That(result.StringEnums, Is.Null);
				Assert.That(result.IntEnums, Is.EqualTo(new[] { SimpleEnum.Value2, SimpleEnum.Value1, SimpleEnum.Value1 }));
			}
		}

		[ActiveIssueNew(1660, ErrorTypeName = "LinqToDB.LinqToDBException",
			ErrorMessage = "Database column type cannot be determined automatically and must be specified explicitly for system type System.Int32[]",
			Details = "Issue number taken from the test's own Description, which the bare attribute did not carry. Same array-column gap as InsertArray.")]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/1660")]
		public void CollectionContainsMapping([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<ArrayTable>();

			var record = new ArrayTable()
			{
				Numbers     = [-1, 0, 5],
				StringEnums = [Gender.Unknown, Gender.Male, Gender.Female],
				IntEnums    = [SimpleEnum.Value2, SimpleEnum.Value3, SimpleEnum.Value1],
			};

			db.Insert(record);

			var result = tb.Where(r => r.Numbers!.Contains(5)).Single();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.Numbers, Is.EqualTo(record.Numbers));
				Assert.That(result.StringEnums, Is.EqualTo(record.StringEnums));
				Assert.That(result.IntEnums, Is.EqualTo(record.IntEnums));
			}

			result = tb.Where(r => r.Numbers!.Contains(6)).SingleOrDefault();

			Assert.That(result, Is.Null);
		}

#if SUPPORTS_DATEONLY
		// PostgreSQL maps the array itself and fails later, on the SQL it emits for it.
		[ActiveIssueNew(3929, ErrorTypeName = "LinqToDB.LinqToDBException",
			ErrorMessage = "Database column type cannot be determined automatically and must be specified explicitly for system type System.DateOnly[]",
			Details = "Issue number taken from the test's own Description, which the bare attribute did not carry. Same array-column gap as InsertArray, on DateOnly.")]
		[ActiveIssueNew(3929, Configuration = TestProvName.AllPostgreSQL, ErrorTypeName = "Npgsql.PostgresException",
			ErrorMessage = "42601: syntax error at or near \"Array\"",
			Details = "PostgreSQL types the array and then emits SQL its own parser rejects.")]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/3929")]
		public void TestDateOnly([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<ArrayTTable<DateOnly>>();

			var record = new ArrayTTable<DateOnly>()
			{
				Id    = 1,
				Value = [TestData.DateOnly, TestData.DateOnly.AddDays(1)]
			};

			db.Insert(record);

			var result = tb.Where(r => r.Id == 1).Single();

			Assert.That(result.Value, Is.EqualTo(record.Value));
		}
#endif
	}
}
