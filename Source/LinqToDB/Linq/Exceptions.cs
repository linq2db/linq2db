using System.Reflection;

namespace LinqToDB.Linq
{
	using System;
	using System.Linq;
	using Common;
	using LinqToDB.Linq.Builder;
	using LinqToDB.Mapping;

	internal static class Exceptions
	{
		internal static object DefaultInheritanceMappingException(object value, Type type)
		{
			return ThrowHelper.ThrowLinqException<object>($"Inheritance mapping is not defined for discriminator value '{value}' in the '{type}' hierarchy.");
		}
	}
}
