using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Commands;

namespace Tests
{
	/// <summary>Operating systems an <see cref="ActiveIssueAttribute"/> governs.</summary>
	[Flags]
	public enum TestPlatform
	{
		/// <summary>Every platform.</summary>
		Any     = 0,
		/// <summary>Windows.</summary>
		Windows = 1,
		/// <summary>Linux.</summary>
		Linux   = 2,
		/// <summary>macOS.</summary>
		MacOS   = 4,
	}

	/// <summary>
	/// Marks a test as failing because of a known issue, and <b>asserts that it still does</b>. Unlike
	/// <see cref="ActiveIssueAttribute"/>, which hides the test from discovery, the test runs and its outcome is
	/// rewritten:
	/// <list type="bullet">
	/// <item>it failed the way the attribute declares — <see cref="ResultState.Inconclusive"/>, so the run stays green;</item>
	/// <item>it passed — <see cref="ResultState.Failure"/>, because the issue is fixed and the attribute must go;</item>
	/// <item>it failed some other way — <see cref="ResultState.Failure"/>, because that is a regression or a moved error message.</item>
	/// </list>
	/// <para>
	/// <see cref="AttributeUsageAttribute.AllowMultiple"/> is <see langword="true"/>: a test broken on two providers
	/// for two different issues carries two attributes, each with its own reference and expected failure.
	/// </para>
	/// </summary>
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
	public sealed class ActiveIssueAttribute : NUnitAttribute, IApplyToTest, IWrapSetUpTearDown
	{
		/// <summary>
		/// Prepended to every message this attribute writes. Also how a wrapper detects that an inner one already
		/// decided the case: attribute instances come from reflection, so they cannot be compared by reference.
		/// </summary>
		internal const string Marker = "[ActiveIssue] ";

		internal const string Category = "ActiveIssue";

		readonly string? _issue;
		string[]?        _configurations;

		/// <summary>Marks the test as failing because of a linq2db issue or pull request.</summary>
		/// <param name="issue">Number of the issue or pull request in the LINQ To DB repository.</param>
		public ActiveIssueAttribute(int issue)
		{
			// we generate link to issues and github redirects us if it is not in issue
			_issue = $"https://github.com/linq2db/linq2db/issues/{issue}";
		}

		/// <summary>Marks the test as failing because of a known issue.</summary>
		/// <param name="issue">Link to a page with additional issue details.</param>
		public ActiveIssueAttribute(string issue)
		{
			_issue = issue;
		}

		/// <summary>Marks the test as failing because of a known issue with no tracking link.</summary>
		public ActiveIssueAttribute()
		{
		}

		/// <summary>Gets or sets additional details for an issue.</summary>
		public string? Details { get; set; }

		/// <summary>
		/// Gets or sets configuration names this attribute applies to.
		/// Applied only to tests marked with attributes based on <see cref="DataSourcesBaseAttribute"/>.
		/// </summary>
		[MaybeNull]
		public string[] Configurations
		{
			get => _configurations;
			set => _configurations = value.SelectMany(p => p.Split(',').Select(_ => _.Trim())).ToArray();
		}

		/// <summary>
		/// Gets or sets comma-separated configuration names this attribute applies to.
		/// Applied only to tests marked with attributes based on <see cref="DataSourcesBaseAttribute"/>.
		/// </summary>
		public string Configuration
		{
			get => _configurations != null ? string.JoinStrings(',', _configurations) : string.Empty;
			set => _configurations = value.Split(',').Select(_ => _.Trim()).ToArray();
		}

		/// <summary>
		/// Gets or sets the operating systems this attribute governs. Default <see cref="TestPlatform.Any"/> governs
		/// everywhere.
		/// <para>
		/// Needed because a failure can be platform-dependent rather than provider-dependent — a provider's native
		/// binary differs per RID, so the same provider answers differently on Linux and on Windows. Without this,
		/// such a gate reddens every run on the platform where the test passes.
		/// </para>
		/// </summary>
		public TestPlatform Platforms { get; set; } = TestPlatform.Any;

		/// <summary>Gets or sets a flag if this attribute should be skipped for a LinqOverWcf test. Default value: <see langword="false"/>.</summary>
		public bool SkipForLinqService { get; set; }

		/// <summary>Gets or sets a flag if this attribute should be skipped for a non-LinqOverWcf test. Default value: <see langword="false"/>.</summary>
		public bool SkipForNonLinqService { get; set; }

		/// <summary>
		/// Gets or sets the exception type the test is expected to fail with. Mutually exclusive with
		/// <see cref="ErrorTypeName"/>. Leave both unset when the expected failure is a wrong-results assertion,
		/// whose message carries no type name.
		/// </summary>
		public Type? ErrorType { get; set; }

		/// <summary>
		/// Gets or sets the full name of the exception type the test is expected to fail with, for provider exception
		/// types that are not referenced on every target framework. Mutually exclusive with <see cref="ErrorType"/>.
		/// </summary>
		public string? ErrorTypeName { get; set; }

		/// <summary>
		/// Gets or sets a fragment of the expected failure message. Matched with
		/// <see cref="ThrowsWhenAttribute.MessageMatches"/>, so a value carrying <c>{0}</c>-style placeholders — an
		/// <c>ErrorHelper</c> format string, named rather than copied — is matched as a pattern.
		/// </summary>
		public string? ErrorMessage { get; set; }

		string ExpectedTypeName => ErrorType?.FullName ?? ErrorTypeName ?? string.Empty;

		bool HasExplicitTargeting => (_configurations != null && _configurations.Length > 0) || SkipForLinqService || SkipForNonLinqService || Platforms != TestPlatform.Any;

		static TestPlatform CurrentPlatform =>
			OperatingSystem.IsWindows() ? TestPlatform.Windows :
			OperatingSystem.IsLinux()   ? TestPlatform.Linux   :
			OperatingSystem.IsMacOS()   ? TestPlatform.MacOS   :
			TestPlatform.Any;

		string Reference
		{
			get
			{
				var reference = string.IsNullOrWhiteSpace(_issue) ? "active issue" : _issue!;

				return string.IsNullOrWhiteSpace(Details) ? reference : $"{reference}. {Details}";
			}
		}

		string Expectation
		{
			get
			{
				var type    = ExpectedTypeName;
				var hasType = !string.IsNullOrEmpty(type);
				var hasText = !string.IsNullOrEmpty(ErrorMessage);

				if (hasType && hasText) return $"<{type}> with message matching '{ErrorMessage}'";
				if (hasType)            return $"<{type}>";
				if (hasText)            return $"a failure with message matching '{ErrorMessage}'";

				return "any failure";
			}
		}

		/// <summary>
		/// Whether this instance governs a test case running against <paramref name="provider"/>. Reproduces
		/// <see cref="ActiveIssueAttribute"/>'s targeting exactly, including that a test with no data-source
		/// parameter (<paramref name="provider"/> is <see langword="null"/>) is governed unconditionally.
		/// </summary>
		public bool AppliesTo(string? provider, bool isLinqService)
		{
			// Before the provider == null shortcut: the platform is ambient, so it bounds a gate on a test with no
			// data-source parameter too.
			if (Platforms != TestPlatform.Any && (Platforms & CurrentPlatform) == 0)
				return false;

			if (provider == null)
				return true;

			if (_configurations != null && _configurations.Length > 0 && !_configurations.Contains(provider))
				return false;

			if (isLinqService && SkipForLinqService)
				return false;

			if (!isLinqService && SkipForNonLinqService)
				return false;

			return true;
		}

		/// <summary>
		/// Whether <paramref name="message"/> is the failure this attribute declares. A remote case is matched with
		/// <c>Contains</c> rather than <c>StartsWith</c> because the remote transport wraps the original exception —
		/// the same distinction <see cref="ThrowsWhenAttribute"/> draws.
		/// </summary>
		public bool Matches(string? message, bool isRemote)
		{
			if (message == null)
				return false;

			var type = ExpectedTypeName;

			if (type.Length > 0)
			{
				var found = isRemote
					? message.Contains(type)
					: message.StartsWith(type, StringComparison.Ordinal);

				if (!found)
					return false;
			}

			if (ErrorMessage is { Length: > 0 } expected)
				return ThrowsWhenAttribute.MessageMatches(message, expected);

			return true;
		}

		/// <summary>
		/// The whole outcome policy, as a pure function of the inner result — which is what makes it testable without
		/// standing up a nested NUnit run. Returns <see langword="null"/> when the result must be left alone.
		/// </summary>
		public static (ResultState State, string Message)? Decide(ActiveIssueAttribute attribute, ResultState innerState, string? innerMessage, bool isRemote, bool throwsGoverns = false)
		{
			// A Throws* attribute on the same method makes a different, more specific claim: this provider is
			// *expected* to reject this query, and it has already rewritten the result to say so. Both families
			// rewrite outcomes and neither can recognise the other's message, so whichever nests innermost would
			// otherwise decide - on a test the Throws* wrapper turned green, this one would report "passed but is
			// marked". Deferring is order-independent, which is what makes NUnit's unspecified nesting harmless.
			if (throwsGoverns)
				return null;

			return innerState.Status switch
			{
				TestStatus.Passed => (ResultState.Failure,
					$"{Marker}Test passed but is marked with [ActiveIssue] ({attribute.Reference}). "
					+ "If the issue is fixed, remove the attribute; if it only passes for some providers, narrow Configuration."),

				TestStatus.Failed => attribute.Matches(innerMessage, isRemote)
					? (ResultState.Inconclusive, $"{Marker}Known issue ({attribute.Reference}), still failing as expected:\n\n{innerMessage}")
					: (ResultState.Failure,
						$"{Marker}Expected {attribute.Expectation} for {attribute.Reference}, but found:\n\n{innerMessage}"),

				// Skipped / Ignored / Inconclusive / Warning: the test opted out of running, so it never produced
				// evidence about the issue either way. Leaving it alone is the only honest answer.
				_ => null
			};
		}

		/// <inheritdoc/>
		public void ApplyToTest(Test test)
		{
			// AllowMultiple, so guard against adding the same category once per attribute.
			if (!test.Properties[PropertyNames.Category].Contains(Category))
				test.Properties.Add(PropertyNames.Category, Category);
		}

		/// <inheritdoc/>
		public TestCommand Wrap(TestCommand command)
		{
			return new ActiveIssueCommand(command);
		}

		static ActiveIssueAttribute[] GetAttributes(ITest test)
		{
			return test.Method?.GetCustomAttributes<ActiveIssueAttribute>(true) ?? [];
		}

		/// <summary>
		/// Whether a <see cref="ThrowsWhenAttribute"/> (or a subclass) on the same method owns this case. See the
		/// deference rule in <see cref="Decide"/>.
		/// </summary>
		static bool ThrowsFamilyGoverns(ITest test)
		{
			foreach (var throws in test.Method?.GetCustomAttributes<ThrowsWhenAttribute>(true) ?? [])
				if (throws.GovernsCurrentCase(test))
					return true;

			return false;
		}

		/// <summary>
		/// Picks the instance that governs this case. An instance that names a provider or transport beats a blanket
		/// one, so "fails everywhere with A, but on Oracle with B" is expressible. Two equally specific instances
		/// both matching is an authoring error, reported rather than silently resolved.
		/// </summary>
		public static ActiveIssueAttribute? SelectGoverning(ActiveIssueAttribute[] attributes, string? provider, bool isLinqService, out bool ambiguous)
		{
			ambiguous = false;

			var applicable = attributes.Where(a => a.AppliesTo(provider, isLinqService)).ToArray();

			if (applicable.Length == 0)
				return null;

			var targeted = applicable.Where(a => a.HasExplicitTargeting).ToArray();
			var pool     = targeted.Length > 0 ? targeted : applicable;

			ambiguous = pool.Length > 1;

			return pool[0];
		}

		sealed class ActiveIssueCommand : DelegatingTestCommand
		{
			public ActiveIssueCommand(TestCommand innerCommand)
				: base(innerCommand)
			{
			}

			public override TestResult Execute(TestExecutionContext context)
			{
				// The TestProgressReporter heartbeat action runs *inside* this IWrapSetUpTearDown wrapper, so the
				// outcome it samples is the pre-rewrite one. Hold the unit back and hand the tracker our final
				// verdict instead. Deferrals nest, so with several attributes only the outermost books.
				var fullName = context.CurrentTest.FullName;

				TestProgressTracker.BeginDeferred(fullName);

				TestResult? testResult = null;

				try
				{
					testResult = ExecuteInner(context);
					return testResult;
				}
				finally
				{
					TestProgressTracker.CommitDeferred(fullName, testResult);
				}
			}

			TestResult ExecuteInner(TestExecutionContext context)
			{
				var testResult = innerCommand.Execute(context);

				// Attribute instances come from reflection, so a wrapper cannot recognise itself by reference. The
				// marker is how an outer wrapper sees that an inner one already decided this case.
				if (testResult.Message != null && testResult.Message.StartsWith(Marker, StringComparison.Ordinal))
					return testResult;

				var test                      = context.CurrentTest;
				var (provider, isLinqService) = NUnitUtils.GetContext(test);
				var governing                 = SelectGoverning(GetAttributes(test), provider, isLinqService, out var ambiguous);

				if (governing == null)
					return testResult;

				if (ambiguous)
				{
					testResult.SetResult(
						ResultState.Failure,
						$"{Marker}More than one equally specific [ActiveIssue] applies to '{provider ?? "(no provider)"}'. "
						+ "Narrow their Configuration so exactly one governs this case.");

					return testResult;
				}

				// Sweep mode: report what the test actually did, as one parsable line, instead of deciding. The
				// whole point of a triage sweep is to see the failure the gate is hiding, so this deliberately
				// reddens every governed case.
				if (TestEnvironment.ActiveIssueSweep)
				{
					var passed = testResult.ResultState.Status == TestStatus.Passed;

					testResult.SetResult(
						ResultState.Failure,
						ActiveIssueSentinel.Format(
							test.FullName,
							provider,
							isLinqService,
							passed,
							ActiveIssueSentinel.ExtractErrorType(testResult.Message),
							testResult.Message));

					return testResult;
				}

				var decision = Decide(governing, testResult.ResultState, testResult.Message, isLinqService, ThrowsFamilyGoverns(test));

				if (decision != null)
					testResult.SetResult(decision.Value.State, decision.Value.Message);

				return testResult;
			}
		}
	}
}
