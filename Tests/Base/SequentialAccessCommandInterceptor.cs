using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB.Common;
using LinqToDB.Interceptors;

namespace Tests
{
	// override only CommandBehavior.Default as we don't want to break schema queries
	public class SequentialAccessCommandInterceptor : CommandInterceptor
	{
		public static IInterceptor Instance = new SequentialAccessCommandInterceptor();

		public override Option<DbDataReader> ExecuteReader(CommandEventData eventData, DbCommand command, CommandBehavior commandBehavior, Option<DbDataReader> result)
		{
			if (commandBehavior == CommandBehavior.Default)
				return command.ExecuteReader(CommandBehavior.SequentialAccess);

			return result;
		}

		public override async Task<Option<DbDataReader>> ExecuteReaderAsync(CommandEventData eventData, DbCommand command, CommandBehavior commandBehavior, Option<DbDataReader> result, CancellationToken cancellationToken)
		{
			if (commandBehavior == CommandBehavior.Default)
				return await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken);

			return result;
		}
	}
}
