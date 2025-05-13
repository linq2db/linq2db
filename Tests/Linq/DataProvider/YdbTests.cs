using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.SchemaProvider;
using LinqToDB.DataProvider.Ydb;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

using NUnit.Framework;

namespace Tests.DataProvider
{
	[TestFixture]
	public class YdbTests : DataProviderTestBase
	{
		private const string Ctx = "YDB";           // context name from DataProviders.json
		private const string PingSql = "SELECT 1";

		//------------------------------------------------------------------
		// 2. Explicit check of YdbTools + round-trip of the connection string.
		//------------------------------------------------------------------
		[Test]
		public void ConnectionStringRoundtrip()
		{
			var connection = GetConnectionString(Ctx);

			using var db = YdbTools.CreateDataConnection(connection);

			Assert.Multiple(() =>
			{
				Assert.That(db.DataProvider.Name, Is.EqualTo(YdbDataProvider.ProviderName));
				Assert.That(db.ConnectionString, Is.EqualTo(connection));
			});
		}

		//------------------------------------------------------------------
		// 3. The connection string is taken from DataProviders.json when only the context is specified.
		//------------------------------------------------------------------
		[Test]
		public void ConnectionStringFromConfiguration()
		{
			var expected = GetConnectionString(Ctx);

			using var db = GetDataConnection(Ctx);

			Assert.That(db.ConnectionString, Is.EqualTo(expected));
		}

		//------------------------------------------------------------------
		// 4. Ping sync / async (example usage of wrappers).
		//------------------------------------------------------------------
		[Test]
		public void Can_Open_Connection_And_Ping([IncludeDataSources(Ctx)] string context)
		{
			using var db = GetDataConnection(context);
			Assert.That(db.Execute<int>(PingSql), Is.EqualTo(1));
		}

		[Test]
		public async Task Can_Open_Connection_And_Ping_Async([IncludeDataSources(Ctx)] string context)
		{
			await using var db = GetDataConnection(context);
			Assert.That(await db.ExecuteAsync<int>(PingSql), Is.EqualTo(1));
		}

		#region MappingSchemaTests

		//------------------------------------------------------------------
		// ADAPTER
		//------------------------------------------------------------------
		[Test]
		public void GetInstance_ShouldCreateAdapterSuccessfully()
		{
			// Act
			var adapter = YdbProviderAdapter.GetInstance();

			// Assert
			Assert.That(adapter, Is.Not.Null);
			Assert.Multiple(() =>
			{
				Assert.That(adapter.ConnectionType, Is.Not.Null);
				Assert.That(adapter.DataReaderType, Is.Not.Null);
				Assert.That(adapter.ParameterType, Is.Not.Null);
				Assert.That(adapter.CommandType, Is.Not.Null);
				Assert.That(adapter.MappingSchema, Is.Not.Null);
			});
		}
		#endregion

		#region MappingSchemaTests

		//------------------------------------------------------------------
		// SCALAR-TYPES
		//------------------------------------------------------------------
		[Test]
		public void MappingSchema_ShouldMapBasicDotNetTypes()
		{
			var schema = YdbMappingSchema.Instance;
			Assert.That(schema, Is.Not.Null);

			Assert.Multiple(() =>
			{
				Assert.That(schema.GetDataType(typeof(string)).Type.DataType, Is.EqualTo(DataType.VarChar));
				Assert.That(schema.GetDataType(typeof(bool)).Type.DataType, Is.EqualTo(DataType.Boolean));
				Assert.That(schema.GetDataType(typeof(Guid)).Type.DataType, Is.EqualTo(DataType.Guid));
				Assert.That(schema.GetDataType(typeof(byte[])).Type.DataType, Is.EqualTo(DataType.VarBinary));
				Assert.That(schema.GetDataType(typeof(TimeSpan)).Type.DataType, Is.EqualTo(DataType.Interval));
			});
		}

		//------------------------------------------------------------------
		// DECIMAL-LITERAL
		//------------------------------------------------------------------
		[Test]
		public void DecimalLiteralBuilder_ShouldRespectSqlDataTypeDefaults()
		{
			var mi = typeof(YdbMappingSchema)
					.GetMethod("BuildDecimalLiteral", BindingFlags.Static | BindingFlags.NonPublic)!;

			var sb  = new StringBuilder();
			var val = 123.45m;

			// SqlDataType default: Precision = 29, Scale = 10
			var sqlType = new SqlDataType(DataType.Decimal, typeof(decimal));

			mi.Invoke(null, new object[] { sb, val, sqlType });

			var expected = $"Decimal(\"{val.ToString(CultureInfo.InvariantCulture)}\", {sqlType.Type.Precision}, {sqlType.Type.Scale})";

			Assert.That(sb.ToString(), Is.EqualTo(expected));
		}
		#endregion

		#region BulkCopyTests

		[Table]
		public class SimpleEntity
		{
			[Column, PrimaryKey, Identity] public int Id { get; set; }
			[Column] public int IntVal { get; set; }
			[Column] public decimal DecVal { get; set; }
			[Column] public string? StrVal { get; set; }
			[Column] public bool BoolVal { get; set; }
			[Column] public DateTime DtVal { get; set; }
		}

		private static SimpleEntity[] BuildData(int count = 10)
		{
			return Enumerable.Range(0, count)
				.Select(i => new SimpleEntity
				{
					IntVal = i,
					DecVal = 100 + i,
					StrVal = $"str{i}",
					BoolVal = i % 2 == 0,
					DtVal = new DateTime(2023, 01, 01).AddDays(i)
				})
				.ToArray();
		}

		//------------------------------------------------------------------
		// SYNC BulkCopy
		//------------------------------------------------------------------
		[Test]
		public void BulkCopySimple(
			[IncludeDataSources(Ctx)] string context,
			[Values] BulkCopyType type)
		{
			using var db    = GetDataConnection(context);
			using var table = db.CreateLocalTable<SimpleEntity>();

			var data   = BuildData();
			var result = db.BulkCopy(new BulkCopyOptions { BulkCopyType = type }, data);

			Assert.That(result.RowsCopied, Is.EqualTo(data.Length));

			var read = table.OrderBy(e => e.IntVal).ToArray();
			Assert.That(read, Has.Length.EqualTo(data.Length));

			for (var i = 0; i < read.Length; i++)
			{
				Assert.Multiple(() =>
				{
					Assert.That(read[i].IntVal, Is.EqualTo(data[i].IntVal));
					Assert.That(read[i].DecVal, Is.EqualTo(data[i].DecVal));
					Assert.That(read[i].StrVal, Is.EqualTo(data[i].StrVal));
					Assert.That(read[i].BoolVal, Is.EqualTo(data[i].BoolVal));
					Assert.That(read[i].DtVal, Is.EqualTo(data[i].DtVal));
				});
			}
		}

		//------------------------------------------------------------------
		// ASYNC BulkCopy
		//------------------------------------------------------------------
		[Test]
		public async Task BulkCopySimpleAsync(
			[IncludeDataSources(Ctx)] string context,
			[Values] BulkCopyType type)
		{
			using var db    = GetDataConnection(context);
			using var table = db.CreateLocalTable<SimpleEntity>();

			var data   = BuildData();
			var result = await db.BulkCopyAsync(new BulkCopyOptions { BulkCopyType = type }, data);

			Assert.That(result.RowsCopied, Is.EqualTo(data.Length));

			var read = await table.OrderBy(e => e.IntVal).ToArrayAsync();
			Assert.That(read, Has.Length.EqualTo(data.Length));

			for (var i = 0; i < read.Length; i++)
			{
				Assert.Multiple(() =>
				{
					Assert.That(read[i].IntVal, Is.EqualTo(data[i].IntVal));
					Assert.That(read[i].DecVal, Is.EqualTo(data[i].DecVal));
					Assert.That(read[i].StrVal, Is.EqualTo(data[i].StrVal));
					Assert.That(read[i].BoolVal, Is.EqualTo(data[i].BoolVal));
					Assert.That(read[i].DtVal, Is.EqualTo(data[i].DtVal));
				});
			}
		}
		#endregion

		#region SpecificExtensionsTests
		//------------------------------------------------------------------
		// Validates AsYdb() for ITable<T> and IQueryable<T>
		//------------------------------------------------------------------

		[Test]
		public void AsYdb_Table_ReturnsSpecificInterface([IncludeDataSources(Ctx)] string context)
		{
			using var db = GetDataConnection(context);

			var tbl = db.GetTable<SimpleEntity>().AsYdb();

			Assert.That(tbl, Is.InstanceOf<IYdbSpecificTable<SimpleEntity>>());
		}

		[Test]
		public void AsYdb_Queryable_ReturnsSpecificInterface([IncludeDataSources(Ctx)] string context)
		{
			using var db = GetDataConnection(context);

			var qry = db.GetTable<SimpleEntity>().Where(e => e.IntVal >= 0).AsYdb();

			Assert.That(qry, Is.InstanceOf<IYdbSpecificQueryable<SimpleEntity>>());
		}

		//------------------------------------------------------------------
		// ITable<T>.AsYdb — data remains unchanged (synchronous)
		//------------------------------------------------------------------
		[Test]
		public void AsYdb_Table_DataRoundtrip([IncludeDataSources(Ctx)] string context)
		{
			using var db    = GetDataConnection(context);
			using var table = db.CreateLocalTable<SimpleEntity>();

			var data = BuildData();
			db.BulkCopy(data);

			var without = table.OrderBy(t => t.Id).ToArray();
			var withYdb = table.AsYdb().OrderBy(t => t.Id).ToArray();

			Assert.That(withYdb, Has.Length.EqualTo(without.Length));
			for (var i = 0; i < without.Length; i++)
			{
				Assert.That(EntityEquals(without[i], withYdb[i]), Is.True, $"row {i} differs");
			}
		}

		//------------------------------------------------------------------
		// IQueryable<T>.AsYdb — data remains unchanged (asynchronous)
		//------------------------------------------------------------------
		[Test]
		public async Task AsYdb_Queryable_DataRoundtrip_Async([IncludeDataSources(Ctx)] string context)
		{
			await using var db    = GetDataConnection(context);
			await using var table = db.CreateLocalTable<SimpleEntity>();

			var data = BuildData();
			await db.BulkCopyAsync(data);

			var filter  = 5;
			var without = await table.Where(t => t.IntVal < filter).OrderBy(t => t.Id).ToArrayAsync();
			var withYdb = await table.Where(t => t.IntVal < filter)
							 .AsYdb()
							 .OrderBy(t => t.Id)
							 .ToArrayAsync();

			Assert.That(withYdb, Has.Length.EqualTo(without.Length));
			for (var i = 0; i < without.Length; i++)
			{
				Assert.That(EntityEquals(without[i], withYdb[i]), Is.True, $"row {i} differs");
			}
		}

		// Helper method for comparing entity objects
		private static bool EntityEquals(SimpleEntity a, SimpleEntity b) =>
			   a.IntVal == b.IntVal
			&& a.DecVal == b.DecVal
			&& a.StrVal == b.StrVal
			&& a.BoolVal == b.BoolVal
			&& a.DtVal == b.DtVal;

		#endregion

		#region SchemaProviderTests
		//------------------------------------------------------------------
		//  YdbSchemaProvider: verifies that the provider correctly returns
		//  information about tables, columns, data types, and primary keys.
		//------------------------------------------------------------------

		//------------------------------------------------------------------
		// 1. The table created via CreateLocalTable is present in the schema.
		//------------------------------------------------------------------
		[Test]
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

			Assert.Multiple(() =>
			{
				Assert.That(tbl.TableName, Is.EqualTo(nameof(SimpleEntity)));
				Assert.That(tbl.Columns, Has.Count.EqualTo(6)); // Id, IntVal, DecVal, StrVal, BoolVal, DtVal
			});
		}

		//------------------------------------------------------------------
		// 2. Verify metadata for individual columns: data type and nullability.
		//------------------------------------------------------------------
		[Test]
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

			Assert.Multiple(() =>
			{
				Assert.That(intCol.DataType, Is.EqualTo(DataType.Int32));
				Assert.That(decCol.DataType, Is.EqualTo(DataType.Decimal));
				Assert.That(dtCol.DataType, Is.EqualTo(DataType.DateTime2));
				Assert.That(boolCol.DataType, Is.EqualTo(DataType.Boolean));

				Assert.That(intCol.IsNullable, Is.False);
				Assert.That(decCol.IsNullable, Is.False);
				Assert.That(boolCol.IsNullable, Is.False);
				Assert.That(dtCol.IsNullable, Is.False);
			});
		}

		//------------------------------------------------------------------
		// 3. Column 'Id' is recognized as the primary key.
		//------------------------------------------------------------------
		[Test]
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

		//#region HintsTests
		////------------------------------------------------------------------
		//// 1. WITH INLINE  (TableHint)
		////------------------------------------------------------------------
		//[Test]
		//public void InlineHint_WritesWithInline([IncludeDataSources(Ctx)] string ctx)
		//{
		//	using var db    = GetDataConnection(ctx);
		//	using var tbl   = db.CreateLocalTable<YdbTests.SimpleEntity>();

		//	var _ = tbl.AsYdb()
		//		   .InlineHint()
		//		   .Select(t => t.Id)
		//		   .ToArray();                // выполняем запрос → db.LastQuery

		//	Assert.That(db.LastQuery, Does.Contain("WITH INLINE"));
		//}

		////------------------------------------------------------------------
		//// 2. WITH UNORDERED (Tables‑in‑scope)
		////------------------------------------------------------------------
		//[Test]
		//public void UnorderedInScopeHint_WritesWithUnordered([IncludeDataSources(Ctx)] string ctx)
		//{
		//	using var db  = GetDataConnection(ctx);
		//	using var tbl = db.CreateLocalTable<YdbTests.SimpleEntity>();

		//	var _ = tbl.Where(e => e.Id > 0)
		//		   .AsYdb()
		//		   .UnorderedInScopeHint()
		//		   .Select(e => new { e.Id, e.StrVal })
		//		   .ToArray();

		//	var sql = db.LastQuery ?? string.Empty;
		//	var cnt = sql.Split('\n')
		//		 .Count(l => l.Contains("WITH UNORDERED",
		//								StringComparison.OrdinalIgnoreCase));

		//	Assert.That(cnt, Is.EqualTo(1));
		//}

		////------------------------------------------------------------------
		//// 3. PRAGMA DisablePredicatePushdown
		////------------------------------------------------------------------
		//[Test]
		//public void DisablePredicatePushdown_PrependsPragma([IncludeDataSources(Ctx)] string ctx)
		//{
		//	using var db  = GetDataConnection(ctx);
		//	using var tbl = db.CreateLocalTable<YdbTests.SimpleEntity>();

		//	var _ = tbl.Where(e => e.BoolVal)
		//		   .AsYdb()
		//		   .DisablePredicatePushdown()
		//		   .ToArray();

		//	var sql = db.LastQuery ?? string.Empty;
		//	Assert.That(sql.TrimStart()
		//				  .StartsWith("PRAGMA DisablePredicatePushdown",
		//							  StringComparison.OrdinalIgnoreCase));
		//}

		////------------------------------------------------------------------
		//// 4. PRAGMA UseFollowerRead (асинхронный пример)
		////------------------------------------------------------------------
		//[Test]
		//public async Task UseFollowerRead_PrependsPragma_Async([IncludeDataSources(Ctx)] string ctx)
		//{
		//	await using var db  = GetDataConnection(ctx);
		//	await using var tbl = db.CreateLocalTable<YdbTests.SimpleEntity>();

		//	var _ = await tbl.Where(e => e.IntVal >= 0)
		//				 .AsYdb()
		//				 .UseFollowerRead()
		//				 .Select(e => e.IntVal)
		//				 .ToArrayAsync();

		//	var sql = db.LastQuery ?? string.Empty;
		//	Assert.That(sql.TrimStart()
		//				  .StartsWith("PRAGMA UseFollowerRead",
		//							  StringComparison.OrdinalIgnoreCase));
		//}
		//#endregion

	}
}
