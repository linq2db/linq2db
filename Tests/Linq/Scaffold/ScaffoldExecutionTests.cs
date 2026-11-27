#if !NETFRAMEWORK
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using LinqToDB;
using LinqToDB.Scaffold;
using LinqToDB.Schema;
using LinqToDB.SqlQuery;

using Microsoft.FSharp.Core;

using NUnit.Framework;

using Shouldly;

using Tests.Scaffold.Harness;

using static Tests.Scaffold.Harness.TestSchema;

namespace Tests.Scaffold
{
	// Code, generated for procedures and functions from test database creation scripts, executed against database.
	// Generated from predefined schema (not from database schema), so it tests code generation, not schema load.
	[TestFixture]
	public sealed class ScaffoldExecutionTests : TestBase
	{
		const SchemaObjects Callables = SchemaObjects.Table | SchemaObjects.StoredProcedure | SchemaObjects.TableFunction | SchemaObjects.ScalarFunction | SchemaObjects.AggregateFunction;

		static void Configure(ScaffoldOptions options)
		{
			options.Schema.LoadedObjects                = Callables;
			options.DataModel.GenerateSchemaAsType      = false;
			options.DataModel.MapProcedureResultToEntity = true;
		}

		static ScaffoldedModel SqlServerModel(ScaffoldLanguage language) => ScaffoldHarness.GetOrBuild(
			$"{nameof(ScaffoldExecutionTests)}_SqlServer_{language}",
			() =>
			{
				var schema = ProviderProfile.SqlServer.CreateSchema();

				schema.AddTable(
					new SqlObjectName("Person", Schema: "dbo"),
					[
						Col("PersonID"  , "int"),
						Col("FirstName" , "nvarchar", length: 50),
						Col("LastName"  , "nvarchar", length: 50),
						Col("MiddleName", "nvarchar", nullable: true, length: 50),
						Col("Gender"    , "nvarchar", length: 1),
					],
					primaryKey: ["PersonID"],
					identity  : "PersonID");

				schema.AddTable(new SqlObjectName("Parent", Schema: "dbo"), [Col("ParentID", "int"), Col("Value1", "int", nullable: true)], primaryKey: ["ParentID"]);

				schema.Procedures.Add(new StoredProcedure(
					new SqlObjectName("OutRefTest", Schema: "dbo"),
					null,
					[
						Param("@ID"            , "int"),
						Param("@outputID"      , "int"    , LinqToDB.Schema.ParameterDirection.Output),
						Param("@inputOutputID" , "int"    , LinqToDB.Schema.ParameterDirection.InputOutput),
						Param("@str"           , "varchar", length: 50),
						Param("@outputStr"     , "varchar", LinqToDB.Schema.ParameterDirection.Output     , length: 50),
						Param("@inputOutputStr", "varchar", LinqToDB.Schema.ParameterDirection.InputOutput, length: 50),
					],
					null,
					null,
					new ScalarResult("@return", new DatabaseType("int", null, null, null), false)));

				schema.Procedures.Add(new StoredProcedure(
					new SqlObjectName("Issue1897", Schema: "dbo"),
					null,
					[],
					null,
					null,
					new ScalarResult("@return", new DatabaseType("int", null, null, null), false)));

				schema.Procedures.Add(new StoredProcedure(
					new SqlObjectName("QueryProcParameters", Schema: "dbo"),
					null,
					[
						Param("@input"  , "int"),
						Param("@output1", "int", LinqToDB.Schema.ParameterDirection.Output),
						Param("@output2", "int", LinqToDB.Schema.ParameterDirection.Output),
					],
					null,
					[PersonColumns()],
					new VoidResult()));

				schema.Procedures.Add(new StoredProcedure(
					new SqlObjectName("Person_SelectByKey", Schema: "dbo"),
					null,
					[Param("@id", "int")],
					null,
					[PersonColumns()],
					new VoidResult()));

				schema.TableFunctions.Add(new TableFunction(new SqlObjectName("GetParentByID"      , Schema: "dbo"       ), null, [Param("@id", "int", nullable: false)], null, ParentColumns()));
				schema.TableFunctions.Add(new TableFunction(new SqlObjectName("SchemaTableFunction", Schema: "TestSchema"), null, [Param("@id", "int", nullable: false)], null, ParentColumns()));

				schema.ScalarFunctions.Add(new ScalarFunction(
					new SqlObjectName("ScalarFunction", Schema: "dbo"),
					null,
					[Param("@value", "int", nullable: false)],
					new ScalarResult(null, new DatabaseType("int", null, null, null), true)));

				return ScaffoldHarness.Build($"{nameof(ScaffoldExecutionTests)}_SqlServer", language, ProviderProfile.SqlServer, schema, Configure);
			});

		static ResultColumn[] PersonColumns() =>
		[
			ResultCol("PersonID"  , "int"     , nullable: false),
			ResultCol("FirstName" , "nvarchar", nullable: false, length: 50),
			ResultCol("LastName"  , "nvarchar", nullable: false, length: 50),
			ResultCol("MiddleName", "nvarchar", nullable: true , length: 50),
			ResultCol("Gender"    , "nvarchar", nullable: false, length: 1),
		];

		static ResultColumn[] ParentColumns() => [ResultCol("ParentID", "int", nullable: false), ResultCol("Value1", "int")];

		static ScaffoldedModel PostgreSqlModel(ScaffoldLanguage language) => ScaffoldHarness.GetOrBuild(
			$"{nameof(ScaffoldExecutionTests)}_PostgreSQL_{language}",
			() =>
			{
				var schema = ProviderProfile.PostgreSQL.CreateSchema();

				schema.AddTable(new SqlObjectName("Parent", Schema: "public"), [Col("ParentID", "int"), Col("Value1", "int", nullable: true)], primaryKey: ["ParentID"]);

				schema.TableFunctions.Add(new TableFunction(
					new SqlObjectName("TestTableFunction", Schema: "public"),
					null,
					[Param("param1", "int", nullable: false)],
					null,
					[ResultCol("param2", "int")]));

				schema.ScalarFunctions.Add(new ScalarFunction(
					new SqlObjectName("TestScalarFunction", Schema: "public"),
					null,
					[Param("param", "int", nullable: false)],
					new ScalarResult(null, new DatabaseType("varchar", 10, null, null), true)));

				schema.ScalarFunctions.Add(new ScalarFunction(
					new SqlObjectName("fnTest", Schema: "SchemaName"),
					null,
					[Param("param", "int", nullable: false)],
					new ScalarResult(null, new DatabaseType("varchar", 20, null, null), true)));

				schema.ScalarFunctions.Add(new ScalarFunction(
					new SqlObjectName("TestFunctionParameters", Schema: "public"),
					null,
					[Param("param1", "int", nullable: false), Param("param2", "int", nullable: false)],
					new TupleResult(
						[
							new ScalarResult("param2", new DatabaseType("int", null, null, null), true),
							new ScalarResult("param3", new DatabaseType("int", null, null, null), true),
						],
						true)));

				schema.AggregateFunctions.Add(new AggregateFunction(
					new SqlObjectName("test_avg", Schema: "public"),
					null,
					[Param("value", "float", nullable: false)],
					new ScalarResult(null, new DatabaseType("float", null, null, null), true)));

				return ScaffoldHarness.Build($"{nameof(ScaffoldExecutionTests)}_PostgreSQL", language, ProviderProfile.PostgreSQL, schema, Configure);
			});

		static IDataContext CreateContext(ScaffoldedModel model, string context)
		{
			var options = new DataOptions()
				.UseConfiguration(context);

			return model.CreateContext(options);
		}

		static object? PropertyValue(object row, string name)
		{
			var value = row
				.GetType()
				.GetProperty(name)!
				.GetValue(row);

			return Value(value);
		}

		static MethodInfo Method(ScaffoldedModel model, string name)
			=> model.Types
				.SelectMany(static t => t.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly))
				.Single(m => m.Name == name);

		// argument value for parameter type: nullable, F# option and by-ref wrappers
		static object? Arg(ParameterInfo parameter, object? value)
		{
			var type = parameter.ParameterType.IsByRef ? parameter.ParameterType.GetElementType()! : parameter.ParameterType;

			if (value == null)
				return null;

			if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(FSharpOption<>))
				return type.GetMethod(nameof(FSharpOption<>.Some))!.Invoke(null, [value]);

			return value;
		}

		static object? Value(object? value)
		{
			var type = value?.GetType();

			if (type is { IsGenericType: true } && type.GetGenericTypeDefinition() == typeof(FSharpOption<>))
				return type
					.GetProperty(nameof(FSharpOption<>.Value))!
					.GetValue(value);

			return value;
		}

		static object?[] Invoke(MethodInfo method, IDataContext db, params object?[] values)
		{
			var parameters = method.GetParameters();
			var args       = new object?[parameters.Length];

			args[0] = db;

			for (var i = 1; i < parameters.Length; i++)
				args[i] = i - 1 < values.Length ? Arg(parameters[i], values[i - 1]) : null;

			var result = method.Invoke(null, args);

			if (result is IEnumerable rows and not string)
				result = rows
					.Cast<object>()
					.ToList();

			return [result, .. args.Skip(1)];
		}

		// OutRefTest exists only in the SQL Server creation script
		[Test]
		public void OutRefTest([IncludeDataSources(TestProvName.AllSqlServer)] string context, [Values] ScaffoldLanguage language)
		{
			var model = SqlServerModel(language);

			using var db = CreateContext(model, context);

			// OutRefTest(id, out outputID, ref inputOutputID, str, out outputStr, ref inputOutputStr, out return)
			var results = Invoke(Method(model, "OutRefTest"), db, 5, null, 10, "John", null, "Smith", null);

			Value(results[2])
				.ShouldBe(5);
			Value(results[3])
				.ShouldBe(15);
			Value(results[5])
				.ShouldBe("John");
			Value(results[6])
				.ShouldBe("JohnSmith");
			Value(results[7])
				.ShouldBe(0);
		}

		// Issue1897 exists only in the SQL Server creation script
		[Test]
		public void ReturnValue([IncludeDataSources(TestProvName.AllSqlServer)] string context, [Values] ScaffoldLanguage language)
		{
			var model = SqlServerModel(language);

			using var db = CreateContext(model, context);

			var results = Invoke(Method(model, "Issue1897"), db);

			Value(results[1])
				.ShouldBe(4);
		}

		// QueryProcParameters exists only in the SQL Server creation script
		[Test]
		public void ProcedureRowsAndOutputs([IncludeDataSources(TestProvName.AllSqlServer)] string context, [Values] ScaffoldLanguage language)
		{
			var model = SqlServerModel(language);

			using var db = CreateContext(model, context);

			var results = Invoke(Method(model, "QueryProcParameters"), db, 10, null, null);

			((IList)results[0]!)
				.Count
				.ShouldBe(db.GetTable<Model.Person>().Count());
			Value(results[2])
				.ShouldBe(11);
			Value(results[3])
				.ShouldBe(12);
		}

		// Person_SelectByKey exists only in the SQL Server creation script
		[Test]
		public void ProcedureResultMappedToEntity([IncludeDataSources(TestProvName.AllSqlServer)] string context, [Values] ScaffoldLanguage language)
		{
			var model = SqlServerModel(language);

			using var db = CreateContext(model, context);

			var rows = (IList)Invoke(Method(model, "PersonSelectByKey"), db, 1)[0]!;

			var person = rows
				.Cast<object>()
				.ShouldHaveSingleItem();

			person
				.GetType()
				.ShouldBe(model.GetType("Person"));
			PropertyValue(person, "PersonId")
				.ShouldBe(1);
			PropertyValue(person, "FirstName")
				.ShouldBe("John");
		}

		// GetParentByID and TestSchema.SchemaTableFunction exist only in the SQL Server creation script
		[Test]
		public void TableFunctions([IncludeDataSources(TestProvName.AllSqlServer)] string context, [Values] ScaffoldLanguage language, [Values("GetParentById", "SchemaTableFunction")] string function)
		{
			var model = SqlServerModel(language);

			using var db = CreateContext(model, context);

			var method = Method(model, function);
			var query  = (IQueryable)method.Invoke(method.IsStatic ? null : db, method.IsStatic ? [db, 1] : [1])!;

			var rows = query
				.Cast<object>()
				.ToList();

			var expected = db
				.GetTable<Model.Parent>()
				.Count(static p => p.ParentID == 1);

			rows.Count.ShouldBe(expected);
			rows.ShouldAllBe(static r => Equals(PropertyValue(r, "ParentId"), 1));
		}

		// ScalarFunction exists only in the SQL Server creation script
		[Test]
		public void ScalarFunction([IncludeDataSources(TestProvName.AllSqlServer)] string context, [Values] ScaffoldLanguage language)
		{
			var model = SqlServerModel(language);

			using var db = CreateContext(model, context);

			var table  = (IQueryable)model
				.GetTables(db)
				.Single(static t => t.EntityType.Name == "Parent")
				.GetTable();
			var values = SqlShape
				.SelectCall(table, Method(model, "ScalarFunction"), "ParentId")
				.Cast<object?>()
				.Select(Value)
				.OrderBy(static v => v)
				.ToList();

			var expected = db
				.GetTable<Model.Parent>()
				.Select(static p => (object?)p.ParentID)
				.OrderBy(static v => v)
				.ToList();

			values.ShouldBe(expected);
		}

		// "TestTableFunction" exists only in the PostgreSQL creation script
		[Test]
		public void PostgreSqlTableFunction([IncludeDataSources(TestProvName.AllPostgreSQL)] string context, [Values] ScaffoldLanguage language)
		{
			var model = PostgreSqlModel(language);

			using var db = CreateContext(model, context);

			var method = Method(model, "TestTableFunction");
			var query  = (IQueryable)method.Invoke(method.IsStatic ? null : db, method.IsStatic ? [db, 5] : [5])!;

			query
				.Cast<object>()
				.Select(static r => PropertyValue(r, "Param2"))
				.ToList()
				.ShouldBe([5, 5]);
		}

		// "TestScalarFunction" and "SchemaName"."fnTest" exist only in the PostgreSQL creation script
		[Test]
		public void PostgreSqlScalarFunctions([IncludeDataSources(TestProvName.AllPostgreSQL)] string context, [Values] ScaffoldLanguage language, [Values("TestScalarFunction:done", "FnTest:issue2679test")] string function)
		{
			var model = PostgreSqlModel(language);

			using var db = CreateContext(model, context);

			var (name, expected) = (function.Split(':')[0], function.Split(':')[1]);

			var table  = (IQueryable)model
				.GetTables(db)
				.Single(static t => t.EntityType.Name == "Parent")
				.GetTable();
			var values = SqlShape
				.SelectCall(table, Method(model, name), "ParentId")
				.Cast<object?>()
				.Select(Value)
				.Distinct()
				.ToList();

			values.ShouldBe([expected]);
		}

		// "TestFunctionParameters" exists only in the PostgreSQL creation script
		[Test]
		public void PostgreSqlTupleFunction([IncludeDataSources(TestProvName.AllPostgreSQL)] string context, [Values] ScaffoldLanguage language)
		{
			var model = PostgreSqlModel(language);

			using var db = CreateContext(model, context);

			var table = (IQueryable)model
				.GetTables(db)
				.Single(static t => t.EntityType.Name == "Parent")
				.GetTable();

			// "TestFunctionParameters"(param1, INOUT param2, OUT param3): SELECT param1, param2
			// distinct arguments: a swapped or merged tuple field gives other values
			const int second = -100;

			var tuples = SqlShape
				.SelectCall(
					table,
					Method(model, "TestFunctionParameters"),
					[
						static e => System.Linq.Expressions.Expression.PropertyOrField(e, "ParentId"),
						static _ => System.Linq.Expressions.Expression.Constant(second),
					])
				.Cast<object>()
				.ToList();

			var param2 = tuples
				.Select(static t => PropertyValue(t, "Param2"))
				.OrderBy(static v => v)
				.ToList();

			var param3 = tuples
				.Select(static t => PropertyValue(t, "Param3"))
				.Distinct()
				.ToList();

			var parentIds = db
				.GetTable<Model.Parent>()
				.OrderBy(static p => p.ParentID)
				.Select(static p => (object?)p.ParentID)
				.ToList();

			param2.ShouldBe(parentIds);
			param3.ShouldBe([second]);
		}

		// test_avg exists only in the PostgreSQL creation script
		[Test]
		public void PostgreSqlAggregate([IncludeDataSources(TestProvName.AllPostgreSQL)] string context, [Values] ScaffoldLanguage language)
		{
			var model = PostgreSqlModel(language);

			using var db = CreateContext(model, context);

			var table  = (IQueryable)model
				.GetTables(db)
				.Single(static t => t.EntityType.Name == "Parent")
				.GetTable();
			var result = SqlShape
				.GroupAggregate(table, Method(model, "TestAvg"), "Value1")
				.Cast<object>()
				.Single();
			var actual = Convert.ToDouble(Value(result), System.Globalization.CultureInfo.InvariantCulture);

			// test_avg is AVG over float8: NULLs ignored
			var expected = db.GetTable<Model.Parent>()
				.Select(static p => p.Value1)
				.ToList()
				.Where(static v => v != null)
				.Average(static v => (double)v!.Value);

			actual.ShouldBe(expected, 1e-9);
		}
	}
}
#endif
