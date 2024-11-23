using System;

namespace LinqToDB.Linq
{
	internal static class Exceptions
	{
		internal static object DefaultInheritanceMappingException(object value, Type type)
		{
			throw new LinqToDBException($"Inheritance mapping is not defined for discriminator value '{value}' in the '{type}' hierarchy.");
		}
	}
}
