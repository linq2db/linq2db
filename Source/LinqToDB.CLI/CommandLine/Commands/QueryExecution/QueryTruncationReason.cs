using System;

namespace LinqToDB.CommandLine.Commands.QueryExecution
{
	internal enum QueryTruncationReason
	{
		MaxRows,
		MaxOutputBytes,
	}
}
