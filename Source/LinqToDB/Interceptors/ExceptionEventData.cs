namespace LinqToDB.Interceptors
{
	/// <summary>
	/// Event arguments for <see cref="ICommandInterceptor"/> events.
	/// </summary>
	public readonly struct ExceptionEventData
	{
		internal ExceptionEventData(
			IDataContext dataContext)
		{
			DataContext    = dataContext;
		}

		/// <summary>
		/// Gets data context, associated with event.
		/// </summary>
		public IDataContext DataContext { get; }
	}
}
