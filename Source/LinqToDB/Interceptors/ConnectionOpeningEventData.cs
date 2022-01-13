using LinqToDB.Data;

namespace LinqToDB.Interceptors
{
	/// <summary>
	/// Event arguments for <see cref="IConnectionInterceptor.ConnectionOpening(ConnectionOpeningEventData, System.Data.Common.DbConnection)"/> or <see cref="IConnectionInterceptor.ConnectionOpeningAsync(ConnectionOpeningEventData, System.Data.Common.DbConnection, System.Threading.CancellationToken)"/> event.
	/// </summary>
	public readonly struct ConnectionOpeningEventData
	{
		internal ConnectionOpeningEventData(DataConnection dataConnection)
		{
			DataConnection = dataConnection;
		}

		/// <summary>
		/// Gets data connection associated with event.
		/// </summary>
		public DataConnection DataConnection { get; }
	}
}
