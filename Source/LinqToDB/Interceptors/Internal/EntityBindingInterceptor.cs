using LinqToDB.Internal.Expressions;

namespace LinqToDB.Interceptors.Internal
{
	public abstract class EntityBindingInterceptor : IEntityBindingInterceptor
	{
		public virtual SqlGenericConstructorExpression ConvertConstructorExpression(SqlGenericConstructorExpression expression) => expression;
	}
}
