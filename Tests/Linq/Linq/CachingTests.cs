using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Expressions;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.SqlQuery;
using LinqToDB.Mapping;

using NUnit.Framework;

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

		[ActiveIssue(4266, ErrorMessage = "Assert.That(query.GetCacheMissCount(), Is.EqualTo(currentMiss + 1))",
			Details = "the collection parameter does not take part in the cache key, so a query that should miss is reused - #4266's subject.")]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4266"), QueryCacheTest]
		public void TestExtensionCollectionParameterSameQuery([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
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

		[ActiveIssue(4266, ErrorMessage = "Expected and result lists are different",
			Details = "the reused query answers with the previous collection's rows - the wrong-results half of #4266.")]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4266"), QueryCacheTest]
		public void TestExtensionCollectionParameterEqualQuery([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
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
		public void QueryCacheExtensionParameterClass([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
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
		public void QueryCacheExtensionParameterStruct([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
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

		// test handling of DefaultExpression in queries
		// roslyn compiler replaces default with constant value, so we need to restore it from constant before execution
		[Test(Description = "https://github.com/linq2db/linq2db/issues/3148"), QueryCacheTest]
		public void CachedDefaultInConditionalProjection([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context, [Values] bool withDefault)
		{
			using var db = GetDataContext(context);
			var query1 = db.Person
					.Select(p => new Person
					{
						Patient = p.Patient != null ? p.Patient : default(Patient)
					});
			var query2 = db.Person
					.Select(p => new Person
					{
						Patient = p.Patient != null ? p.Patient : default(Patient)
					});

			if (withDefault)
			{
				query1 = query1.Provider.CreateQuery<Person>(RestoreDefaultExpressions(query1.Expression));
				query2 = query2.Provider.CreateQuery<Person>(RestoreDefaultExpressions(query2.Expression));
			}

			query1.ToArray();
			var cacheMiss = db.Person.GetCacheMissCount();
			query2.ToArray();
			Assert.That(db.Person.GetCacheMissCount(), Is.EqualTo(cacheMiss));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3148"), QueryCacheTest]
		public void CachedDefaultInWhereIntComparison([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context, [Values] bool withDefault)
		{
			using var db = GetDataContext(context);
			var query1 = db.Person
					.Where(r => r.ID != default);
			var query2 = db.Person
					.Where(r => r.ID != default);

			if (withDefault)
			{
				query1 = query1.Provider.CreateQuery<Person>(RestoreDefaultExpressions(query1.Expression));
				query2 = query2.Provider.CreateQuery<Person>(RestoreDefaultExpressions(query2.Expression));
			}

			query1.ToArray();
			var cacheMiss = db.Person.GetCacheMissCount();
			query2.ToArray();
			Assert.That(db.Person.GetCacheMissCount(), Is.EqualTo(cacheMiss));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3148"), QueryCacheTest]
		public void CachedDefaultInWhereStringComparison([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context, [Values] bool withDefault)
		{
			using var db = GetDataContext(context);
			var query1 = db.Person
					.Where(r => r.MiddleName != default);
			var query2 = db.Person
					.Where(r => r.MiddleName != default);

			if (withDefault)
			{
				query1 = query1.Provider.CreateQuery<Person>(RestoreDefaultExpressions(query1.Expression));
				query2 = query2.Provider.CreateQuery<Person>(RestoreDefaultExpressions(query2.Expression));
			}

			query1.ToArray();
			var cacheMiss = db.Person.GetCacheMissCount();
			query2.ToArray();
			Assert.That(db.Person.GetCacheMissCount(), Is.EqualTo(cacheMiss));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3148"), QueryCacheTest]
		public void CachedDefaultInProjectionInitializer([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context, [Values] bool withDefault)
		{
			using var db = GetDataContext(context);
			var query1 = db.Parent
					.Select(p => new Parent()
					{
						Children      = p.Children,
						GrandChildren = default!,
						ParentID      = default,
					});
			var query2 = db.Parent
					.Select(p => new Parent()
					{
						Children      = p.Children,
						GrandChildren = default!,
						ParentID      = default,
					});

			if (withDefault)
			{
				query1 = query1.Provider.CreateQuery<Parent>(RestoreDefaultExpressions(query1.Expression));
				query2 = query2.Provider.CreateQuery<Parent>(RestoreDefaultExpressions(query2.Expression));
			}

			query1.ToArray();
			var cacheMiss = db.Parent.GetCacheMissCount();
			query2.ToArray();
			Assert.That(db.Parent.GetCacheMissCount(), Is.EqualTo(cacheMiss));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3148"), QueryCacheTest]
		public void CachedDefaultInProjectionWithFilteredChildren([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context, [Values] bool withDefault)
		{
			using var db = GetDataContext(context);
			var query1 = db.Parent
					.Select(p => new Parent()
					{
						Children      = p.Children.Where(c => c.ParentID != default).ToList(),
						GrandChildren = default!,
						ParentID      = default,
					});
			var query2 = db.Parent
					.Select(p => new Parent()
					{
						Children = p.Children.Where(c => c.ParentID != default).ToList(),
						GrandChildren = default!,
						ParentID = default,
					});

			if (withDefault)
			{
				query1 = query1.Provider.CreateQuery<Parent>(RestoreDefaultExpressions(query1.Expression));
				query2 = query2.Provider.CreateQuery<Parent>(RestoreDefaultExpressions(query2.Expression));
			}

			query1.ToArray();
			var cacheMiss = db.Parent.GetCacheMissCount();
			query2.ToArray();
			Assert.That(db.Parent.GetCacheMissCount(), Is.EqualTo(cacheMiss));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3148"), QueryCacheTest]
		public void CachedDefaultEntityComparisonInChildFilter([IncludeDataSources(true, TestProvName.AllSQLite)] string context, [Values] bool withDefault)
		{
			using var db = GetDataContext(context);
			var query1 = db.Parent
					.Select(p => new Parent()
					{
						Children      = p.Children.Where(c => c.Parent != default).ToList(),
						GrandChildren = default!,
						ParentID      = default,
					});
			var query2 = db.Parent
					.Select(p => new Parent()
					{
						Children      = p.Children.Where(c => c.Parent != default).ToList(),
						GrandChildren = default!,
						ParentID      = default,
					});

			if (withDefault)
			{
				query1 = query1.Provider.CreateQuery<Parent>(RestoreDefaultExpressions(query1.Expression));
				query2 = query2.Provider.CreateQuery<Parent>(RestoreDefaultExpressions(query2.Expression));
			}

			query1.ToArray();
			var cacheMiss = db.Parent.GetCacheMissCount();
			query2.ToArray();
			Assert.That(db.Parent.GetCacheMissCount(), Is.EqualTo(cacheMiss));
		}

		// Test requires OUTER/LATERAL APPLY support from Provider
		[Test(Description = "https://github.com/linq2db/linq2db/issues/3148"), QueryCacheTest]
		public void CachedDefaultInDefaultIfEmptyArgument([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus, TestProvName.AllPostgreSQL)] string context, [Values] bool withDefault)
		{
			using var db = GetDataContext(context);
			var query1 = db.Child
					.Where(x => x.Parent != x
							.GrandChildren
							.Select(e => e.Child!.Parent)
							.DefaultIfEmpty(default)
							.FirstOrDefault()
							&&
						x.ParentID != x
							.Parent!.Children
							.Select(p => p.ChildID)
							.DefaultIfEmpty(default)
							.FirstOrDefault());
			var query2 = db.Child
					.Where(x => x.Parent != x
							.GrandChildren
							.Select(e => e.Child!.Parent)
							.DefaultIfEmpty(default)
							.FirstOrDefault()
							&&
						x.ParentID != x
							.Parent!.Children
							.Select(p => p.ChildID)
							.DefaultIfEmpty(default)
							.FirstOrDefault());

			if (withDefault)
			{
				query1 = query1.Provider.CreateQuery<Child>(RestoreDefaultExpressions(query1.Expression));
				query2 = query2.Provider.CreateQuery<Child>(RestoreDefaultExpressions(query2.Expression));
			}

			query1.ToArray();
			var cacheMiss = db.Child.GetCacheMissCount();
			query2.ToArray();
			Assert.That(db.Child.GetCacheMissCount(), Is.EqualTo(cacheMiss));
		}

		// Test requires OUTER/LATERAL APPLY support from Provider
		[Test(Description = "https://github.com/linq2db/linq2db/issues/3148"), QueryCacheTest]
		public void CachedDefaultInDefaultIfEmptyOnGrandChildren([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus, TestProvName.AllPostgreSQL)] string context, [Values] bool withDefault)
		{
			using var db = GetDataContext(context);
			var query1 = db.Child
					.Where(x => x.GrandChildren.FirstOrDefault() != x
							.GrandChildren
							.DefaultIfEmpty(default)
							.FirstOrDefault()
							&&
						x.ParentID != x
							.Parent!.Children
							.Select(p => p.ChildID)
							.DefaultIfEmpty(default)
							.FirstOrDefault());
			var query2 = db.Child
					.Where(x => x.GrandChildren.FirstOrDefault() != x
							.GrandChildren
							.DefaultIfEmpty(default)
							.FirstOrDefault()
							&&
						x.ParentID != x
							.Parent!.Children
							.Select(p => p.ChildID)
							.DefaultIfEmpty(default)
							.FirstOrDefault());

			if (withDefault)
			{
				query1 = query1.Provider.CreateQuery<Child>(RestoreDefaultExpressions(query1.Expression));
				query2 = query2.Provider.CreateQuery<Child>(RestoreDefaultExpressions(query2.Expression));
			}

			query1.ToArray();
			var cacheMiss = db.Child.GetCacheMissCount();
			query2.ToArray();
			Assert.That(db.Child.GetCacheMissCount(), Is.EqualTo(cacheMiss));
		}

		// Test requires OUTER/LATERAL APPLY or ROW_NUMBER Window function support from Provider
		[Test(Description = "https://github.com/linq2db/linq2db/issues/3148"), QueryCacheTest]
		public void CachedFirstOrDefaultSubqueriesWithoutDefault([IncludeDataSources(true, ProviderName.SQLiteClassic, TestProvName.AllSqlServer2008Plus, TestProvName.AllPostgreSQL)] string context)
		{
			using var db = GetDataContext(context);
			var query1 = db.Child
					.Where(x => x.GrandChildren.FirstOrDefault() != x
									.GrandChildren
									.FirstOrDefault()!
								&&
								x.ParentID != x
									.Parent!.Children
									.Select(p => p.ChildID)
									.FirstOrDefault());
			var query2 = db.Child
					.Where(x => x.GrandChildren.FirstOrDefault() != x
									.GrandChildren
									.FirstOrDefault()!
								&&
								x.ParentID != x
									.Parent!.Children
									.Select(p => p.ChildID)
									.FirstOrDefault());

			query1.ToArray();
			var cacheMiss = db.Child.GetCacheMissCount();
			query2.ToArray();
			Assert.That(db.Child.GetCacheMissCount(), Is.EqualTo(cacheMiss));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3148")]
		public void DefaultInTruncateAndDropArguments([IncludeDataSources(TestProvName.AllSqlServer2016Plus)] string context, [Values] bool withDefault)
		{
			using var db = new TestDataConnection(context);
			using var tb = db.CreateLocalTable<DefaultExprTable>();
			var provider = ((IQueryable)tb).Provider;
			Expression<Func<int>> query1 = () => tb.Truncate(default);
			Expression<Func<int>> query2 = () => tb.Drop(default);

			if (withDefault)
			{
				query1 = RestoreDefaultExpressions(query1);
				query2 = RestoreDefaultExpressions(query2);
			}

			provider.Execute<int>(query1.Body);
			Assert.That(db.LastQuery!, Does.Contain("DELETE FROM"));
			provider.Execute<int>(query2.Body);
			Assert.That(db.LastQuery!, Does.Contain("DROP TABLE IF EXISTS"));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3148")]
		public void DefaultArgumentsInSapHanaCalculationViewParameters([IncludeDataSources(TestProvName.AllSapHana)] string context, [Values] bool withDefault)
		{
			using var db = new SapHanaTests.CalcViewInputParameters(context);
			var provider = ((IQueryable)db.GetTable<DefaultExprTable>()).Provider;
			var var1     = "mandatory1";

			Expression<Func<IQueryable<SapHanaTests.FIT_CA_PARAM_TEST>>> query = () => db.CaParamTest(10, default, var1, default, null, default!);

			if (withDefault)
				query = RestoreDefaultExpressions(query);

			try
			{
				provider.CreateQuery<SapHanaTests.FIT_CA_PARAM_TEST>(query.Body).ToArray();
				Assert.Fail();
			}
			catch (Exception e)
			{
				Assert.That(e.Message, Does.Contain("invalid table name"));
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3148"), QueryCacheTest]
		public void CachedDefaultInFromSqlFormatArguments([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context, [Values] bool withDefault)
		{
			using var db = new TestDataConnection(context);
			var provider = ((IQueryable)db.GetTable<DefaultExprTable>()).Provider;

			int? var1 = 4;
			Expression<Func<IQueryable<DefaultExprTable>>> expr1 = () => db.FromSql<DefaultExprTable>("SELECT {0} as Id, {1} as Field1, {2} as Field2, {3} as Field3", default(int), default(int?), default(string), var1);
			Expression<Func<IQueryable<DefaultExprTable>>> expr2 = () => db.FromSql<DefaultExprTable>("SELECT {0} as Id, {1} as Field1, {2} as Field2, {3} as Field3", default(int), default(int?), default(string), var1);

			var body1 = expr1.Body;
			var body2 = expr2.Body;
			if (withDefault)
			{
				body1 = RestoreDefaultExpressions(body1);
				body2 = RestoreDefaultExpressions(body2);
			}

			var query1 = provider.CreateQuery<DefaultExprTable>(body1);
			var query2 = provider.CreateQuery<DefaultExprTable>(body2);

			query1.ToArray();
			var cacheMiss = db.GetTable<DefaultExprTable>().GetCacheMissCount();
			query2.ToArray();
			Assert.That(db.GetTable<DefaultExprTable>().GetCacheMissCount(), Is.EqualTo(cacheMiss));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3148"), QueryCacheTest]
		public void CachedDefaultInChainedComparison([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context, [Values] bool withDefault)
		{
			using var db = GetDataContext(context);
			var query1 = db.Person
					.Where(r => r.ID != default != default);
			var query2 = db.Person
					.Where(r => r.ID != default != default);

			if (withDefault)
			{
				query1 = query1.Provider.CreateQuery<Person>(RestoreDefaultExpressions(query1.Expression));
				query2 = query2.Provider.CreateQuery<Person>(RestoreDefaultExpressions(query2.Expression));
			}

			query1.ToArray();
			var cacheMiss = db.Person.GetCacheMissCount();
			query2.ToArray();
			Assert.That(db.Person.GetCacheMissCount(), Is.EqualTo(cacheMiss));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3148"), QueryCacheTest]
		public void CachedDefaultInStringCoalesce([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context, [Values] bool withDefault)
		{
			using var db = GetDataContext(context);
			var query1 = db.Person
					.Where(r => ((r.FirstName + "1") ?? default) == default);
			var query2 = db.Person
					.Where(r => ((r.FirstName + "1") ?? default) == default);

			if (withDefault)
			{
				query1 = query1.Provider.CreateQuery<Person>(RestoreDefaultExpressions(query1.Expression));
				query2 = query2.Provider.CreateQuery<Person>(RestoreDefaultExpressions(query2.Expression));
			}

			query1.ToArray();
			var cacheMiss = db.Person.GetCacheMissCount();
			query2.ToArray();
			Assert.That(db.Person.GetCacheMissCount(), Is.EqualTo(cacheMiss));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3148"), QueryCacheTest]
		public void CachedDefaultInNullableIntCoalesce([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context, [Values] bool withDefault)
		{
			using var db = GetDataContext(context);
			var query1 = db.Person
					.Where(r => (((int?)r.ID + 1) ?? default) == default);
			var query2 = db.Person
					.Where(r => (((int?)r.ID + 1) ?? default) == default);

			if (withDefault)
			{
				query1 = query1.Provider.CreateQuery<Person>(RestoreDefaultExpressions(query1.Expression));
				query2 = query2.Provider.CreateQuery<Person>(RestoreDefaultExpressions(query2.Expression));
			}

			query1.ToArray();
			var cacheMiss = db.Person.GetCacheMissCount();
			query2.ToArray();
			Assert.That(db.Person.GetCacheMissCount(), Is.EqualTo(cacheMiss));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3148"), QueryCacheTest]
		public void CachedDefaultInGroupByKey([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context, [Values] bool withDefault)
		{
			using var db = GetDataContext(context);
			var query1 = db.Person
					.GroupBy(_ => new { _.LastName, f1 = default(int), f2 = default(Gender?), f3 = default(string) })
					.Select(g => new Person()
					{
						LastName   = g.Key.LastName,
						ID         = g.Key.f1,
						MiddleName = g.Key.f3,
						Gender     = g.Key.f2 ?? default
					});
			var query2 = db.Person
					.GroupBy(_ => new { _.LastName, f1 = default(int), f2 = default(Gender?), f3 = default(string) })
					.Select(g => new Person()
					{
						LastName   = g.Key.LastName,
						ID         = g.Key.f1,
						MiddleName = g.Key.f3,
						Gender     = g.Key.f2 ?? default
					});

			if (withDefault)
			{
				query1 = query1.Provider.CreateQuery<Person>(RestoreDefaultExpressions(query1.Expression));
				query2 = query2.Provider.CreateQuery<Person>(RestoreDefaultExpressions(query2.Expression));
			}

			query1.ToArray();
			var cacheMiss = db.Person.GetCacheMissCount();
			query2.ToArray();
			Assert.That(db.Person.GetCacheMissCount(), Is.EqualTo(cacheMiss));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3148"), QueryCacheTest]
		public void CachedDefaultIntProjection([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context, [Values] bool withDefault)
		{
			using var db = GetDataContext(context);
			var query1 = db.Child.Select(p => default(int));
			var query2 = db.Child.Select(p => default(int));

			if (withDefault)
			{
				query1 = query1.Provider.CreateQuery<int>(RestoreDefaultExpressions(query1.Expression));
				query2 = query2.Provider.CreateQuery<int>(RestoreDefaultExpressions(query2.Expression));
			}

			query1.ToArray();
			var cacheMiss = query2.GetCacheMissCount();
			query2.ToArray();
			Assert.That(query2.GetCacheMissCount(), Is.EqualTo(cacheMiss));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3148"), QueryCacheTest]
		public void CachedDefaultNullableIntProjection([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context, [Values] bool withDefault)
		{
			using var db = GetDataContext(context);
			var query1 = db.Child.Select(p => default(int?));
			var query2 = db.Child.Select(p => default(int?));

			if (withDefault)
			{
				query1 = query1.Provider.CreateQuery<int?>(RestoreDefaultExpressions(query1.Expression));
				query2 = query2.Provider.CreateQuery<int?>(RestoreDefaultExpressions(query2.Expression));
			}

			query1.ToArray();
			var cacheMiss = query2.GetCacheMissCount();
			query2.ToArray();
			Assert.That(query2.GetCacheMissCount(), Is.EqualTo(cacheMiss));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3148"), QueryCacheTest]
		public void CachedDefaultStringProjection([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context, [Values] bool withDefault)
		{
			using var db = GetDataContext(context);
			db.Select(() => 1);
			var query1 = db.Child.Select(p => default(string));
			var query2 = db.Child.Select(p => default(string));

			if (withDefault)
			{
				query1 = query1.Provider.CreateQuery<string>(RestoreDefaultExpressions(query1.Expression));
				query2 = query2.Provider.CreateQuery<string>(RestoreDefaultExpressions(query2.Expression));
			}

			query1.ToArray();
			var cacheMiss = query2.GetCacheMissCount();
			query2.ToArray();
			Assert.That(query2.GetCacheMissCount(), Is.EqualTo(cacheMiss));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3148"), QueryCacheTest]
		public void CachedDefaultInEndsWithArgument([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllSqlServer, TestProvName.AllClickHouse)] string context, [Values] bool withDefault)
		{
			using var db = GetDataContext(context);
			var query1 = db.Person
					.Where(r => r.LastName.EndsWith("x", default));
			var query2 = db.Person
					.Where(r => r.LastName.EndsWith("x", default));

			if (withDefault)
			{
				query1 = query1.Provider.CreateQuery<Person>(RestoreDefaultExpressions(query1.Expression));
				query2 = query2.Provider.CreateQuery<Person>(RestoreDefaultExpressions(query2.Expression));
			}

			query1.ToArray();
			var cacheMiss = db.Person.GetCacheMissCount();
			query2.ToArray();
			Assert.That(db.Person.GetCacheMissCount(), Is.EqualTo(cacheMiss));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3148"), QueryCacheTest]
		public void CachedDefaultInFromSqlInterpolatedArguments([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context, [Values] bool withDefault)
		{
			using var db = new TestDataConnection(context);
			var provider = ((IQueryable)db.GetTable<DefaultExprTable>()).Provider;

			int? var1 = 4;
			Expression<Func<IQueryable<DefaultExprTable>>> expr1 = () => db.FromSql<DefaultExprTable>($"SELECT {default(int)} as Id, {default(int?)} as Field1, {default(string)} as Field2, {var1} as Field3");
			Expression<Func<IQueryable<DefaultExprTable>>> expr2 = () => db.FromSql<DefaultExprTable>($"SELECT {default(int)} as Id, {default(int?)} as Field1, {default(string)} as Field2, {var1} as Field3");

			var body1 = expr1.Body;
			var body2 = expr2.Body;
			if (withDefault)
			{
				body1 = RestoreDefaultExpressions(body1);
				body2 = RestoreDefaultExpressions(body2);
			}

			var query1 = provider.CreateQuery<DefaultExprTable>(body1);
			var query2 = provider.CreateQuery<DefaultExprTable>(body2);

			query1.ToArray();
			var cacheMiss = db.GetTable<DefaultExprTable>().GetCacheMissCount();
			query2.ToArray();
			Assert.That(db.GetTable<DefaultExprTable>().GetCacheMissCount(), Is.EqualTo(cacheMiss));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3148"), QueryCacheTest]
		public void CachedDefaultInJoinKeySelectors([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context, [Values] bool withDefault)
		{
			using var db = GetDataContext(context);
			var query1 = from person in db.Person
						 join doctor in db.Doctor.Select(d => new Doctor() { PersonID = default, Taxonomy = default! }) on
									new {person.ID, person.FirstName, person.LastName, Gender = (Gender?)person.Gender }
								equals
									new { ID = default(int), FirstName = default(string), LastName = doctor.Taxonomy, Gender = default(Gender?) } into j
						 from r in j.DefaultIfEmpty()
						 select r;
			var query2 = from person in db.Person
						 join doctor in db.Doctor.Select(d => new Doctor() { PersonID = default, Taxonomy = default! }) on
									new {person.ID, person.FirstName, person.LastName, Gender = (Gender?)person.Gender }
								equals
									new { ID = default(int), FirstName = default(string), LastName = doctor.Taxonomy, Gender = default(Gender?) } into j
						 from r in j.DefaultIfEmpty()
						 select r;

			if (withDefault)
			{
				query1 = query1.Provider.CreateQuery<Doctor>(RestoreDefaultExpressions(query1.Expression));
				query2 = query2.Provider.CreateQuery<Doctor>(RestoreDefaultExpressions(query2.Expression));
			}

			query1.ToArray();
			var cacheMiss = db.Doctor.GetCacheMissCount();
			query2.ToArray();
			Assert.That(db.Doctor.GetCacheMissCount(), Is.EqualTo(cacheMiss));
		}

		// query shouldn't have explicit null/0 values, or they will be rewritten too
		private static Expression RestoreDefaultConstant(Expression e)
		{
			if (e is ConstantExpression c)
			{
				if (c.Value == null)
					return Expression.Default(e.Type);

				if (c.Type.IsValueType && Equals(c.Value, Activator.CreateInstance(c.Type)))
					return Expression.Default(e.Type);
			}

			return e;
		}

		private static T RestoreDefaultExpressions<T>(T expr)
			where T: Expression
		{
			var restored = expr.Transform(RestoreDefaultConstant);

			Assert.That(restored, Is.Not.EqualTo(expr));
			Assert.That(restored.Find(e => e.NodeType == ExpressionType.Default), Is.Not.Null);

			return (T)restored;
		}

		[Table]
		public class DefaultExprTable
		{
			[PrimaryKey, Identity] public int     Id     { get; set; }
			[Column              ] public int?    Field1 { get; set; }
			[Column              ] public string? Field2 { get; set; }
			[Column              ] public int?    Field3 { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4300")]
		public void CompiledLambdaInsideQueryHonoursCapturedValue([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using var db = GetDataContext(context);
			using var tbl1 = db.CreateLocalTable(new[]
			{
				new CompiledLambdaParent { Id1 = 1, Name1 = "Some1" },
				new CompiledLambdaParent { Id1 = 2, Name1 = "Some2" },
			});
			using var tbl2 = db.CreateLocalTable(new[]
			{
				new CompiledLambdaChild { Id2 = 10, Table1Id = 1 },
				new CompiledLambdaChild { Id2 = 20, Table1Id = 2 },
			});

			TestMe("Some1", 10); // works correctly
			TestMe("Some2", 20); // fails with error that 10 was received instead of 20 (unless above line is commented out)
			
			void TestMe(string str, int expectedTable2Id)
			{
				Expression<Func<CompiledLambdaParent, bool>> expr = t => t.Name1 == str;
				//var query1 = tbl1.Where(expr).Select(row1 => row1.Id1); //works
				var query1 = tbl1.Where(row1 => expr.Compile()(row1)).Select(row1 => row1.Id1); //doesn't work
				var query2 = tbl2.Where(row2 => query1.Contains(row2.Table1Id)).Select(row2 => row2.Id2);
				var ret    = query2.ToList();
				Assert.That(ret, Has.Count.EqualTo(1));
				Assert.That(ret[0], Is.EqualTo(expectedTable2Id));
			}
		}

		[Table("CompiledLambdaParent")]
		public class CompiledLambdaParent
		{
			[Column("ID1"), PrimaryKey, NotNull] public int     Id1   { get; set; }
			[Column("NAME1"), Nullable]          public string? Name1 { get; set; }
		}

		[Table("CompiledLambdaChild")]
		public class CompiledLambdaChild
		{
			[Column("ID2"), PrimaryKey, NotNull] public int Id2      { get; set; }
			[Column("TABLE1ID"), NotNull]        public int Table1Id { get; set; }
		}

		static (CacheCompareMaster[] masters, CacheCompareDetail[] details, CacheCompareSubDetail[] subDetails) GenerateCacheCompareData()
		{
			var masters = Enumerable.Range(1, 5)
				.Select(i => new CacheCompareMaster { Id1 = i, Value = "M" + i })
				.ToArray();

			var details = masters
				.SelectMany(m => Enumerable.Range(1, 3).Select(i => new CacheCompareDetail
				{
					DetailId    = m.Id1 * 100 + i,
					MasterId    = m.Id1,
					DetailValue = "D" + (m.Id1 * 100 + i),
				}))
				.ToArray();

			var subDetails = details
				.SelectMany(d => Enumerable.Range(1, 2).Select(i => new CacheCompareSubDetail
				{
					SubDetailId    = d.DetailId * 10 + i,
					DetailId       = d.DetailId,
					SubDetailValue = "S" + (d.DetailId * 10 + i),
				}))
				.ToArray();

			return (masters, details, subDetails);
		}

		// Regression test for #5154. Before the fix `Sql.Expr<T>(RawSqlString, params object[])` carried
		// `[SqlQueryDependentParams]` on its `parameters` arg; the cache-compare path
		// (EqualsToVisitor -> SqlQueryDependentParamsAttribute.ExpressionsEqual ->
		// ExpressionEvaluator.EvaluateExpression) compiled each parameter sub-expression on its own.
		// Multi-level eager-loaded projections capture transparent identifiers from outer scope; after
		// ExpressionQuery mutates `this.Expression` to `expressions.MainExpression`, those identifiers
		// are orphaned and the per-parameter compile throws `InvalidOperationException`.
		[Test(Description = "https://github.com/linq2db/linq2db/issues/5154")]
		public void ToSqlQueryAndToArrayInEitherOrder([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var (masterRecords, detailRecords, subDetailRecords) = GenerateCacheCompareData();

			using var db        = GetDataContext(context);
			using var master    = db.CreateLocalTable(masterRecords);
			using var detail    = db.CreateLocalTable(detailRecords);
			using var subDetail = db.CreateLocalTable(subDetailRecords);

			var buildQuery = () =>
				from m in master
				let detailsRaw = detail.Where(d => d.MasterId == m.Id1)
				let mTag       = Sql.Expr<string>("'M' || {0}", m.Id1)
				select new
				{
					m.Id1,
					mTag,
					Details = (from d in detailsRaw
							   let dTag = Sql.Expr<string>("'D' || {0} || '/' || {1}", m.Id1, d.DetailId)
							   select new
							   {
								   d.DetailId,
								   dTag,
								   SubDetails = subDetail
									   .Where(s => s.DetailId == d.DetailId)
									   .Select(s => new
									   {
										   s.SubDetailId,
										   sTag = Sql.Expr<string>("'S' || {0} || '/' || {1} || '/' || {2}", m.Id1, d.DetailId, s.SubDetailId)
									   })
									   .ToArray(),
								   Another = d.SubDetails
							   }).ToArray()
				};

			// Order 1: ToSqlQuery -> ToArray on the same query instance. First call seeds the cache;
			// second call walks the tree against the cached entry.
			{
				var query = buildQuery();
				string? sql    = null;
				object? result = null;
				sql    = query.ToSqlQuery().Sql;
				result = query.ToArray();
				Assert.That(sql,    Is.Not.Null.And.Not.Empty);
				Assert.That(result, Is.Not.Null);
			}

			// Order 2: ToArray -> ToSqlQuery on a fresh, structurally identical query. The cache-compare
			// path runs on the first call here (it finds the entry seeded by the first ordering), which
			// is the failure mode the original report described.
			{
				var query = buildQuery();
				object? result = null;
				string? sql    = null;
				result = query.ToArray();
				sql    = query.ToSqlQuery().Sql;
				Assert.That(result, Is.Not.Null);
				Assert.That(sql,    Is.Not.Null.And.Not.Empty);
			}
		}

		[Table]
		sealed class CacheCompareMaster
		{
			[Column] [PrimaryKey] public int     Id1   { get; set; }
			[Column]              public string? Value { get; set; }

			[Association(ThisKey = nameof(Id1), OtherKey = nameof(CacheCompareDetail.MasterId))]
			public List<CacheCompareDetail> Details { get; set; } = null!;
		}

		[Table]
		sealed class CacheCompareDetail
		{
			[Column] [PrimaryKey] public int     DetailId    { get; set; }
			[Column]              public int?    MasterId    { get; set; }
			[Column]              public string? DetailValue { get; set; }

			[Association(ThisKey = nameof(DetailId), OtherKey = nameof(CacheCompareSubDetail.DetailId))]
			public CacheCompareSubDetail[] SubDetails { get; set; } = null!;
		}

		[Table]
		sealed class CacheCompareSubDetail
		{
			[Column] [PrimaryKey] public int     SubDetailId    { get; set; }
			[Column]              public int?    DetailId       { get; set; }
			[Column]              public string? SubDetailValue { get; set; }
		}
	}
}
