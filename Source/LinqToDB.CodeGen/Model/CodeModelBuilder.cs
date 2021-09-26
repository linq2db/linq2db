using System;
using System.Collections.Generic;
using System.Linq;
using LinqToDB.CodeGen.Model;

namespace LinqToDB.CodeGen.CodeGeneration
{
	public class CodeModelBuilder
	{
		private readonly CodeGenerationSettings _settings;
		private readonly ILanguageProvider _languageProvider;
		private readonly CodeBuilder _builder;

		public CodeModelBuilder(
			CodeGenerationSettings settings,
			ILanguageProvider languageProvider,
			CodeBuilder codeBuilder)
		{
			_settings              = settings;
			_languageProvider      = languageProvider;
			_builder               = codeBuilder;
		}

		public string[] GetSourceCode(IReadOnlyList<CodeFile> files)
		{
			var results = new string[files.Count];

			var namesNormalize = _languageProvider.GetIdentifiersNormalizer();
			foreach (var file in files)
				namesNormalize.Visit(file);

			var importsCollector = new ImportsCollector(_languageProvider);
			var nameScopes = new NameScopesCollector(_languageProvider);
			foreach (var file in files)
				nameScopes.Visit(file);

			for (var i = 0; i < files.Count; i++)
			{
				var file = files[i];

				importsCollector.Reset();
				importsCollector.Visit(file);

				foreach (var import in importsCollector.Imports.OrderBy(_ => _, _languageProvider.FullNameComparer))
					file.Imports.Add(_builder.Import(import));

				foreach (var name in _settings.ConflictingNames)
				{
					// TODO: add separate method to parse names only
					var parsedName = _languageProvider.TypeParser.Parse(name, false);
					if (parsedName is RegularType type && type.Parent == null)
					{
						var scope = type.Namespace ?? Array.Empty<CodeIdentifier>();
						if (!nameScopes.ScopesWithNames.TryGetValue(scope, out var names))
							nameScopes.ScopesWithNames.Add(scope, names = new HashSet<CodeIdentifier>(_languageProvider.IdentifierEqualityComparer));
						names.Add(type.Name);
					}
					else
						throw new InvalidOperationException($"Cannot parse name: {name}");
				}

				var codeGenerator = _languageProvider.GetCodeGenerator(
					_settings.NewLine,
					_settings.Indent ?? "\t",
					_settings.NullableReferenceTypes,
					nameScopes.TypesNamespaces,
					nameScopes.ScopesWithNames);

				codeGenerator.Visit(file);

				results[i] = codeGenerator.GetResult();
			}

			return results;
		}
	}
}
