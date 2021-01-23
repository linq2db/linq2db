using System;
using System.Data;
using System.Data.Common;

namespace LinqToDB.Data
{
	/// <summary>
	/// Event handler delegate for <see cref="DataConnection.OnCommandInitialized"/> event.
	/// </summary>
	/// <param name="args">Event arguments</param>
	public delegate void OnCommandInitializedEventHandler(OnCommandInitializedEventArgs args);

	/// <summary>
	/// Event arguments for <see cref="DataConnection.OnCommandInitialized"/> event.
	/// </summary>
	public class OnCommandInitializedEventArgs
	{
		internal OnCommandInitializedEventArgs(DataConnection dataConnection, DbCommand command)
		{
			DataConnection = dataConnection;
			_command       = command;
		}

		/// <summary>
		/// Gets data connection associated with event command.
		/// </summary>
		public DataConnection DataConnection { get; }

		private DbCommand _command;

		/// <summary>
		/// Get or set command.
		/// </summary>
		public DbCommand Command
		{
			get => _command;
			set 
			{
				if (value == null)
					throw new ArgumentNullException(nameof(value));

				if (!ReferenceEquals(_command, value))
				{
					_command       = value;
					CommandChanged = true;
				}
			}
		}

		internal bool CommandChanged { get; private set; }
	}
}
