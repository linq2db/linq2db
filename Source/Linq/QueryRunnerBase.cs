using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.Linq
{
	using Data;
	using Extensions;

	abstract class QueryRunBase : IQueryRunner1
	{
		protected QueryRunBase(
			Query        query,
			IDataContext dataContext,
			Expression   expression,
			object[]     parameters,
			int          queryNumber)
		{
			_query       = query;
			_dataContext = dataContext;
			_expression  = expression;
			_parameters  = parameters;
			_queryNumber = queryNumber;
		}

		protected readonly Query        _query;
		protected readonly IDataContext _dataContext;

		readonly Expression   _expression;
		readonly object[]     _parameters;
		protected readonly int          _queryNumber;

		public abstract Expression MapperExpression { get; set; }

		public int RowsCount { get; set; }

		public virtual void Dispose()
		{
			if (_dataContext.CloseAfterUse)
				_dataContext.Close();
		}

		protected abstract void        SetQuery     (QueryInfo queryInfo);
		public    abstract IDataReader ExecuteReader();

		void SetParameters()
		{
			foreach (var p in _query.Queries[_queryNumber].Parameters)
			{
				var value = p.Accessor(_expression, _parameters);

				var vs = value as IEnumerable;

				if (vs != null)
				{
					var type  = vs.GetType();
					var etype = type.GetItemType();

					if (etype == null || etype == typeof(object) || etype.IsEnumEx() ||
						(type.IsGenericTypeEx() && type.GetGenericTypeDefinition() == typeof(Nullable<>) && etype.GetGenericArgumentsEx()[0].IsEnumEx()))
					{
						var values = new List<object>();

						foreach (var v in vs)
						{
							value = v;

							if (v != null)
							{
								var valueType = v.GetType();

								if (valueType.ToNullableUnderlying().IsEnumEx())
									value = _query.GetConvertedEnum(valueType, value);
							}

							values.Add(value);
						}

						value = values;
					}
				}

				p.SqlParameter.Value = value;

				var dataType = p.DataTypeAccessor(_expression, _parameters);

				if (dataType != DataType.Undefined)
					p.SqlParameter.DataType = dataType;
			}
		}

		protected void SetCommand(bool clearQueryHints)
		{
			lock (_query)
			{
				SetParameters();

				var query = _query.Queries[_queryNumber];

				if (_queryNumber == 0 && (_dataContext.QueryHints.Count > 0 || _dataContext.NextQueryHints.Count > 0))
				{
					query.QueryHints = new List<string>(_dataContext.QueryHints);
					query.QueryHints.AddRange(_dataContext.NextQueryHints);

					if (clearQueryHints)
						_dataContext.NextQueryHints.Clear();
				}

				SetQuery(query);
			}
		}
	}


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
