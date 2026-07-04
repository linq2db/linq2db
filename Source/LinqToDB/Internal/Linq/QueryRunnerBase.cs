using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Data;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;

namespace LinqToDB.Internal.Linq
{
	abstract class QueryRunnerBase : IQueryRunner
	{
		protected QueryRunnerBase(Query query, int queryNumber, IDataContext dataContext, IDataContext parametersContext, IQueryExpressions expressions, object?[]? parameters, object?[]? preambles)
		{
			Query             = query;
			DataContext       = dataContext;
			ParametersContext = parametersContext;
			Expressions       = expressions;
			QueryNumber       = queryNumber;
			Parameters        = parameters;
			// Wrap the legacy preamble results array in an execution context. Behavior-neutral: the context adopts the same
			// array reference, so Preambles returns it unchanged and the mapper reads the same per-step results.
			_executionContext = preambles is null ? null : new SqlCommandExecutionContext(preambles);
		}

		protected readonly Query    Query;

		readonly SqlCommandExecutionContext? _executionContext;

		public          IDataContext      DataContext       { get; }
		public          IDataContext      ParametersContext { get; }
		public          IQueryExpressions Expressions       { get; }
		public          object?[]?        Parameters        { get; }
		public          object?[]?        Preambles         => _executionContext?.Results;
		public          SqlCommandExecutionContext? ExecutionContext => _executionContext;
		public abstract Expression?       MapperExpression  { get; set; }

		public abstract int                    ExecuteNonQuery();
		public abstract object?                ExecuteScalar  ();
		public abstract IDataReaderAsync       ExecuteReader  ();
		public abstract Task<object?>          ExecuteScalarAsync  (CancellationToken cancellationToken);
		public abstract Task<IDataReaderAsync> ExecuteReaderAsync  (CancellationToken cancellationToken);
		public abstract Task<int>              ExecuteNonQueryAsync(CancellationToken cancellationToken);

		public int RowsCount   { get; set; }
		public int QueryNumber { get; set; }

		public virtual void Dispose()
		{
			if (DataContext.CloseAfterUse)
				DataContext.Close();
		}

		public virtual ValueTask DisposeAsync()
		{
			if (DataContext.CloseAfterUse)
				return new ValueTask(DataContext.CloseAsync());

			return default;
		}

		protected virtual void SetCommand(bool forGetSqlText)
		{
			var parameterValues = new SqlParameterValues();

			QueryRunner.SetParameters(Query, Expressions, ParametersContext, Parameters, parameterValues);

			SetQuery(parameterValues, forGetSqlText);
		}

		protected abstract void SetQuery(IReadOnlyParameterValues parameterValues, bool forGetSqlText);

		public abstract IReadOnlyList<QuerySql> GetSqlText();
	}
}
