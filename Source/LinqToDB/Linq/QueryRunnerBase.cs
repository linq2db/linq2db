using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.Linq
{
	using SqlQuery;

	abstract class QueryRunnerBase : IQueryRunner
	{
		protected QueryRunnerBase(Query query, int queryNumber, IDataContext dataContext, Expression expression, object?[]? parameters, object?[]? preambles)
		{
			Query        = query;
			DataContext  = dataContext;
			Expression   = expression;
			QueryNumber  = queryNumber;
			Parameters   = parameters;
			Preambles    = preambles;
		}

		protected readonly Query    Query;

		public IDataContext         DataContext      { get; set; }
		public Expression           Expression       { get; set; }
		public object?[]?           Parameters       { get; set; }
		public object?[]?           Preambles        { get; set; }
		public abstract Expression? MapperExpression { get; set; }

		public abstract int                    ExecuteNonQuery();
		public abstract object?                ExecuteScalar  ();
		public abstract IDataReader            ExecuteReader  ();
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

#if !NATIVE_ASYNC
		public virtual Task DisposeAsync()
		{
			if (DataContext.CloseAfterUse)
				return DataContext.CloseAsync();

			return TaskEx.CompletedTask;
		}
#else
		public virtual ValueTask DisposeAsync()
		{
			if (DataContext.CloseAfterUse)
				return new ValueTask(DataContext.CloseAsync());

			return default;
		}
#endif


		protected virtual void SetCommand(bool forGetSqlText)
		{
			var parameterValues = new SqlParameterValues();

			QueryRunner.SetParameters(Query, Expression, DataContext, Parameters, QueryNumber, parameterValues);

			SetQuery(parameterValues, forGetSqlText);
		}

		protected abstract void SetQuery(IReadOnlyParameterValues parameterValues, bool forGetSqlText);

		public    abstract string GetSqlText();
	}
}
