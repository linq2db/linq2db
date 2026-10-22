using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using LinqToDB;
using LinqToDB.CodeModel;
using LinqToDB.DataModel;
using LinqToDB.Extensions;
using LinqToDB.Scaffold;
using LinqToDB.Schema;
using LinqToDB.SqlQuery;

using NUnit.Framework;

using Shouldly;

using Tests.Scaffold.Harness;

using static Tests.Scaffold.Harness.TestSchema;

using DescriptionAttribute = System.ComponentModel.DescriptionAttribute;

namespace Tests.Scaffold
{
	// each test compares model with interceptor against model without it (control)
	[TestFixture]
	public sealed class InterceptorTests
	{
		sealed class TestInterceptors : ScaffoldInterceptors
		{
			public Func<IEnumerable<Table>            , IEnumerable<Table>>?             Tables;
			public Func<IEnumerable<View>             , IEnumerable<View>>?              Views;
			public Func<IEnumerable<ForeignKey>       , IEnumerable<ForeignKey>>?        ForeignKeys;
			public Func<IEnumerable<StoredProcedure>  , IEnumerable<StoredProcedure>>?   Procedures;
			public Func<IEnumerable<TableFunction>    , IEnumerable<TableFunction>>?     TableFunctions;
			public Func<IEnumerable<ScalarFunction>   , IEnumerable<ScalarFunction>>?    ScalarFunctions;
			public Func<IEnumerable<AggregateFunction>, IEnumerable<AggregateFunction>>? AggregateFunctions;

			public Func<DatabaseType, ITypeParser, TypeMapping?, TypeMapping?>? TypeMapping;

			public Action<ITypeParser, EntityModel>?            Entity;
			public Action<ITypeParser, StoredProcedureModel>?   Procedure;
			public Action<ITypeParser, TableFunctionModel>?     TableFunction;
			public Action<ITypeParser, ScalarFunctionModel>?    ScalarFunction;
			public Action<ITypeParser, AggregateFunctionModel>? AggregateFunction;
			public Action<ITypeParser, AssociationModel>?       Association;

			public FinalDataModel? Final;

			public override IEnumerable<Table>             GetTables            (IEnumerable<Table>             tables    ) => Tables            ?.Invoke(tables    ) ?? tables;
			public override IEnumerable<View>              GetViews             (IEnumerable<View>              views     ) => Views             ?.Invoke(views     ) ?? views;
			public override IEnumerable<ForeignKey>        GetForeignKeys       (IEnumerable<ForeignKey>        keys      ) => ForeignKeys       ?.Invoke(keys      ) ?? keys;
			public override IEnumerable<StoredProcedure>   GetProcedures        (IEnumerable<StoredProcedure>   procedures) => Procedures        ?.Invoke(procedures) ?? procedures;
			public override IEnumerable<TableFunction>     GetTableFunctions    (IEnumerable<TableFunction>     functions ) => TableFunctions    ?.Invoke(functions ) ?? functions;
			public override IEnumerable<ScalarFunction>    GetScalarFunctions   (IEnumerable<ScalarFunction>    functions ) => ScalarFunctions   ?.Invoke(functions ) ?? functions;
			public override IEnumerable<AggregateFunction> GetAggregateFunctions(IEnumerable<AggregateFunction> functions ) => AggregateFunctions?.Invoke(functions ) ?? functions;

			public override TypeMapping? GetTypeMapping(DatabaseType databaseType, ITypeParser typeParser, TypeMapping? defaultMapping)
				=> TypeMapping != null ? TypeMapping(databaseType, typeParser, defaultMapping) : defaultMapping;

			public override void PreprocessEntity           (ITypeParser typeParser, EntityModel            entityModel     ) => Entity           ?.Invoke(typeParser, entityModel);
			public override void PreprocessStoredProcedure  (ITypeParser typeParser, StoredProcedureModel   procedureModel  ) => Procedure        ?.Invoke(typeParser, procedureModel);
			public override void PreprocessTableFunction    (ITypeParser typeParser, TableFunctionModel     functionModel   ) => TableFunction    ?.Invoke(typeParser, functionModel);
			public override void PreprocessScalarFunction   (ITypeParser typeParser, ScalarFunctionModel    functionModel   ) => ScalarFunction   ?.Invoke(typeParser, functionModel);
			public override void PreprocessAggregateFunction(ITypeParser typeParser, AggregateFunctionModel functionModel   ) => AggregateFunction?.Invoke(typeParser, functionModel);
			public override void PreprocessAssociation      (ITypeParser typeParser, AssociationModel       associationModel) => Association      ?.Invoke(typeParser, associationModel);

			public override void AfterSourceCodeGenerated(FinalDataModel model) => Final = model;
		}

		static TestSchema TablesSchema()
		{
			var schema = ProviderProfile.SQLite.CreateSchema();

			schema.AddTable("Parent", [Col("Id", "int"), Col("Name", "text")], primaryKey: ["Id"]);
			schema.AddTable("Child" , [Col("Id", "int"), Col("ParentId", "int")], primaryKey: ["Id"]);
			schema.AddView(new SqlObjectName("ParentView"), [Col("Id", "int"), Col("Name", "text", nullable: true)]);

			return schema;
		}

		static (ScaffoldedModel Model, ScaffoldedModel Control) Build(string name, ScaffoldLanguage language, ProviderProfile profile, Func<TestSchema> schema, TestInterceptors interceptors, Action<ScaffoldOptions>? configure = null)
		{
			var model   = ScaffoldHarness.Build($"{nameof(InterceptorTests)}_{name}"        , language, profile, schema(), configure, interceptors);
			var control = ScaffoldHarness.Build($"{nameof(InterceptorTests)}_{name}_Control", language, profile, schema(), configure);

			return (model, control);
		}

		static ModelSmoke.Report Execute(ScaffoldedModel model)
		{
			using var db = new SqliteTestDb();
			return ModelSmoke.Run(model, db.Options);
		}

		[Test]
		public void GetTables([Values] ScaffoldLanguage language)
		{
			var interceptors = new TestInterceptors
			{
				Tables = tables => tables
					.Where(static t => t.Name.Name != "Child")
					.Append(new Table(new SqlObjectName("Added"), null, [Col("Id", "int", ordinal: 0)], null, new PrimaryKey(null, ["Id"]))),
			};

			var (model, control) = Build(nameof(GetTables), language, ProviderProfile.SQLite, TablesSchema, interceptors);

			control.FindType("Child").ShouldNotBeNull();
			control.FindType("Added").ShouldBeNull();
			model  .FindType("Child").ShouldBeNull();
			model  .FindType("Added").ShouldNotBeNull();

			Execute(model).Tables.ShouldBe(3);
		}

		[Test]
		public void GetViews([Values] ScaffoldLanguage language)
		{
			var interceptors = new TestInterceptors { Views = static _ => [] };

			var (model, control) = Build(nameof(GetViews), language, ProviderProfile.SQLite, TablesSchema, interceptors);

			control.FindType("ParentView").ShouldNotBeNull();
			model  .FindType("ParentView").ShouldBeNull();

			Execute(model).Tables.ShouldBe(2);
		}

		[Test]
		public void GetForeignKeys([Values] ScaffoldLanguage language)
		{
			var interceptors = new TestInterceptors
			{
				ForeignKeys = static keys => keys.Append(new ForeignKey("FK_Child_Parent", new SqlObjectName("Child"), new SqlObjectName("Parent"), [new ForeignKeyColumnMapping("ParentId", "Id")])),
			};

			var (model, control) = Build(nameof(GetForeignKeys), language, ProviderProfile.SQLite, TablesSchema, interceptors);

			Execute(control).Associations.ShouldBe(0);
			Execute(model)  .Associations.ShouldBe(2);
		}

		[Test]
		public void GetTypeMapping([Values] ScaffoldLanguage language)
		{
			var interceptors = new TestInterceptors
			{
				TypeMapping = static (type, parser, mapping) => type.Name == "text" ? new TypeMapping(parser.Parse<Guid>(), DataType.Guid) : mapping,
			};

			var (model, control) = Build(nameof(GetTypeMapping), language, ProviderProfile.SQLite, TablesSchema, interceptors);

			control.GetType("Parent").GetProperty("Name")!.PropertyType.ShouldBe(typeof(string));
			model  .GetType("Parent").GetProperty("Name")!.PropertyType.ShouldBe(typeof(Guid));

			Execute(model).Rows.ShouldBe(3 * ModelSmoke.RowCount);
		}

		[Test]
		public void PreprocessEntity([Values] ScaffoldLanguage language)
		{
			var interceptors = new TestInterceptors
			{
				Entity = static (parser, entity) =>
				{
					if (entity.Metadata.Name?.Name != "Parent")
						return;

					entity.Class.Name = "Renamed";
					entity.Columns.Single(static c => c.Metadata.Name == "Name").Property.Name = "Title";
					(entity.Class.CustomAttributes ??= []).Add(new CodeAttribute(parser.Parse<DescriptionAttribute>()));
					entity.ContextProperty = null;
					entity.FindExtensions  = FindTypes.None;
				},
			};

			var (model, control) = Build(nameof(PreprocessEntity), language, ProviderProfile.SQLite, TablesSchema, interceptors);

			control.FindType("Renamed").ShouldBeNull();

			var renamed = model.GetType("Renamed");

			renamed.GetProperty("Title").ShouldNotBeNull();
			renamed.GetAttribute<DescriptionAttribute>().ShouldNotBeNull();
			model.ContextType.GetProperties().ShouldNotContain(static p => p.PropertyType.IsGenericType && p.PropertyType.GetGenericArguments()[0].Name == "Renamed");
			ModelSmoke.FindMethods(model).ShouldNotContain(m => m.GetParameters()[0].ParameterType.GetGenericArguments().Contains(renamed));
			ModelSmoke.FindMethods(control).Count.ShouldBeGreaterThan(ModelSmoke.FindMethods(model).Count);

			// renamed entity has no context property, so smoke covers remaining tables
			Execute(model).Tables.ShouldBe(2);
		}

		[Test]
		public void PreprocessAssociation([Values] ScaffoldLanguage language)
		{
			var interceptors = new TestInterceptors
			{
				ForeignKeys = static keys => keys.Append(new ForeignKey("FK_Child_Parent", new SqlObjectName("Child"), new SqlObjectName("Parent"), [new ForeignKeyColumnMapping("ParentId", "Id")])),
				Association = static (_, association) =>
				{
					association.Property!.Name              = "Owner";
					association.BackreferenceProperty!.Name = "Owned";
				},
			};

			var model = ScaffoldHarness.Build($"{nameof(InterceptorTests)}_{nameof(PreprocessAssociation)}", language, ProviderProfile.SQLite, TablesSchema(), interceptors: interceptors);

			model.GetType("Child") .GetProperty("Owner").ShouldNotBeNull();
			model.GetType("Parent").GetProperty("Owned").ShouldNotBeNull();

			Execute(model).Associations.ShouldBe(2);
		}

		[Test]
		public void AfterSourceCodeGenerated([Values] ScaffoldLanguage language)
		{
			var schema = ProviderProfile.SQLite.CreateSchema();
			schema.AddTable("person_info", [Col("person_id", "int"), Col("first_name", "text")], primaryKey: ["person_id"]);

			var interceptors = new TestInterceptors();

			var model = ScaffoldHarness.Build($"{nameof(InterceptorTests)}_{nameof(AfterSourceCodeGenerated)}", language, ProviderProfile.SQLite, schema, interceptors: interceptors);

			var final  = interceptors.Final.ShouldNotBeNull();
			var entity = final.Entities.ShouldHaveSingleItem();

			// final names differ from database names: normalized by naming options
			entity.Class.Name.ShouldBe("PersonInfo");

			var type = model.GetType(entity.Class.Name);

			foreach (var column in entity.Columns)
				type.GetProperty(column.Property.Name).ShouldNotBeNull(column.Property.Name);
		}

		static TestSchema CallablesSchema()
		{
			var schema = ProviderProfile.SqlServer.CreateSchema();

			schema.AddTable(new SqlObjectName("Parent", Schema: "dbo"), [Col("Id", "int"), Col("Value", "float")], primaryKey: ["Id"]);

			foreach (var name in new[] { "Proc1", "Proc2" })
				schema.Procedures.Add(new StoredProcedure(new SqlObjectName(name, Schema: "dbo"), null, [Param("@id", "int")], null, null, new VoidResult()));

			foreach (var name in new[] { "Tvf1", "Tvf2" })
				schema.TableFunctions.Add(new TableFunction(new SqlObjectName(name, Schema: "dbo"), null, [Param("@id", "int")], null, [ResultCol("Id", "int")]));

			foreach (var name in new[] { "Scalar1", "Scalar2" })
				schema.ScalarFunctions.Add(new ScalarFunction(new SqlObjectName(name, Schema: "dbo"), null, [Param("@id", "int")], new ScalarResult(null, new DatabaseType("int", null, null, null), true)));

			foreach (var name in new[] { "Agg1", "Agg2" })
				schema.AggregateFunctions.Add(new AggregateFunction(new SqlObjectName(name, Schema: "dbo"), null, [Param("value", "float")], new ScalarResult(null, new DatabaseType("float", null, null, null), true)));

			return schema;
		}

		static void Callables(ScaffoldOptions options)
			=> options.Schema.LoadedObjects = SchemaObjects.Table | SchemaObjects.StoredProcedure | SchemaObjects.TableFunction | SchemaObjects.ScalarFunction | SchemaObjects.AggregateFunction;

		static bool HasMethod(ScaffoldedModel model, string name)
			=> model.Types.SelectMany(static t => t.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)).Any(m => m.Name == name);

		static IEnumerable<TestCaseData> CallableCases()
		{
			foreach (var language in Enum.GetValues<ScaffoldLanguage>())
			{
				yield return new TestCaseData(language, "Procedures"        , "Proc1"  , "Proc2"  , new TestInterceptors { Procedures         = static p => p.Where(static o => o.Name.Name != "Proc2")   }).SetArgDisplayNames(language.ToString(), "GetProcedures");
				yield return new TestCaseData(language, "TableFunctions"    , "Tvf1"   , "Tvf2"   , new TestInterceptors { TableFunctions     = static p => p.Where(static o => o.Name.Name != "Tvf2")    }).SetArgDisplayNames(language.ToString(), "GetTableFunctions");
				yield return new TestCaseData(language, "ScalarFunctions"   , "Scalar1", "Scalar2", new TestInterceptors { ScalarFunctions    = static p => p.Where(static o => o.Name.Name != "Scalar2") }).SetArgDisplayNames(language.ToString(), "GetScalarFunctions");
				yield return new TestCaseData(language, "AggregateFunctions", "Agg1"   , "Agg2"   , new TestInterceptors { AggregateFunctions = static p => p.Where(static o => o.Name.Name != "Agg2")    }).SetArgDisplayNames(language.ToString(), "GetAggregateFunctions");

				yield return new TestCaseData(language, "PreprocessProcedure"        , "Renamed", "Proc2"  , new TestInterceptors { Procedure         = static (_, m) => { if (m.Name.Name == "Proc2"  ) m.Method.Name = "Renamed"; } }).SetArgDisplayNames(language.ToString(), "PreprocessStoredProcedure");
				yield return new TestCaseData(language, "PreprocessTableFunction"    , "Renamed", "Tvf2"   , new TestInterceptors { TableFunction     = static (_, m) => { if (m.Name.Name == "Tvf2"   ) m.Method.Name = "Renamed"; } }).SetArgDisplayNames(language.ToString(), "PreprocessTableFunction");
				yield return new TestCaseData(language, "PreprocessScalarFunction"   , "Renamed", "Scalar2", new TestInterceptors { ScalarFunction    = static (_, m) => { if (m.Name.Name == "Scalar2") m.Method.Name = "Renamed"; } }).SetArgDisplayNames(language.ToString(), "PreprocessScalarFunction");
				yield return new TestCaseData(language, "PreprocessAggregateFunction", "Renamed", "Agg2"   , new TestInterceptors { AggregateFunction = static (_, m) => { if (m.Name.Name == "Agg2"   ) m.Method.Name = "Renamed"; } }).SetArgDisplayNames(language.ToString(), "PreprocessAggregateFunction");
			}
		}

		[TestCaseSource(nameof(CallableCases))]
		public void Callable(ScaffoldLanguage language, string name, string present, string absent, ScaffoldInterceptors interceptors)
		{
			var (model, control) = Build(name, language, ProviderProfile.SqlServer, CallablesSchema, (TestInterceptors)interceptors, Callables);

			HasMethod(control, absent) .ShouldBeTrue();
			HasMethod(control, present).ShouldBe(present != "Renamed");

			HasMethod(model, present).ShouldBeTrue();
			HasMethod(model, absent) .ShouldBeFalse();
		}
	}
}
