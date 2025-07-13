using System;
using System.Collections.Generic;
using System.Linq;

using LinqToDB.CodeModel;
using LinqToDB.DataModel;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Metadata;
using LinqToDB.Naming;
using LinqToDB.Schema;

namespace LinqToDB.Scaffold
{
	/// <summary>
	/// Helper class to simplify common scenario of data model generation from database.
	/// </summary>
	public sealed class Scaffolder
	{
		private readonly NamingServices        _namingServices;
		private readonly ScaffoldOptions       _options;
		private readonly ScaffoldInterceptors? _interceptors;

		/// <summary>
		/// Creates instance of data model codegerator from database connection.
		/// </summary>
		/// <param name="languageProvider">Language provider to use for data model codegeneration.</param>
		/// <param name="nameConverter"><see cref="INameConversionProvider"/> pluralization service implementation.</param>
		/// <param name="options">Scaffolding process customization options.</param>
		/// <param name="interceptors">Optional custom scaffold interceptors.</param>
		public Scaffolder(ILanguageProvider languageProvider, INameConversionProvider nameConverter, ScaffoldOptions options, ScaffoldInterceptors? interceptors)
		{
			Language        = languageProvider;
			_namingServices = new NamingServices(nameConverter);
			_options        = options;
			_interceptors   = interceptors;
		}

		/// <summary>
		/// Gets language provider, used by current instance.
		/// </summary>
		public ILanguageProvider Language { get; }

		/// <summary>
		/// Loads database schema into <see cref="DatabaseModel"/> object.
		/// </summary>
		/// <param name="schemaProvider">Database schema provider.</param>
		/// <param name="typeMappingsProvider">Database types mappings provider.</param>
		/// <returns>Loaded database model instance.</returns>
		public DatabaseModel LoadDataModel(
			ISchemaProvider      schemaProvider,
			ITypeMappingProvider typeMappingsProvider)
		{
			return new DataModelLoader(
					_namingServices,
					Language,
					schemaProvider,
					typeMappingsProvider,
					_options,
					_interceptors)
				.LoadSchema();
		}

		/// <summary>
		/// Converts database model to code model (AST).
		/// </summary>
		/// <param name="sqlBuilder">Database-specific <see cref="ISqlBuilder"/> instance.</param>
		/// <param name="dataModel">Database model.</param>
		/// <param name="metadataBuilder">Data model metadata builder.</param>
		/// <param name="modelConverters">Optional AST post-processing converters.</param>
		/// <returns>Code model as collection of code file models.</returns>
		public CodeFile[] GenerateCodeModel(
			       ISqlBuilder               sqlBuilder,
			       DatabaseModel             dataModel,
			       IMetadataBuilder?         metadataBuilder,
			params ConvertCodeModelVisitor[] modelConverters)
		{
			var generator = new DataModelGenerator(
				Language,
				dataModel,
				metadataBuilder,
				name => _namingServices.NormalizeIdentifier(_options.DataModel.FindParameterNameOptions, name),
				sqlBuilder,
				_options);

			var files = generator.ConvertToCodeModel();

			foreach (var converter in modelConverters)
			{
				for (var i = 0; i < files.Length; i++)
					files[i] = (CodeFile)converter.Visit(files[i]);
			}

			return files;
		}

		/// <summary>
		/// Converts per-file code models (AST) to source code using current language (used by current instance).
		/// </summary>
		/// <param name="dataModel">Data model, used for code generation.</param>
		/// <param name="files">Code models.</param>
		/// <returns>Source code with file names.</returns>
		public SourceCodeFile[] GenerateSourceCode(DatabaseModel dataModel, params CodeFile[] files)
		{
			var sources = new string[files.Length];

			var namesNormalize = Language.GetIdentifiersNormalizer();

			// normalize names in model to have valid and non-conflicting identifiers
			foreach (var file in files)
				namesNormalize.Visit(file);

			var importsCollector = new ImportsCollector(Language);
			var nameScopes       = new NameScopesCollector(Language);

			// collect identifiers information in various naming scopes (global, namespace, class)
			// and resolve naming conflicts and identify required usings (imports) for each file
			foreach (var file in files)
				nameScopes.Visit(file);

			for (var i = 0; i < files.Length; i++)
			{
				var file = files[i];

				importsCollector.Reset();
				importsCollector.Visit(file);

				// add required imports to file code model
				foreach (var import in importsCollector.Imports.OrderBy(_ => _, Language.FullNameComparer))
					file.AddImport(Language.ASTBuilder.Import(import));

				// resolve naming conflicts for external conflicting names, provider by user
				foreach (var name in _options.CodeGeneration.ConflictingNames)
				{
					var parsedName = Language.TypeParser.ParseNamespaceOrRegularTypeName(name, false);
					if (parsedName.Length > 0)
					{
						var scope = parsedName.Length > 1 ? parsedName.Take(parsedName.Length - 1).ToArray() : [];
						if (!nameScopes.ScopesWithNames.TryGetValue(scope, out var names))
							nameScopes.ScopesWithNames.Add(scope, names = new HashSet<CodeIdentifier>(Language.IdentifierEqualityComparer));
						names.Add(parsedName[parsedName.Length - 1]);
						if (!nameScopes.ScopesWithTypeNames.TryGetValue(scope, out names))
							nameScopes.ScopesWithTypeNames.Add(scope, names = new HashSet<CodeIdentifier>(Language.IdentifierEqualityComparer));
						names.Add(parsedName[parsedName.Length - 1]);
					}
					else
						throw new InvalidOperationException($"Cannot parse name: {name}");
				}

				// convert AST to source code
				var codeGenerator = Language.GetCodeGenerator(
					_options.CodeGeneration.NewLine,
					_options.CodeGeneration.Indent,
					_options.CodeGeneration.EnableNullableReferenceTypes,
					nameScopes.TypesNamespaces,
					nameScopes.ScopesWithNames,
					nameScopes.ScopesWithTypeNames);

				codeGenerator.Visit(file);

				sources[i] = codeGenerator.GetResult();
			}

			var results = new SourceCodeFile[files.Length];
			for (var i = 0; i < results.Length; i++)
				results[i] = new SourceCodeFile($"{files[i].FileName}{(_options.CodeGeneration.AddGeneratedFileSuffix ? ".generated" : null)}.{Language.FileExtension}", sources[i]);

			if (_interceptors != null)
			{
				var model = new FinalDataModel();

				model.Associations.AddRange(dataModel.DataContext.Associations);

				PopulateSchema(model, dataModel.DataContext);
				foreach (var schema in dataModel.DataContext.AdditionalSchemas.Values)
					PopulateSchema(model, schema);

				_interceptors.AfterSourceCodeGenerated(model);
			}

			return results;

			static void PopulateSchema(FinalDataModel model, SchemaModelBase schema)
			{
				model.Entities          .AddRange(schema.Entities);
				model.StoredProcedures  .AddRange(schema.StoredProcedures);
				model.ScalarFunctions   .AddRange(schema.ScalarFunctions);
				model.TableFunctions    .AddRange(schema.TableFunctions);
				model.AggregateFunctions.AddRange(schema.AggregateFunctions);
			}
		}
	}
}
