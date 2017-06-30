using System;

namespace LinqToDB.Configuration
{
	interface IProxy<T>
	{
		T UnderlyingObject { get; }
	}

	static class Proxy
	{
		internal static T GetUnderlyingObject<T>(T obj)
		{
			while (obj is IProxy<T>)
				obj = ((IProxy<T>)obj).UnderlyingObject;

			return obj;
		}
	}
}
