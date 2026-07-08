using System;

using LinqToDB.CommandLine;
using LinqToDB.CommandLine.Options;

namespace LinqToDB.CommandLine.Commands.QueryExecution
{
	internal sealed record SqlGuardResult(bool IsAllowed, string? Error)
	{
		public static SqlGuardResult Allowed { get; } = new(true, null);

		public static SqlGuardResult Rejected(string error)
		{
			return new(false, error);
		}
	}
}
