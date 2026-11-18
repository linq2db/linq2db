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

		/// <summary>The accepted script writes temporary tables: it fills them before its query or drops them after it.</summary>
		internal bool WritesTemporaryTables { get; private init; }

		/// <summary>
		/// The call may leave session state that the provider's driver would hand to the next user of a pooled connection,
		/// so its connection must be closed instead of returned to the pool.
		/// </summary>
		internal bool RequiresUnpooledConnection { get; private init; }

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

		internal static SqlGuardResult Accepted(bool writesTemporaryTables, bool requiresUnpooledConnection)
		{
			return writesTemporaryTables || requiresUnpooledConnection
				? new(true, null) { WritesTemporaryTables = writesTemporaryTables, RequiresUnpooledConnection = requiresUnpooledConnection }
				: Allowed;
		}
	}
}
