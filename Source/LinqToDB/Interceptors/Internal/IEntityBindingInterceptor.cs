namespace LinqToDB.Interceptors.Internal
{
	using LinqToDB.Interceptors;
	using LinqToDB.Internal.Expressions;

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
