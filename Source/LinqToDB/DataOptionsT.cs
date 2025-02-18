namespace LinqToDB
{
	/// <summary>
	/// Typed <see cref="DataOptions"/> wrapper to support multiple option objects registration in DI containers.
	/// </summary>
	/// <typeparam name="T">Associated database context type.</typeparam>
	public class DataOptions<T>
		where T : IDataContext
	{
		public DataOptions(DataOptions options)
		{
			Options = options;
		}

		/// <summary>
		/// Gets wrapped <see cref="DataOptions"/> instance.
		/// </summary>
		public DataOptions Options { get; }
	}
}
