using System;

using LinqToDB;

using NUnit.Framework;
using NUnit.Framework.Interfaces;

using Shouldly;

namespace Tests.Infrastructure
{
	/// <summary>
	/// The decision logic behind <see cref="ActiveIssueNewAttribute"/>, tested as the pure functions it is factored
	/// into. Exercising it end-to-end is not possible from inside the suite: a gated test that passes is reported as
	/// a failure <em>by construction</em>, so it could not sit here green, and a nested
	/// <c>NUnitTestAssemblyRunner</c> would share the static <see cref="TestProgressTracker"/> and mark the outer run
	/// done part-way through. <see cref="ActiveIssueNewAttribute.Decide"/> exists so the policy is checkable without
	/// either.
	/// </summary>
	[TestFixture]
	public class ActiveIssueNewTests : TestBase
	{
		const string SqlError    = "LinqToDB.LinqToDBException : The LINQ expression could not be converted to SQL.";
		const string OtherError  = "System.InvalidOperationException : something else entirely";
		const string AssertError = "  Expected: 3\r\n  But was:  7\r\n";

		static ActiveIssueNewAttribute Expecting(string? type = null, string? message = null) => new(1234)
		{
			ErrorTypeName = type,
			ErrorMessage  = message,
		};

		#region Decide — SC-1 / SC-2 / SC-3 / SC-3b

		[Test]
		public void Decide_PassingTest_IsFailure()
		{
			var decision = ActiveIssueNewAttribute.Decide(Expecting("LinqToDB.LinqToDBException"), ResultState.Success, null, isRemote: false);

			decision.ShouldNotBeNull();
			decision!.Value.State.Status.ShouldBe(TestStatus.Failed);
			decision.Value.Message.ShouldContain("Test passed but is marked");
			decision.Value.Message.ShouldContain("1234");
		}

		[Test]
		public void Decide_DeclaredError_IsInconclusive()
		{
			var decision = ActiveIssueNewAttribute.Decide(Expecting("LinqToDB.LinqToDBException"), ResultState.Error, SqlError, isRemote: false);

			decision.ShouldNotBeNull();
			decision!.Value.State.Status.ShouldBe(TestStatus.Inconclusive);
			decision.Value.Message.ShouldContain("Known issue");
			decision.Value.Message.ShouldContain(SqlError);
		}

		[Test]
		public void Decide_DeclaredErrorWithMessageFragment_IsInconclusive()
		{
			var decision = ActiveIssueNewAttribute.Decide(
				Expecting("LinqToDB.LinqToDBException", "could not be converted to SQL."),
				ResultState.Failure,
				SqlError,
				isRemote: false);

			decision!.Value.State.Status.ShouldBe(TestStatus.Inconclusive);
		}

		[Test]
		public void Decide_DifferentErrorType_IsFailure()
		{
			var decision = ActiveIssueNewAttribute.Decide(Expecting("LinqToDB.LinqToDBException"), ResultState.Error, OtherError, isRemote: false);

			decision!.Value.State.Status.ShouldBe(TestStatus.Failed);

			// Both halves must be named, or the reader cannot tell a moved message from a real regression.
			decision.Value.Message.ShouldContain("LinqToDB.LinqToDBException");
			decision.Value.Message.ShouldContain(OtherError);
		}

		[Test]
		public void Decide_DifferentErrorMessage_IsFailure()
		{
			var decision = ActiveIssueNewAttribute.Decide(
				Expecting("LinqToDB.LinqToDBException", "some other wording"),
				ResultState.Failure,
				SqlError,
				isRemote: false);

			decision!.Value.State.Status.ShouldBe(TestStatus.Failed);
			decision.Value.Message.ShouldContain("some other wording");
		}

		[Test]
		public void Decide_WrongResultsAssertion_MatchesOnMessageAlone()
		{
			// An assertion failure's message carries no type name, so leaving ErrorType unset is how a
			// wrong-results issue is declared.
			var decision = ActiveIssueNewAttribute.Decide(Expecting(message: "But was:  7"), ResultState.Failure, AssertError, isRemote: false);

			decision!.Value.State.Status.ShouldBe(TestStatus.Inconclusive);
		}

		[Test]
		public void Decide_NoExpectationDeclared_AnyFailureMatches()
		{
			var decision = ActiveIssueNewAttribute.Decide(Expecting(), ResultState.Failure, OtherError, isRemote: false);

			decision!.Value.State.Status.ShouldBe(TestStatus.Inconclusive);
		}

		[TestCase("Skipped")]
		[TestCase("Ignored")]
		[TestCase("Inconclusive")]
		[TestCase("Warning")]
		public void Decide_InnerNonVerdictOutcome_PassesThrough(string outcome)
		{
			// The test opted out of running, so it produced no evidence about the issue either way. The pattern this
			// attribute is modelled on gets this wrong and reports an ignored test as a failure.
			var state = outcome switch
			{
				"Skipped"      => ResultState.Skipped,
				"Ignored"      => ResultState.Ignored,
				"Inconclusive" => ResultState.Inconclusive,
				_              => ResultState.Warning,
			};

			ActiveIssueNewAttribute.Decide(Expecting("LinqToDB.LinqToDBException"), state, "provider not configured", isRemote: false)
				.ShouldBeNull();
		}

		[Test]
		public void Decide_RemoteWrapsTheException_StillMatches()
		{
			// The remote transport wraps the original exception, so the type name is no longer at the start.
			const string wrapped = "System.Exception : remote call failed ---> LinqToDB.LinqToDBException : nope";

			ActiveIssueNewAttribute.Decide(Expecting("LinqToDB.LinqToDBException"), ResultState.Error, wrapped, isRemote: true)!
				.Value.State.Status.ShouldBe(TestStatus.Inconclusive);

			// ... and the same message under a direct context is a mismatch, which is what makes the distinction real.
			ActiveIssueNewAttribute.Decide(Expecting("LinqToDB.LinqToDBException"), ResultState.Error, wrapped, isRemote: false)!
				.Value.State.Status.ShouldBe(TestStatus.Failed);
		}

		#endregion

		#region AppliesTo — SC-4

		[Test]
		public void AppliesTo_NoTargeting_GovernsEveryProvider()
		{
			var attr = new ActiveIssueNewAttribute();

			attr.AppliesTo("SQLite.MS", isLinqService: false).ShouldBeTrue();
			attr.AppliesTo("Oracle.23.Managed", isLinqService: true).ShouldBeTrue();
		}

		[Test]
		public void AppliesTo_NamedConfiguration_GovernsOnlyThatProvider()
		{
			var attr = new ActiveIssueNewAttribute { Configuration = "SQLite.MS" };

			attr.AppliesTo("SQLite.MS", isLinqService: false).ShouldBeTrue();
			attr.AppliesTo("SQLite.Classic", isLinqService: false).ShouldBeFalse();
		}

		[Test]
		public void AppliesTo_CommaSeparatedConfiguration_IsSplit()
		{
			var attr = new ActiveIssueNewAttribute { Configuration = TestProvName.AllFirebird };

			attr.AppliesTo(ProviderName.Firebird5, isLinqService: false).ShouldBeTrue();
			attr.AppliesTo("SQLite.MS", isLinqService: false).ShouldBeFalse();
		}

		[Test]
		public void AppliesTo_ConfigurationsArray_IsFlattened()
		{
			var attr = new ActiveIssueNewAttribute { Configurations = [TestProvName.AllFirebird, "SQLite.MS"] };

			attr.AppliesTo(ProviderName.Firebird5, isLinqService: false).ShouldBeTrue();
			attr.AppliesTo("SQLite.MS", isLinqService: false).ShouldBeTrue();
			attr.AppliesTo("Oracle.23.Managed", isLinqService: false).ShouldBeFalse();
		}

		[Test]
		public void AppliesTo_SkipForLinqService_DropsTheRemoteCase()
		{
			var attr = new ActiveIssueNewAttribute { SkipForLinqService = true };

			attr.AppliesTo("SQLite.MS", isLinqService: false).ShouldBeTrue();
			attr.AppliesTo("SQLite.MS", isLinqService: true).ShouldBeFalse();
		}

		[Test]
		public void AppliesTo_SkipForNonLinqService_DropsTheDirectCase()
		{
			var attr = new ActiveIssueNewAttribute { SkipForNonLinqService = true };

			attr.AppliesTo("SQLite.MS", isLinqService: false).ShouldBeFalse();
			attr.AppliesTo("SQLite.MS", isLinqService: true).ShouldBeTrue();
		}

		[Test]
		public void AppliesTo_NoProviderParameter_GovernsUnconditionally()
		{
			// Matches ActiveIssueAttribute: a test with no data-source parameter is gated whatever Configuration
			// says. Recorded because it means a Configuration on such a test is silently inert.
			new ActiveIssueNewAttribute { Configuration = "SQLite.MS" }
				.AppliesTo(null, isLinqService: false).ShouldBeTrue();
		}

		#endregion

		#region SelectGoverning — precedence and overlap

		[Test]
		public void SelectGoverning_TwoProvidersTwoIssues_EachGovernsItsOwn()
		{
			// The capability AllowMultiple=false denies today.
			var sqlite = new ActiveIssueNewAttribute(1) { Configuration = "SQLite.MS" };
			var oracle = new ActiveIssueNewAttribute(2) { Configuration = "Oracle.23.Managed" };

			ActiveIssueNewAttribute.SelectGoverning([sqlite, oracle], "SQLite.MS", false, out var ambiguous).ShouldBeSameAs(sqlite);
			ambiguous.ShouldBeFalse();

			ActiveIssueNewAttribute.SelectGoverning([sqlite, oracle], "Oracle.23.Managed", false, out ambiguous).ShouldBeSameAs(oracle);
			ambiguous.ShouldBeFalse();

			ActiveIssueNewAttribute.SelectGoverning([sqlite, oracle], "SqlServer.2022.MS", false, out _).ShouldBeNull();
		}

		[Test]
		public void SelectGoverning_TargetedBeatsBlanket()
		{
			var blanket = new ActiveIssueNewAttribute(1);
			var oracle  = new ActiveIssueNewAttribute(2) { Configuration = "Oracle.23.Managed" };

			ActiveIssueNewAttribute.SelectGoverning([blanket, oracle], "Oracle.23.Managed", false, out var ambiguous).ShouldBeSameAs(oracle);
			ambiguous.ShouldBeFalse();

			ActiveIssueNewAttribute.SelectGoverning([blanket, oracle], "SQLite.MS", false, out ambiguous).ShouldBeSameAs(blanket);
			ambiguous.ShouldBeFalse();
		}

		[Test]
		public void SelectGoverning_TwoEquallySpecificMatches_IsAmbiguous()
		{
			var first  = new ActiveIssueNewAttribute(1) { Configuration = "SQLite.MS" };
			var second = new ActiveIssueNewAttribute(2) { Configuration = "SQLite.MS" };

			ActiveIssueNewAttribute.SelectGoverning([first, second], "SQLite.MS", false, out var ambiguous).ShouldNotBeNull();
			ambiguous.ShouldBeTrue();
		}

		#endregion

		#region Sentinel — SC-7

		[Test]
		public void Sentinel_RoundTripsAwkwardCharacters()
		{
			const string name    = "Tests.Linq.FooTests.Bar(\"SQLite.MS\")";
			const string message = "a | b % c\r\nsecond line";

			var line = ActiveIssueSentinel.Format(name, "SQLite.MS", isRemote: true, passed: false, "LinqToDB.LinqToDBException", message);

			// One line, or the harvester's line-oriented scan splits one record into two.
			line.ShouldNotContain("\r");
			line.ShouldNotContain("\n");

			ActiveIssueSentinel.TryParse("  " + line, out var record).ShouldBeTrue();

			record!.FullName .ShouldBe(name);
			record.Provider  .ShouldBe("SQLite.MS");
			record.IsRemote  .ShouldBeTrue();
			record.Passed    .ShouldBeFalse();
			record.ErrorType .ShouldBe("LinqToDB.LinqToDBException");
			record.Message   .ShouldBe(message);
		}

		[Test]
		public void Sentinel_AbsentProviderAndTypeRoundTripAsNull()
		{
			var line = ActiveIssueSentinel.Format("Tests.Linq.FooTests.Bar", null, isRemote: false, passed: true, null, null);

			ActiveIssueSentinel.TryParse(line, out var record).ShouldBeTrue();

			record!.Provider.ShouldBeNull();
			record.ErrorType.ShouldBeNull();
			record.Passed   .ShouldBeTrue();
		}

		[Test]
		public void Sentinel_LongMessageIsCapped()
		{
			var line = ActiveIssueSentinel.Format("T.M", "SQLite.MS", false, false, null, new string('x', 5000));

			line.Length.ShouldBeLessThan(1000);
		}

		[Test]
		public void Sentinel_ExtractsExceptionTypeButNotAssertionProse()
		{
			ActiveIssueSentinel.ExtractErrorType(SqlError).ShouldBe("LinqToDB.LinqToDBException");
			ActiveIssueSentinel.ExtractErrorType(OtherError).ShouldBe("System.InvalidOperationException");

			// An assertion failure carries no type, which is how a wrong-results site is recognised during triage.
			ActiveIssueSentinel.ExtractErrorType(AssertError).ShouldBeNull();
			ActiveIssueSentinel.ExtractErrorType("Expected 3 : but was 7").ShouldBeNull();
			ActiveIssueSentinel.ExtractErrorType(null).ShouldBeNull();
		}

		[Test]
		public void Sentinel_RejectsForeignLines()
		{
			ActiveIssueSentinel.TryParse("failed SomeTest (12ms)", out var record).ShouldBeFalse();
			record.ShouldBeNull();
		}

		#endregion
	}
}
