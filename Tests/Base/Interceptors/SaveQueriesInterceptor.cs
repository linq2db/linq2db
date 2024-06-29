using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using LinqToDB.Interceptors;

namespace Tests
{
	public sealed class SaveQueriesInterceptor : CommandInterceptor
	{
		public List<string> Queries { get; } = new ();

		public override DbCommand CommandInitialized(CommandEventData eventData, DbCommand command)
		{
			Queries.Add(command.CommandText);

			return command;
		}
	}
}
