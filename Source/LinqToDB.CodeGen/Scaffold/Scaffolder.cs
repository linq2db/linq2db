using System;
using System.Collections.Generic;
using System.Linq;
using LinqToDB.CodeGen.CodeGeneration;
using LinqToDB.CodeGen.ContextModel;
using LinqToDB.Metadata;
using LinqToDB.Naming;
using LinqToDB.CodeModel;
using LinqToDB.Data;
using LinqToDB.DataModel;
using LinqToDB.Schema;
using LinqToDB.SqlProvider;
using LinqToDB.CodeGen.Metadata;

namespace LinqToDB.Scaffold
{
	/// <summary>
	/// Helper class to simplify common scenario of data model generation from database.
	/// </summary>
	public sealed class Scaffolder
	{
		private readonly NamingServices         _namingServices;
		private readonly CodeGenerationSettings _codeGenerationSettings;

		/// <summary>
		/// Creates instance of data model codegerator from database connection.
		/// </summary>
		/// <param name="languageProvider">Language provider to use for data model codegeneration.</param>
		/// <param name="nameConverter"><see cref="INameConversionProvider"/> pluralization service implementation.</param>
		/// <param name="codeGenerationSettings">Code generation settings.</param>
		public Scaffolder(ILanguageProvider languageProvider, INameConversionProvider nameConverter, CodeGenerationSettings codeGenerationSettings)
		{
			Language                = languageProvider;
			_namingServices         = new NamingServices(nameConverter);
			_codeGenerationSettings = codeGenerationSettings;
		}

		/// <summary>
		/// Gets language provider, used by current instance.
		/// </summary>
		public ILanguageProvider Language { get; }

		/// <summary>
		/// Loads database schema into <see cref="DatabaseModel"/> object.
		/// </summary>
		/// <returns>Loaded database model instance.</returns>
		public DatabaseModel LoadDataModel(DataConnection dataConnection)
		{
			var schemaSettings                   = new SchemaSettings();
			var contextSettings                  = new ContextModelSettings();
			contextSettings.GenerateSchemaAsType = true;

			var schemaProvider = new LegacySchemaProvider(dataConnection, schemaSettings, Language);
			
			return new DataModelLoader(
				_namingServices,
				Language,
				_codeGenerationSettings,
				schemaProvider,
				contextSettings,
				schemaSettings,
				schemaProvider
				).LoadSchema();
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
			       IMetadataBuilder          metadataBuilder,
			params ConvertCodeModelVisitor[] modelConverters)
		{
			var generator = new DataModelGenerator(
					Language,
					dataModel,
					metadataBuilder,
					name => _namingServices.NormalizeIdentifier(_codeGenerationSettings.ParameterNameNormalization, name),
					sqlBuilder);

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
		/// <param name="files">Code models.</param>
		/// <returns>Source code with file names.</returns>
		public SourceCodeFile[] GenerateSourceCode(params CodeFile[] files)
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
				foreach (var name in _codeGenerationSettings.ConflictingNames)
				{
					var parsedName = Language.TypeParser.ParseNamespaceOrRegularTypeName(name, false);
					if (parsedName.Length > 0)
					{
						var scope = parsedName.Length > 1 ? parsedName.Take(parsedName.Length - 1).ToArray() : Array.Empty<CodeIdentifier>();
						if (!nameScopes.ScopesWithNames.TryGetValue(scope, out var names))
							nameScopes.ScopesWithNames.Add(scope, names = new HashSet<CodeIdentifier>(Language.IdentifierEqualityComparer));
						names.Add(parsedName[parsedName.Length - 1]);
					}
					else
						throw new InvalidOperationException($"Cannot parse name: {name}");
				}

				// convert AST to source code
				var codeGenerator = Language.GetCodeGenerator(
					_codeGenerationSettings.NewLine,
					_codeGenerationSettings.Indent ?? "\t",
					_codeGenerationSettings.NullableReferenceTypes,
					nameScopes.TypesNamespaces,
					nameScopes.ScopesWithNames);

				codeGenerator.Visit(file);

				sources[i] = codeGenerator.GetResult();
			}

			var results = new SourceCodeFile[files.Length];
			for (var i = 0; i < results.Length; i++)
				results[i] = new SourceCodeFile($"{files[i].FileName}.{Language.FileExtension}", sources[i]);

			return results;
		}
	}
}
