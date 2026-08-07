using System;
using System.Text.RegularExpressions;

using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Commands;

namespace Tests
{
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
	public class ThrowsWhenAttribute : NUnitAttribute, IApplyToTest, IWrapSetUpTearDown
	{
		public ThrowsWhenAttribute(string parameterName, Type expectedException, object expectedValue)
		{
			ParameterName     = parameterName;
			ExpectedValue     = expectedValue;
			ExpectedException = expectedException.FullName!;
		}

		public ThrowsWhenAttribute(string parameterName, string expectedException, object expectedValue)
		{
			ParameterName = parameterName;
			ExpectedValue = expectedValue;
			ExpectedException = expectedException;
		}

		public string  ParameterName     { get; }
		public object  ExpectedValue     { get; }
		public string  ExpectedException { get; }
		public string? ErrorMessage      { get; set; }

		public virtual void ApplyToTest(Test test)
		{
			// Add a property to the test to indicate that it expects an exception
			test.Properties.Add("ThrowsWhen", this);
		}

		/// <summary>
		/// Whether the thrown message matches the expected one. A message carrying <c>{0}</c>-style placeholders is
		/// matched as a pattern rather than literally.
		/// </summary>
		/// <remarks>
		/// Lets a test name an <c>ErrorHelper</c> constant that happens to be a format string, instead of copying
		/// its wording with the arguments filled in. Copying is what rots: the constant changes, the test keeps
		/// passing against the stale text it embedded. Each placeholder matches any run of characters, so the
		/// assertion stays on the wording and not on the values substituted into it.
		/// </remarks>
		internal static bool MessageMatches(string actual, string expected)
		{
			if (!Regex.IsMatch(expected, @"\{\d+\}"))
				return actual.Contains(expected);

			var pattern = Regex.Replace(Regex.Escape(expected), @"\\\{\d+\}", ".*?");

			// Singleline so a placeholder can also swallow a line break - linq2db composes multi-line messages.
			return Regex.IsMatch(actual, pattern, RegexOptions.Singleline);
		}

		public TestCommand Wrap(TestCommand command)
		{
			// Wrap the test command with a custom command that checks for the exception
			return new ThrowsWhenCommand(command, this);
		}

		public virtual bool ExpectsFirst(object parameterValue)
		{
			return parameterValue is string strValue && !strValue.EndsWith(TestBase.LinqServiceSuffix);
		}

		public virtual bool ExpectsException(object parameterValue)
		{
			if (parameterValue is string strValue && ExpectedValue is string expectedStrValue)
			{
				if (strValue.Contains(expectedStrValue))
				{
					return true;
				}
			}
			else if (parameterValue.Equals(ExpectedValue))
			{
				return true;
			}

			return false;
		}

		public class ThrowsWhenCommand : DelegatingTestCommand
		{
			readonly ThrowsWhenAttribute _attribute;

			public ThrowsWhenCommand(TestCommand innerCommand, ThrowsWhenAttribute attribute)
				: base(innerCommand)
			{
				_attribute = attribute;
			}

			static int GetParameterIndex(IParameterInfo[] parameters, string parameterName)
			{
				for (var i = 0; i < parameters.Length; i++)
				{
					if (parameters[i].ParameterInfo.Name == parameterName)
					{
						return i;
					}
				}

				return -1;
			}

			public override TestResult Execute(TestExecutionContext context)
			{
				// The TestProgressReporter heartbeat action runs *inside* this IWrapSetUpTearDown wrapper, so the
				// outcome it samples is the pre-rewrite one. Hold the unit back for the duration of this wrapper and
				// hand the tracker our final verdict instead, so no provisional result is ever counted or published.
				// Deferrals nest: with several ThrowsWhen attributes on one test only the outermost wrapper — the one
				// that sees the final result — books the unit.
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
				var expectsException = false;
				var expectsFirst     = true;

				if (context.CurrentTest.Method != null)
				{
					var parameters = context.CurrentTest.Method.GetParameters();
					var idx        = GetParameterIndex(parameters, _attribute.ParameterName);

					Assert.That(idx, Is.GreaterThanOrEqualTo(0), $"Invalid parameter name '{_attribute.ParameterName}' for '{nameof(ThrowsWhenAttribute)}'.");

					var parameterValue = context.CurrentTest.Arguments[idx];
					if (parameterValue != null)
					{
						expectsException = _attribute.ExpectsException(parameterValue);

						if (expectsException)
						{
							expectsFirst = _attribute.ExpectsFirst(parameterValue);
						}
					}
				}

				// If no, execute the test normally
				var testResult = innerCommand.Execute(context);

				// Check if the parameter value matches the expected value
				if (expectsException)
				{
					if (testResult.Message == null)
					{
						testResult.SetResult(ResultState.Failure, $"Expected a <{_attribute.ExpectedException}> to be thrown, but no exception was thrown");
					}
					else if (expectsFirst && !testResult.Message.StartsWith(_attribute.ExpectedException))
					{
						testResult.SetResult(ResultState.Failure, $"Expected a <{_attribute.ExpectedException}> to be thrown, but found: '{testResult.Message}'");
					}
					else if (!expectsFirst && !testResult.Message.Contains(_attribute.ExpectedException))
					{
						testResult.SetResult(ResultState.Failure, $"Expected a <{_attribute.ExpectedException}> to be thrown, but found: '{testResult.Message}'");
					}
					else
					{
						if (!string.IsNullOrEmpty(_attribute.ErrorMessage) && !MessageMatches(testResult.Message, _attribute.ErrorMessage))
						{
							testResult.SetResult(ResultState.Failure, $"Expected a <{_attribute.ExpectedException}> to be thrown with message containing '{_attribute.ErrorMessage}', but found: '{testResult.Message}'");
						}
						else
							testResult.SetResult(ResultState.Success, "Required exception was thrown:\n\n" + testResult.Message);
					}
				}

				return testResult;
			}
		}
	}
}
