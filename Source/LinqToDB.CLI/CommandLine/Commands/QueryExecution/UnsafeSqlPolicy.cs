using System;

using LinqToDB.CommandLine;
using LinqToDB.CommandLine.Options;

namespace LinqToDB.CommandLine.Commands.QueryExecution
{
	internal enum UnsafeSqlPolicy
	{
		Deny,
		Confirm,
		Allow,
	}
}
