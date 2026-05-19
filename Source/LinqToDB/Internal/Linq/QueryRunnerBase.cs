using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Data;
using LinqToDB.Internal.SqlQuery;

namespace LinqToDB.Internal.Linq
{
	abstract class QueryRunnerBase : IQueryRunner, IExecutionContextAwareRunner
	{
		protected QueryRunnerBase(Query query, int queryNumber, IDataContext dataContext, IDataContext parametersContext, IQueryExpressions expressions, object?[]? parameters, object?[]? preambles)
		{
			Query             = query;
			DataContext       = dataContext;
			ParametersContext = parametersContext;
			Expressions       = expressions;
			QueryNumber       = queryNumber;
			Parameters        = parameters;
			Preambles         = preambles;
		}

		protected readonly Query    Query;

		public          IDataContext      DataContext       { get; }
		public          IDataContext      ParametersContext { get; }
		public          IQueryExpressions Expressions       { get; }
		public          object?[]?        Parameters        { get; }
		public          object?[]?        Preambles         { get; }
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

		/// <summary>
		/// Per-execute shared context attached by the caller (via
		/// <c>QueryRunnerExtensions.GetQueryRunner</c> 8-arg overload) so the SQL builder can
		/// look up the matching init-query's Setup-time decision (use temp table vs. inline
		/// VALUES) during emission. Explicit interface implementation keeps this off the
		/// public-facing <see cref="IQueryRunner"/> surface.
		/// </summary>
		QueryExecutionContext? IExecutionContextAwareRunner.ExecutionContext { get; set; }

		internal QueryExecutionContext? ExecutionContext
		{
			get => ((IExecutionContextAwareRunner)this).ExecutionContext;
			set => ((IExecutionContextAwareRunner)this).ExecutionContext = value;
		}
	}
}
