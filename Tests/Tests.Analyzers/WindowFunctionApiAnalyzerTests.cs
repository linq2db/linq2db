using System.Threading.Tasks;

using LinqToDB.Analyzers;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;

using NUnit.Framework;

using Verify = Tests.Analyzers.AnalyzerVerifier<LinqToDB.Analyzers.WindowFunctionApiAnalyzer>;

namespace Tests.Analyzers
{
	[TestFixture]
	public sealed class WindowFunctionApiAnalyzerTests
	{
		[Test]
		public Task ReportsLegacySqlExtWindowChain()
		{
			const string source = """
				using LinqToDB;

				class C
				{
					long M(int x) => {|#0:Sql.Ext.RowNumber().Over().PartitionBy(x).OrderBy(x).ToValue()|};
				}
				""";

			var expected = new DiagnosticResult(WindowFunctionApiAnalyzer.DiagnosticId, DiagnosticSeverity.Info)
				.WithLocation(0)
				.WithArguments("RowNumber");

			return Verify.VerifyAsync(source, expected);
		}

		[Test]
		public Task DoesNotReportNewSqlWindowApi()
		{
			const string source = """
				using LinqToDB;

				class C
				{
					long M(int x) => Sql.Window.RowNumber(f => f.PartitionBy(x).OrderBy(x));
				}
				""";

			return Verify.VerifyAsync(source);
		}
	}
}
