using JetBrains.Annotations;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.Data.DbCommandProcessor
{
	[PublicAPI]
	public class DbCommandDefaultProcessor : IDbCommandProcessor
	{
		public int ExecuteNonQuery(DbCommand cmd)
			=> cmd.ExecuteNonQuery();

		public Task<int> ExecuteNonQueryAsync(DbCommand cmd, CancellationToken ct)
			=> cmd.ExecuteNonQueryAsync(ct);

		public DbDataReader ExecuteReader(DbCommand cmd, CommandBehavior commandBehavior)
			=> cmd.ExecuteReader(commandBehavior);

		public Task<DbDataReader> ExecuteReaderAsync(DbCommand cmd, CommandBehavior commandBehavior, CancellationToken ct)
			=> cmd.ExecuteReaderAsync(commandBehavior, ct);

		public object ExecuteScalar(DbCommand cmd)
			=> cmd.ExecuteScalar();

		public Task<object> ExecuteScalarAsync(DbCommand cmd, CancellationToken ct)
			=> cmd.ExecuteScalarAsync(ct);
	}
}
