using System;
using System.Collections.Generic;
using System.Linq;
using LinqToDB.CodeGen.CodeGeneration;
using LinqToDB.CodeGen.ContextModel;
using LinqToDB.CodeGen.Model;
using LinqToDB.Data;
using LinqToDB.SqlProvider;

namespace LinqToDB.CodeGen.DataModel
{
	public partial class DataModelGenerator
	{
		private readonly CodeBuilder _code;
		private readonly DatabaseModel _dataModel;
		private readonly ILanguageProvider _languageProvider;
		private readonly IMetadataBuilder _metadataBuilder;
		private readonly ISqlBuilder _sqlBuilder;

		private readonly NamingServices _namingServices;
		private readonly ObjectNormalizationSettings _parameterNameNormalization;

		private readonly Dictionary<string, (CodeFile file, Dictionary<string, ClassGroup> classesPerNamespace)> _files = new ();
		
		private readonly Dictionary<EntityModel, ClassBuilder> _entityBuilders = new ();
		private readonly Dictionary<ColumnModel, PropertyBuilder> _columnBuilders = new ();
		private readonly Dictionary<AdditionalSchemaModel, (ClassBuilder schemaWrapper, ClassBuilder schemaContext)> _schemaProperties = new ();

		public DataModelGenerator(
			ILanguageProvider languageProvider,
			DatabaseModel dataModel,
			IMetadataBuilder metadataBuilder,
			NamingServices namingServices,
			ObjectNormalizationSettings parameterNameNormalization,
			ISqlBuilder sqlBuilder)
		{
			_languageProvider = languageProvider;
			_dataModel = dataModel;
			_metadataBuilder = metadataBuilder;
			_namingServices = namingServices;
			_parameterNameNormalization = parameterNameNormalization;
			_sqlBuilder = sqlBuilder;

			_code = new CodeBuilder(languageProvider);
		}

		public CodeFile[] ConvertToCodeModel()
		{
			BuildDataContext();

			NormalizeFileNames();

			return _files.Values.Select(static _ => _.file).ToArray();
		}

		private void BuildDataContext()
		{
			var (dataContextClass, contextClassGroup) = DefineFileClass(_dataModel.DataContext.Class);

			var contextIsDataConnection = _dataModel.DataContext.Class.BaseType != null && _languageProvider.TypeEqualityComparerWithoutNRT.Equals(_dataModel.DataContext.Class.BaseType, _languageProvider.TypeParser.Parse<DataConnection>());

			CodeIdentifier? initSchemasMethodName = null;
			RegionBuilder? schemasRegion = null;
			BlockBuilder? initMethodBody = null;
			if (_dataModel.DataContext.AdditionalSchemas.Count > 0)
			{
				schemasRegion = dataContextClass.Regions().New("Schemas");
				var schemaProperties = schemasRegion.Properties(true);
				var initMethod = schemasRegion.Methods(false).New(_code.Identifier("InitSchemas")).Public();
				initMethodBody = initMethod.Body();
				initSchemasMethodName = initMethod.Method.Name;
			}

			BuildDataContextConstructors(
				dataContextClass.Type,
				dataContextClass.Constructors(),
				dataContextClass.Methods(true),
				initSchemasMethodName);

			ClassBuilder? extensionsClass = null;
			MethodGroup? findMethodsGroup = null;

			BuildEntities(
				_dataModel.DataContext.Entities,
				entity => DefineFileClass(entity.Class).builder,
				dataContextClass.Properties(true),
				contextIsDataConnection,
				dataContextClass.Type.This,
				() => findMethodsGroup ??= getExtensionsClass().Methods(false));

			if (_dataModel.DataContext.AdditionalSchemas.Count > 0)
			{
				var contextSchemaProperties = schemasRegion!.Properties(true);

				foreach (var schema in _dataModel.DataContext.AdditionalSchemas.Values)
				{
					var schemaContextType = BuildAdditionalSchema(schema);

					// add schema to data context
					var schemaProp = contextSchemaProperties.New(_code.Identifier(schema.DataContextPropertyName), schemaContextType)
						.Public()
						.Default(true);
					initMethodBody!.Append(
						_code.Assign(
							_code.Member(dataContextClass.Type.This, schemaProp.Property.Name, schemaProp.Property.Type.Type),
							_code.New(schemaContextType, new ICodeExpression[] { dataContextClass.Type.This }, Array.Empty<CodeAssignmentStatement>())));
				}
			}

			
			BuildAssociations(getExtensionsClass);

			BuildAllFunctions(dataContextClass, contextIsDataConnection, getExtensionsClass);

			ClassBuilder getExtensionsClass()
			{
				return extensionsClass ??= contextClassGroup!
					.New(_code.Identifier("SqlFunctions"))
					.Public()
					.Static()
					.Partial();
			}
		}
	}
}
