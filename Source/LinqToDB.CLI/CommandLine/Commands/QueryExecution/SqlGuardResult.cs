using System;

using LinqToDB.CommandLine;
using LinqToDB.CommandLine.Options;

namespace LinqToDB.CommandLine.Commands.QueryExecution
{
	/// <summary>
	/// SQL guard validation result.
	/// </summary>
	public sealed class SqlGuardResult
	{
		internal static SqlGuardResult Allowed { get; } = new(true, null);

		/// <summary>Indicates whether SQL passed validation.</summary>
		public bool IsAllowed { get; }

		/// <summary>Validation error, when SQL was rejected.</summary>
		public string? Error { get; }

		SqlGuardResult(bool isAllowed, string? error)
		{
			IsAllowed = isAllowed;
			Error     = error;
		}

		internal static SqlGuardResult Rejected(string error)
		{
			return new(false, error);
		}
	}
}
