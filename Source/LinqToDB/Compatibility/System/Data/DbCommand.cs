using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace System.Data
{
	static partial class DbDataExtensions
	{
		public static async Task<int> ExecuteNonQueryAsync(this IDbCommand command, CancellationToken cancellationToken)
		{
			return await Task.Factory.StartNew(command.ExecuteNonQuery, cancellationToken);
		}

		public static async Task<object> ExecuteScalarAsync(this IDbCommand command, CancellationToken cancellationToken)
		{
			return await Task.Factory.StartNew(command.ExecuteScalar, cancellationToken);
		}

		public static async Task<DbDataReader> ExecuteReaderAsync(this IDbCommand command, CommandBehavior commandBehavior, CancellationToken cancellationToken)
		{
			return await Task.Factory.StartNew(() => (DbDataReader)command.ExecuteReader(commandBehavior), cancellationToken);
		}
	}
}

