using LinqToDB.Data;

namespace LinqToDB.Interceptors
{
	/// <summary>
	/// Event arguments for <see cref="ICommandInterceptor.CommandInitialized"/> event.
	/// </summary>
	public readonly struct CommandInitializedEventData
	{
		internal CommandInitializedEventData(DataConnection dataConnection)
		{
			DataConnection = dataConnection;
		}

		/// <summary>
		/// Gets data connection associated with event command.
		/// </summary>
		public DataConnection DataConnection { get; }
	}
}
