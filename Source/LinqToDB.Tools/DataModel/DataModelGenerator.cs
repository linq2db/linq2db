﻿namespace LinqToDB.DataModel
{
	using CodeModel;
	using Metadata;
	using Scaffold;
	using SqlProvider;

	/// <summary>
	/// Implements code model AST generation for database model and produce AST for:
	/// <list type="bullet">
	/// <item>database context class</item>
	/// <item>table mappings</item>
	/// <item>procedures and functions mappings</item>
	/// <item>classes for non-default schemas</item>
	/// </list>
	/// </summary>
	public sealed partial class DataModelGenerator
	{
		// current data model
		private readonly DatabaseModel               _dataModel;
		// language-specific services
		private readonly ILanguageProvider           _languageProvider;
		// data model metadata generator
		private readonly IMetadataBuilder            _metadataBuilder;
		// used for full function name generation
		private readonly ISqlBuilder                 _sqlBuilder;
		// adjust parameter name according to naming rules for parameter
		private readonly Func<string, string>        _findMethodParameterNameNormalizer;
		// scaffolding options
		private readonly ScaffoldOptions             _options;

		// generated AST files
		private readonly Dictionary<string, (CodeFile file, Dictionary<string, ClassGroup> classesPerNamespace)> _files = new ();
		
		// various lookups to map data model descriptors to corresponding generated AST objects or builders
		//
		// stores entity class AST builder for each entity
		// used to map entity descriptor to generated class e.g. for association generation logic
		private readonly Dictionary<EntityModel, ClassBuilder>    _entityBuilders   = new ();
		// stores entity column AST builder for each entity column
		// used to map entity column descriptor to generated class e.g. for association generation logic or Find method generator
		private readonly Dictionary<ColumnModel, CodeProperty>    _columnProperties = new ();
		// stores AST objects for additional/non-default schema descriptors
		private readonly Dictionary<AdditionalSchemaModel, (ClassBuilder schemaWrapperClass, ClassBuilder schemaContextClass)> _schemaClasses = new ();


		/// <summary>
		/// Creates data model to AST generator instance.
		/// </summary>
		/// <param name="languageProvider">Language-specific services.</param>
		/// <param name="dataModel">Data model to convert.</param>
		/// <param name="metadataBuilder">Data model mappings generation service.</param>
		/// <param name="findMethodParameterNameNormalizer">Find extension method parameter name normalization action.</param>
		/// <param name="sqlBuilder">SQL builder for current database provider.</param>
		/// <param name="options">Scaffolding options.</param>
		public DataModelGenerator(
			ILanguageProvider    languageProvider,
			DatabaseModel        dataModel,
			IMetadataBuilder     metadataBuilder,
			Func<string, string> findMethodParameterNameNormalizer,
			ISqlBuilder          sqlBuilder,
			ScaffoldOptions      options)
		{
			_languageProvider                  = languageProvider;
			_dataModel                         = dataModel;
			_metadataBuilder                   = metadataBuilder;
			_findMethodParameterNameNormalizer = findMethodParameterNameNormalizer;
			_sqlBuilder                        = sqlBuilder;
			_options                           = options;
		}

		private CodeBuilder AST => _languageProvider.ASTBuilder;

		/// <summary>
		/// Performs conversion of data model to AST.
		/// </summary>
		/// <returns>Generated AST as collection of files.</returns>
		public CodeFile[] ConvertToCodeModel()
		{
			// generate AST
			BuildDataContext();

			// correct file names to be unique
			DeduplicateFileNames();

			return _files.Values.Select(static _ => _.file).ToArray();
		}

		/// <summary>
		/// Contains main (top-level) AST generation logic.
		/// </summary>
		private void BuildDataContext()
		{
			var dataContextBuilder = DefineFileClass(_dataModel.DataContext.Class);

			// check wether custom data context is inherited from DataConnection
			// as it affects available APIs/generated code
			var contextIsDataConnection = _dataModel.DataContext.Class.BaseType != null
				&& _languageProvider.TypeEqualityComparerWithoutNRT.Equals(_dataModel.DataContext.Class.BaseType, WellKnownTypes.LinqToDB.Data.DataConnection);

			// optional method to initialize references to additional schemas
			MethodBuilder? initSchemasMethod       = null;
			// optional region with properties to reference additional schemas
			PropertyGroup? contextSchemaProperties = null;

			// if we have additional schemas, create region with init method in data context for them
			if (_dataModel.DataContext.AdditionalSchemas.Count > 0)
			{
				var schemasRegion = dataContextBuilder
					.Regions()
						.New(SCHEMAS_CONTEXT_REGION);

				initSchemasMethod = schemasRegion
					.Methods(false)
						.New(AST.Name(SCHEMAS_INIT_METHOD))
							.SetModifiers(Modifiers.Public);

				contextSchemaProperties = schemasRegion.Properties(true);
			}

			// generate data context constructors
			BuildDataContextConstructors(dataContextBuilder, initSchemasMethod?.Method.Name);

			// extensions class builder (e.g. for association extensions, Find methods, etc)
			ClassBuilder? extensionsClass  = null;
			// method group for Find extension methods
			MethodGroup?  findMethodsGroup = null;

			// generate classes for entities from main data context
			BuildEntities(
				_dataModel.DataContext.Entities,
				entity => DefineFileClass(entity.Class),
				dataContextBuilder.Properties(true),
				dataContextBuilder.Type.Type,
				dataContextBuilder.Type.This,
				() => findMethodsGroup ??= getExtensionsClass().Regions().New(FIND_METHODS_REGION).Methods(false));

			// generate classes for entities for additional schemas alongside with other schema-related
			// code (except associations and procedures/functions)
			foreach (var schema in _dataModel.DataContext.AdditionalSchemas.Values)
			{
				// build schema classes and entities
				var schemaContextType = BuildAdditionalSchema(schema, dataContextBuilder.Type.Type);

				// add schema reference to data context class
				var schemaProp = contextSchemaProperties!
					.New(AST.Name(schema.DataContextPropertyName), schemaContextType)
						.SetModifiers(Modifiers.Public)
						.Default(true);

				initSchemasMethod!
					.Body()
						.Append(
							AST.Assign(
								AST.Member(dataContextBuilder.Type.This, schemaProp.Property.Reference),
								AST.New(schemaContextType, dataContextBuilder.Type.This)));
			}
			
			// generate associations for all entities after all entity classes generated for both
			// main context and additional schemas, as they need to reference entity classes and properties
			BuildAssociations(getExtensionsClass);

			// generate functions and stored procedures last, as they can reference entity classes and properties
			BuildAllFunctions(dataContextBuilder, contextIsDataConnection, getExtensionsClass);

			// action to access extensions class builder with class declaration on first call
			ClassBuilder getExtensionsClass()
			{
				return extensionsClass ??= dataContextBuilder
					.Group
						.New(AST.Name(EXTENSIONS_CLASS))
							.SetModifiers(Modifiers.Public | Modifiers.Static | Modifiers.Partial);
			}
		}
	}
}
