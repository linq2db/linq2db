namespace LinqToDB.Scaffold
{
	/// <summary>
	/// Provides default no-op interceptor instance to use when user didn't provided custom interceptors (to avoid nullable interceptor instances).
	/// </summary>
	internal sealed class NoOpScaffoldInterceptors : ScaffoldInterceptors
	{
		public static readonly ScaffoldInterceptors Instance = new NoOpScaffoldInterceptors();

		private NoOpScaffoldInterceptors() { }
	}
}
