using System;
using System.Data;
using System.Linq.Expressions;
using System.Text;
using System.Threading;

#if !NOASYNC
using System.Threading.Tasks;
#endif

namespace LinqToDB.ServiceModel
{
	using Linq;

	public abstract partial class RemoteDataContextBase
	{
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

			ILinqClient _client;

			public override Expression MapperExpression { get; set; }

			protected override void SetQuery()
			{
			}

			#region GetSqlText

			public override string GetSqlText()
			{
				SetCommand(false);

				var query      = Query.Queries[QueryNumber];
				var sqlBuilder = DataContext.CreateSqlProvider();
				var sb         = new StringBuilder();

				sb
					.Append("-- ")
					.Append("ServiceModel")
					.Append(' ')
					.Append(DataContext.ContextID)
					.Append(' ')
					.Append(sqlBuilder.Name)
					.AppendLine();

				if (query.SelectQuery.Parameters != null && query.SelectQuery.Parameters.Count > 0)
				{
					foreach (var p in query.SelectQuery.Parameters)
					{
						var value = p.Value;

						sb
							.Append("-- DECLARE ")
							.Append(p.Name)
							.Append(' ')
							.Append(value == null ? p.SystemType.ToString() : value.GetType().Name)
							.AppendLine();
					}

					sb.AppendLine();

					foreach (var p in query.SelectQuery.Parameters)
					{
						var value = p.Value;

						if (value is string || value is char)
							value = "'" + value.ToString().Replace("'", "''") + "'";

						sb
							.Append("-- SET ")
							.Append(p.Name)
							.Append(" = ")
							.Append(value)
							.AppendLine();
					}

					sb.AppendLine();
				}

				var cc = sqlBuilder.CommandCount(query.SelectQuery);

				for (var i = 0; i < cc; i++)
				{
					sqlBuilder.BuildSql(i, query.SelectQuery, sb);

					if (i == 0 && query.QueryHints != null && query.QueryHints.Count > 0)
					{
						var sql = sb.ToString();

						sql = sqlBuilder.ApplyQueryHints(sql, query.QueryHints);

						sb = new StringBuilder(sql);
					}
				}

				return sb.ToString();
			}

			#endregion

			public override void Dispose()
			{
				var disposable = _client as IDisposable;
				if (disposable != null)
					disposable.Dispose();

				base.Dispose();
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

#if !NOASYNC

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

				public async Task QueryForEachAsync<T>(Func<IDataReader,T> objectReader, Func<T,bool> action, CancellationToken cancellationToken)
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
									if (!action(objectReader(reader)))
										return;
						}
					},
					cancellationToken);
				}
			}

			public override async Task<IDataReaderAsync> ExecuteReaderAsync(CancellationToken cancellationToken)
			{
				if (_dataContext._batchCounter > 0)
					throw new LinqException("Incompatible batch operation.");

				SetCommand(true);

				var queryContext = Query.Queries[QueryNumber];

				_client = _dataContext.GetClient();

				var q   = queryContext.SelectQuery.ProcessParameters(_dataContext.MappingSchema);
				var ret = await _client.ExecuteReaderAsync(
					_dataContext.Configuration,
					LinqServiceSerializer.Serialize(
						q,
						q.IsParameterDependent ? q.Parameters.ToArray() : queryContext.GetParameters(),
						QueryHints));

				return new DataReaderAsync(_dataContext, ret, SkipAction, TakeAction);
			}

			public override async Task<object> ExecuteScalarAsync(CancellationToken cancellationToken)
			{
				SetCommand(true);

				if (_dataContext._batchCounter > 0)
					throw new LinqException("Incompatible batch operation.");

				var queryContext = Query.Queries[QueryNumber];

				_client = _dataContext.GetClient();

				var q = queryContext.SelectQuery.ProcessParameters(_dataContext.MappingSchema);

				return await _client.ExecuteScalarAsync(
					_dataContext.Configuration,
					LinqServiceSerializer.Serialize(
						q,
						q.IsParameterDependent ? q.Parameters.ToArray() : queryContext.GetParameters(), QueryHints));
			}

			public override async Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
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

				return await _client.ExecuteNonQueryAsync(_dataContext.Configuration, data);
			}

#endif
		}
	}
}
