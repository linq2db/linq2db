using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB.Data.DbCommandProcessor;

namespace Tests
{
	public class SequentialAccessCommandProcessor : IDbCommandProcessor
	{
		DbDataReader IDbCommandProcessor.ExecuteReader(DbCommand command, CommandBehavior commandBehavior)
		{
			// override only Default behavior, we don't want to break schema queries
			return command.ExecuteReader(commandBehavior == CommandBehavior.Default ? CommandBehavior.SequentialAccess : commandBehavior);
		}

		Task<DbDataReader> IDbCommandProcessor.ExecuteReaderAsync(DbCommand command, CommandBehavior commandBehavior, CancellationToken cancellationToken)
		{
			return command.ExecuteReaderAsync(commandBehavior == CommandBehavior.Default ? CommandBehavior.SequentialAccess : commandBehavior, cancellationToken);
		}

		int IDbCommandProcessor.ExecuteNonQuery(DbCommand command) => command.ExecuteNonQuery();
		Task<int> IDbCommandProcessor.ExecuteNonQueryAsync(DbCommand command, CancellationToken cancellationToken) => command.ExecuteNonQueryAsync(cancellationToken);
		object? IDbCommandProcessor.ExecuteScalar(DbCommand command) => command.ExecuteScalar();
		Task<object?> IDbCommandProcessor.ExecuteScalarAsync(DbCommand command, CancellationToken cancellationToken) => command.ExecuteScalarAsync(cancellationToken);
	}
}
