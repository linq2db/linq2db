using System;

namespace LinqToDB.Reflection
{
	public interface IObjectFactory
	{
		object CreateInstance(TypeAccessor typeAccessor);
	}
}
