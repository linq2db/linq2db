using System;
using System.Linq;

using LinqToDB.CodeModel;
using LinqToDB.Metadata;
using LinqToDB.Scaffold;
using LinqToDB.SqlProvider;

namespace LinqToDB.DataModel
{
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
		private readonly IMetadataBuilder?           _metadataBuilder;
		// used for full function name generation
		private readonly SqlBuilder                 _sqlBuilder;
		// adjust parameter name according to naming rules for parameter
		private readonly Func<string, string>        _findMethodParameterNameNormalizer;
		// scaffolding options
		private readonly ScaffoldOptions             _options;

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
			IMetadataBuilder?    metadataBuilder,
			Func<string, string> findMethodParameterNameNormalizer,
			SqlBuilder          sqlBuilder,
			ScaffoldOptions      options)
		{
			_languageProvider                  = languageProvider;
			_dataModel                         = dataModel;
			_metadataBuilder                   = metadataBuilder;
			_findMethodParameterNameNormalizer = findMethodParameterNameNormalizer;
			_sqlBuilder                        = sqlBuilder;
			_options                           = options;
		}

		/// <summary>
		/// Performs conversion of data model to AST.
		/// </summary>
		/// <returns>Generated AST as collection of files.</returns>
		public CodeFile[] ConvertToCodeModel()
		{
			IDataModelGenerationContext context = new DataModelGenerationContext(
				_options.DataModel,
				_languageProvider,
				_dataModel,
				_sqlBuilder,
				_metadataBuilder,
				_findMethodParameterNameNormalizer);

			// generate AST
			BuildDataContext(context);

			// correct file names to be unique
			context.DeduplicateFileNames();

			return context.Files.ToArray();
		}

		/// <summary>
		/// Contains main (top-level) AST generation logic.
		/// </summary>
		/// <param name="context">Model generation context.</param>
		private static void BuildDataContext(IDataModelGenerationContext context)
		{
			// optional method to initialize references to additional schemas
			MethodBuilder? initSchemasMethod       = null;
			// optional region with properties to reference additional schemas
			PropertyGroup? contextSchemaProperties = null;

			// if we have additional schemas, create region with init method in data context for them
			if (context.Model.DataContext.AdditionalSchemas.Count > 0)
			{
				initSchemasMethod = context.SchemasContextRegion
					.Methods(false)
						.New(context.AST.Name(DataModelConstants.SCHEMAS_INIT_METHOD))
							.SetModifiers(Modifiers.Public);

				contextSchemaProperties = context.SchemasContextRegion.Properties(true);
			}

			// generate classes for entities from main data context
			BuildEntities(context, context.Model.DataContext.Entities, entity => context.DefineFileClass(entity.Class));

			// generate classes for entities for additional schemas alongside with other schema-related
			// code (except associations and procedures/functions)
			foreach (var schema in context.Model.DataContext.AdditionalSchemas.Values)
			{
				// build schema classes and entities
				var schemaContextType = BuildAdditionalSchema(context, schema);

				// add schema reference to data context class
				var schemaProp = contextSchemaProperties!
					.New(context.AST.Name(schema.DataContextPropertyName), schemaContextType)
						.SetModifiers(Modifiers.Public)
						.Default(true);

				initSchemasMethod!
					.Body()
						.Append(
							context.AST.Assign(
								context.AST.Member(context.ContextReference, schemaProp.Property.Reference),
								context.AST.New(schemaContextType, context.ContextReference)));
			}
			
			// generate associations for all entities after all entity classes generated for both
			// main context and additional schemas, as they need to reference entity classes and properties
			BuildAssociations(context);

			// generate functions and stored procedures last, as they can reference entity classes and properties
			BuildAllFunctions(context);

			context.MetadataBuilder?.Complete(context);

			// partial static init method, called by static constructor, which could be used by user to add
			// additional initialization logic
			if (context.Options.GenerateStaticInitDataContextMethod)
			{
				var staticInitDataContext = context.MainDataContextPartialMethods
					.New(context.AST.Name(DataModelConstants.CONTEXT_STATIC_INIT_METHOD))
					.SetModifiers(Modifiers.Static | Modifiers.Partial);

				context.StaticInitializer.Append(
					context.AST.Call(context.ContextReference, staticInitDataContext.Method.Name)
				);
			}

			// generate data context constructors (at the end to add context mapping schema support if needed)
			BuildDataContextConstructors(context, initSchemasMethod?.Method.Name);

		}
	}
}
