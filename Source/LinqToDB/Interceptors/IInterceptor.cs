namespace LinqToDB.Interceptors
{
	/// <summary>
	/// Marker interface for LinqToDB interception surfaces.
	/// </summary>
	/// <remarks>
	/// Use interceptors for cross-cutting behavior around supported LinqToDB operations.
	/// Implement a more specific interceptor interface to select the operation stage.
	/// Register implementations with <see cref="DataOptionsExtensions.UseInterceptor(DataOptions, IInterceptor)"/> or
	/// <see cref="DataOptionsExtensions.UseInterceptors(DataOptions, System.Collections.Generic.IEnumerable{IInterceptor})"/>.
	/// A context instance can also accept interceptors through <see cref="IDataContext.AddInterceptor(IInterceptor)"/>.
	/// A single interceptor object can implement multiple interceptor interfaces.
	/// </remarks>
	public interface IInterceptor
	{
	}
}
