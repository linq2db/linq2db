using System;
using System.Linq;

using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Commands;

namespace Tests.Scaffold.Harness
{
	/// <summary>
	/// Marks a test as failing because of a known issue and <b>asserts that it still does</b>, with the outcome policy of
	/// <c>Tests.ActiveIssueAttribute</c> for projects without the provider test infrastructure:
	/// <list type="bullet">
	/// <item>it failed the way the attribute declares — <see cref="ResultState.Inconclusive"/>;</item>
	/// <item>it passed — <see cref="ResultState.Failure"/>, because the issue is fixed and the attribute must go;</item>
	/// <item>it failed some other way — <see cref="ResultState.Failure"/>.</item>
	/// </list>
	/// </summary>
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
	public sealed class ActiveIssueAttribute(int issue) : NUnitAttribute, IApplyToTest, IWrapSetUpTearDown
	{
		const string Marker   = "[ActiveIssue] ";
		const string Category = "ActiveIssue";

		/// <summary>Gets or sets additional details for an issue.</summary>
		public string? Details { get; set; }

		/// <summary>Gets or sets the exception type the test is expected to fail with.</summary>
		public Type? ErrorType { get; set; }

		/// <summary>Gets or sets a fragment of the expected failure message.</summary>
		public string? ErrorMessage { get; set; }

		/// <summary>
		/// Gets or sets comma-separated test case argument values this attribute applies to (e.g. <c>FSharp,C1</c>). A case
		/// is governed when every listed value equals string representation of one of its arguments. Empty governs all
		/// cases. Use multiple attributes for alternatives.
		/// </summary>
		public string? Arguments { get; set; }

		string Reference => $"https://github.com/linq2db/linq2db/issues/{issue}{(string.IsNullOrWhiteSpace(Details) ? null : ". " + Details)}";

		bool AppliesTo(ITest test)
		{
			if (string.IsNullOrWhiteSpace(Arguments))
				return true;

			var arguments = test.Arguments.Select(static a => a?.ToString()).ToArray();

			return Arguments.Split(',').Select(static a => a.Trim()).All(v => arguments.Contains(v, StringComparer.Ordinal));
		}

		bool Matches(string? message)
		{
			if (message == null)
				return false;

			if (ErrorType != null && !message.StartsWith(ErrorType.FullName!, StringComparison.Ordinal))
				return false;

			return ErrorMessage == null || message.Contains(ErrorMessage, StringComparison.Ordinal);
		}

		/// <inheritdoc/>
		public void ApplyToTest(Test test)
		{
			if (!test.Properties[PropertyNames.Category].Contains(Category))
				test.Properties.Add(PropertyNames.Category, Category);
		}

		/// <inheritdoc/>
		public TestCommand Wrap(TestCommand command) => new ActiveIssueCommand(command, this);

		sealed class ActiveIssueCommand(TestCommand innerCommand, ActiveIssueAttribute attribute) : DelegatingTestCommand(innerCommand)
		{
			public override TestResult Execute(TestExecutionContext context)
			{
				TestResult result;

				// test exception is not recorded to result yet at this level
				try
				{
					result = innerCommand.Execute(context);
				}
				catch (Exception ex)
				{
					result = context.CurrentResult;
					result.RecordException(ex);
				}

				if (result.Message?.StartsWith(Marker, StringComparison.Ordinal) == true || !attribute.AppliesTo(context.CurrentTest))
					return result;

				switch (result.ResultState.Status)
				{
					case TestStatus.Passed:
						result.SetResult(ResultState.Failure, $"{Marker}Test passed but is marked with [ActiveIssue] ({attribute.Reference}). If the issue is fixed, remove the attribute.");
						break;

					case TestStatus.Failed:
						if (attribute.Matches(result.Message))
							result.SetResult(ResultState.Inconclusive, $"{Marker}Known issue ({attribute.Reference}), still failing as expected:\n\n{result.Message}");
						else
							result.SetResult(ResultState.Failure, $"{Marker}Expected failure for {attribute.Reference}, but found:\n\n{result.Message}");
						break;
				}

				return result;
			}
		}
	}
}
