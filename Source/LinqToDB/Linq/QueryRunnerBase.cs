using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.Linq
{
	using Common.Internal;
	using Data;
	using SqlQuery;

	abstract class QueryRunnerBase : IQueryRunner
	{
		protected QueryRunnerBase(Query query, int queryNumber, IDataContext dataContext, IDataContext parametersContext, Expression expression, object?[]? parameters, object?[]? preambles)
		{
			Query             = query;
			DataContext       = dataContext;
			ParametersContext = parametersContext;
			Expression        = expression;
			QueryNumber       = queryNumber;
			Parameters        = parameters;
			Preambles         = preambles;
		}

		protected readonly Query    Query;

		public          IDataContext DataContext       { get; }
		public          IDataContext ParametersContext { get; }
		public          Expression   Expression        { get; }
		public          object?[]?   Parameters        { get; }
		public          object?[]?   Preambles         { get; }
		public abstract Expression?  MapperExpression  { get; set; }

		public abstract int                    ExecuteNonQuery();
		public abstract object?                ExecuteScalar  ();
		public abstract DataReaderWrapper      ExecuteReader  ();
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

			QueryRunner.SetParameters(Query, Expression, ParametersContext, Parameters, QueryNumber, parameterValues);

			SetQuery(parameterValues, forGetSqlText);
		}

		protected abstract void SetQuery(IReadOnlyParameterValues parameterValues, bool forGetSqlText);

		public    abstract string GetSqlText();
	}
}
