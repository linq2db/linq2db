namespace LinqToDB.Internal.Interceptors
{
	/// <summary>
	/// A context that counts the changes of its registered interceptors.
	/// </summary>
	interface IInterceptorsGeneration
	{
		/// <summary>
		/// Changes on every AddInterceptor / RemoveInterceptor.
		/// </summary>
		int InterceptorsGeneration { get; }
	}
}
