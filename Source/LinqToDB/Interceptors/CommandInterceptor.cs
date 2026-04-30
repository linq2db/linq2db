using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Common;

namespace LinqToDB.Interceptors
{
	/// <summary>
	/// Base class with pass-through implementations for <see cref="ICommandInterceptor"/>.
	/// </summary>
	/// <remarks>
	/// Derive from this class when overriding only selected command interception methods.
	/// For callback timing and return-value contracts, see <see cref="ICommandInterceptor"/>.
	/// </remarks>
	public abstract class CommandInterceptor : ICommandInterceptor
	{
		/// <inheritdoc />
		public virtual DbCommand                  CommandInitialized      (CommandEventData eventData, DbCommand command) => command;

		/// <inheritdoc />
		public virtual Option<int>                ExecuteNonQuery         (CommandEventData eventData, DbCommand command, Option<int> result) => result;
		/// <inheritdoc />
		public virtual Task<Option<int>>          ExecuteNonQueryAsync    (CommandEventData eventData, DbCommand command, Option<int> result, CancellationToken cancellationToken) => Task.FromResult(result);

		/// <inheritdoc />
		public virtual Option<DbDataReader>       ExecuteReader           (CommandEventData eventData, DbCommand command, CommandBehavior commandBehavior, Option<DbDataReader> result) => result;
		/// <inheritdoc />
		public virtual Task<Option<DbDataReader>> ExecuteReaderAsync      (CommandEventData eventData, DbCommand command, CommandBehavior commandBehavior, Option<DbDataReader> result, CancellationToken cancellationToken) => Task.FromResult(result);

		/// <inheritdoc />
		public virtual void                       AfterExecuteReader      (CommandEventData eventData, DbCommand command, CommandBehavior commandBehavior, DbDataReader dataReader) {}

		/// <inheritdoc />
		public virtual Option<object?>            ExecuteScalar           (CommandEventData eventData, DbCommand command, Option<object?> result) => result;
		/// <inheritdoc />
		public virtual Task<Option<object?>>      ExecuteScalarAsync      (CommandEventData eventData, DbCommand command, Option<object?> result, CancellationToken cancellationToken) => Task.FromResult(result);

		/// <inheritdoc />
		public virtual void                       BeforeReaderDispose     (CommandEventData eventData, DbCommand? command, DbDataReader dataReader) { }
		/// <inheritdoc />
		public virtual Task                       BeforeReaderDisposeAsync(CommandEventData eventData, DbCommand? command, DbDataReader dataReader) => Task.CompletedTask;
	}
}
