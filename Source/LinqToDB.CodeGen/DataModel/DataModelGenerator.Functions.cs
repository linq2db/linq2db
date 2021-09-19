using System;
using System.Text;
using LinqToDB.CodeGen.Model;
using LinqToDB.CodeGen.Schema;
using LinqToDB.Data;
using LinqToDB.Mapping;
using LinqToDB.SqlProvider;

namespace LinqToDB.CodeGen.DataModel
{
	partial class DataModelGenerator
	{
		private void BuildAllFunctions(
			ClassBuilder dataContextClass,
			bool contextIsDataConnection,
			Func<ClassBuilder> getExtensionsClass)
		{
			RegionGroup? proceduresRegion = null;
			RegionGroup? aggregatesRegion = null;
			RegionGroup? scalarFunctionRegion = null;

			BlockBuilder? _schemaInitializer = null;
			CodeReference? _schemaProperty = null;

			var spContextParameterType = contextIsDataConnection ? dataContextClass.Type.Type : _languageProvider.TypeParser.Parse<DataConnection>();

			BuildFunctions(
				dataContextClass.Type,
				_dataModel.DataContext,
				spContextParameterType,
				() => proceduresRegion ??= getExtensionsClass().Regions().New("Stored Procedures").Regions(),
				() => aggregatesRegion ??= getExtensionsClass().Regions().New("Aggregate Functions").Regions(),
				() => scalarFunctionRegion ??= getExtensionsClass().Regions().New("Scalar Functions").Regions(),
				dataContextClass.Regions().New("Table Functions").Regions(),
				getSchemaInitializer);

			foreach (var schema in _dataModel.DataContext.AdditionalSchemas.Values)
			{
				var (wrapper, schemaContext) = _schemaProperties[schema];
				BuildFunctions(
					dataContextClass.Type,
					schema,
					spContextParameterType,
					() => wrapper.Regions().New("Stored Procedures").Regions(),
					() => wrapper.Regions().New("Aggregate Functions").Regions(),
					() => wrapper.Regions().New("Scalar Functions").Regions(),
					schemaContext.Regions().New("Table Functions").Regions(),
					getSchemaInitializer);
			}

			(BlockBuilder initializer, CodeReference schemaProperty) getSchemaInitializer()
			{
				if (_schemaInitializer == null)
				{
					var schemaProp = dataContextClass.Properties(true)!.New(_code.Identifier("ContextSchema"), _code.Type(typeof(MappingSchema), false))
						.Public()
						.Static()
						.Default(false)
						.SetInitializer(_code.New(_code.Type(typeof(MappingSchema),false), Array.Empty<ICodeExpression>(), Array.Empty<CodeAssignmentStatement>()));
					_schemaProperty = schemaProp.Property.Reference;

					var cctor = dataContextClass.TypeInitializer();
					_schemaInitializer = cctor.Body();
				}

				return (_schemaInitializer, _schemaProperty!);
			}
		}

		private void BuildFunctions(
			CodeClass context,
			SchemaModelBase schema,
			IType dataContextType,
			Func<RegionGroup> storedProceduresRegion,
			Func<RegionGroup> aggregatesRegion,
			Func<RegionGroup> scalarFunctionsRegion,
			RegionGroup tableFunctionsRegion,
			Func<(BlockBuilder cctorBody, CodeReference schema)> getSchemaConfigurator)
		{
			// procedures and functions should be generated after entities as they could use entity classes for return parameters
			// (apply to both main context and additional schemas)
			foreach (var proc in schema.StoredProcedures)
				BuildStoredProcedure(
					proc,
					storedProceduresRegion,
					dataContextType);

			foreach (var func in schema.ScalarFunctions)
				BuildScalarFunction(func, scalarFunctionsRegion, getSchemaConfigurator);
			foreach (var func in schema.AggregateFunctions)
				BuildAggregateFunction(func, aggregatesRegion);
			foreach (var func in schema.TableFunctions)
				BuildTableFunction(func, tableFunctionsRegion, context);
		}

		private (ClassBuilder resultClassBuilder, CodeProperty[] properties) BuildCustomResultClass(
			ResultTableModel model,
			RegionBuilder region,
			bool withMapping)
		{
			var properties = new CodeProperty[model.Columns.Count];

			var resultClassBuilder = DefineClass(model.Class, region.Classes());

			var columnsGroup = resultClassBuilder.Properties(true);

			for (var i = 0; i < model.Columns.Count; i++)
			{
				var columnModel = model.Columns[i];
				var columnBuilder = DefineProperty(columnsGroup, columnModel.Property);
				if (withMapping)
					_metadataBuilder.BuildColumnMetadata(columnModel.Metadata!, columnBuilder);
				properties[i] = columnBuilder.Property;
			}

			return (resultClassBuilder, properties);
		}

		private string BuildFunctionName(ObjectName name)
		{
			// TODO: as we still miss API for stored procedures and functions that takes not prepared(escaped) full name but FQN components
			// we need to generate such name from FQN
			// also we use BuildTableName as there is no API for function-like objects
			return _sqlBuilder.BuildTableName(
				new StringBuilder(),
				name.Server == null ? null : _sqlBuilder.ConvertInline(name.Server, ConvertType.NameToServer),
				name.Database == null ? null : _sqlBuilder.ConvertInline(name.Database, ConvertType.NameToDatabase),
				name.Schema == null ? null : _sqlBuilder.ConvertInline(name.Schema, ConvertType.NameToSchema),
											  // NameToQueryTable used as we don't have separate ConvertType for procedures/functions
											  _sqlBuilder.ConvertInline(name.Name, ConvertType.NameToQueryTable),
				TableOptions.NotSet
			).ToString();
		}
	}
}
