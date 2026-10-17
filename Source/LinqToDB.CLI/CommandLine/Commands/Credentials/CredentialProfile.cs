using System;

namespace LinqToDB.CommandLine.Commands.Credentials
{
	/// <summary>
	/// Non-secret information about a stored linq2db credential record.
	/// </summary>
	/// <param name="Name">Record name without the <c>linq2db/</c> target prefix.</param>
	/// <param name="User">Stored database user name.</param>
	public sealed record CredentialProfile(string Name, string User);
}
