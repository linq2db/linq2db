using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.Linq
{
	using LinqToDB.Data;
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

		protected List<string>?     QueryHints;

		public IDataContext         DataContext      { get; set; }
		public Expression           Expression       { get; set; }
		public object?[]?           Parameters       { get; set; }
		public object?[]?           Preambles        { get; set; }
		public abstract Expression? MapperExpression { get; set; }

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

		protected virtual void SetCommand(bool clearQueryHints)
		{
			if (QueryNumber == 0 && (DataContext.QueryHints.Count > 0 || DataContext.NextQueryHints.Count > 0))
			{
				var queryContext = Query.Queries[QueryNumber];

				queryContext.QueryHints = new List<string>(DataContext.QueryHints);
				queryContext.QueryHints.AddRange(DataContext.NextQueryHints);

				if (QueryHints == null)
					QueryHints = new List<string>(DataContext.QueryHints.Count + DataContext.NextQueryHints.Count);

				QueryHints.AddRange(DataContext.QueryHints);
				QueryHints.AddRange(DataContext.NextQueryHints);

				if (clearQueryHints)
					DataContext.NextQueryHints.Clear();
			}

			var parameterValues = new SqlParameterValues();
			QueryRunner.SetParameters(Query, Expression, DataContext, Parameters, QueryNumber, parameterValues);

			SetQuery(parameterValues);
		}

		protected abstract void SetQuery(IReadOnlyParameterValues parameterValues);

		public    abstract string GetSqlText();
	}
}
