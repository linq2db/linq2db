using System;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis.Testing;

using NUnit.Framework;

using Verify = Tests.Analyzers.CodeFixVerifier<
	LinqToDB.Analyzers.ServerSideOnlyContractAnalyzer,
	LinqToDB.Analyzers.CodeFixes.ServerSideOnlyContractCodeFixProvider>;

namespace Tests.Analyzers
{
	[TestFixture]
	public sealed class ServerSideOnlyContractCodeFixTests
	{
		const string Usings = """
			using System;
			using LinqToDB;
			using LinqToDB.Mapping;

			""";

		// The snippets are tab-indented like the rest of the repo. Without this the fix's formatting pass
		// falls back to its default (four spaces) and every expected/actual comparison differs on whitespace
		// alone - so this also asserts the fix honours the consumer's indent_style rather than imposing one.
		const string TabIndent = """
			root = true

			[*.cs]
			indent_style = tab
			end_of_line = lf
			""";

		// .gitattributes pins `*.cs text eol=crlf`, so these raw string literals hold CRLF on every platform
		// once git has normalised the file - but a freshly written working copy can still hold LF. Feeding
		// the harness whichever the checkout happens to have makes the expected text disagree with what the
		// formatter emits under `end_of_line = lf`, which passes locally and fails on CI. Normalise both
		// sides so the comparison is about the fix, not about the checkout.
		static Task Run(string source, string fixedSource) => Verify.VerifyAsync(
			source     .Replace("\r\n", "\n", StringComparison.Ordinal),
			fixedSource.Replace("\r\n", "\n", StringComparison.Ordinal),
			TabIndent);

		// A property's diagnostic is reported on the property while the analyzed block is the getter, which the
		// SDK classifies as non-local and refuses to fix. `dotnet format` over the same code applies the fix, so
		// the check is the harness being stricter than the product, not a defect - skip it for property cases.
		static Task RunOnProperty(string source, string fixedSource) => Verify.VerifyAsync(
			source     .Replace("\r\n", "\n", StringComparison.Ordinal),
			fixedSource.Replace("\r\n", "\n", StringComparison.Ordinal),
			TabIndent,
			CodeFixTestBehaviors.SkipLocalDiagnosticCheck);

		[Test]
		public Task AddsServerSideOnlyAttributeWhenNoAttributeIsPresent()
		{
			var source = Usings + """
				static class C
				{
					public static int {|L2DB1003:M|}() => throw new ServerSideOnlyException(nameof(M));
				}
				""";

			var fixedSource = Usings + """
				static class C
				{
					[ServerSideOnly]
					public static int M() => throw new ServerSideOnlyException(nameof(M));
				}
				""";

			return Run(source, fixedSource);
		}

		[Test]
		public Task SetsNamedArgumentOnAnExistingSqlAttribute()
		{
			// Must add the named argument rather than a second attribute: the member already declares how it
			// translates, and two markers on one member is not the shape the codebase uses.
			//
			// The stub already throws ServerSideOnlyException so exactly one fix applies. With a
			// NotImplementedException body the two rules compose - adding the marker makes the member marked,
			// which then exposes it to L2DB1004 - and the harness would need two iterations declared. That
			// composition is worth knowing about but belongs in its own test, not folded into this one.
			var source = Usings + """
				static class C
				{
					[Sql.Function("F")]
					public static int {|L2DB1003:M|}() => throw new ServerSideOnlyException(nameof(M));
				}
				""";

			var fixedSource = Usings + """
				static class C
				{
					[Sql.Function("F", ServerSideOnly = true)]
					public static int M() => throw new ServerSideOnlyException(nameof(M));
				}
				""";

			return Run(source, fixedSource);
		}

		[Test]
		public Task UpdatesAnExistingServerSideOnlyFalseArgument()
		{
			// Appending a second ServerSideOnly argument here is CS0643. Dogfooding over Tests/Linq caught
			// exactly this - the fix compiled everywhere else and broke on the one site that already said
			// ServerSideOnly = false.
			var source = Usings + """
				static class C
				{
					[Sql.Function("F", ServerSideOnly = false)]
					public static int {|L2DB1003:M|}() => throw new ServerSideOnlyException(nameof(M));
				}
				""";

			var fixedSource = Usings + """
				static class C
				{
					[Sql.Function("F", ServerSideOnly = true)]
					public static int M() => throw new ServerSideOnlyException(nameof(M));
				}
				""";

			return Run(source, fixedSource);
		}

		[Test]
		public Task ReplacesTheThrownExceptionInAMarkedStub()
		{
			var source = Usings + """
				static class C
				{
					[ServerSideOnly]
					public static int {|L2DB1004:M|}() => throw new NotImplementedException();
				}
				""";

			var fixedSource = Usings + """
				static class C
				{
					[ServerSideOnly]
					public static int M() => throw new ServerSideOnlyException(nameof(M));
				}
				""";

			return Run(source, fixedSource);
		}

		[Test]
		public Task ReplacesTheThrownExceptionInABlockBodiedStub()
		{
			var source = Usings + """
				static class C
				{
					[ServerSideOnly]
					public static int {|L2DB1004:M|}()
					{
						throw new NotImplementedException();
					}
				}
				""";

			var fixedSource = Usings + """
				static class C
				{
					[ServerSideOnly]
					public static int M()
					{
						throw new ServerSideOnlyException(nameof(M));
					}
				}
				""";

			return Run(source, fixedSource);
		}

		[Test]
		public Task ReplacesTheGetterStubWhenTheSetterIsDeclaredFirst()
		{
			// The analyzer classifies the getter, but the node the fix rewrites is the whole property - so a
			// descendant scan over the declaration reaches the setter's throw first and rewrites the wrong
			// accessor, leaving the reported getter untouched. Accessor order is the only trigger.
			var source = Usings + """
				static class C
				{
					[ServerSideOnly]
					public static int {|L2DB1004:P|}
					{
						set => throw new NotImplementedException();
						get => throw new NotImplementedException();
					}
				}
				""";

			var fixedSource = Usings + """
				static class C
				{
					[ServerSideOnly]
					public static int P
					{
						set => throw new NotImplementedException();
						get => throw new ServerSideOnlyException(nameof(P));
					}
				}
				""";

			return RunOnProperty(source, fixedSource);
		}

		[Test]
		public Task ReplacesTheStubOfAnExpressionBodiedProperty()
		{
			// An expression-bodied property has no accessor list at all, so it takes the other branch of the
			// narrowing above. Pairs with the setter-first case: together they pin both property shapes.
			var source = Usings + """
				static class C
				{
					[ServerSideOnly]
					public static int {|L2DB1004:P|} => throw new NotImplementedException();
				}
				""";

			var fixedSource = Usings + """
				static class C
				{
					[ServerSideOnly]
					public static int P => throw new ServerSideOnlyException(nameof(P));
				}
				""";

			return RunOnProperty(source, fixedSource);
		}

		[Test]
		public Task PreservesDocCommentsAndTriviaWhenAddingTheAttribute()
		{
			var source = Usings + """
				static class C
				{
					/// <summary>Does a thing.</summary>
					/// <returns>A number.</returns>
					// a trailing implementation note
					public static int {|L2DB1003:M|}() => throw new ServerSideOnlyException(nameof(M));
				}
				""";

			var fixedSource = Usings + """
				static class C
				{
					/// <summary>Does a thing.</summary>
					/// <returns>A number.</returns>
					// a trailing implementation note
					[ServerSideOnly]
					public static int M() => throw new ServerSideOnlyException(nameof(M));
				}
				""";

			return Run(source, fixedSource);
		}

		[Test]
		public Task FixAllConvertsEveryAdjacentOccurrence()
		{
			// The shape BatchFixer silently under-applies: several diagnostics physically adjacent in one
			// type. Sql.Row.generated.cs has ten such overloads in a row, so this is the rule's most common
			// real target, not a synthetic edge case.
			var source = Usings + """
				static class C
				{
					[ServerSideOnly] public static int {|L2DB1004:M1|}() => throw new NotImplementedException();
					[ServerSideOnly] public static int {|L2DB1004:M2|}() => throw new NotImplementedException();
					[ServerSideOnly] public static int {|L2DB1004:M3|}() => throw new NotImplementedException();
					[ServerSideOnly] public static int {|L2DB1004:M4|}() => throw new NotImplementedException();
				}
				""";

			var fixedSource = Usings + """
				static class C
				{
					[ServerSideOnly] public static int M1() => throw new ServerSideOnlyException(nameof(M1));
					[ServerSideOnly] public static int M2() => throw new ServerSideOnlyException(nameof(M2));
					[ServerSideOnly] public static int M3() => throw new ServerSideOnlyException(nameof(M3));
					[ServerSideOnly] public static int M4() => throw new ServerSideOnlyException(nameof(M4));
				}
				""";

			return Run(source, fixedSource);
		}
	}
}
