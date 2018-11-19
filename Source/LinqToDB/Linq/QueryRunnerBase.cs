﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.Linq
{
	using Data;

	abstract class QueryRunnerBase : IQueryRunner
	{
		protected QueryRunnerBase(Query query, int queryNumber, IDataContext dataContext, Expression expression, object[] parameters)
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

		public IDataContext         DataContext      { get; set; }
		public Expression           Expression       { get; set; }
		public object[]             Parameters       { get; set; }
		public abstract Expression  MapperExpression { get; set; }

		public abstract int                    ExecuteNonQuery();
		public abstract object                 ExecuteScalar  ();
		public abstract IDataReader            ExecuteReader  ();
		public abstract Task<object>           ExecuteScalarAsync  (CancellationToken cancellationToken);
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
			// TODO: can we refactory query to be thread-safe to remove this lock?
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

				QueryRunner.SetParameters(Query, DataContext, Expression, Parameters, QueryNumber);
				SetQuery();
			}
		}

		protected abstract void   SetQuery  ();

		public    abstract string GetSqlText();
	}
}
