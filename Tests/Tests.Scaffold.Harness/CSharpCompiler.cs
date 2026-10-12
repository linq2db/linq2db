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
		static readonly Lazy<MetadataReference[]> _references = new(static () => HostReferences.Paths.Select(static p => (MetadataReference)MetadataReference.CreateFromFile(p)).ToArray());

		/// <summary>
		/// Compiles sources to in-memory assembly and loads it, or writes it to <paramref name="outputPath"/> without loading.
		/// </summary>
		public static CompileResult Compile(string assemblyName, IReadOnlyList<SourceFile> sources, IReadOnlyCollection<string>? allowedWarnings = null, string? outputPath = null)
		{
			var parseOptions = new CSharpParseOptions(LanguageVersion.Latest);
			var trees        = sources.Select(s => CSharpSyntaxTree.ParseText(s.Code, parseOptions, s.FileName)).ToArray();

			// nullable context comes from #nullable directives, emitted by generator when NRT enabled
			var compilation = CSharpCompilation.Create(
				assemblyName,
				trees,
				_references.Value,
				new CSharpCompilationOptions(
					OutputKind.DynamicallyLinkedLibrary,
					nullableContextOptions: NullableContextOptions.Disable,
					// reference unification warnings, suppressed by .NET SDK by default
					specificDiagnosticOptions: [new("CS1701", ReportDiagnostic.Suppress), new("CS1702", ReportDiagnostic.Suppress)]));

			using var ms = new MemoryStream();

			var result = compilation.Emit(ms);

			var diagnostics = result.Diagnostics
				.Where(static d => !d.IsSuppressed && d.Severity is DiagnosticSeverity.Warning or DiagnosticSeverity.Error)
				.Select(static d =>
				{
					var span = d.Location.GetLineSpan();
					return new CompileDiagnostic(
						d.Severity == DiagnosticSeverity.Error ? CompileDiagnosticSeverity.Error : CompileDiagnosticSeverity.Warning,
						d.Id,
						d.GetMessage(System.Globalization.CultureInfo.InvariantCulture),
						span.Path,
						span.StartLinePosition.Line + 1,
						span.StartLinePosition.Character + 1);
				})
				.ToList();

			System.Reflection.Assembly? assembly = null;

			if (result.Success)
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

			return new CompileResult(sources, diagnostics, allowedWarnings ?? [], result.Success, assembly);
		}
	}
}
