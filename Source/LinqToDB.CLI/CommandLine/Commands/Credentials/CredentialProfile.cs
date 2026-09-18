using System;

namespace LinqToDB.CommandLine.Commands.Credentials
{
	/// <summary>
	/// Non-secret information about a stored linq2db credential profile.
	/// </summary>
	/// <param name="Name">Credential profile name without the store-specific target prefix.</param>
	/// <param name="User">Stored database user name.</param>
	public sealed record CredentialProfile(string Name, string User);
}
