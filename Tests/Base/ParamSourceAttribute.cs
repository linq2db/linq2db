using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace Tests
{
	[AttributeUsage(AttributeTargets.Parameter)]
	public class ParamSourceAttribute : NUnitAttribute, IParameterDataSource
	{
		public string MethodName { get; }

		public ParamSourceAttribute(string methodName)
		{
			MethodName = methodName;
		}

		public IEnumerable GetData(IParameterInfo parameter)
		{
			var method = parameter.Method.MethodInfo.DeclaringType?.GetMethod(MethodName,
				BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

			if (method == null)
				throw new InvalidOperationException($"Static method `{MethodName}` not found.");

			var result = method.Invoke(null, new object[0]) as IEnumerable;
			
			if (result == null)
				throw new InvalidOperationException("Static method `{MethodName}` not returned values.");
			
			return result;
		}
	}
}
