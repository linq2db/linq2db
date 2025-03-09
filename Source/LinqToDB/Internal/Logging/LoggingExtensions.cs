using System.Diagnostics;

using LinqToDB.Data;

namespace LinqToDB.Internal.Logging
{
	public static class LoggingExtensions
	{
		/// <summary>
		/// Write line to trace associated with provided context.
		/// </summary>
		/// <param name="context">Context instance.</param>
		/// <param name="message">Message text.</param>
		/// <param name="category">Message category.</param>
		/// <param name="level">Trace level.</param>
		public static void WriteTraceLine(
			this IDataContext context,
			     string       message,
			     string       category,
			     TraceLevel   level)
		{
			switch (context)
			{
				case DataConnection connection:
					connection.WriteTraceLineConnection(message, category, level);
					break;
				default:
					DataConnection.WriteTraceLine(message, category, level);
					break;
			}
		}

		/// <summary>
		/// Returns <see cref="TraceSwitch"/> tracing options, used by provided context.
		/// </summary>
		/// <param name="context">Context instance.</param>
		/// <returns><see cref="TraceSwitch"/> instance, used for tracing by provided context.</returns>
		public static TraceSwitch GetTraceSwitch(this IDataContext context)
		{
			return context switch
			{
				DataConnection connection => connection.TraceSwitchConnection,
				_                         => DataConnection.TraceSwitch
			};
		}
	}
}
