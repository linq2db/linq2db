using System;

namespace LinqToDB.CommandLine.Commands.Credentials
{
	/// <summary>
	/// Wire protocol spoken by a configured credential helper.
	/// </summary>
	public enum CredentialHelperProtocol
	{
		/// <summary>linq2db-cli credential helper protocol (key=value lines).</summary>
		Linq2Db,
		/// <summary>Compatibility adapter for docker-credential-* programs (Docker's JSON contract).</summary>
		Docker,
	}
}
