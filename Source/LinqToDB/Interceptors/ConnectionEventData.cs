using LinqToDB.Data;

namespace LinqToDB.Interceptors
{
	/// <summary>
	/// Event arguments for <see cref="IConnectionInterceptor"/> events.
	/// </summary>
	public readonly struct ConnectionEventData
	{
		internal ConnectionEventData(DataConnection dataConnection)
		{
			DataConnection = dataConnection;
		}

		/// <summary>
		/// Gets data connection associated with event.
		/// </summary>
		public DataConnection DataConnection { get; }
	}
}
