using System;

using LinqToDB.CommandLine;
using LinqToDB.CommandLine.Options;

namespace LinqToDB.CommandLine.Commands.QueryExecution
{
	/// <summary>
	/// Query execution status returned to command adapters.
	/// </summary>
	internal sealed record QueryExecutionResult(int StatusCode, string? Error, bool Truncated);
}
