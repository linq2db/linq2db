namespace LinqToDB.Interceptors.Internal
{
	using LinqToDB.Internal.Expressions;

	public abstract class EntityBindingInterceptor : IEntityBindingInterceptor
	{
		public virtual SqlGenericConstructorExpression ConvertConstructorExpression(SqlGenericConstructorExpression expression) => expression;
	}
}
