using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB.Expressions;
using LinqToDB.Extensions;
using LinqToDB.SqlQuery;

namespace LinqToDB.ServiceModel
{
	using Linq;

	public abstract partial class RemoteDataContextBase
	{
		Func<IDataContext,Expression,object[],int,IEnumerable<IDataReader>> GetQuery<T>(Query<T> queryT)
		{
			queryT.FinalizeQuery();

			if (queryT.Queries.Count != 1)
				throw new InvalidOperationException();

			Func<IDataContext,Expression,object[],int,IEnumerable<IDataReader>> query =
				(dataContext, expr, parameters, queryNumber) => RunQuery(queryT, dataContext, expr, parameters, queryNumber);

			var select = queryT.Queries[0].SelectQuery.Select;

			if (select.SkipValue != null && !queryT.SqlProviderFlags.GetIsSkipSupportedFlag(queryT.Queries[0].SelectQuery))
			{
				var q = query;

				var value = select.SkipValue as SqlValue;
				if (value != null)
				{
					var n = (int)((IValueContainer)select.SkipValue).Value;

					if (n > 0)
						query = (db, expr, ps, qn) => q(db, expr, ps, qn).Skip(n);
				}
				else if (select.SkipValue is SqlParameter)
				{
					var i = GetParameterIndex(queryT, select.SkipValue);
					query = (db, expr, ps, qn) => q(db, expr, ps, qn).Skip((int)queryT.Queries[0].Parameters[i].Accessor(expr, ps));
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
						query = (db, expr, ps, qn) => q(db, expr, ps, qn).Take(n);
				}
				else if (select.TakeValue is SqlParameter)
				{
					var i = GetParameterIndex(queryT, select.TakeValue);
					query = (db, expr, ps, qn) => q(db, expr, ps, qn).Take((int)queryT.Queries[0].Parameters[i].Accessor(expr, ps));
				}
			}

			return query;
		}

		internal void SetParameters<T>(Query<T> queryT, Expression expr, object[] parameters, int idx)
		{
			foreach (var p in queryT.Queries[idx].Parameters)
			{
				var value = p.Accessor(expr, parameters);

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
									value = queryT.GetConvertedEnum(valueType, value);
							}

							values.Add(value);
						}

						value = values;
					}
				}

				p.SqlParameter.Value = value;

				var dataType = p.DataTypeAccessor(expr, parameters);

				if (dataType != DataType.Undefined)
					p.SqlParameter.DataType = dataType;
			}
		}


		QueryContext SetCommand<T>(Query<T> queryT, IDataContext dataContext, Expression expr, object[] parameters, int idx, bool clearQueryHints)
		{
			lock (this)
			{
				SetParameters(queryT, expr, parameters, idx);

				var query = queryT.Queries[idx];

				if (idx == 0 && (dataContext.QueryHints.Count > 0 || dataContext.NextQueryHints.Count > 0))
				{
					query.QueryHints = new List<string>(dataContext.QueryHints);
					query.QueryHints.AddRange(dataContext.NextQueryHints);

					if (clearQueryHints)
						dataContext.NextQueryHints.Clear();
				}

				return new QueryContext { Query = query };
			}
		}

		IEnumerable<IDataReader> RunQuery<T>(Query<T> queryT, IDataContext dataContext, Expression expr, object[] parameters, int queryNumber)
		{
			QueryContext query = null;

			try
			{
				query = SetCommand(queryT, dataContext, expr, parameters, queryNumber, true);

				using (var dr = dataContext.ExecuteReader(query))
					while (dr.Read())
						yield return dr;
			}
			finally
			{
				if (query != null && query.Client != null)
					((IDisposable)query.Client).Dispose();

				if (dataContext.CloseAfterUse)
					dataContext.Close();
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

		class MapInfo<T>
		{
			public MapInfo([JetBrains.Annotations.NotNull] Expression<Func<Linq.QueryContext,IDataContext,IDataReader,Expression,object[],T>> expression)
			{
				if (expression == null)
					throw new ArgumentNullException("expression");
				Expression = expression;
			}

			[JetBrains.Annotations.NotNull]
			public readonly Expression<Func<Linq.QueryContext,IDataContext,IDataReader,Expression,object[],T>> Expression;

			public            Func<Linq.QueryContext,IDataContext,IDataReader,Expression,object[],T>  Mapper;
			public Expression<Func<Linq.QueryContext,IDataContext,IDataReader,Expression,object[],T>> MapperExpression;
		}

		static IEnumerable<T> Map<T>(
			IEnumerable<IDataReader> data,
			Linq.QueryContext        queryContext,
			IDataContext             dataContext,
			Expression               expr,
			object[]                 ps,
			MapInfo<T>               mapInfo)
		{
			if (queryContext == null)
				queryContext = new Linq.QueryContext(dataContext, expr, ps);

			var isFaulted = false;

			foreach (var dr in data)
			{
				var mapper = mapInfo.Mapper;

				if (mapper == null)
				{
					mapInfo.MapperExpression = mapInfo.Expression.Transform(e =>
					{
						var ex = e as ConvertFromDataReaderExpression;
						return ex != null ? ex.Reduce(dr) : e;
					}) as Expression<Func<Linq.QueryContext,IDataContext,IDataReader,Expression,object[],T>>;

					// IT : # MapperExpression.Compile()
					//
					Debug.Assert(mapInfo.MapperExpression != null, "mapInfo.MapperExpression != null");
					mapInfo.Mapper = mapper = mapInfo.MapperExpression.Compile();
				}

				T result;

				try
				{
					result = mapper(queryContext, dataContext, dr, expr, ps);
				}
				catch (FormatException)
				{
					if (isFaulted)
						throw;

					isFaulted = true;

					mapInfo.Mapper = mapInfo.Expression.Compile();
					result = mapInfo.Mapper(queryContext, dataContext, dr, expr, ps);
				}
				catch (InvalidCastException)
				{
					if (isFaulted)
						throw;

					isFaulted = true;

					mapInfo.Mapper = mapInfo.Expression.Compile();
					result = mapInfo.Mapper(queryContext, dataContext, dr, expr, ps);
				}

				yield return result;
			}
		}

		internal void SetRunQuery<T>(Query<T> queryT, Expression<Func<Linq.QueryContext,IDataContext,IDataReader,Expression,object[],T>> expression)
		{
			var query   = GetQuery(queryT);
			var mapInfo = new MapInfo<T>(expression);

			foreach (var q in queryT.Queries)
				foreach (var sqlParameter in q.Parameters)
					sqlParameter.Expression = null;

			queryT.GetIEnumerable = (ctx,db,expr,ps) => Map<T>(query(db, expr, ps, 0), ctx, db, expr, ps, mapInfo);
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

			QueryContext _query;

			public override void Dispose()
			{
				var disposable = _client as IDisposable;
				if (disposable != null)
					disposable.Dispose();

				if (_query != null && _query.Client != null)
					((IDisposable)_query.Client).Dispose();

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
				_query = (QueryContext)Query.SetCommand(DataContext, Expression, Parameters, QueryNumber, true);

				if (_dataContext._batchCounter > 0)
					throw new LinqException("Incompatible batch operation.");

				var ctx = _query;

				ctx.Client = _dataContext.GetClient();

				var q      = ctx.Query.SelectQuery.ProcessParameters(_dataContext.MappingSchema);
				var ret    = ctx.Client.ExecuteReader(
					_dataContext.Configuration,
					LinqServiceSerializer.Serialize(q, q.IsParameterDependent ? q.Parameters.ToArray() : ctx.Query.GetParameters(), ctx.Query.QueryHints));
				var result = LinqServiceSerializer.DeserializeResult(ret);

				return new ServiceModelDataReader(_dataContext.MappingSchema, result);
			}

			/*
			public override IDataReader ExecuteReader()
			{
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
			*/

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
