using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Expressions;

using NUnit.Framework;

namespace Tests.Linq
{
	using DataProvider;
	using Model;

	public class CachingTests: TestBase
	{
		class AggregateFuncBuilder: Sql.IExtensionCallBuilder
		{
			public void Build(Sql.ISqExtensionBuilder builder)
			{
				builder.AddExpression("funcName",  builder.GetValue<string>("funcName"));
				builder.AddExpression("fieldName", builder.GetValue<string>("fieldName"));
			}
		}

		[Sql.Extension("{funcName}({fieldName})", BuilderType = typeof(AggregateFuncBuilder), ServerSideOnly = true)]
		static double AggregateFunc([SqlQueryDependent] string funcName, [SqlQueryDependent] string fieldName)
		{
			throw new NotImplementedException();
		}

		[Test]
		public void TesSqlQueryDependent(
			[Values(
				"MIN",
				"MAX",
				"AVG",
				"COUNT"
			)] string funcName,
			[Values(
				nameof(ALLTYPE.ID),
				nameof(ALLTYPE.BIGINTDATATYPE),
				nameof(ALLTYPE.SMALLINTDATATYPE),
				nameof(ALLTYPE.DECIMALDATATYPE),
				nameof(ALLTYPE.DECFLOATDATATYPE),
				nameof(ALLTYPE.INTDATATYPE),
				nameof(ALLTYPE.REALDATATYPE),
				nameof(ALLTYPE.TIMEDATATYPE)
			)] string fieldName)
		{
			if (!UserProviders.Contains(ProviderName.SQLiteClassic))
				return;

			using (var db = GetDataContext(ProviderName.SQLiteClassic))
			{
				var query =
					from t in db.GetTable<ALLTYPE>()
					from c in db.GetTable<Child>()
					select new
					{
						Aggregate = AggregateFunc(funcName, fieldName)
					};

				var sql = query.ToString();
				Console.WriteLine(sql);

				Assert.That(sql, Contains.Substring(funcName).And.Contains(fieldName));
			}
		}

		static IQueryable<T> GetTestTable<T>(IDataContext context,
			string tableName,
			string databaseName,
			string schemaName)
		where T : class
		{
			return context.GetTable<T>().DatabaseName(databaseName).SchemaName(schemaName)
				.TableName(tableName);
		}

		static int CountOccurrences(string source, string subString)
		{
			var count = 0;
			var n     = 0;

			if (subString != "")
			{
				while ((n = source.IndexOf(subString, n, StringComparison.Ordinal)) != -1)
				{
					n += subString.Length;
					++count;
				}
			}

			return count;
		}

		[Test]
		public void TestByCall(
			[Values("tableName1", "tableName2")] string tableName,
			[Values("database1",  "database2")]  string databaseName,
			[Values("schema1",    "schema2")]    string schemaName
		)
		{
			if (!UserProviders.Contains(ProviderName.SqlServer))
				return;

			using (var db = GetDataContext(ProviderName.SqlServer))
			{
				var query =
					from c in db.Child
					from cc in (
						from c1 in GetTestTable<Child>(db, tableName, databaseName, schemaName)
						from c2 in GetTestTable<Child>(db, tableName, databaseName, schemaName)
						select new {c1, c2}
					)
					select cc;

				var sql = query.ToString();
				Console.WriteLine(sql);

				Assert.That(CountOccurrences(sql, tableName),    Is.EqualTo(2));
				Assert.That(CountOccurrences(sql, databaseName), Is.EqualTo(2));
				Assert.That(CountOccurrences(sql, schemaName),   Is.EqualTo(2));
			}
		}

		[Test]
		public void TestInlined(
			[Values("tableName1", "tableName2")] string tableName,
			[Values("database1",  "database2")]  string databaseName,
			[Values("schema1",    "schema2")]    string schemaName
		)
		{
			if (!UserProviders.Contains(ProviderName.SqlServer))
				return;

			using (var db = GetDataContext(ProviderName.SqlServer))
			{
				var query =
					from c in db.Child
					from cc in
					(
						from c1 in db.Child.DatabaseName(databaseName).SchemaName(schemaName).TableName(tableName)
						from c2 in db.Child.DatabaseName(databaseName).SchemaName(schemaName).TableName(tableName)
						select new {c1, c2}
					)
					select cc;

				var sql = query.ToString();
				Console.WriteLine(sql);

				Assert.That(CountOccurrences(sql, tableName),    Is.EqualTo(2));
				Assert.That(CountOccurrences(sql, databaseName), Is.EqualTo(2));
				Assert.That(CountOccurrences(sql, schemaName),   Is.EqualTo(2));
			}
		}

		[Test]
		public void TakeHint(
			[Values(TakeHints.Percent, TakeHints.WithTies, TakeHints.Percent | TakeHints.WithTies)] TakeHints takeHint
		)
		{
			if (!UserProviders.Contains(ProviderName.SqlServer))
				return;

			using (var db = GetDataContext(ProviderName.SqlServer))
			{
				var query =
					from c1 in db.Child
					from c2 in db.Child.Take(10, takeHint)
					select new {c1, c2};

				var sql = query.ToString();
				Console.WriteLine(sql);

				if (takeHint.HasFlag(TakeHints.Percent))
					Assert.That(sql, Contains.Substring("PERCENT"));

				if (takeHint.HasFlag(TakeHints.WithTies))
					Assert.That(sql, Contains.Substring("WITH TIES"));
			}
		}
	}
}
