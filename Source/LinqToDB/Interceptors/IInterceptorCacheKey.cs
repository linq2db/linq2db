namespace LinqToDB.Interceptors
{
	/// <summary>
	/// Implemented by a query translation interceptor, such as <see cref="IQueryExpressionInterceptor"/>, to declare that
	/// its instances translate queries the same way when they return equal keys. A query translated for one context is then
	/// reused for another context whose interceptors are of the same types and return equal keys.
	/// </summary>
	/// <remarks>
	/// Every distinct key gets its own translated queries in the query cache, so an interceptor returns keys from a small
	/// set. An <see cref="Internal.Interceptors.IEntityBindingInterceptor"/> that does not implement this interface is treated as translating
	/// queries alike for all its instances.
	/// </remarks>
	public interface IInterceptorCacheKey
	{
		/// <summary>
		/// Identifies how this instance translates queries. Instances that translate a query differently, the values they
		/// insert into it included, return different keys. The key does not change while the interceptor is registered.
		/// </summary>
		string? CacheKey { get; }
	}
}
