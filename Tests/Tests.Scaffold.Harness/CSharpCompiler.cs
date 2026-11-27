using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Loader;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Tests.Scaffold.Harness
{
	public static class CSharpCompiler
	{
		static readonly Lazy<MetadataReference[]> _references = new(static () => HostReferences
			.Paths
			.Select(static p => (MetadataReference)MetadataReference.CreateFromFile(p))
			.ToArray());

		/// <summary>
		/// Language versions for generated code: the floor (default versions of net462 and netstandard2.0 projects, C# 8 for
		/// nullable annotations), then the newest the compiler supports.
		/// </summary>
		public static IReadOnlyList<LanguageVersion> GeneratedCodeVersions(bool nullable)
			=> [nullable ? LanguageVersion.CSharp8 : LanguageVersion.CSharp7_3, LanguageVersion.Latest];

		/// <summary>
		/// Compiles sources with each of <paramref name="languageVersions"/> (default: latest) to in-memory assembly and
		/// loads it, or writes it to <paramref name="outputPath"/> without loading. The first version produces the assembly.
		/// </summary>
		public static CompileResult Compile(
			string                          assemblyName,
			IReadOnlyList<SourceFile>       sources,
			IReadOnlyCollection<string>?    allowedWarnings  = null,
			string?                         outputPath       = null,
			IReadOnlyList<LanguageVersion>? languageVersions = null)
		{
			languageVersions ??= [LanguageVersion.Latest];

			var diagnostics = new List<CompileDiagnostic>();
			var emitted     = true;

			using var ms = new MemoryStream();

			for (var i = 0; i < languageVersions.Count; i++)
			{
				var compilation = CreateCompilation(assemblyName, sources, languageVersions[i]);

				// assembly is emitted once, other versions only check
				IEnumerable<Diagnostic> result;

				if (i == 0)
				{
					var emit = compilation.Emit(ms);

					result   = emit.Diagnostics;
					emitted &= emit.Success;
				}
				else
				{
					result   = compilation.GetDiagnostics();
					emitted &= !result.Any(static d => d.Severity == DiagnosticSeverity.Error);
				}

				diagnostics.AddRange(Convert(result, languageVersions[i]));
			}

			System.Reflection.Assembly? assembly = null;

			if (emitted)
			{
				ms.Position = 0;

				if (outputPath != null)
				{
					using var file = File.Create(outputPath);
					ms.CopyTo(file);
				}
				else
					assembly = AssemblyLoadContext.Default.LoadFromStream(ms);
			}

			return new CompileResult(sources, diagnostics, allowedWarnings ?? [], emitted, assembly);
		}

		static CSharpCompilation CreateCompilation(string assemblyName, IReadOnlyList<SourceFile> sources, LanguageVersion languageVersion)
		{
			var parseOptions = new CSharpParseOptions(languageVersion);
			var trees        = sources
				.Select(s => CSharpSyntaxTree.ParseText(s.Code, parseOptions, s.FileName))
				.ToArray();

			// nullable context comes from #nullable directives, emitted by generator when NRT enabled
			return CSharpCompilation.Create(
				assemblyName,
				trees,
				_references.Value,
				new CSharpCompilationOptions(
					OutputKind.DynamicallyLinkedLibrary,
					nullableContextOptions: NullableContextOptions.Disable,
					// .NET SDK projects compile at WarningLevel = AnalysisLevel (all warning waves), not the API default 4
					warningLevel: 9999,
					// reference unification warnings, suppressed by .NET SDK by default
					specificDiagnosticOptions: [new("CS1701", ReportDiagnostic.Suppress), new("CS1702", ReportDiagnostic.Suppress)]));
		}

		static IEnumerable<CompileDiagnostic> Convert(IEnumerable<Diagnostic> diagnostics, LanguageVersion languageVersion)
		{
			var version = languageVersion.ToDisplayString();

			return diagnostics
				.Where(static d => !d.IsSuppressed && d.Severity is DiagnosticSeverity.Warning or DiagnosticSeverity.Error)
				.Select(d =>
				{
					var span = d.Location.GetLineSpan();
					return new CompileDiagnostic(
						d.Severity == DiagnosticSeverity.Error ? CompileDiagnosticSeverity.Error : CompileDiagnosticSeverity.Warning,
						d.Id,
						$"[langversion:{version}] {d.GetMessage(System.Globalization.CultureInfo.InvariantCulture)}",
						span.Path,
						span.StartLinePosition.Line + 1,
						span.StartLinePosition.Character + 1);
				});
		}
	}
}
