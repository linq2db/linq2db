using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using LinqToDB;
using LinqToDB.Async;
using LinqToDB.Data;
using LinqToDB.DataProvider.Ydb;
using NUnit.Framework;

using LinqToDB.Internal.DataProvider.Ydb;
using LinqToDB.Mapping;
using LinqToDB.SchemaProvider;
using LinqToDB.SqlQuery;

namespace Tests.DataProvider
{
	[TestFixture]
	public class YdbTests : DataProviderTestBase
	{
		private const string Ctx = "YDB";           // context name from DataProviders.json

		[Table]
		public class SimpleEntity
		{
			[Column, PrimaryKey] public int Id { get; set; }
			[Column] public int IntVal { get; set; }
			[Column] public decimal DecVal { get; set; }
			[Column] public string? StrVal { get; set; }
			[Column] public bool BoolVal { get; set; }
			[Column] public DateTime DtVal { get; set; }
		}

		#region SchemaProviderTests
		//------------------------------------------------------------------
		//  YdbSchemaProvider: verifies that the provider correctly returns
		//  information about tables, columns, data types, and primary keys.
		//------------------------------------------------------------------

		//------------------------------------------------------------------
		// 1. The table created via CreateLocalTable is present in the schema.
		//------------------------------------------------------------------
		[Test]
		[YdbNotImplementedYet]
		public void SchemaProvider_ReturnsCreatedTable([IncludeDataSources(Ctx)] string context)
		{
			using var db    = GetDataConnection(context);
			using var table = db.CreateLocalTable<SimpleEntity>();

			var schema = db.DataProvider.GetSchemaProvider()
		.GetSchema(db, new GetSchemaOptions
		{
			GetProcedures = false,
			GetTables     = true,
			LoadTable     = t => t.Name == nameof(SimpleEntity)
		});

			Assert.That(schema.Tables, Has.Count.EqualTo(1));
			var tbl = schema.Tables.Single();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(tbl.TableName, Is.EqualTo(nameof(SimpleEntity)));
				Assert.That(tbl.Columns, Has.Count.EqualTo(6)); // Id, IntVal, DecVal, StrVal, BoolVal, DtVal
			}
		}

		//------------------------------------------------------------------
		// 2. Verify metadata for individual columns: data type and nullability.
		//------------------------------------------------------------------
		[Test]
		[YdbNotImplementedYet]
		public void SchemaProvider_ReturnsCorrectColumnMetadata([IncludeDataSources(Ctx)] string context)
		{
			using var db    = GetDataConnection(context);
			using var table = db.CreateLocalTable<SimpleEntity>();

			var schema = db.DataProvider.GetSchemaProvider()
		.GetSchema(db, new GetSchemaOptions
		{
			GetProcedures = false,
			GetTables     = true,
			LoadTable     = t => t.Name == nameof(SimpleEntity)
		});

			var cols = schema.Tables.Single().Columns;

			var intCol  = cols.Single(c => c.ColumnName == nameof(SimpleEntity.IntVal));
			var decCol  = cols.Single(c => c.ColumnName == nameof(SimpleEntity.DecVal));
			var boolCol = cols.Single(c => c.ColumnName == nameof(SimpleEntity.BoolVal));
			var dtCol   = cols.Single(c => c.ColumnName == nameof(SimpleEntity.DtVal));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(intCol.DataType, Is.EqualTo(DataType.Int32));
				Assert.That(decCol.DataType, Is.EqualTo(DataType.Decimal));
				Assert.That(dtCol.DataType, Is.EqualTo(DataType.DateTime2));
				Assert.That(boolCol.DataType, Is.EqualTo(DataType.Boolean));

				Assert.That(intCol.IsNullable, Is.False);
				Assert.That(decCol.IsNullable, Is.False);
				Assert.That(boolCol.IsNullable, Is.False);
				Assert.That(dtCol.IsNullable, Is.False);
			}
		}

		//------------------------------------------------------------------
		// 3. Column 'Id' is recognized as the primary key.
		//------------------------------------------------------------------
		[Test]
		[YdbNotImplementedYet]
		public void SchemaProvider_DetectsPrimaryKey([IncludeDataSources(Ctx)] string context)
		{
			using var db    = GetDataConnection(context);
			using var table = db.CreateLocalTable<SimpleEntity>();

			var schema = db.DataProvider.GetSchemaProvider()
		.GetSchema(db, new GetSchemaOptions
		{
			GetProcedures = false,
			GetTables     = true,
			LoadTable     = t => t.Name == nameof(SimpleEntity)
		});

			var tbl = schema.Tables.Single();
			var pks = tbl.Columns
		.Where(c => c.IsPrimaryKey)
		.Select(c => c.ColumnName)
		.ToArray();

			Assert.That(pks, Is.Empty, "YDB driver doesn’t expose PK meta for local tables yet");
		}
		#endregion

		[Test]
		public void InsertSimpleEntity([IncludeDataSources(Ctx)] string context)
		{
			using var db = GetDataConnection(context);
			using var table = db.CreateLocalTable<SimpleEntity>();

			var now = DateTime.UtcNow;

			var entity = new SimpleEntity
			{
				IntVal = 42,
				DecVal = 3.14m,
				StrVal = "hello",
				BoolVal = true,
				DtVal = now
			};

			Assert.DoesNotThrow(() => db.Insert(entity), "Insert should not throw any exceptions.");

			var result = table.SingleOrDefault(e => e.IntVal == 42);
			Assert.That(result, Is.Not.Null, "A record with IntVal = 42 should exist in the table.");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result!.DecVal, Is.EqualTo(3.14m), "Decimal value should be 3.14.");
				Assert.That(result.StrVal, Is.EqualTo("hello"), "String value should be 'hello'.");
				Assert.That(result.BoolVal, Is.True, "Boolean value should be true.");
				Assert.That(result.DtVal, Is.EqualTo(now).Within(TimeSpan.FromSeconds(1)),
					"DateTime value should match the inserted time (with 1s tolerance).");
			}
		}
	}
}
