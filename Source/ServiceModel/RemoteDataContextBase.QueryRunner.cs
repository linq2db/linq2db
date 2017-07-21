using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.ServiceModel
{
	using Extensions;
	using Linq;
	using SqlQuery;

	public abstract partial class RemoteDataContextBase
	{
		IQueryRunner1 IDataContextEx.GetQueryRun(Query query, int queryNumber, Expression expression, object[] parameters)
		{
			return new QueryRun(query, this, expression, parameters, queryNumber, true);
			//return new QueryRun2(query, this, expression, parameters, queryNumber);
		}

		Func<Linq.QueryContext,Query<T>.Mapper,IDataContext,Expression,object[],int,IEnumerable<T>> GetQuery<T>(
			Query<T> queryT,
			Func<Linq.QueryContext,Query<T>.Mapper,IDataContext,Expression,object[],int,IEnumerable<T>> query)
		{
			queryT.FinalizeQuery();

			if (queryT.Queries.Count != 1)
				throw new InvalidOperationException();

			var select = queryT.Queries[0].SelectQuery.Select;

			if (select.SkipValue != null && !queryT.SqlProviderFlags.GetIsSkipSupportedFlag(queryT.Queries[0].SelectQuery))
			{
				var q = query;

				var value = select.SkipValue as SqlValue;
				if (value != null)
				{
					var n = (int)((IValueContainer)select.SkipValue).Value;

					if (n > 0)
						query = (ctx, qm, db, expr, ps, qn) => q(ctx, qm, db, expr, ps, qn).Skip(n);
				}
				else if (select.SkipValue is SqlParameter)
				{
					var i = GetParameterIndex(queryT, select.SkipValue);
					query = (ctx, qm, db, expr, ps, qn) => q(ctx, qm, db, expr, ps, qn).Skip((int)queryT.Queries[0].Parameters[i].Accessor(expr, ps));
				}
			}

			if (select.TakeValue != null && !queryT.SqlProviderFlags.IsTakeSupported)
			{
				var q = query;

				var value = select.TakeValue as SqlValue;
				if (value != null)
				{
					var n = (int)((IValueContainer)select.TakeValue).Value;

					if (n > 0)
						query = (ctx, qm, db, expr, ps, qn) => q(ctx, qm, db, expr, ps, qn).Take(n);
				}
				else if (select.TakeValue is SqlParameter)
				{
					var i = GetParameterIndex(queryT, select.TakeValue);
					query = (ctx, qm, db, expr, ps, qn) => q(ctx, qm, db, expr, ps, qn).Take((int)queryT.Queries[0].Parameters[i].Accessor(expr, ps));
				}
			}

			return query;
		}

		internal class QueryRun : IQueryRunner1
		{
			public QueryRun(
				Query        query,
				IDataContext dataContext,
				Expression   expression,
				object[]     parameters,
				int          queryNumber,
				bool         clearQueryHints)
			{
				_query           = query;
				_dataContext     = dataContext;
				_expression      = expression;
				_parameters      = parameters;
				_queryNumber     = queryNumber;
				_clearQueryHints = clearQueryHints;
			}

			readonly Query        _query;
			readonly IDataContext _dataContext;

			readonly Expression   _expression;
			readonly object[]     _parameters;
			readonly int          _queryNumber;
			readonly bool         _clearQueryHints;

			ILinqService _client;

			public void Dispose()
			{
				if (_client != null)
					((IDisposable)_client).Dispose();

				if (_dataContext.CloseAfterUse)
					_dataContext.Close();
			}

			public IDataReader ExecuteReader()
			{
				((RemoteDataContextBase)_dataContext).ThrowOnDisposed();

				SetCommand(_clearQueryHints);

				if (((RemoteDataContextBase)_dataContext)._batchCounter > 0)
					throw new LinqException("Incompatible batch operation.");

				var query = _query.Queries[_queryNumber];

				_client = ((RemoteDataContextBase)_dataContext).GetClient();

				var q      = query.SelectQuery.ProcessParameters(((RemoteDataContextBase)_dataContext).MappingSchema);
				var ret    = _client.ExecuteReader(
					((RemoteDataContextBase)_dataContext).Configuration,
					LinqServiceSerializer.Serialize(q, q.IsParameterDependent ? q.Parameters.ToArray() : query.GetParameters(), query.QueryHints));
				var result = LinqServiceSerializer.DeserializeResult(ret);

				return new ServiceModelDataReader(((RemoteDataContextBase)_dataContext).MappingSchema, result);
			}

			public Expression MapperExpression { get; set; }
			public int RowsCount { get; set; }

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

			void SetQuery(QueryInfo queryInfo)
			{
			}

			void SetCommand(bool clearQueryHints)
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

		internal class QueryRun2 : QueryRunBase
		{
			public QueryRun2(
				Query query,
				IDataContext dataContext,
				Expression expression,
				object[] parameters,
				int queryNumber)
				: base(query, dataContext, expression, parameters, queryNumber)
			{
			}

			public override Expression MapperExpression { get; set; }

			ILinqService _client;

			public override void Dispose()
			{
				if (_client != null)
					((IDisposable)_client).Dispose();

				base.Dispose();
			}

			public override IDataReader ExecuteReader()
			{
				((RemoteDataContextBase)_dataContext).ThrowOnDisposed();

				SetCommand(true);

				if (((RemoteDataContextBase)_dataContext)._batchCounter > 0)
					throw new LinqException("Incompatible batch operation.");

				var query = _query.Queries[_queryNumber];

				_client = ((RemoteDataContextBase)_dataContext).GetClient();

				var q      = query.SelectQuery.ProcessParameters(((RemoteDataContextBase)_dataContext).MappingSchema);
				var ret    = _client.ExecuteReader(
					((RemoteDataContextBase)_dataContext).Configuration,
					LinqServiceSerializer.Serialize(q, q.IsParameterDependent ? q.Parameters.ToArray() : query.GetParameters(), query.QueryHints));
				var result = LinqServiceSerializer.DeserializeResult(ret);

				return new ServiceModelDataReader(((RemoteDataContextBase)_dataContext).MappingSchema, result);
			}

			protected override void SetQuery(QueryInfo queryInfo)
			{
			}
		}

		IEnumerable<T> RunQuery<T>(
			Linq.QueryContext queryContext,
			Query<T>.Mapper   qmapper,
			Query             queryT,
			IDataContext      dataContext,
			Expression        expr,
			object[]          parameters,
			int               queryNumber)
		{
			if (queryContext == null)
				queryContext = new Linq.QueryContext(dataContext, expr, parameters);

			using (var qr = new QueryRun(queryT, dataContext, expr, parameters, queryNumber, true))
			using (var dr = qr.ExecuteReader())
			{
				while (dr.Read())
				{
					yield return qmapper.Map(queryContext, dataContext, dr, expr, parameters);
				}
			}
		}

		int GetParameterIndex<T>(Query<T> queryT, ISqlExpression parameter)
		{
			for (var i = 0; i < queryT.Queries[0].Parameters.Count; i++)
			{
				var p = queryT.Queries[0].Parameters[i].SqlParameter;

				if (p == parameter)
					return i;
			}

			throw new InvalidOperationException();
		}

		internal void SetRunQuery<T>(Query<T> queryT,
			Expression<Func<Linq.QueryContext,IDataContext,IDataReader,Expression,object[],T>> expression,
			Query<T>.Mapper mapper)
		{
			var query = GetQuery<T>(queryT, (ctx, qm, dataContext, expr, parameters, queryNumber) => RunQuery(ctx, qm, queryT, dataContext, expr, parameters, queryNumber));

			foreach (var q in queryT.Queries)
				foreach (var sqlParameter in q.Parameters)
					sqlParameter.Expression = null;

			queryT.GetIEnumerable = (ctx,db,expr,ps) => query(ctx, mapper, db, expr, ps, 0);
		}

		IQueryRunner IDataContextEx.GetQueryRunner(Query query, int queryNumber, Expression expression, object[] parameters)
		{
			ThrowOnDisposed();
			return new QueryRunner(query, queryNumber, this, expression, parameters);
		}

		// IT : QueryRunner - RemoteDataContextBase
		//
		class QueryRunner : QueryRunnerBase
		{
			public QueryRunner(Query query, int queryNumber, RemoteDataContextBase dataContext, Expression expression, object[] parameters)
				: base(query, queryNumber, dataContext, expression, parameters)
			{
				_dataContext = dataContext;
			}

			readonly RemoteDataContextBase _dataContext;

			ILinqService _client;

			public override Expression MapperExpression { get; set; }

			protected override void SetQuery()
			{
			}

			public override void Dispose()
			{
				var disposable = _client as IDisposable;
				if (disposable != null)
					disposable.Dispose();

				if (DataContext.CloseAfterUse)
					DataContext.Close();
			}

			public override int ExecuteNonQuery()
			{
				SetCommand(true);

				var queryContext = Query.Queries[QueryNumber];

				var q    = queryContext.SelectQuery.ProcessParameters(_dataContext.MappingSchema);
				var data = LinqServiceSerializer.Serialize(
					q,
					q.IsParameterDependent ? q.Parameters.ToArray() : queryContext.GetParameters(),
					QueryHints);

				if (_dataContext._batchCounter > 0)
				{
					_dataContext._queryBatch.Add(data);
					return -1;
				}

				_client = _dataContext.GetClient();

				return _client.ExecuteNonQuery(_dataContext.Configuration, data);
			}

			public override object ExecuteScalar()
			{
				SetCommand(true);

				if (_dataContext._batchCounter > 0)
					throw new LinqException("Incompatible batch operation.");

				var queryContext = Query.Queries[QueryNumber];

				_client = _dataContext.GetClient();

				var q = queryContext.SelectQuery.ProcessParameters(_dataContext.MappingSchema);

				return _client.ExecuteScalar(
					_dataContext.Configuration,
					LinqServiceSerializer.Serialize(
						q,
						q.IsParameterDependent ? q.Parameters.ToArray() : queryContext.GetParameters(), QueryHints));
			}

			public override IDataReader ExecuteReader()
			{
				_dataContext.ThrowOnDisposed();

				SetCommand(true);

				if (_dataContext._batchCounter > 0)
					throw new LinqException("Incompatible batch operation.");

				var queryContext = Query.Queries[QueryNumber];

				_client = _dataContext.GetClient();

				var q   = queryContext.SelectQuery.ProcessParameters(_dataContext.MappingSchema);
				var ret = _client.ExecuteReader(
					_dataContext.Configuration,
					LinqServiceSerializer.Serialize(
						q,
						q.IsParameterDependent ? q.Parameters.ToArray() : queryContext.GetParameters(),
						QueryHints));

				var result = LinqServiceSerializer.DeserializeResult(ret);

				return new ServiceModelDataReader(_dataContext.MappingSchema, result);
			}

			class DataReaderAsync : IDataReaderAsync
			{
				public DataReaderAsync(RemoteDataContextBase dataContext, string result, Func<int> skipAction, Func<int> takeAction)
				{
					_dataContext = dataContext;
					_result      = result;
					_skipAction  = skipAction;
					_takeAction  = takeAction;
				}

				readonly RemoteDataContextBase _dataContext;
				readonly string                _result;
				readonly Func<int>             _skipAction;
				readonly Func<int>             _takeAction;

				public async Task QueryForEachAsync<T>(Func<IDataReader,T> objectReader, Action<T> action, CancellationToken cancellationToken)
				{
					_dataContext.ThrowOnDisposed();

					await Task.Run(() =>
					{
						var result = LinqServiceSerializer.DeserializeResult(_result);

						using (var reader = new ServiceModelDataReader(_dataContext.MappingSchema, result))
						{
							var skip = _skipAction == null ? 0 : _skipAction();

							while (skip-- > 0 && reader.Read())
								if (cancellationToken.IsCancellationRequested)
									return;

							var take = _takeAction == null ? int.MaxValue : _takeAction();
				
							while (take-- > 0 && reader.Read())
								if (cancellationToken.IsCancellationRequested)
									return;
								else
									action(objectReader(reader));
						}
					},
					cancellationToken);
				}
			}

			public override async Task<IDataReaderAsync> ExecuteReaderAsync(CancellationToken cancellationToken, TaskCreationOptions options)
			{
				if (_dataContext._batchCounter > 0)
					throw new LinqException("Incompatible batch operation.");

				SetCommand(true);

				return await Task.Run(() =>
				{
					var queryContext = Query.Queries[QueryNumber];

					_client = _dataContext.GetClient();

					var q   = queryContext.SelectQuery.ProcessParameters(_dataContext.MappingSchema);
					var ret = _client.ExecuteReader(
						_dataContext.Configuration,
						LinqServiceSerializer.Serialize(
							q,
							q.IsParameterDependent ? q.Parameters.ToArray() : queryContext.GetParameters(),
							QueryHints));

					return new DataReaderAsync(_dataContext, ret, SkipAction, TakeAction);
				},
				cancellationToken);
			}
		}
	}
}
