using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider.Ydb;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

using NUnit.Framework;

namespace Tests.DataProvider
{
	[TestFixture]
	public class YdbTests : DataProviderTestBase

	{
		private const string Ctx = "YDB";           // имя контекста из DataProviders.json
		private const string PingSql = "SELECT 1";

		//------------------------------------------------------------------
		// 2.  Явная проверка YdbTools + round-trip строки подключения.
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
		// 3.  Строка берётся из DataProviders.json, когда указан только контекст.
		//------------------------------------------------------------------
		[Test]
		public void ConnectionStringFromConfiguration()
		{
			var expected = GetConnectionString(Ctx);

			using var db = GetDataConnection(Ctx);

			Assert.That(db.ConnectionString, Is.EqualTo(expected));
		}

		//------------------------------------------------------------------
		// 4. Ping sync / async (пример использования wrappers).
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
		//  ADAPTER
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
		//  SCALAR-TYPES
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
		//  DECIMAL-LITERAL
		//------------------------------------------------------------------
		[Test]
		public void DecimalLiteralBuilder_ShouldRespectSqlDataTypeDefaults()
		{
			var mi = typeof(YdbMappingSchema)
					.GetMethod("BuildDecimalLiteral", BindingFlags.Static | BindingFlags.NonPublic)!;

			var sb  = new StringBuilder();
			var val = 123.45m;

			// SqlDataType by default Precision=29, Scale=10.
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
		//  SYNC BulkCopy
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
		//  ASYNC BulkCopy
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

	}
}
