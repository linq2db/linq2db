using LinqToDB.Data;

namespace LinqToDB.Interceptors
{
	/// <summary>
	/// Event arguments for <see cref="IConnectionInterceptor"/> events.
	/// </summary>
	public readonly struct ConnectionEventData
	{
		internal ConnectionEventData(DataConnection? dataConnection)
		{
			DataConnection = dataConnection;
		}

		/// <summary>
		/// Gets data connection associated with event.
		/// Could be <c>null</c> when used for connections, created not from <see cref="DataConnection"/>.
		/// E.g. in provider detection logic or for some databases in bulk copy code.
		/// </summary>
		public DataConnection? DataConnection { get; }
	}
}
