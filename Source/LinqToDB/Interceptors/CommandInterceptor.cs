using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Common;

namespace LinqToDB.Interceptors
{
	public abstract class CommandInterceptor : ICommandInterceptor
	{
		public virtual DbCommand                  CommandInitialized      (CommandEventData eventData, DbCommand command) => command;

		public virtual Option<int>                ExecuteNonQuery         (CommandEventData eventData, DbCommand command, Option<int> result) => result;
		public virtual Task<Option<int>>          ExecuteNonQueryAsync    (CommandEventData eventData, DbCommand command, Option<int> result, CancellationToken cancellationToken) => Task.FromResult(result);

		public virtual Option<DbDataReader>       ExecuteReader           (CommandEventData eventData, DbCommand command, CommandBehavior commandBehavior, Option<DbDataReader> result) => result;
		public virtual Task<Option<DbDataReader>> ExecuteReaderAsync      (CommandEventData eventData, DbCommand command, CommandBehavior commandBehavior, Option<DbDataReader> result, CancellationToken cancellationToken) => Task.FromResult(result);

		public virtual void                       AfterExecuteReader      (CommandEventData eventData, DbCommand command, CommandBehavior commandBehavior, DbDataReader dataReader) {}

		public virtual Option<object?>            ExecuteScalar           (CommandEventData eventData, DbCommand command, Option<object?> result) => result;
		public virtual Task<Option<object?>>      ExecuteScalarAsync      (CommandEventData eventData, DbCommand command, Option<object?> result, CancellationToken cancellationToken) => Task.FromResult(result);

		public virtual void                       BeforeReaderDispose     (CommandEventData eventData, DbCommand? command, DbDataReader dataReader) { }
		public virtual Task                       BeforeReaderDisposeAsync(CommandEventData eventData, DbCommand? command, DbDataReader dataReader) => Task.CompletedTask;
	}
}
