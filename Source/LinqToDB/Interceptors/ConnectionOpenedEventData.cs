using LinqToDB.Data;

namespace LinqToDB.Interceptors
{
	/// <summary>
	/// Event arguments for <see cref="IConnectionInterceptor.ConnectionOpened(ConnectionOpenedEventData, System.Data.Common.DbConnection)"/> or <see cref="IConnectionInterceptor.ConnectionOpenedAsync(ConnectionOpenedEventData, System.Data.Common.DbConnection, System.Threading.CancellationToken)"/> event.
	/// </summary>
	public readonly struct ConnectionOpenedEventData
	{
		internal ConnectionOpenedEventData(DataConnection dataConnection)
		{
			DataConnection = dataConnection;
		}

		/// <summary>
		/// Gets data connection associated with event.
		/// </summary>
		public DataConnection DataConnection { get; }
	}
}
