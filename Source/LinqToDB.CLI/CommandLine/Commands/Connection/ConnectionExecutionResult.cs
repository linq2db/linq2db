using System;

namespace LinqToDB.CommandLine.Commands.Connection
{
	/// <summary>
	/// Result of shared connection-bound execution.
	/// </summary>
	internal sealed record ConnectionExecutionResult<T>(int StatusCode, string? Error, T? Value);
}
