using System;

namespace LinqToDB.CommandLine.Commands.QueryExecution
{
	/// <summary>
	/// SQL execution contract selected by command or MCP tool adapter.
	/// </summary>
	internal enum QueryExecutionMode
	{
		Query,
		Execute,
	}
}
