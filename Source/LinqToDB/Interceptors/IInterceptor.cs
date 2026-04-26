namespace LinqToDB.Interceptors
{
	/// <summary>
	/// Marker interface for LinqToDB interception surfaces.
	/// </summary>
	/// <remarks>
	/// Register implementations with <see cref="DataOptionsExtensions.UseInterceptor(DataOptions, IInterceptor)"/> or
	/// <see cref="DataOptionsExtensions.UseInterceptors(DataOptions, System.Collections.Generic.IEnumerable{IInterceptor})"/>.
	/// The specific sub-interface implemented by a given interceptor determines which stage of the pipeline it participates in.
	/// </remarks>
	public interface IInterceptor
	{
	}
}