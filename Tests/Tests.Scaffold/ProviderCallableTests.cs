using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using LinqToDB.Scaffold;
using LinqToDB.Schema;
using LinqToDB.SqlQuery;

using NUnit.Framework;

using Shouldly;

using Tests.Scaffold.Harness;

using static Tests.Scaffold.Harness.TestSchema;

namespace Tests.Scaffold
{
	// Procedures and functions, not executable on SQLite: generated code is compiled and SQL of function calls is
	// generated without database. Execution against SQL Server and PostgreSQL: Tests/Linq/Scaffold/ScaffoldExecutionTests.
	[TestFixture]
	public sealed class ProviderCallableTests
	{
		const SchemaObjects Callables = SchemaObjects.Table | SchemaObjects.StoredProcedure | SchemaObjects.TableFunction | SchemaObjects.ScalarFunction | SchemaObjects.AggregateFunction;

		static ScaffoldedModel Build(string name, ScaffoldLanguage language, ProviderProfile profile, TestSchema schema, Action<ScaffoldOptions>? configure = null)
		{
			return ScaffoldHarness.Build(
				$"{nameof(ProviderCallableTests)}_{name}_{profile}",
				language,
				profile,
				schema,
				options =>
				{
					options.Schema.LoadedObjects = Callables;
					configure?.Invoke(options);
				});
		}

		static IReadOnlyList<MethodInfo> Methods(ScaffoldedModel model, string name)
			=> model.Types
				.SelectMany(static t => t.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly))
				.Where(m => string.Equals(m.Name, name, StringComparison.Ordinal))
				.ToList();

		static StoredProcedure OutRefProcedure(Exception? schemaError = null) => new(
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
			schemaError,
			null,
			new ScalarResult("@return", new DatabaseType("int", null, null, null), false));

		[Test]
		public void ProcedureParameterDirections([Values] ScaffoldLanguage language)
		{
			var schema = ProviderProfile.SqlServer.CreateSchema();
			schema.Procedures.Add(OutRefProcedure());

			var model  = Build(nameof(ProcedureParameterDirections), language, ProviderProfile.SqlServer, schema);
			var method = Methods(model, "OutRefTest").ShouldHaveSingleItem();

			var parameters = method.GetParameters().ToDictionary(static p => p.Name!, StringComparer.OrdinalIgnoreCase);

			// context, 6 procedure parameters and return value
			parameters.Count.ShouldBe(8);

			if (language == ScaffoldLanguage.CSharp)
			{
				parameters["outputID"]      .IsOut.ShouldBeTrue();
				parameters["inputOutputID"] .ParameterType.IsByRef.ShouldBeTrue();
				parameters["inputOutputID"] .IsOut.ShouldBeFalse();
				parameters["outputStr"]     .IsOut.ShouldBeTrue();
				parameters["inputOutputStr"].ParameterType.IsByRef.ShouldBeTrue();
				parameters["return"]        .IsOut.ShouldBeTrue();
				parameters["ID"]            .ParameterType.IsByRef.ShouldBeFalse();
			}
			else
			{
				parameters["outputID"]      .ParameterType.IsByRef.ShouldBeTrue();
				parameters["inputOutputID"] .ParameterType.IsByRef.ShouldBeTrue();
				parameters["return"]        .ParameterType.IsByRef.ShouldBeTrue();
				parameters["ID"]            .ParameterType.IsByRef.ShouldBeFalse();
			}
		}

		[Test]
		public void ProcedureResultMappedToEntity([Values] ScaffoldLanguage language)
		{
			var schema = ProviderProfile.SqlServer.CreateSchema();

			schema.AddTable(new SqlObjectName("Person", Schema: "dbo"), [Col("PersonID", "int"), Col("FirstName", "nvarchar", length: 50)], primaryKey: ["PersonID"]);
			schema.Procedures.Add(new StoredProcedure(
				new SqlObjectName("Person_SelectByKey", Schema: "dbo"),
				null,
				[Param("@id", "int")],
				null,
				[[ResultCol("PersonID", "int", nullable: false), ResultCol("FirstName", "nvarchar", nullable: false, length: 50)]],
				new VoidResult()));

			var model  = Build(nameof(ProcedureResultMappedToEntity), language, ProviderProfile.SqlServer, schema, options => options.DataModel.MapProcedureResultToEntity = true);
			var method = Methods(model, "PersonSelectByKey").ShouldHaveSingleItem();

			ElementType(method.ReturnType).ShouldBe(model.GetType("Person"));
		}

		static Type ElementType(Type type)
			=> type.GetInterfaces().Append(type)
				.Single(static i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>))
				.GetGenericArguments()[0];

		static StoredProcedure SchemaErrorProcedure() => new(
			new SqlObjectName("Broken", Schema: "dbo"),
			null,
			[Param("@id", "int")],
			new InvalidOperationException("schema load failed"),
			null,
			new VoidResult());

		// error generation is intended to stop compilation of generated code; F# has no error directive, so the option is
		// reset and procedure is generated without result set
		[Test]
		public void ProcedureSchemaErrorGenerated([Values] ScaffoldLanguage language)
		{
			var schema = ProviderProfile.SqlServer.CreateSchema();
			schema.Procedures.Add(SchemaErrorProcedure());

			var source = Generator.Generate(
				$"{nameof(ProviderCallableTests)}_{nameof(ProcedureSchemaErrorGenerated)}",
				language,
				ProviderProfile.SqlServer,
				schema,
				options =>
				{
					options.Schema.LoadedObjects                     = Callables;
					options.DataModel.GenerateProceduresSchemaError  = true;
					options.DataModel.SkipProceduresWithSchemaErrors = false;
				});

			var result = ScaffoldHarness.TryCompile(source);

			if (language == ScaffoldLanguage.CSharp)
			{
				result.Success.ShouldBeFalse();
				result.Failures.ShouldContain(static d => d.Id == "CS1029" && d.Message.Contains("schema load failed"));
			}
			else
			{
				result.Success.ShouldBeTrue(string.Join("\n", result.Failures.Select(static d => d.Message)));
				source.Code.ShouldNotContain("schema load failed");
			}
		}

		[Test]
		public void ProcedureSchemaErrorSkipped([Values] ScaffoldLanguage language, [Values] bool skip)
		{
			var schema = ProviderProfile.SqlServer.CreateSchema();
			schema.Procedures.Add(SchemaErrorProcedure());

			var model = Build(
				$"{nameof(ProcedureSchemaErrorSkipped)}_{skip}",
				language,
				ProviderProfile.SqlServer,
				schema,
				options =>
				{
					options.DataModel.GenerateProceduresSchemaError  = false;
					options.DataModel.SkipProceduresWithSchemaErrors = skip;
				});

			Methods(model, "Broken").Count.ShouldBe(skip ? 0 : 1);
		}

		[Test]
		public void ProcedureSyncAsync([Values] ScaffoldLanguage language, [Values] bool sync, [Values] bool async)
		{
			var schema = ProviderProfile.SqlServer.CreateSchema();
			schema.Procedures.Add(OutRefProcedure());

			var model = Build(
				$"{nameof(ProcedureSyncAsync)}_{sync}_{async}",
				language,
				ProviderProfile.SqlServer,
				schema,
				options =>
				{
					options.DataModel.GenerateProcedureSync  = sync;
					options.DataModel.GenerateProcedureAsync = async;
				});

			Methods(model, "OutRefTest")     .Count.ShouldBe(sync  ? 1 : 0);
			Methods(model, "OutRefTestAsync").Count.ShouldBe(async ? 1 : 0);
		}

		static IEnumerable<TestCaseData> FunctionCases()
		{
			foreach (var language in Enum.GetValues<ScaffoldLanguage>())
			{
				// table and aggregate functions in non-default schema; scalar function in default schema, which SQL Server
				// requires for scalar function calls
				yield return new TestCaseData(language, ProviderProfile.SqlServer , "[custom].[GetParentByID](" , "[dbo].[ScalarFunction](", "[custom].[test_avg](").SetArgDisplayNames(language.ToString(), "SqlServer");
				yield return new TestCaseData(language, ProviderProfile.PostgreSQL, "custom.\"GetParentByID\"(", "\"ScalarFunction\"("     , "custom.test_avg("    ).SetArgDisplayNames(language.ToString(), "PostgreSQL");
			}
		}

		static TestSchema FunctionsSchema(ProviderProfile profile)
		{
			const string schemaName = "custom";

			var schema = profile.CreateSchema();

			schema.AddTable(new SqlObjectName("Parent", Schema: profile.DefaultSchema), [Col("ParentID", "int"), Col("Value1", "float", nullable: true)], primaryKey: ["ParentID"]);

			schema.TableFunctions.Add(new TableFunction(
				new SqlObjectName("GetParentByID", Schema: schemaName),
				null,
				[Param("@id", "int", nullable: false)],
				null,
				[ResultCol("ParentID", "int", nullable: false), ResultCol("Value1", "float")]));

			schema.ScalarFunctions.Add(new ScalarFunction(
				new SqlObjectName("ScalarFunction", Schema: profile.DefaultSchema),
				null,
				[Param("@value", "int", nullable: false)],
				new ScalarResult(null, new DatabaseType("int", null, null, null), true)));

			schema.AggregateFunctions.Add(new AggregateFunction(
				new SqlObjectName("test_avg", Schema: schemaName),
				null,
				[Param("value", "float", nullable: false)],
				new ScalarResult(null, new DatabaseType("float", null, null, null), true)));

			return schema;
		}

		// generation-only assertions: SQL Server and PostgreSQL are not available to this project (see class comment)
		[TestCaseSource(nameof(FunctionCases))]
		public void FunctionCallSql(ScaffoldLanguage language, ProviderProfile profile, string tableFunctionSql, string scalarFunctionSql, string aggregateSql)
		{
			var model = Build(nameof(FunctionCallSql), language, profile, FunctionsSchema(profile), options => options.DataModel.GenerateSchemaAsType = false);

			using var db = model.CreateContext(profile.CreateOfflineOptions());

			var table = (IQueryable)model.GetTables(db).Single(static t => t.EntityType.Name == "Parent").GetTable();

			var tableFunction = Methods(model, "GetParentById").ShouldHaveSingleItem();
			var tvfQuery      = (IQueryable)tableFunction.Invoke(tableFunction.IsStatic ? null : db, tableFunction.IsStatic ? [db, 1] : [1])!;

			SqlShape.GetSql(tvfQuery).ShouldContain(tableFunctionSql);

			var scalar = Methods(model, "ScalarFunction").ShouldHaveSingleItem();
			SqlShape.GetSql(SqlShape.SelectCall(table, scalar, "ParentId")).ShouldContain(scalarFunctionSql);

			var aggregate = Methods(model, "TestAvg").ShouldHaveSingleItem();
			SqlShape.GetSql(SqlShape.GroupAggregate(table, aggregate, "Value1")).ShouldContain(aggregateSql);
		}

		[Test]
		public void TupleScalarFunction([Values] ScaffoldLanguage language)
		{
			var schema = ProviderProfile.PostgreSQL.CreateSchema();

			schema.ScalarFunctions.Add(new ScalarFunction(
				new SqlObjectName("TestFunctionParameters", Schema: "public"),
				null,
				[Param("param1", "int", nullable: false)],
				new TupleResult(
					[
						new ScalarResult("param2", new DatabaseType("int", null, null, null), true),
						new ScalarResult("param3", new DatabaseType("int", null, null, null), true),
					],
					true)));

			var model = Build(nameof(TupleScalarFunction), language, ProviderProfile.PostgreSQL, schema);

			var tuple = model.GetType("TestFunctionParametersResult");

			tuple.GetProperty("Param2").ShouldNotBeNull();
			tuple.GetProperty("Param3").ShouldNotBeNull();
		}

		[Test]
		public void SqliteScalarFunctionExecutes([Values] ScaffoldLanguage language)
		{
			var schema = ProviderProfile.SQLite.CreateSchema();

			schema.AddTable("Data", [Col("Id", "int")], primaryKey: ["Id"]);
			schema.ScalarFunctions.Add(new ScalarFunction(
				new SqlObjectName("twice"),
				null,
				[Param("x", "int", nullable: false)],
				new ScalarResult(null, new DatabaseType("int", null, null, null), false)));

			var model = Build(nameof(SqliteScalarFunctionExecutes), language, ProviderProfile.SQLite, schema);

			using var sqlite = new SqliteTestDb();
			sqlite.Connection.CreateFunction("twice", static (int x) => x * 2);

			ModelSmoke.Run(model, sqlite.Options);

			using var db = model.CreateContext(sqlite.Options);

			var table  = (IQueryable)model.GetTables(db).Single().GetTable();
			var values = ((IEnumerable)SqlShape.SelectCall(table, Methods(model, "Twice").ShouldHaveSingleItem(), "Id")).Cast<object>().Select(static v => Convert.ToInt32(v, System.Globalization.CultureInfo.InvariantCulture)).OrderBy(static v => v).ToList();

			values.ShouldBe([2, 4]);
		}
	}
}
