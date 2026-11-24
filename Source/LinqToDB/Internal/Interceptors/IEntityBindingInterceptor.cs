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
		/// Called only when a query is translated, never when a cached query is reused, so instances of one type that convert
		/// differently implement <see cref="IInterceptorCacheKey"/>.
		/// </remarks>
		SqlGenericConstructorExpression ConvertConstructorExpression(SqlGenericConstructorExpression expression);
	}
}
