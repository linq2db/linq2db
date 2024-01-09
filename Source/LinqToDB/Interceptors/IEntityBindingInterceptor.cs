using System;
using System.Collections.Generic;
using System.Reflection;

using LinqToDB.Reflection;

namespace LinqToDB.Interceptors.Internal
{
	/// <summary>
	/// Internal API.
	/// </summary>
	public interface IEntityBindingInterceptor : IInterceptor
	{
		/// <summary>
		/// Method returns map between type member and member initialization parameter index in type constructor.
		/// Should return <c>null</c> if type is not supported by interceptor.
		/// </summary>
		IReadOnlyDictionary<int, MemberAccessor>? TryMapMembersToConstructor(TypeAccessor typeAccessor);
	}
}
