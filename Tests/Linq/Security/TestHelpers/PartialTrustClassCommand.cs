using System;
using System.Collections.Generic;
using System.Linq;

using Xunit.Sdk;

namespace Tests.Security.TestHelpers
{
	class PartialTrustClassCommand : ITestClassCommand
	{
		readonly ITestClassCommand _classCommand = new TestClassCommand();

		public int ChooseNextTest(ICollection<IMethodInfo> testsLeftToRun)
		{
			return _classCommand.ChooseNextTest(testsLeftToRun);
		}

		public Exception ClassFinish()
		{
			return _classCommand.ClassFinish();
		}

		public Exception ClassStart()
		{
			return _classCommand.ClassStart();
		}

		public IEnumerable<ITestCommand> EnumerateTestCommands(IMethodInfo testMethod)
		{
			return _classCommand.EnumerateTestCommands(testMethod)
				.Select(cmd => cmd is PartialTrustCommand ? cmd : new PartialTrustCommand(cmd));
		}

		public IEnumerable<IMethodInfo> EnumerateTestMethods()
		{
			return _classCommand.EnumerateTestMethods();
		}

		public bool IsTestMethod(IMethodInfo testMethod)
		{
			return _classCommand.IsTestMethod(testMethod);
		}

		public object ObjectUnderTest
		{
			get { return _classCommand.ObjectUnderTest; }
		}

		public ITypeInfo TypeUnderTest
		{
			get { return _classCommand.TypeUnderTest;  }
			set { _classCommand.TypeUnderTest = value; }
		}
	}
}
