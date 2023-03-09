﻿using System;
using System.Collections.Generic;
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
		private readonly ISqlBuilder                 _sqlBuilder;
		// adjust parameter name according to naming rules for parameter
		private readonly Func<string, string>        _findMethodParameterNameNormalizer;
		// scaffolding options
		private readonly ScaffoldOptions             _options;

		private readonly record struct TypeWithRegion(CodeClass Type, RegionGroup Region);

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

			// generate classes for entities from main data context
			BuildEntities(_dataModel.DataContext.Entities, entity => DefineFileClass(entity.Class));

			// generate classes for entities for additional schemas alongside with other schema-related
			// code (except associations and procedures/functions)
			foreach (var schema in _dataModel.DataContext.AdditionalSchemas.Values)
			{
				// build schema classes and entities
				var schemaContextType = BuildAdditionalSchema(schema);

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
			BuildAssociations();

			// generate functions and stored procedures last, as they can reference entity classes and properties
			BuildAllFunctions(dataContextBuilder, contextIsDataConnection);

			_metadataBuilder?.Complete(context);
		}
	}
}
