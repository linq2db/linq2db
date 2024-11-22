using System;

namespace LinqToDB.Linq
{
	internal static class Exceptions
	{
		internal static object DefaultInheritanceMappingException(object value, Type type)
		{
			throw new LinqException("Inheritance mapping is not defined for discriminator value '{0}' in the '{1}' hierarchy.", value, type);
		}
	}
}
