using System;

namespace LinqToDB.Configuration
{
	/// <summary>
	/// Proxy object interface.
	/// </summary>
	/// <typeparam name="T">Proxyfied type.</typeparam>
	interface IProxy<T>
	{
		/// <summary>
		/// Proxified object.
		/// </summary>
		T UnderlyingObject { get; }
	}

	/// <summary>
	/// Proxy helpers.
	/// </summary>
	static class Proxy
	{
		/// <summary>
		/// Unwraps all proxies, applied to passed object and returns unproxyfied value.
		/// </summary>
		/// <typeparam name="T">Type of proxified object.</typeparam>
		/// <param name="obj">Object, that must be stripped of proxies.</param>
		/// <returns>Unproxified object.</returns>
		internal static T GetUnderlyingObject<T>(T obj)
		{
			while (obj is IProxy<T>)
				obj = ((IProxy<T>)obj).UnderlyingObject;

			return obj;
		}
	}
}
