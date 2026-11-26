using System;

using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Commands;

using Shouldly;

using Tests.Scaffold.Harness;

using static Tests.Scaffold.Harness.TestSchema;

namespace Tests.Scaffold
{
	[TestFixture]
	public sealed class HarnessSelfTests
	{
		[Test]
		public void CSharpCompileErrorFails()
		{
			var result = CSharpCompiler.Compile(
				"SelfTest_CSharpError_" + Guid
					.NewGuid()
					.ToString("N"),
				[new SourceFile("Broken.cs", "namespace Broken { public class C { public int M() { return 1 } } }")]);

			result.Success.ShouldBeFalse();
			result.Failures.ShouldContain(d => d.Id == "CS1002");
			result
				.Report()
				.ShouldContain("    1: namespace Broken");
		}

		[Test]
		public void FSharpCompileErrorFails()
		{
			var result = FSharpCompiler.Compile(
				"SelfTest_FSharpError_" + Guid
					.NewGuid()
					.ToString("N"),
				[new SourceFile("Broken.fs", "namespace Broken\n\nmodule M =\n    let f (x : int) : string = x\n")],
				nullable: true);

			result.Success.ShouldBeFalse();
			result.Failures.ShouldContain(d => d.Id == "FS0001");
			result
				.Report()
				.ShouldContain("    4:     let f (x : int) : string = x");
		}

		[Test]
		public void CSharpWarningFailsUnlessAllowed()
		{
			var source = new SourceFile("Warning.cs", "namespace W { public class C { public void M() { int unused; } } }");

			var failed  = CSharpCompiler.Compile($"SelfTest_CSharpWarning_{Guid.NewGuid():N}", [source]);
			var allowed = CSharpCompiler.Compile($"SelfTest_CSharpWarning_{Guid.NewGuid():N}", [source], ["CS0168"]);

			failed.Success.ShouldBeFalse();
			failed.Failures.ShouldContain(d => d.Id == "CS0168" && d.Severity == CompileDiagnosticSeverity.Warning);
			allowed.Success.ShouldBeTrue();
		}

		[Test]
		public void FSharpWarningFailsUnlessAllowed()
		{
			// FS0025: incomplete pattern match
			var source = new SourceFile("Warning.fs", "namespace W\n\nmodule M =\n    let f (x : int option) = match x with Some v -> v\n");

			var failed  = FSharpCompiler.Compile($"SelfTest_FSharpWarning_{Guid.NewGuid():N}", [source], nullable: true);
			var allowed = FSharpCompiler.Compile($"SelfTest_FSharpWarning_{Guid.NewGuid():N}", [source], nullable: true, ["FS0025"]);

			failed.Success.ShouldBeFalse();
			failed.Failures.ShouldContain(d => d.Id == "FS0025" && d.Severity == CompileDiagnosticSeverity.Warning);
			allowed.Success.ShouldBeTrue();
		}

		[Test]
		public void FSharpFloorLanguageVersionChecked()
		{
			// accessor-level access modifiers on an auto-property need F# 10
			var result = FSharpCompiler.Compile(
				"SelfTest_FSharpFloor_" + Guid
					.NewGuid()
					.ToString("N"),
				[new SourceFile("Floor.fs", "namespace Floor\n\ntype C() =\n    member val X = 0 with public get, private set\n")],
				nullable: true);

			result.Success.ShouldBeFalse();
			result.Failures.ShouldContain(d => d.Severity == CompileDiagnosticSeverity.Error && d.Message.StartsWith("[langversion:9.0]"));
			result.Failures.ShouldNotContain(d => d.Message.StartsWith("[langversion:latest]"));
		}

		[Test]
		public void FSharpLatestLanguageVersionChecked()
		{
			// F# 11 reads [NotNullIfNotNull] on ExpressionVisitor.Visit, so nonNull over its result is FS3262
			var result = FSharpCompiler.Compile(
				"SelfTest_FSharpLatest_" + Guid
					.NewGuid()
					.ToString("N"),
				[new SourceFile("Latest.fs", "namespace Latest\n\nopen System.Linq.Expressions\n\nmodule M =\n    let f (v : ExpressionVisitor) (x : Expression) = v.Visit(x) |> nonNull\n")],
				nullable: true);

			result.Success.ShouldBeFalse();
			result.Failures.ShouldContain(d => d.Id == "FS3262" && d.Message.StartsWith("[langversion:latest]"));
			result.Failures.ShouldNotContain(d => d.Message.StartsWith("[langversion:9.0]"));
		}

		[Test]
		[ActiveIssue(1, ErrorMessage = "expected failure")]
		public void ActiveIssueHoldsOnDeclaredFailure()
		{
			throw new InvalidOperationException("expected failure");
		}

		[TestCase(ScaffoldLanguage.CSharp)]
		[TestCase(ScaffoldLanguage.FSharp)]
		[ActiveIssue(1, Arguments = "FSharp", ErrorMessage = "expected failure")]
		public void ActiveIssueTargetsArguments(ScaffoldLanguage language)
		{
			if (language == ScaffoldLanguage.FSharp)
				throw new InvalidOperationException("expected failure");
		}

		// pinned test body: passes, or fails with specified message
		sealed class PinnedBody(Test test, string? failure) : TestCommand(test)
		{
			public override TestResult Execute(TestExecutionContext context)
			{
				if (failure != null)
					throw new InvalidOperationException(failure);

				context.CurrentResult.SetResult(ResultState.Success);
				return context.CurrentResult;
			}
		}

		static class PinnedFixture
		{
			public static void Method()
			{
			}
		}

		// outcomes that fail the run can't be asserted by a pinned test itself: the attribute command runs over a stub body
		static TestResult ExecutePinned(string? failure)
		{
			var test    = new TestMethod(new MethodWrapper(typeof(PinnedFixture), nameof(PinnedFixture.Method)));
			var context = new TestExecutionContext { CurrentTest = test, CurrentResult = test.MakeTestResult() };

			return new ActiveIssueAttribute(1) { ErrorMessage = "expected failure" }
				.Wrap(new PinnedBody(test, failure))
				.Execute(context);
		}

		[Test]
		public void ActiveIssueFailsWhenTestPasses()
		{
			var result = ExecutePinned(null);

			result.ResultState.ShouldBe(ResultState.Failure);
			result.Message.ShouldStartWith("[ActiveIssue] Test passed");
		}

		[Test]
		public void ActiveIssueFailsOnUndeclaredFailure()
		{
			var result = ExecutePinned("unrelated failure");

			result.ResultState.ShouldBe(ResultState.Failure);
			result.Message.ShouldStartWith("[ActiveIssue] Expected failure");
		}

		[Test]
		public void GeneratedModelLoads([Values] ScaffoldLanguage language)
		{
			var schema = ProviderProfile.SQLite.CreateSchema();
			schema.AddTable("Person", [Col("Id", "int"), Col("Name", "text", nullable: true)], primaryKey: ["Id"]);

			var model = ScaffoldHarness.Build(nameof(GeneratedModelLoads), language, ProviderProfile.SQLite, schema);

			model.ContextType.Name.ShouldBe("TestDataContext");
			model
				.GetType("Person")
				.ShouldNotBeNull();

			using var db = new SqliteTestDb();

			var report = ModelSmoke.Run(model, db.Options);

			report.Tables.ShouldBe(1);
			report.Rows.ShouldBe(ModelSmoke.RowCount);
			report.FindMethods.ShouldNotBeEmpty();
		}
	}
}
