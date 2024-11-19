using System;

using Humanizer;

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
			ExpectedException = expectedException;
		}

		public string  ParameterName     { get; }
		public object  ExpectedValue     { get; }
		public Type    ExpectedException { get; }
		public string? ErrorMessage      { get; set; }

		public void ApplyToTest(Test test)
		{
			// Add a property to the test to indicate that it expects an exception
			test.Properties.Add("ThrowsWhen", this);
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
					else if (expectsFirst && !testResult.Message.StartsWith(_attribute.ExpectedException.FullName!))
					{
						testResult.SetResult(ResultState.Failure, $"Expected a <{_attribute.ExpectedException}> to be thrown, but found: '{testResult.Message}'");
					}
					else if (!expectsFirst && !testResult.Message.Contains(_attribute.ExpectedException.FullName!))
					{
						testResult.SetResult(ResultState.Failure, $"Expected a <{_attribute.ExpectedException}> to be thrown, but found: '{testResult.Message}'");
					}
					else
					{
						if (!string.IsNullOrEmpty(_attribute.ErrorMessage) && !testResult.Message.Contains(_attribute.ErrorMessage))
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
