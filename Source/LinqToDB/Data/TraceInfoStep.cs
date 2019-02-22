using System;

namespace LinqToDB.Data
{
	/// <summary>
	/// Tracing steps for the <see cref="DataConnection"/> trace events.
	/// </summary>
	/// <seealso cref="TraceInfo"/>
	public enum TraceInfoStep
	{
		/// <summary>
		/// Occurs before executing a command.
		/// </summary>
		BeforeExecute,

		/// <summary>
		/// Occurs after a command is executed.
		/// </summary>
		AfterExecute,

		/// <summary>
		/// Occurs when an error happened during the command execution.
		/// </summary>
		Error,

		/// <summary>
		/// Occurs when the result mapper was created.
		/// </summary>
		MapperCreated,

		/// <summary>
		/// Occurs when an operation is completed and its associated <see cref="DataReader"/> is closed.
		/// </summary>
		Completed,
	}
}
