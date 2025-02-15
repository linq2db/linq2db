using LinqToDB.Expressions;

namespace LinqToDB.Interceptors.Internal
{
	/// <summary>
	/// Internal API.
	/// </summary>
	public interface IEntityBindingInterceptor : IInterceptor
	{
		/// <summary>
		/// Converts <see cref="SqlGenericConstructorExpression"/> expression to new one if needed.
		/// </summary>
		SqlGenericConstructorExpression ConvertConstructorExpression(SqlGenericConstructorExpression expression);
	}
}
