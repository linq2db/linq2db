using System;
using System.Linq;
using System.Linq.Expressions;
using System.Text.Json;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Async;
using LinqToDB.Data;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	internal sealed class DbObjectSetExtensionCallBuilder : Sql.IExtensionCallBuilder
	{
		public void Build(Sql.ISqlExtensionBuilder builder)
		{
			builder.Expression = "JSON_MODIFY({source}, {path}, {value})";

			builder.AddParameter("source", builder.GetExpression(0)!);

			var member = (MemberExpression) ((LambdaExpression) ((UnaryExpression) builder.Arguments[1]).Operand).Body;

			builder.AddParameter("path", $"$.{member.Member.Name}");

			var value = builder.EvaluateExpression(builder.Arguments[2]);

			builder.AddParameter("value", new SqlValue(builder.Arguments[2].Type, value));
		}
	}

	public static class DbObjectExtensions
	{
		[Sql.Extension("Set", ServerSideOnly = true, BuilderType = typeof(DbObjectSetExtensionCallBuilder))]
		public static Issue2418Tests.DbObject<T> Set<T, TValue>(this Issue2418Tests.DbObject<T> dbObject, Expression<Func<T, TValue>> propertyExpression, [SqlQueryDependent] TValue propertyValue)
			where T : class
		{
			return dbObject;
		}
	}

	[TestFixture]
	public class Issue2418Tests : TestBase
	{
		[Table("TestTable")]
		public class TestTable
		{
			[Column]
			public Guid Id { get; set; }

			[Column(DataType = DataType.NVarChar)]
			public DbObject<TestJson>? Json { get; set; }
		}

		public class TestJson
		{
			public int Number { get; set; }

			public string? String { get; set; }
		}

		public class DbObject<T>
			where T : class
		{
			public DbObject(T value)
			{
				Value = value;
			}

			public T Value { get; }
		}

		[Test]
		public async Task UpdateJsonValueTest([IncludeDataSources(false, TestProvName.AllSqlServer2016Plus)] string context)
		{
			var schema = new MappingSchema();

			schema.SetConverter<DbObject<TestJson>, string>(v => JsonSerializer.Serialize(v.Value));
			schema.SetConverter<DbObject<TestJson>, DataParameter>(v => new DataParameter
			{
				DataType = DataType.NVarChar,
				Value    = JsonSerializer.Serialize(v.Value)
			});
			schema.SetConverter<string, DbObject<TestJson>>(json => new DbObject<TestJson>(JsonSerializer.Deserialize<TestJson>(json)!));

			using var db = (DataConnection)GetDataContext(context, schema);
			using var table = db.CreateLocalTable<TestTable>();
			var newRecord = new TestTable()
			{
				Id   = TestData.Guid1,
				Json = new DbObject<TestJson>(new TestJson
				{
					String = "Test",
					Number = 1
				})
			};

			db.Insert(newRecord);

			var savedRecord = await table.FirstAsync(x => x.Id == newRecord.Id).ConfigureAwait(false);

			var newJson = new TestJson()
			{
				String = "Test1",
				Number = 10
			};

			await table
				.Where(o => o.Id == savedRecord.Id)
				.Set(o => o.Json, o => o.Json!
					.Set(j => j.Number, newJson.Number)
					.Set(j => j.String, newJson.String)
				).UpdateAsync()
				.ConfigureAwait(false);

			var lastQuery = db.LastQuery;

			savedRecord = await table.FirstAsync(x => x.Id == newRecord.Id).ConfigureAwait(false);

			//Assert.AreEqual(savedRecord.Json.Value, newJson);

			Assert.That(lastQuery, Contains.Substring("JSON_MODIFY(JSON_MODIFY"));
		}
	}
}
