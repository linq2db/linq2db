using LinqToDB.Interceptors;
using LinqToDB.Internal.Expressions;

namespace LinqToDB.Internal.Interceptors
{
	public interface IEntityBindingInterceptor : IInterceptor
	{
		/// <summary>
		/// Converts <see cref="SqlGenericConstructorExpression"/> expression to new one if needed.
		/// </summary>
		/// <remarks>
		/// Called while a query is translated, possibly several times for one constructor, and not called when a cached
		/// query is reused. The result must depend only on <paramref name="expression"/>: the same input must give the
		/// same result on every call and for every instance of the interceptor type, because the query cache key holds
		/// the interceptor's type, not its instance.
		/// </remarks>
		SqlGenericConstructorExpression ConvertConstructorExpression(SqlGenericConstructorExpression expression);
	}
}
