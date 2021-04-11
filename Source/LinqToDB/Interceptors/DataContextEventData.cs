namespace LinqToDB.Interceptors
{
	/// <summary>
	/// Event arguments for <see cref="IDataContextInterceptor"/> events.
	/// </summary>
	public readonly struct DataContextEventData
	{
		internal DataContextEventData(IDataContext context)
		{
			Context = context;
		}

		/// <summary>
		/// Gets data context, associated with event.
		/// </summary>
		public IDataContext Context { get; }
	}
}
