using System.Threading;
using System.Threading.Tasks;

using LinqToDB;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace Tests.Analyzers
{
	// Wrapper over the Roslyn code-fix testing SDK. The fixed source is also compiled + re-analyzed, so it must
	// build (guards expression-tree validity) and produce no diagnostic (proves the fix removes the legacy usage).
	internal static class CodeFixVerifier<TAnalyzer, TCodeFix>
		where TAnalyzer : DiagnosticAnalyzer, new()
		where TCodeFix  : CodeFixProvider, new()
	{
		public static Task VerifyAsync(string source, string fixedSource, string? editorConfig = null)
		{
			var test = new CSharpCodeFixTest<TAnalyzer, TCodeFix, DefaultVerifier>
			{
				TestCode            = source,
				FixedCode           = fixedSource,
				ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
			};

			var reference = MetadataReference.CreateFromFile(typeof(Sql).Assembly.Location);
			test.TestState.AdditionalReferences.Add(reference);
			test.FixedState.AdditionalReferences.Add(reference);

			// Inject an .editorconfig to exercise a code-fix option (default-off behaviors, etc.).
			if (editorConfig is not null)
			{
				test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", editorConfig));
				test.FixedState.AnalyzerConfigFiles.Add(("/.editorconfig", editorConfig));
			}

			return test.RunAsync(CancellationToken.None);
		}
	}
}
