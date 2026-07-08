using System;

using LinqToDB.CommandLine;
using LinqToDB.CommandLine.Options;

namespace LinqToDB.CommandLine.Commands.QueryExecution
{
	enum WindowsImpersonationMode
	{
		NetworkCleartext = 8,
		Interactive      = 2,
		Network          = 3,
		NewCredentials   = 9,
	}
}
