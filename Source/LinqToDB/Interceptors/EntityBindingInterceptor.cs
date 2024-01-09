using System;
using System.Collections.Generic;
using System.Reflection;

using LinqToDB.Reflection;

namespace LinqToDB.Interceptors.Internal
{
	public abstract class EntityBindingInterceptor : IEntityBindingInterceptor
	{
		public virtual IReadOnlyDictionary<int, MemberAccessor>? TryMapMembersToConstructor(TypeAccessor typeAccessor) => null;
	}
}
