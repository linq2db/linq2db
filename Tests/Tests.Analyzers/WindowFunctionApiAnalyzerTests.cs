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
		public Task DoesNotReportPlainAggregateWithoutOver()
		{
			// No .Over(): a plain aggregate is not a window function and has no Sql.Window migration target, so
			// the rule must not fire (reporting it would give an unactionable "migrate to Sql.Window" message).
			const string source = """
				using LinqToDB;

				class C
				{
					int M(int x) => Sql.Ext.Sum(x).ToValue();
				}
				""";

			return Verify.VerifyAsync(source);
		}

		[Test]
		public Task ReportsFramedAggregate()
		{
			const string source = """
				using LinqToDB;

				class C
				{
					int M(int x) => {|#0:Sql.Ext.Count().Over().PartitionBy(x).OrderBy(x).Range.Between.UnboundedPreceding.And.CurrentRow.ToValue()|};
				}
				""";

			var expected = new DiagnosticResult(WindowFunctionApiAnalyzer.DiagnosticId, DiagnosticSeverity.Info)
				.WithLocation(0)
				.WithArguments("Count");

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

		[Test]
		public Task DoesNotReportUnrelatedToValueMethod()
		{
			// A ToValue() that is not declared inside AnalyticFunctions must never be flagged.
			const string source = """
				using LinqToDB;

				class C
				{
					interface IThing { int ToValue(); }

					int M(IThing t) => t.ToValue();
				}
				""";

			return Verify.VerifyAsync(source);
		}
	}
}
