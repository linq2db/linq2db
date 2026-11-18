namespace LinqToDB.Interceptors
{
	/// <summary>
	/// Implemented by a query translation interceptor, such as <see cref="IQueryExpressionInterceptor"/>, whose
	/// instances of one type translate queries differently. A query translated for one context is reused by another
	/// context only when their interceptors are of the same types and return equal keys.
	/// </summary>
	public interface IInterceptorCacheKey
	{
		/// <summary>
		/// Identifies how this instance translates queries. Instances that translate a query differently return
		/// different keys. The key does not change while the interceptor is registered.
		/// </summary>
		string? CacheKey { get; }
	}
}
