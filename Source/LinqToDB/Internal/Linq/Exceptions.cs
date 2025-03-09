using System;

namespace LinqToDB.Internal.Linq
{
	internal static class Exceptions
	{
		internal static object DefaultInheritanceMappingException(object value, Type type)
		{
			throw new LinqToDBException($"Inheritance mapping is not defined for discriminator value '{value}' in the '{type}' hierarchy.");
		}
	}
}
