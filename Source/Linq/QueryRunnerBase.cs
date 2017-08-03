using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using System.Threading;

#if !SL4
using System.Threading.Tasks;
#endif

namespace LinqToDB.Linq
{
	using Data;
	using Extensions;

	abstract class QueryRunnerBase : IQueryRunner
	{
		protected QueryRunnerBase(Query query, int queryNumber, IDataContextEx dataContext, Expression expression, object[] parameters)
		{
			Query        = query;
			DataContext  = dataContext;
			Expression   = expression;
			QueryNumber  = queryNumber;
			Parameters   = parameters;
		}

		protected readonly Query    Query;

		protected List<string>      QueryHints = new List<string>();
		protected DataParameter[]   DataParameters;

		public QueryContext         QueryContext { get; set; }
		public IDataContextEx       DataContext  { get; set; }
		public Expression           Expression   { get; set; }
		public object[]             Parameters   { get; set; }

		public abstract int         ExecuteNonQuery();
		public abstract object      ExecuteScalar();
		public abstract IDataReader ExecuteReader();
		public abstract Expression  MapperExpression { get; set; }
#if !NOASYNC
		public abstract Task<object>           ExecuteScalarAsync  (CancellationToken cancellationToken, TaskCreationOptions options);
		public abstract Task<IDataReaderAsync> ExecuteReaderAsync  (CancellationToken cancellationToken, TaskCreationOptions options);
		public abstract Task<int>              ExecuteNonQueryAsync(CancellationToken cancellationToken, TaskCreationOptions options);
#endif

		public Func<int> SkipAction  { get; set; }
		public Func<int> TakeAction  { get; set; }
		public int       RowsCount   { get; set; }
		public int       QueryNumber { get; set; }

		public virtual void Dispose()
		{
			if (DataContext.CloseAfterUse)
				DataContext.Close();
		}

		protected virtual void SetCommand(bool clearQueryHints)
		{
			lock (Query)
			{
				if (QueryNumber == 0 && (DataContext.QueryHints.Count > 0 || DataContext.NextQueryHints.Count > 0))
				{
					var queryContext = Query.Queries[QueryNumber];

					queryContext.QueryHints = new List<string>(DataContext.QueryHints);
					queryContext.QueryHints.AddRange(DataContext.NextQueryHints);

					QueryHints.AddRange(DataContext.QueryHints);
					QueryHints.AddRange(DataContext.NextQueryHints);

					if (clearQueryHints)
						DataContext.NextQueryHints.Clear();
				}

				QueryRunner.SetParameters(Query, Expression, Parameters, QueryNumber);
				SetQuery();
			}
		}

		protected abstract void SetQuery();

		public abstract string GetSqlText();
	}
}
