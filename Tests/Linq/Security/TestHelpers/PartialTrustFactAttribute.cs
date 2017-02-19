using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LinqToDB.Extensions;
using Xunit;
using Xunit.Sdk;

namespace Tests.Security.TestHelpers
{
	class PartialTrustFactAttribute : FactAttribute
	{
		protected override IEnumerable<ITestCommand> EnumerateTestCommands(IMethodInfo method)
		{
			var fixtures = GetFixtures(method.Class);

			return base.EnumerateTestCommands(method).Select(tc => new PartialTrustCommand(tc, fixtures));
		}

		static IDictionary<MethodInfo, object> GetFixtures(ITypeInfo typeUnderTest)
		{
			var fixtures = new Dictionary<MethodInfo,object>();

			foreach (var intf in typeUnderTest.Type.GetInterfaces())
			{
				if (intf.IsGenericType)
				{
					var genericDefinition = intf.GetGenericTypeDefinition();

					if (genericDefinition == typeof(IUseFixture<>))
					{
						var dataType    = intf.GetGenericArguments()[0];
						var fixtureData = Activator.CreateInstance(dataType);
						var method      = GetDeclaredMethod(intf, "SetFixture", new[] { dataType });

						fixtures[method] = fixtureData;
					}
				}
			}

			return fixtures;
		}

		public static IEnumerable<MethodInfo> GetDeclaredMethods(Type type, string name)
		{
			const BindingFlags bindingFlags =
				BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
			return type.GetMember(name, MemberTypes.Method, bindingFlags).OfType<MethodInfo>();
		}

		public static MethodInfo GetDeclaredMethod(Type type, string name, params Type[] parameterTypes)
		{
			return GetDeclaredMethods(type, name)
				.SingleOrDefault(m => m.GetParameters().Select(p => p.ParameterType).SequenceEqual(parameterTypes));
		}
	}
}
