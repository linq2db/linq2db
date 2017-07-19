using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB.Data;
using LinqToDB.Extensions;

namespace LinqToDB.Linq
{
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

		protected readonly Query          Query;
		protected readonly IDataContextEx DataContext;
		protected readonly Expression     Expression;
		protected readonly int            QueryNumber;
		protected readonly object[]       Parameters;

		protected List<string>            QueryHints = new List<string>();
		protected DataParameter[]         DataParameters;

		public abstract void                   Dispose();
		public abstract int                    ExecuteNonQuery();
		public abstract object                 ExecuteScalar();
		public abstract IDataReader            ExecuteReader();
		public abstract Task<IDataReaderAsync> ExecuteReaderAsync(CancellationToken cancellationToken, TaskCreationOptions options);
		public abstract Expression             MapperExpression { get; set; }

		public Func<int> SkipAction { get; set; }
		public Func<int> TakeAction { get; set; }
		public int       RowsCount  { get; set; }

		internal void SetParameters()
		{
			var queryContext = Query.Queries[QueryNumber];

			//DataParameters = new DataParameter[queryContext.Parameters.Count];

			for (var i = 0; i < queryContext.Parameters.Count; i++)
			{
				var p     = queryContext.Parameters[i];
				var value = p.Accessor(Expression, Parameters);

				var vs = value as IEnumerable;

				if (vs != null)
				{
					var type = vs.GetType();
					var etype = type.GetItemType();

					if (etype == null || etype == typeof(object) || etype.IsEnumEx() ||
						type.IsGenericTypeEx() && type.GetGenericTypeDefinition() == typeof(Nullable<>) &&
						etype.GetGenericArgumentsEx()[0].IsEnumEx())
					{
						var values = new List<object>();

						foreach (var v in vs)
						{
							value = v;

							if (v != null)
							{
								var valueType = v.GetType();

								if (valueType.ToNullableUnderlying().IsEnumEx())
									value = Query.GetConvertedEnum(valueType, value);
							}

							values.Add(value);
						}

						value = values;
					}
				}

				p.SqlParameter.Value = value;

				var dataType = p.DataTypeAccessor(Expression, Parameters);

				if (dataType == DataType.Undefined)
					p.SqlParameter.DataType = dataType = p.SqlParameter.DataType;

				//DataParameters[i] = new DataParameter(p.SqlParameter.Name, value, dataType);
			}
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

				SetParameters();
				SetQuery();
			}
		}

		protected abstract void SetQuery();
	}
}
