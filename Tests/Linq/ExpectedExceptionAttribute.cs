using System;
using LinqToDB.Common;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Commands;

namespace Tests
{
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
	public class ExpectedExceptionAttribute : NUnitAttribute, IWrapTestMethod
	{
		readonly Type _expectedExceptionType;

		public ExpectedExceptionAttribute(Type type)
		{
			_expectedExceptionType = type;
		}

		public TestCommand Wrap(TestCommand command)
		{
			return new ExpectedExceptionCommand(command, _expectedExceptionType, ExpectedMessage);
		}

		public string ExpectedMessage;

		class ExpectedExceptionCommand : DelegatingTestCommand
		{
			readonly Type   _expectedType;
			readonly string _expectedMessage;

			public ExpectedExceptionCommand(TestCommand innerCommand, Type expectedType, string expectedMessage)
				: base(innerCommand)
			{
				_expectedType    = expectedType;
				_expectedMessage = expectedMessage;
			}

			public override TestResult Execute(TestExecutionContext context)
			{
				Type      caughtType = null;
				Exception exception = null;

				try
				{
					innerCommand.Execute(context);
				}
				catch (Exception ex)
				{
					exception = ex;

					if (exception is NUnitException)
						exception = ex.InnerException;

					caughtType = exception.GetType();
				}

				if (caughtType == _expectedType)
				{
					if (_expectedMessage == null || _expectedMessage == exception.Message)
						context.CurrentResult.SetResult(ResultState.Success);
					else
						context.CurrentResult.SetResult(ResultState.Failure,
							"Expected {0} but got {1}".Args(_expectedMessage, exception.Message));

				}
				else if (caughtType != null)
				{
					context.CurrentResult.SetResult(ResultState.Failure,
						"Expected {0} but got {1}".Args(_expectedType.Name, caughtType.Name));
				}
				else
				{
					context.CurrentResult.SetResult(ResultState.Failure,
						"Expected {0} but no exception was thrown".Args(_expectedType.Name));
				}

				return context.CurrentResult;
			}
		}
	}}
