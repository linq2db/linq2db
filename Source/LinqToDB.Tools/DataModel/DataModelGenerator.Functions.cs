using System.Text;
using LinqToDB.Schema;
using LinqToDB.CodeModel;
using LinqToDB.Data;
using LinqToDB.SqlProvider;
using LinqToDB.SqlQuery;

namespace LinqToDB.DataModel;

// basic functionality for function/procedure mappings generation
partial class DataModelGenerator
{
	/// <summary>
	/// Generates mappings for all data model functions and procedures.
	/// </summary>
	/// <param name="dataContextClass">Data context class builder.</param>
	/// <param name="contextIsDataConnection">Indicate wether data context is based on <see cref="DataConnection"/>.</param>
	/// <param name="getExtensionsClass">Extensions class builder provider.</param>
	private void BuildAllFunctions(
		ClassBuilder       dataContextClass,
		bool               contextIsDataConnection,
		Func<ClassBuilder> getExtensionsClass)
	{
		// regions in extensions class for specific function types
		RegionGroup? proceduresRegion     = null;
		RegionGroup? aggregatesRegion     = null;
		RegionGroup? scalarFunctionRegion = null;

		// mapping schema initialization method body
		// (some functions require additional mapping configuration on mapping schema level)
		BlockBuilder?  schemaInitializer  = null;
		// mapping schema property accessor for schema initializer
		CodeReference? schemaProperty     = null;

		// TODO: requires linq2db refactoring
		// currently stored procedures API requires DataConnection-based context
		// so we cannot use generated data context type for parameter if it is not inherited from DataConnection
		var spContextParameterType = contextIsDataConnection
			? dataContextClass.Type.Type
			: WellKnownTypes.LinqToDB.Data.DataConnection;

		// build functions for main context
		BuildFunctions(
			dataContextClass.Type,
			_dataModel.DataContext,
			spContextParameterType,
			() => proceduresRegion     ??= getExtensionsClass().Regions().New(EXTENSIONS_STORED_PROCEDURES_REGION).Regions(),
			() => aggregatesRegion     ??= getExtensionsClass().Regions().New(EXTENSIONS_AGGREGATES_REGION       ).Regions(),
			() => scalarFunctionRegion ??= getExtensionsClass().Regions().New(EXTENSIONS_SCALAR_FUNCTIONS_REGION ).Regions(),
			dataContextClass.Regions().New(CONTEXT_TABLE_FUNCTIONS_REGION).Regions(),
			getSchemaInitializer);

		// build functions for additional schemas
		foreach (var schema in _dataModel.DataContext.AdditionalSchemas.Values)
		{
			var (wrapper, schemaContext) = _schemaClasses[schema];
			BuildFunctions(
				dataContextClass.Type,
				schema,
				spContextParameterType,
				() => wrapper.Regions().New(EXTENSIONS_STORED_PROCEDURES_REGION).Regions(),
				() => wrapper.Regions().New(EXTENSIONS_AGGREGATES_REGION       ).Regions(),
				() => wrapper.Regions().New(EXTENSIONS_SCALAR_FUNCTIONS_REGION ).Regions(),
				schemaContext.Regions().New(CONTEXT_TABLE_FUNCTIONS_REGION     ).Regions(),
				getSchemaInitializer);
		}

		// helper function to define custom mapping schema initializer only when needed (on request)
		(BlockBuilder initializer, CodeReference schemaProperty) getSchemaInitializer()
		{
			// schema initializer (and schema) are static to avoid:
			// - schema initialization on each context creation
			// - non-static schema will render query caching almost useless
			if (schemaInitializer == null)
			{
				// declare static mapping schema property
				var schemaProp = dataContextClass
					.Properties(true)
						.New(AST.Name(CONTEXT_SCHEMA_PROPERTY), WellKnownTypes.LinqToDB.Mapping.MappingSchema)
							.SetModifiers(Modifiers.Public | Modifiers.Static)
							.Default(false)
							.SetInitializer(AST.New(WellKnownTypes.LinqToDB.Mapping.MappingSchema));
				schemaProperty = schemaProp.Property.Reference;

				// declare static constructor, where we will add custom schema initialization logic
				schemaInitializer = dataContextClass.TypeInitializer().Body();
			}

			return (schemaInitializer, schemaProperty!);
		}
	}

	/// <summary>
	/// Generates mappings for functions and procedures for specific schema (or main context).
	/// </summary>
	/// <param name="context">Main data context class.</param>
	/// <param name="schema">Current schema.</param>
	/// <param name="procedureDataContextType">Data context type for stored procedure parameter.</param>
	/// <param name="storedProceduresRegion">Region for stored procedure mappings.</param>
	/// <param name="aggregatesRegion">Region for aggregates mappings.</param>
	/// <param name="scalarFunctionsRegion">Region for scalar functions mappings.</param>
	/// <param name="tableFunctionsRegion">Region for table functions mappings.</param>
	/// <param name="getSchemaConfigurator">Static mapping schema initializer provider.</param>
	private void BuildFunctions(
		CodeClass                                            context,
		SchemaModelBase                                      schema,
		IType                                                procedureDataContextType,
		Func<RegionGroup>                                    storedProceduresRegion,
		Func<RegionGroup>                                    aggregatesRegion,
		Func<RegionGroup>                                    scalarFunctionsRegion,
		RegionGroup                                          tableFunctionsRegion,
		Func<(BlockBuilder cctorBody, CodeReference schema)> getSchemaConfigurator)
	{
		if (_options.DataModel.GenerateProcedureSync || _options.DataModel.GenerateProcedureAsync)
		{
			foreach (var proc in schema.StoredProcedures)
				BuildStoredProcedure(proc, storedProceduresRegion, procedureDataContextType);
		}

		foreach (var func in schema.ScalarFunctions)
			BuildScalarFunction(func, scalarFunctionsRegion, getSchemaConfigurator);

		foreach (var func in schema.AggregateFunctions)
			BuildAggregateFunction(func, aggregatesRegion);

		foreach (var func in schema.TableFunctions)
			BuildTableFunction(func, tableFunctionsRegion, context);
	}

	/// <summary>
	/// Generates custom class for table function/stored procedure result record
	/// in cases when there is no suitable entity mapping for it.
	/// </summary>
	/// <param name="model">Result record model.</param>
	/// <param name="classes">Procedure classes group.</param>
	/// <param name="withMapping">Indicate wether we need to generate mapping attributes for generated class columns.
	/// Column mappings needed for table function records (as we use them as query sources) and could be missing
	/// for stored procedures in cases when returned columns cannot be mapped due to duplicate/missing column names, which
	/// is possible for stored procedures.</param>
	/// <returns>Generated class and column properties.</returns>
	private (IType resultClassType, CodeProperty[] properties) BuildCustomResultClass(
		ResultTableModel model,
		ClassGroup       classes,
		bool             withMapping)
	{
		var properties = new CodeProperty[model.Columns.Count];

		var resultClassBuilder = DefineClass(classes, model.Class);

		var columnsGroup = resultClassBuilder.Properties(true);

		for (var i = 0; i < model.Columns.Count; i++)
		{
			var columnModel = model.Columns[i];
			var columnBuilder = DefineProperty(columnsGroup, columnModel.Property);

			if (withMapping)
				_metadataBuilder.BuildColumnMetadata(columnModel.Metadata, columnBuilder);

			properties[i] = columnBuilder.Property;
		}

		return (resultClassBuilder.Type.Type, properties);
	}

	/// <summary>
	/// Temporary (hopefully) helper to generate fully-qualified procedure or function name.
	/// </summary>
	/// <param name="name">Procedure/function object name.</param>
	/// <returns>Fully-qualified name of procedure or function.</returns>
	private string BuildFunctionName(SqlObjectName name)
	{
		// TODO: linq2db refactoring
		// This method needed only because right now we don't have API that accepts name components
		// for some function API
		return _sqlBuilder.BuildObjectName(new (), name, ConvertType.NameToProcedure).ToString();
	}
}
