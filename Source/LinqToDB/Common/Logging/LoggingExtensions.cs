using System;
using System.Diagnostics;
using LinqToDB.Data;

namespace LinqToDB.Common.Logging
{
	public static class LoggingExtensions
	{
		public static void WriteTraceLine(this IDataContext context,
			string message,
			string displayName,
			TraceLevel level)
		{
			switch (context)
			{
				case DataConnection connection:
					connection.WriteTraceLineConnection(message, displayName, level);
					break;
					default:
						DataConnection.WriteTraceLine(message, displayName, level);
						break;
			}
		}

		public static TraceSwitch GetTraceSwitch(this IDataContext context)
		{
			return context switch
			{
				DataConnection connection => connection.TraceSwitchConnection,
				_ => DataConnection.TraceSwitch
			};
		}
	}
}
