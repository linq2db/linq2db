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
			throw new LinqException("Inheritance mapping is not defined for discriminator value '{0}' in the '{1}' hierarchy.", value, type);
		}
	}
}
