using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider.SqlServer;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.SqlQuery;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

using Tests.DataProvider;

using Tests.Model;

namespace Tests.Linq
{
	public class CachingTests: TestBase
	{
		sealed class AggregateFuncBuilder : Sql.IExtensionCallBuilder
		{
			public void Build(Sql.ISqlExtensionBuilder builder)
			{
				builder.AddFragment("funcName",  builder.GetValue<string>("funcName"));
				builder.AddFragment("fieldName", builder.GetValue<string>("fieldName"));
			}
		}

		[Sql.Extension("{funcName}({fieldName})", BuilderType = typeof(AggregateFuncBuilder), ServerSideOnly = true)]
		static double AggregateFunc([SqlQueryDependent] string funcName, [SqlQueryDependent] string fieldName)
		{
			throw new ServerSideOnlyException(nameof(AggregateFunc));
		}

		[Test]
		public void TestSqlQueryDependent(
			[IncludeDataSources(ProviderName.SQLiteClassic)] string context,
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
				nameof(ALLTYPE.INTDATATYPE),
				nameof(ALLTYPE.REALDATATYPE)
			)] string fieldName)
		{
			using var db = GetDataContext(context);
			var query =
					from t in db.GetTable<ALLTYPE>()
					from c in db.GetTable<Child>()
					select new
					{
						Aggregate = AggregateFunc(funcName, fieldName)
					};

			query.ToArray();
			var sql = query.ToSqlQuery().Sql;

			Assert.That(sql, Contains.Substring(funcName).And.Contains(fieldName));
		}

		[Test]
		public void TestSqlQueryDependent_DecFloat(
			[IncludeDataSources(TestProvName.AllDB2)] string context,
			[Values(
				"MIN",
				"MAX",
				"AVG",
				"COUNT"
			)] string funcName,
			[Values(
				nameof(ALLTYPE.DECFLOATDATATYPE)
			)] string fieldName)
		{
			using var db = GetDataContext(context);
			var query =
					from t in db.GetTable<ALLTYPE>()
					from c in db.GetTable<Child>()
					select new
					{
						Aggregate = AggregateFunc(funcName, fieldName)
					};

			query.ToArray();
			var sql = query.ToSqlQuery().Sql;

			Assert.That(sql, Contains.Substring(funcName).And.Contains(fieldName));
		}

		[Test]
		public void TestSqlQueryDependent_Time(
			[IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context,
			[Values(
				"MIN",
				"MAX",
				// SQL Server doesn't support AVG(time)
				//"AVG",
				"COUNT"
			)] string funcName)
		{
			const string fieldName = "timeDataType";

			using var db = GetDataContext(context);
			var query =
					from t in db
						.GetTable<ALLTYPE>()
						.TableName("AllTypes")
					from c in db.GetTable<Child>()
					select new
					{
						Aggregate = AggregateFunc(funcName, fieldName)
					};

			query.ToArray();
			var sql = query.ToSqlQuery().Sql;

			Assert.That(sql, Contains.Substring(funcName).And.Contains(fieldName));
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

			if (subString.Length != 0)
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
			[IncludeDataSources(TestProvName.AllSqlServer)] string context,
			[Values("tableName1", "tableName2")] string tableName,
			[Values("database1",  "database2")]  string databaseName,
			[Values("schema1",    "schema2")]    string schemaName
		)
		{
			using var db = GetDataContext(context);
			var query =
					from c in db.Child
					from cc in (
						from c1 in GetTestTable<Child>(db, tableName, databaseName, schemaName)
						from c2 in GetTestTable<Child>(db, tableName, databaseName, schemaName)
						select new {c1, c2}
					)
					select cc;

			var sql = query.ToSqlQuery().Sql;
			BaselinesManager.LogQuery(sql);
			using (Assert.EnterMultipleScope())
			{
				Assert.That(CountOccurrences(sql, tableName), Is.EqualTo(2));
				Assert.That(CountOccurrences(sql, databaseName), Is.EqualTo(2));
				Assert.That(CountOccurrences(sql, schemaName), Is.EqualTo(2));
			}
		}

		[Test]
		public void TestInlined(
			[IncludeDataSources(TestProvName.AllSqlServer)] string context,
			[Values("tableName1", "tableName2")] string tableName,
			[Values("database1",  "database2")]  string databaseName,
			[Values("schema1",    "schema2")]    string schemaName
		)
		{
			using var db = GetDataContext(context);
			var query =
					from c in db.Child
					from cc in
					(
						from c1 in db.Child.DatabaseName(databaseName).SchemaName(schemaName).TableName(tableName)
						from c2 in db.Child.DatabaseName(databaseName).SchemaName(schemaName).TableName(tableName)
						select new {c1, c2}
					)
					select cc;

			var sql = query.ToSqlQuery().Sql;
			BaselinesManager.LogQuery(sql);
			using (Assert.EnterMultipleScope())
			{
				Assert.That(CountOccurrences(sql, tableName), Is.EqualTo(2));
				Assert.That(CountOccurrences(sql, databaseName), Is.EqualTo(2));
				Assert.That(CountOccurrences(sql, schemaName), Is.EqualTo(2));
			}
		}

		[Test]
		public void TakeHint(
			[IncludeDataSources(TestProvName.AllSqlServer, TestProvName.AllClickHouse, TestProvName.AllAccessLibRed)] string context,
			[Values(TakeHints.Percent, TakeHints.WithTies, TakeHints.Percent | TakeHints.WithTies)] TakeHints takeHint)
		{
			if (takeHint.HasFlag(TakeHints.Percent) && context.IsAnyOf(TestProvName.AllClickHouse))
				Assert.Inconclusive($"ClickHouse doesn't support '{takeHint}' hint");

			using var db = GetDataContext(context);
			var query =
					from c1 in db.Child
					from c2 in db.Child.OrderBy(r => r.ParentID).Take(10, takeHint)
					select new {c1, c2};

			query.ToArray();
			var sql = query.ToSqlQuery().Sql;

			if (takeHint.HasFlag(TakeHints.Percent))
				Assert.That(sql, Contains.Substring("PERCENT"));

			if (takeHint.HasFlag(TakeHints.WithTies))
				Assert.That(sql, Contains.Substring("WITH TIES"));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4266"), QueryCacheTest]
		public void TestExtensionCollectionParameterSameQuery([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			// the miss counts below need a query no earlier test cached
			LinqToDB.Internal.Linq.Query.ClearCaches();

			using var db = GetDataContext(context);

			db.Execute("IF EXISTS (SELECT * FROM sys.types WHERE name = 'IntTableType') DROP TYPE IntTableType");
			db.Execute("CREATE TYPE IntTableType AS TABLE(Id INT)");

			try
			{
				var persons = new List<int>() { 1, 2 };
				var query = from p in db.GetTable<Person>()
							where InExt(p.ID, persons)
							orderby p.ID
							select p.ID;

				var currentMiss = query.GetCacheMissCount();

				var result =  query.ToList();
				AreEqual(persons, result);
				Assert.That(query.GetCacheMissCount(), Is.EqualTo(currentMiss + 1));

				persons.AddRange(new int[] { 3, 4 });

				result = query.ToList();

				AreEqual(persons, result);
				// cache miss
				Assert.That(query.GetCacheMissCount(), Is.EqualTo(currentMiss + 2));
			}
			finally
			{
				db.Execute("IF EXISTS (SELECT * FROM sys.types WHERE name = 'IntTableType') DROP TYPE IntTableType");
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4266"), QueryCacheTest]
		public void TestExtensionCollectionParameterEqualQuery([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			// the miss counts below need a query no earlier test cached
			LinqToDB.Internal.Linq.Query.ClearCaches();

			using var db = GetDataContext(context);

			db.Execute("IF EXISTS (SELECT * FROM sys.types WHERE name = 'IntTableType') DROP TYPE IntTableType");
			db.Execute("CREATE TYPE IntTableType AS TABLE(Id INT)");

			try
			{
				var persons = new List<int>() { 1, 2 };
				var query = from p in db.GetTable<Person>()
							where InExt(p.ID, persons)
							orderby p.ID
							select p.ID;

				var currentMiss = query.GetCacheMissCount();

				var result =  query.ToList();

				AreEqual(persons, result);
				Assert.That(query.GetCacheMissCount(), Is.EqualTo(currentMiss + 1));

				persons.AddRange(new int[] { 3, 4 });

				query = from p in db.GetTable<Person>()
						where InExt(p.ID, persons)
						orderby p.ID
						select p.ID;

				result = query.ToList();

				AreEqual(persons, result);
				// cache miss
				Assert.That(query.GetCacheMissCount(), Is.EqualTo(currentMiss + 2));
			}
			finally
			{
				db.Execute("IF EXISTS (SELECT * FROM sys.types WHERE name = 'IntTableType') DROP TYPE IntTableType");
			}
		}

		[Sql.Extension("{field} IN ({values, ', '})", IsPredicate = true, BuilderType = typeof(InlineValuesBuilder), ServerSideOnly = true)]
		static bool InValues([ExprParameter] int field, [SqlQueryDependent] IEnumerable<int> values) => throw new ServerSideOnlyException(nameof(InValues));

		// The values are inlined: the SQL depends on them.
		sealed class InlineValuesBuilder : Sql.IExtensionCallBuilder
		{
			public void Build(Sql.ISqlExtensionBuilder builder)
			{
				foreach (var value in builder.GetValue<IEnumerable<int>>("values"))
					builder.AddParameter("values", new SqlValue(value));
			}
		}

		int[] ExpectedPersonIds(IEnumerable<int> ids)
		{
			return Person
				.Select(p => p.ID)
				.Where(ids.Contains)
				.OrderBy(id => id)
				.ToArray();
		}

		static int[] PersonIdsIn(ITestDataContext db, IEnumerable<int> ids)
		{
			var query =
				from p in db.Person
				where InValues(p.ID, ids)
				select p.ID;

			return query
				.AsEnumerable()
				.OrderBy(id => id)
				.ToArray();
		}

		// A [SqlQueryDependent] collection that is not an array: the query built for one collection must not answer for
		// another. PostgreSQL maps such a list as a scalar (an array type), the other providers do not.
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4266"), QueryCacheTest]
		public void SqlQueryDependentCollection_OtherValues([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllPostgreSQL)] string context)
		{
			using var db = GetDataContext(context);

			foreach (var ids in new[] { new List<int> { 1, 2 }, new List<int> { 3, 4 }, new List<int> { 1, 2 } })
			{
				PersonIdsIn(db, ids)
					.ShouldBe(ExpectedPersonIds(ids), $"[{string.Join(", ", ids)}]");
			}
		}

		// The same collection - a list or an array - changed in place after the query was built.
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4266"), QueryCacheTest]
		public void SqlQueryDependentCollection_ChangedInPlace(
			[IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllPostgreSQL)] string context,
			[Values]                                                                       bool   sameQueryable,
			[Values]                                                                       bool   array)
		{
			using var db = GetDataContext(context);

			IList<int> ids = array ? new[] { 1, 2 } : new List<int> { 1, 2 };

			var query =
				from p in db.Person
				where InValues(p.ID, ids)
				select p.ID;

			query
				.AsEnumerable()
				.OrderBy(id => id)
				.ToArray()
				.ShouldBe(ExpectedPersonIds(ids), "before the change");

			ids[0] = 3;
			ids[1] = 4;

			if (!sameQueryable)
			{
				query =
					from p in db.Person
					where InValues(p.ID, ids)
					select p.ID;
			}

			query
				.AsEnumerable()
				.OrderBy(id => id)
				.ToArray()
				.ShouldBe(ExpectedPersonIds(ids), "after the change");
		}

		static readonly Func<IDataContext, IList<int>, IQueryable<int>> _compiledPersonIdsIn =
			CompiledQuery.Compile((IDataContext db, IList<int> ids) =>
				from p in db.GetTable<Person>()
				where InValues(p.ID, ids)
				select p.ID);

		// The same through a compiled query, whose cache key holds the argument itself.
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4266"), QueryCacheTest]
		public void SqlQueryDependentCollection_ChangedInPlace_CompiledQuery(
			[IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllPostgreSQL)] string context,
			[Values]                                                                       bool   array)
		{
			using var db = GetDataContext(context);

			IList<int> ids = array ? new[] { 1, 2 } : new List<int> { 1, 2 };

			_compiledPersonIdsIn(db, ids)
				.AsEnumerable()
				.OrderBy(id => id)
				.ToArray()
				.ShouldBe(ExpectedPersonIds(ids), "before the change");

			ids[0] = 3;
			ids[1] = 4;

			_compiledPersonIdsIn(db, ids)
				.AsEnumerable()
				.OrderBy(id => id)
				.ToArray()
				.ShouldBe(ExpectedPersonIds(ids), "after the change");
		}

		// A [SqlQueryDependent] object: a SqlType is created anew by every query, and equal types share one cached query.
		[Test(Description = "https://github.com/linq2db/linq2db/issues/5810"), QueryCacheTest]
		public void SqlQueryDependentObject_EqualSqlTypesShareQuery()
		{
			LinqToDB.Internal.Linq.Query.ClearCaches();

			using var db = SqlServerConnectionForSqlOnly();

			IQueryable<string?> Names(int size)
			{
				return
					from p in db.GetTable<Person>()
					select SqlFn.Cast(p.FirstName, SqlType.VarChar(size));
			}

			var query     = Names(10);
			var missCount = query.GetCacheMissCount();

			foreach (var size in new[] { 10, 10, 20, 10 })
			{
				Names(size)
					.ToSqlQuery()
					.Sql
					.ShouldContain($"varchar({size})");
			}

			(query.GetCacheMissCount() - missCount)
				.ShouldBe(2);
		}

		// The same for a data type given as a function - a method group or a lambda: every query creates the delegate
		// anew, and functions that return equal types share one cached query.
		[Test(Description = "https://github.com/linq2db/linq2db/issues/5810"), QueryCacheTest]
		public void SqlQueryDependentObject_EqualSqlTypeFunctionsShareQuery([Values] bool lambda)
		{
			LinqToDB.Internal.Linq.Query.ClearCaches();

			using var db = SqlServerConnectionForSqlOnly();

			IQueryable<string?> Names(bool unicode)
			{
				if (lambda)
				{
					if (unicode)
					{
						return
							from p in db.GetTable<Person>()
							select SqlFn.Cast(p.FirstName, () => SqlType.NVarChar());
					}

					return
						from p in db.GetTable<Person>()
						select SqlFn.Cast(p.FirstName, () => SqlType.VarChar());
				}

				if (unicode)
				{
					return
						from p in db.GetTable<Person>()
						select SqlFn.Cast(p.FirstName, SqlType.NVarChar);
				}

				return
					from p in db.GetTable<Person>()
					select SqlFn.Cast(p.FirstName, SqlType.VarChar);
			}

			var query     = Names(false);
			var missCount = query.GetCacheMissCount();

			foreach (var unicode in new[] { false, false, true, false })
			{
				Names(unicode)
					.ToSqlQuery()
					.Sql
					.ShouldContain(unicode ? " as nvarchar)" : " as varchar)");
			}

			(query.GetCacheMissCount() - missCount)
				.ShouldBe(2);
		}

		// The same for a lambda that reads a variable, which changes between the queries: each query takes the type the
		// function returns when the query runs.
		[Test(Description = "https://github.com/linq2db/linq2db/issues/5810"), QueryCacheTest]
		public void SqlQueryDependentObject_SqlTypeFunctionReadsVariable()
		{
			LinqToDB.Internal.Linq.Query.ClearCaches();

			using var db = SqlServerConnectionForSqlOnly();

			var size = 10;

			IQueryable<string?> Names()
			{
				return
					from p in db.GetTable<Person>()
					select SqlFn.Cast(p.FirstName, () => SqlType.VarChar(size));
			}

			var query     = Names();
			var missCount = query.GetCacheMissCount();

			foreach (var newSize in new[] { 10, 10, 20, 10 })
			{
				size = newSize;

				Names()
					.ToSqlQuery()
					.Sql
					.ShouldContain($"varchar({newSize})");
			}

			(query.GetCacheMissCount() - missCount)
				.ShouldBe(2);
		}

		// SQL generation only: nothing connects.
		static DataConnection SqlServerConnectionForSqlOnly()
		{
			var options = new DataOptions()
				.UseSqlServer("Server=.", SqlServerVersion.v2019, SqlServerProvider.MicrosoftDataSqlClient);

			return new DataConnection(options);
		}

		interface IPersonIdLimit
		{
			int Value { get; }
		}

		sealed class PersonIdLimit(int value) : IPersonIdLimit
		{
			public int Value => value;
		}

		sealed record PersonIdLimitWithEquals(int Value) : IPersonIdLimit;

		// The limit is inlined: the SQL depends on it.
		sealed class InlineLimitBuilder : Sql.IExtensionCallBuilder
		{
			public void Build(Sql.ISqlExtensionBuilder builder)
			{
				builder.AddParameter("limit", new SqlValue(builder.GetValue<IPersonIdLimit>("limit").Value));
			}
		}

		[Sql.Extension("{field} <= {limit}", IsPredicate = true, BuilderType = typeof(InlineLimitBuilder), ServerSideOnly = true)]
		static bool AtMost([ExprParameter] int field, [SqlQueryDependent] IPersonIdLimit limit) => throw new ServerSideOnlyException(nameof(AtMost));

		// Limits 2, 2, 3, 2: with Equals a new instance each time, without it the same instance for the same limit.
		static IPersonIdLimit[] PersonIdLimits(bool withEquals)
		{
			if (withEquals)
			{
				return [new PersonIdLimitWithEquals(2), new PersonIdLimitWithEquals(2), new PersonIdLimitWithEquals(3), new PersonIdLimitWithEquals(2)];
			}

			var two   = new PersonIdLimit(2);
			var three = new PersonIdLimit(3);

			return [two, two, three, two];
		}

		int[] ExpectedPersonIdsAtMost(int limit)
		{
			return Person
				.Select(p => p.ID)
				.Where(id => id <= limit)
				.OrderBy(id => id)
				.ToArray();
		}

		static int[] SortedIds(IQueryable<int> query)
		{
			return query
				.AsEnumerable()
				.OrderBy(id => id)
				.ToArray();
		}

		// A [SqlQueryDependent] object of a user type: one without Equals compares by reference, one with Equals by value.
		// The query built for one limit must not answer for another, and equal limits share one cached query.
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4266"), QueryCacheTest]
		public void SqlQueryDependentObject_UserType(
			[IncludeDataSources(true, TestProvName.AllSQLite)] string context,
			[Values]                                           bool   withEquals)
		{
			LinqToDB.Internal.Linq.Query.ClearCaches();

			using var db = GetDataContext(context);

			IQueryable<int> PersonIdsAtMost(IPersonIdLimit limit)
			{
				return
					from p in db.Person
					where AtMost(p.ID, limit)
					select p.ID;
			}

			var limits    = PersonIdLimits(withEquals);
			var query     = PersonIdsAtMost(limits[0]);
			var missCount = query.GetCacheMissCount();

			foreach (var limit in limits)
			{
				SortedIds(PersonIdsAtMost(limit))
					.ShouldBe(ExpectedPersonIdsAtMost(limit.Value), $"limit {limit.Value}");
			}

			(query.GetCacheMissCount() - missCount)
				.ShouldBe(2);
		}

		static readonly Func<IDataContext, IPersonIdLimit, IQueryable<int>> _compiledPersonIdsAtMost =
			CompiledQuery.Compile((IDataContext db, IPersonIdLimit limit) =>
				from p in db.GetTable<Person>()
				where AtMost(p.ID, limit)
				select p.ID);

		// The same through a compiled query, whose cache key holds the argument itself.
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4266"), QueryCacheTest]
		public void SqlQueryDependentObject_UserType_CompiledQuery(
			[IncludeDataSources(true, TestProvName.AllSQLite)] string context,
			[Values]                                           bool   withEquals)
		{
			using var db = GetDataContext(context);

			var queries = new Dictionary<int, object>();

			foreach (var limit in PersonIdLimits(withEquals))
			{
				var query = _compiledPersonIdsAtMost(db, limit);
				var info  = query.GetCompiledQueryInfo();

				if (queries.TryGetValue(limit.Value, out var first))
				{
					info.ShouldBeSameAs(first, $"limit {limit.Value}");
				}
				else
				{
					queries.Add(limit.Value, info);
				}

				SortedIds(query)
					.ShouldBe(ExpectedPersonIdsAtMost(limit.Value), $"limit {limit.Value}");
			}

			queries[3].ShouldNotBeSameAs(queries[2]);
		}

		// IEquatable<T>.Equals deliberately disagrees with the inherited field-wise Equals(object).
#pragma warning disable CA1067 // the test needs IEquatable<T>.Equals and Equals(object) to differ
		readonly struct LooseValue(int n) : IEquatable<LooseValue>
		{
			public int N { get; } = n;

			public bool Equals(LooseValue other)
			{
				return N % 10 == other.N % 10;
			}
		}
#pragma warning restore CA1067

		sealed class BakeBuilder : Sql.IExtensionCallBuilder
		{
			public void Build(Sql.ISqlExtensionBuilder builder)
			{
				builder.ResultExpression = new SqlValue(builder.GetValue<LooseValue>(0).N);
			}
		}

		[Sql.Extension("", BuilderType = typeof(BakeBuilder), ServerSideOnly = true)]
		static int Bake(LooseValue value) => throw new ServerSideOnlyException(nameof(Bake));

		// A value an extension builder bakes into the SQL is compared by Equals(object), as it always was, not by
		// IEquatable<T>.Equals.
		[Test(Description = "https://github.com/linq2db/linq2db/issues/6303"), QueryCacheTest]
		public void BakedStructValue_ComparedByEqualsObject([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			LinqToDB.Internal.Linq.Query.ClearCaches();

			using var db = GetDataContext(context);

			var value = new LooseValue(1);

			db
				.Select(() => Bake(value))
				.ShouldBe(1);

			value = new LooseValue(11);

			db
				.Select(() => Bake(value))
				.ShouldBe(11);
		}

		[Sql.Extension("{field} IN (select * from {values})", IsPredicate = true, BuilderType = typeof(InExtExpressionItemBuilder), ServerSideOnly = true)]
		private static bool InExt<T>([ExprParameter] T field, [SqlQueryDependent] IEnumerable<T> values) where T : struct, IEquatable<int>
		{
			throw new ServerSideOnlyException(nameof(InExt));
		}

		public sealed class InExtExpressionItemBuilder : Sql.IExtensionCallBuilder
		{
			public void Build(Sql.ISqlExtensionBuilder builder)
			{
				var parameterName = (builder.Arguments[1] as MemberExpression)?.Member.Name ?? "p";

				var values = builder.GetValue<System.Collections.IEnumerable>("values")?.OfType<int>().ToArray();

				if (values == null)
				{
#pragma warning disable CA2208 // Instantiate argument exceptions correctly
					throw new ArgumentNullException("values", "Values for \"In/Any\" operation should not be empty");
#pragma warning restore CA2208 // Instantiate argument exceptions correctly
				}

				using var dataTable = new DataTable("IntTableType");
				dataTable.Columns.Add("Id", typeof(int));

				foreach (var x in values.Distinct())
				{
					var newRow = dataTable.Rows.Add();
					newRow[0] = x;
				}

				dataTable.AcceptChanges();

				var param = new SqlParameter(new DbDataType(dataTable.GetType() ?? typeof(object), "IntTableType"), parameterName, dataTable);

				builder.AddParameter("values", param);
			}
		}

		[Sql.Extension("{field} IN (select * from {values})", IsPredicate = true, ServerSideOnly = true)]
		private static bool InExtClass<T>([ExprParameter] T field, [ExprParameter] IntArrayClass values) where T : struct, IEquatable<int> => throw new ServerSideOnlyException(nameof(InExtClass));

		[Sql.Extension("{field} IN (select * from {values})", IsPredicate = true, ServerSideOnly = true)]
		private static bool InExtStruct<T>([ExprParameter] T field, [ExprParameter] IntArrayStruct values) where T : struct, IEquatable<int> => throw new ServerSideOnlyException(nameof(InExtStruct));

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4266"), QueryCacheTest]
		public void Issue4266Test_Class([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			var ms = new MappingSchema();
			ms.SetConverter<IntArrayClass, DataParameter>(v => v.CreateParameter());
			ms.AddScalarType(typeof(IntArrayClass), new SqlDataType(IntArrayClass.Type));

			using var db = GetDataContext(context, ms);

			db.Execute("IF EXISTS (SELECT * FROM sys.types WHERE name = 'IntTableType') DROP TYPE IntTableType");
			db.Execute("CREATE TYPE IntTableType AS TABLE(Id INT)");

			try
			{
				var persons = new List<int>() { 1, 2 };
				var query = from p in db.GetTable<Person>()
							where InExtClass(p.ID, persons) && InExtClass(p.ID, new IntArrayClass(persons))
							orderby p.ID
							select p.ID;

				var currentMiss = query.GetCacheMissCount();

				var result =  query.ToList();
				AreEqual(persons, result);
				Assert.That(query.GetCacheMissCount(), Is.EqualTo(currentMiss + 1));

				persons.AddRange(new int[] { 3, 4 });

				result = query.ToList();

				AreEqual(persons, result);
				Assert.That(query.GetCacheMissCount(), Is.EqualTo(currentMiss + 1));
			}
			finally
			{
				db.Execute("IF EXISTS (SELECT * FROM sys.types WHERE name = 'IntTableType') DROP TYPE IntTableType");
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4266"), QueryCacheTest]
		public void Issue4266Test_Struct([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			var ms = new MappingSchema();
			ms.SetConverter<IntArrayStruct, DataParameter>(v => v.CreateParameter());
			ms.SetDataType(typeof(IntArrayStruct), new SqlDataType(IntArrayStruct.Type));

			using var db = GetDataContext(context, ms);

			db.Execute("IF EXISTS (SELECT * FROM sys.types WHERE name = 'IntTableType') DROP TYPE IntTableType");
			db.Execute("CREATE TYPE IntTableType AS TABLE(Id INT)");

			try
			{
				var persons = new List<int>() { 1, 2 };
				var query = from p in db.GetTable<Person>()
							where InExtStruct(p.ID, persons) && InExtStruct(p.ID, new IntArrayStruct(persons))
							orderby p.ID
							select p.ID;

				var currentMiss = query.GetCacheMissCount();

				var result =  query.ToList();
				AreEqual(persons, result);
				Assert.That(query.GetCacheMissCount(), Is.EqualTo(currentMiss + 1));

				persons.AddRange(new int[] { 3, 4 });

				result = query.ToList();

				AreEqual(persons, result);
				Assert.That(query.GetCacheMissCount(), Is.EqualTo(currentMiss + 1));
			}
			finally
			{
				db.Execute("IF EXISTS (SELECT * FROM sys.types WHERE name = 'IntTableType') DROP TYPE IntTableType");
			}
		}

		sealed class IntArrayClass(IEnumerable<int> values)
		{
			public static readonly DbDataType Type = new DbDataType(typeof(DataTable), "IntTableType");

			public DataParameter CreateParameter()
			{
				using var dataTable = new DataTable("IntTableType");
				dataTable.Columns.Add("Id", typeof(int));

				foreach (var x in values.Distinct())
				{
					var newRow = dataTable.Rows.Add();
					newRow[0] = x;
				}

				dataTable.AcceptChanges();

				return new DataParameter(null, dataTable);
			}

			public static implicit operator IntArrayClass(List<int> values) => new(values);
		}

		struct IntArrayStruct(IEnumerable<int> values)
		{
			public static readonly DbDataType Type = new DbDataType(typeof(DataTable), "IntTableType");

			public readonly DataParameter CreateParameter()
			{
				using var dataTable = new DataTable("IntTableType");
				dataTable.Columns.Add("Id", typeof(int));

				foreach (var x in values.Distinct())
				{
					var newRow = dataTable.Rows.Add();
					newRow[0] = x;
				}

				dataTable.AcceptChanges();

				return new DataParameter(null, dataTable);
			}

			public static implicit operator IntArrayStruct(List<int> values) => new(values);
		}
	}
}
