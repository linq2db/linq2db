using LinqToDB.Internal.Expressions;

namespace LinqToDB.Internal.Interceptors
{
	public abstract class EntityBindingInterceptor : IEntityBindingInterceptor
	{
		public virtual SqlGenericConstructorExpression ConvertConstructorExpression(SqlGenericConstructorExpression expression) => expression;
	}
}
