namespace LinqToDB.Interceptors
{
	using Data;

	/// <summary>
	/// Event arguments for <see cref="ICommandInterceptor"/> events.
	/// </summary>
	public readonly struct CommandEventData
	{
		internal CommandEventData(DataConnection dataConnection)
		{
			DataConnection = dataConnection;
		}

		/// <summary>
		/// Gets data connection associated with event.
		/// </summary>
		public DataConnection DataConnection { get; }
	}
}
