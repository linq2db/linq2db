using System.Threading;
using System.Threading.Tasks;

using LinqToDB;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace Tests.Analyzers
{
	// Thin wrapper over the Roslyn testing SDK: a snippet is compiled against the .NET 8 reference assemblies plus -
	// unless one of the VerifyWithoutLinqToDBAsync overloads is used - the real linq2db assembly, so Sql.Ext /
	// Sql.Window symbols resolve. The test project targets net8.0 so the loaded linq2db build matches the Net80
	// reference pack (a higher ref pack would trip CS1705), which is also why a caller-chosen reference set comes
	// only in the overload that drops linq2db: a lower pack cannot carry the net8.0 assembly.
	internal static class AnalyzerVerifier<TAnalyzer>
		where TAnalyzer : DiagnosticAnalyzer, new()
	{
		public static Task VerifyAsync(string source, params DiagnosticResult[] expected)
		{
			return RunAsync(source, withLinqToDB: true, ReferenceAssemblies.Net.Net80, expected);
		}

		// Without the linq2db reference, so a rule's capability gate can be exercised: an analyzer that resolves its
		// anchor types out of the compilation has to stay silent - and not throw - when they are absent.
		public static Task VerifyWithoutLinqToDBAsync(string source, params DiagnosticResult[] expected)
		{
			return RunAsync(source, withLinqToDB: false, ReferenceAssemblies.Net.Net80, expected);
		}

		// As above but against a caller-chosen reference set, for a rule that reads its behaviour off symbol presence
		// and so answers differently per target framework. The snippet declares the linq2db types it needs itself:
		// GetTypeByMetadataName resolves a source-declared type, so dropping the reference costs nothing here.
		public static Task VerifyWithoutLinqToDBAsync(string source, ReferenceAssemblies referenceAssemblies, params DiagnosticResult[] expected)
		{
			return RunAsync(source, withLinqToDB: false, referenceAssemblies, expected);
		}

		static Task RunAsync(string source, bool withLinqToDB, ReferenceAssemblies referenceAssemblies, DiagnosticResult[] expected)
		{
			var test = new CSharpAnalyzerTest<TAnalyzer, DefaultVerifier>
			{
				TestCode            = source,
				ReferenceAssemblies = referenceAssemblies,
			};

			if (withLinqToDB)
				test.TestState.AdditionalReferences.Add(MetadataReference.CreateFromFile(typeof(Sql).Assembly.Location));

			test.ExpectedDiagnostics.AddRange(expected);

			return test.RunAsync(CancellationToken.None);
		}
	}
}
