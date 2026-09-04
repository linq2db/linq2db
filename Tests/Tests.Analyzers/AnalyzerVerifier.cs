using System.Threading;
using System.Threading.Tasks;

using LinqToDB;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace Tests.Analyzers
{
	// Thin wrapper over the Roslyn testing SDK: every analyzed snippet is compiled against the .NET 8 reference
	// assemblies plus the real linq2db assembly, so Sql.Ext / Sql.Window symbols resolve. The test project targets
	// net8.0 so the loaded linq2db build matches the Net80 reference pack (a higher ref pack would trip CS1705).
	internal static class AnalyzerVerifier<TAnalyzer>
		where TAnalyzer : DiagnosticAnalyzer, new()
	{
		public static Task VerifyAsync(string source, params DiagnosticResult[] expected)
		{
			var test = new CSharpAnalyzerTest<TAnalyzer, DefaultVerifier>
			{
				TestCode            = source,
				ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
			};

			test.TestState.AdditionalReferences.Add(MetadataReference.CreateFromFile(typeof(Sql).Assembly.Location));
			test.ExpectedDiagnostics.AddRange(expected);

			return test.RunAsync(CancellationToken.None);
		}
	}
}
