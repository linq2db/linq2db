using System;

namespace LinqToDB.CommandLine.Commands.SchemaInspection
{
	/// <summary>
	/// Schema inspection execution result.
	/// </summary>
	internal sealed record SchemaInspectionResult(int StatusCode, string? Error);
}
