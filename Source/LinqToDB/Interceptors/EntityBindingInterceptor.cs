using System.Collections.Generic;

namespace LinqToDB.Interceptors.Internal
{
	using LinqToDB.Expressions;
	using LinqToDB.Reflection;

	public abstract class EntityBindingInterceptor : IEntityBindingInterceptor
	{
		public virtual IReadOnlyDictionary<int, MemberAccessor>? TryMapMembersToConstructor(TypeAccessor typeAccessor) => null;

		public virtual SqlGenericConstructorExpression ConvertConstructorExpression(SqlGenericConstructorExpression expression) => expression;
	}
}
