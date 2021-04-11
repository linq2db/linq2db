namespace LinqToDB.Interceptors
{
	/// <summary>
	/// Event arguments for <see cref="IDataContextInterceptor.EntityCreated(EntityCreatedEventData, object)"/> event.
	/// </summary>
	public readonly struct EntityCreatedEventData
	{
		internal EntityCreatedEventData(IDataContext context)
		{
			Context = context;
		}

		/// <summary>
		/// Gets data context, associated with event.
		/// </summary>
		public IDataContext Context { get; }
	}
}
