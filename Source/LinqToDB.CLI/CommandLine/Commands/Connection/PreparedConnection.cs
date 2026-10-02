using System;

using LinqToDB.DataProvider;

namespace LinqToDB.CommandLine.Commands.Connection
{
	/// <summary>
	/// Provider and options prepared for a database connection.
	/// </summary>
	internal sealed record PreparedConnection(DataOptions DataOptions, IDataProvider DataProvider);
}
