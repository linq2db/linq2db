using System.Threading.Tasks;

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

			return Verify.VerifyAsync(source, fixedSource, TabIndent);
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

			return Verify.VerifyAsync(source, fixedSource, TabIndent);
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

			return Verify.VerifyAsync(source, fixedSource, TabIndent);
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

			return Verify.VerifyAsync(source, fixedSource, TabIndent);
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

			return Verify.VerifyAsync(source, fixedSource, TabIndent);
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

			return Verify.VerifyAsync(source, fixedSource, TabIndent);
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

			return Verify.VerifyAsync(source, fixedSource, TabIndent);
		}
	}
}
