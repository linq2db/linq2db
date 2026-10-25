using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

using LinqToDB.Scaffold;

using NUnit.Framework;

namespace Tests.Scaffold.Harness
{
	/// <summary>
	/// Schema → generate → compile → load pipeline.
	/// </summary>
	public static class ScaffoldHarness
	{
		static readonly ConcurrentDictionary<string, Lazy<ScaffoldedModel>> _cache = new(StringComparer.Ordinal);

		/// <summary>
		/// Compiles generated source. Doesn't fail on compilation errors.
		/// </summary>
		public static CompileResult TryCompile(GeneratedSource source, IReadOnlyCollection<string>? allowedWarnings = null, string? outputPath = null)
		{
			var assemblyName = $"Gen_{Generator.ToIdentifier(source.Name)}_{source.Language}_{Guid.NewGuid():N}";
			var files        = source.Files.Select(static f => new SourceFile(f.FileName, f.Code)).ToArray();

			return source.Language == ScaffoldLanguage.CSharp
				? CSharpCompiler.Compile(assemblyName, files, allowedWarnings, outputPath)
				: FSharpCompiler.Compile(assemblyName, files, source.Options.CodeGeneration.EnableNullableReferenceTypes, allowedWarnings, outputPath);
		}

		/// <summary>
		/// Compiles and loads generated source. Fails test with diagnostics and source listing on compilation errors or
		/// warnings, not listed in <paramref name="allowedWarnings"/>.
		/// </summary>
		public static ScaffoldedModel Compile(GeneratedSource source, IReadOnlyCollection<string>? allowedWarnings = null)
		{
			var result = TryCompile(source, allowedWarnings);

			if (!result.Success)
				Assert.Fail(result.Report());

			return new ScaffoldedModel(source, result.Assembly!);
		}

		/// <summary>
		/// Generates and compiles model.
		/// </summary>
		public static ScaffoldedModel Build(
			string                       name,
			ScaffoldLanguage             language,
			ProviderProfile              profile,
			TestSchema                   schema,
			Action<ScaffoldOptions>?     configure       = null,
			ScaffoldInterceptors?        interceptors    = null,
			TestTypeMappings?            typeMappings    = null,
			IReadOnlyCollection<string>? allowedWarnings = null)
		{
			return Compile(Generator.Generate(name, language, profile, schema, configure, interceptors, typeMappings), allowedWarnings);
		}

		/// <summary>
		/// Returns model, compiled once per process for <paramref name="key"/>. For tests, executed in parallel for multiple
		/// database contexts with the same model.
		/// </summary>
		public static ScaffoldedModel GetOrBuild(string key, Func<ScaffoldedModel> build)
		{
			return _cache.GetOrAdd(key, _ => new Lazy<ScaffoldedModel>(build)).Value;
		}
	}
}
