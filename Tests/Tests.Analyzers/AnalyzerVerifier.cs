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
	// assemblies plus - unless VerifyWithoutLinqToDBAsync is used - the real linq2db assembly, so Sql.Ext /
	// Sql.Window symbols resolve. The test project targets net8.0 so the loaded linq2db build matches the Net80
	// reference pack (a higher ref pack would trip CS1705).
	internal static class AnalyzerVerifier<TAnalyzer>
		where TAnalyzer : DiagnosticAnalyzer, new()
	{
		public static Task VerifyAsync(string source, params DiagnosticResult[] expected)
		{
			return RunAsync(source, withLinqToDB: true, expected);
		}

		// Without the linq2db reference, so a rule's capability gate can be exercised: an analyzer that resolves its
		// anchor types out of the compilation has to stay silent - and not throw - when they are absent.
		public static Task VerifyWithoutLinqToDBAsync(string source, params DiagnosticResult[] expected)
		{
			return RunAsync(source, withLinqToDB: false, expected);
		}

		static Task RunAsync(string source, bool withLinqToDB, DiagnosticResult[] expected)
		{
			var test = new CSharpAnalyzerTest<TAnalyzer, DefaultVerifier>
			{
				TestCode            = source,
				ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
			};

			if (withLinqToDB)
				test.TestState.AdditionalReferences.Add(MetadataReference.CreateFromFile(typeof(Sql).Assembly.Location));

			test.ExpectedDiagnostics.AddRange(expected);

			return test.RunAsync(CancellationToken.None);
		}
	}
}
