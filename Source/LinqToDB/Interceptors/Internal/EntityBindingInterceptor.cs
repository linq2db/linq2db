using System.Collections.Generic;

namespace LinqToDB.Interceptors.Internal
{
	using Expressions;
	using Reflection;

	public abstract class EntityBindingInterceptor : IEntityBindingInterceptor
	{
		public virtual SqlGenericConstructorExpression ConvertConstructorExpression(SqlGenericConstructorExpression expression) => expression;
	}
}
