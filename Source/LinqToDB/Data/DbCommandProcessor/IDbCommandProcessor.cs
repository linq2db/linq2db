using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

using JetBrains.Annotations;

namespace LinqToDB.Data.DbCommandProcessor
{
	[PublicAPI]
	public interface IDbCommandProcessor
	{
		object             ExecuteScalar       (DbCommand command);
		Task<object>       ExecuteScalarAsync  (DbCommand command, CancellationToken cancellationToken);
		int                ExecuteNonQuery     (DbCommand command);
		Task<int>          ExecuteNonQueryAsync(DbCommand command, CancellationToken cancellationToken);
		DbDataReader       ExecuteReader       (DbCommand command, CommandBehavior commandBehavior);
		Task<DbDataReader> ExecuteReaderAsync  (DbCommand command, CommandBehavior commandBehavior, CancellationToken cancellationToken);
	}
}
