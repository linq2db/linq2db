namespace LinqToDB.Linq
{
	using System;

	internal static class Exceptions
	{
		internal static object DefaultInheritanceMappingException(object value, Type type)
		{
			return ThrowHelper.ThrowLinqException<object>($"Inheritance mapping is not defined for discriminator value '{value}' in the '{type}' hierarchy.");
		}
	}
}
