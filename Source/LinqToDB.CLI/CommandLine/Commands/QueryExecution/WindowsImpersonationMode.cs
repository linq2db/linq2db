using System;

using LinqToDB.CommandLine;
using LinqToDB.CommandLine.Options;

namespace LinqToDB.CommandLine.Commands.QueryExecution
{
	/// <summary>
	/// Windows logon modes supported for query execution impersonation.
	/// </summary>
	public enum WindowsImpersonationMode
	{
		/// <summary>Authenticates credentials and preserves access to network resources.</summary>
		NetworkCleartext = 8,
		/// <summary>Creates an interactive logon session.</summary>
		Interactive      = 2,
		/// <summary>Creates a network logon session.</summary>
		Network          = 3,
		/// <summary>Uses credentials for outbound network access without changing the local identity.</summary>
		NewCredentials   = 9,
	}
}
