using JetBrains.Annotations;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.Data.DbCommandProcessor
{
	[PublicAPI]
	public interface IDbCommandProcessor
	{
		object ExecuteScalar(DbCommand cmd);
		Task<object> ExecuteScalarAsync(DbCommand cmd, CancellationToken ct);
		int ExecuteNonQuery(DbCommand cmd);
		Task<int> ExecuteNonQueryAsync(DbCommand cmd, CancellationToken ct);
		DbDataReader ExecuteReader(DbCommand cmd, CommandBehavior commandBehavior);
		Task<DbDataReader> ExecuteReaderAsync(DbCommand cmd, CommandBehavior commandBehavior, CancellationToken ct);
	}
}
