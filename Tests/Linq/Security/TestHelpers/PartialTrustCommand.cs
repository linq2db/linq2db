using System;
using System.Collections.Generic;
using System.Reflection;
using System.Xml;

using LinqToDB.Common;

using Xunit;
using Xunit.Sdk;

namespace Tests.Security.TestHelpers
{
	class PartialTrustCommand : ITestCommand
	{
		private readonly ITestCommand _command;
		private readonly IDictionary<MethodInfo, object> _fixtures;

		public PartialTrustCommand(ITestCommand command, IDictionary<MethodInfo, object> fixtures = null)
		{
			_command  = command;
			_fixtures = fixtures;
		}

		public string DisplayName
		{
			get { return _command.DisplayName; }
		}

		public bool ShouldCreateInstance
		{
			get { return _command.ShouldCreateInstance; }
		}

		public int Timeout
		{
			get { return _command.Timeout; }
		}

		public MethodResult Execute(object testClass)
		{
			object sandboxedClass = null;

			try
			{
				if (testClass != null)
				{
					var testClassType = testClass.GetType();

					if (!typeof(MarshalByRefObject).IsAssignableFrom(testClassType))
					{
						throw new InvalidOperationException(
							$"In order to use the partial trust attributes here, '{testClassType.FullName}' must derive from MarshalByRefObject.");
					}

					sandboxedClass = PartialTrustSandbox.Default.CreateInstance(testClassType);
					ApplyFixtures(sandboxedClass);
				}
				else
				{
					Assert.IsType<SkipCommand>(_command);
				}

				return _command.Execute(sandboxedClass);
			}
			finally
			{
				var asDisposable = sandboxedClass as IDisposable;
				if (asDisposable != null)
					asDisposable.Dispose();
			}
		}

		public XmlNode ToStartXml()
		{
			return _command.ToStartXml();
		}

		void ApplyFixtures(object testClass)
		{
			if (_fixtures != null)
				foreach (var fixture in _fixtures)
					fixture.Key.Invoke(testClass, new[] {fixture.Value});
		}
	}
}
