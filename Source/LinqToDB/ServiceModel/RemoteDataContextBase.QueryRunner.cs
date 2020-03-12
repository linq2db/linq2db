#nullable disable
using System;
using System.Data;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.ServiceModel
{
	using Linq;
	using SqlProvider;

	public abstract partial class RemoteDataContextBase
	{
		IQueryRunner IDataContext.GetQueryRunner(Query query, int queryNumber, Expression expression, object[] parameters)
		{
			ThrowOnDisposed();
			return new QueryRunner(query, queryNumber, this, expression, parameters);
		}

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
				lock (Query)
				{
					SetCommand(false);

					var query = Query.Queries[QueryNumber];
					var sqlBuilder = DataContext.CreateSqlProvider();
					var sb = new StringBuilder();

					sb
						.Append("-- ")
						.Append("ServiceModel")
						.Append(' ')
						.Append(DataContext.ContextID)
						.Append(' ')
						.Append(sqlBuilder.Name)
						.AppendLine();

					if (query.Statement.Parameters != null && query.Statement.Parameters.Count > 0)
					{
						foreach (var p in query.Statement.Parameters)
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

						foreach (var p in query.Statement.Parameters)
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

					var cc = sqlBuilder.CommandCount(query.Statement);

					for (var i = 0; i < cc; i++)
					{
						sqlBuilder.BuildSql(i, query.Statement, sb);

						if (i == 0 && query.QueryHints != null && query.QueryHints.Count > 0)
						{
							var sql = sb.ToString();

							sql = sqlBuilder.ApplyQueryHints(sql, query.QueryHints);

							sb = new StringBuilder(sql);
						}
					}

					return sb.ToString();
				}
			}

			#endregion

			public override void Dispose()
			{
				if (_client is IDisposable disposable)
					disposable.Dispose();

				base.Dispose();
			}

			public override int ExecuteNonQuery()
			{
				string data;

				// locks are bad, m'kay?
				lock (Query)
				{
					SetCommand(true);

					var queryContext = Query.Queries[QueryNumber];

					var q = _dataContext.GetSqlOptimizer().OptimizeStatement(queryContext.Statement, _dataContext.MappingSchema, _dataContext.InlineParameters);

					data = LinqServiceSerializer.Serialize(
						_dataContext.SerializationMappingSchema,
						q,
						q.IsParameterDependent ? q.Parameters.ToArray() : queryContext.GetParameters(),
						QueryHints);
				}

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
				if (_dataContext._batchCounter > 0)
					throw new LinqException("Incompatible batch operation.");

				string data;

				lock (Query)
				{
					SetCommand(true);

					var queryContext = Query.Queries[QueryNumber];

					var q = _dataContext.GetSqlOptimizer().OptimizeStatement(queryContext.Statement, _dataContext.MappingSchema, _dataContext.InlineParameters);

					data = LinqServiceSerializer.Serialize(
						_dataContext.SerializationMappingSchema,
						q,
						q.IsParameterDependent ? q.Parameters.ToArray() : queryContext.GetParameters(), QueryHints);
				}

				_client = _dataContext.GetClient();

				return _client.ExecuteScalar(_dataContext.Configuration, data);
			}

			public override IDataReader ExecuteReader()
			{
				_dataContext.ThrowOnDisposed();

				if (_dataContext._batchCounter > 0)
					throw new LinqException("Incompatible batch operation.");

				string data;

				lock (Query)
				{
					SetCommand(true);

					var queryContext = Query.Queries[QueryNumber];

					var q = _dataContext.GetSqlOptimizer().OptimizeStatement(queryContext.Statement, _dataContext.MappingSchema, _dataContext.InlineParameters);

					data = LinqServiceSerializer.Serialize(
						_dataContext.SerializationMappingSchema,
						q,
						q.IsParameterDependent ? q.Parameters.ToArray() : queryContext.GetParameters(),
						QueryHints);
				}

				_client = _dataContext.GetClient();

				var ret = _client.ExecuteReader(_dataContext.Configuration, data);

				var result = LinqServiceSerializer.DeserializeResult(_dataContext.SerializationMappingSchema, ret);

				return new ServiceModelDataReader(_dataContext.SerializationMappingSchema, result);
			}

			class DataReaderAsync : IDataReaderAsync
			{
				public DataReaderAsync(ServiceModelDataReader dataReader)
				{
					DataReader = dataReader;
				}

				public IDataReader DataReader { get; }

				static Task<bool> _trueTask;
				static Task<bool> _falseTask;

				static Task<bool> TrueTask  => _trueTask  ?? (_trueTask  = Task.FromResult(true));
				static Task<bool> FalseTask => _falseTask ?? (_falseTask = Task.FromResult(false));

				public Task<bool> ReadAsync(CancellationToken cancellationToken)
				{
					if (cancellationToken.IsCancellationRequested)
					{
						var task = new TaskCompletionSource<bool>();
						task.SetCanceled();
						return task.Task;
					}

					try
					{
						return DataReader.Read() ? TrueTask : FalseTask;
					}
					catch (Exception ex)
					{
						var task = new TaskCompletionSource<bool>();
						task.SetException(ex);
						return task.Task;
					}
				}

				public void Dispose()
				{
					DataReader.Dispose();
				}
			}

			public override async Task<IDataReaderAsync> ExecuteReaderAsync(CancellationToken cancellationToken)
			{
				if (_dataContext._batchCounter > 0)
					throw new LinqException("Incompatible batch operation.");

				string data;

				lock (Query)
				{
					SetCommand(true);

					var queryContext = Query.Queries[QueryNumber];

					var q = _dataContext.GetSqlOptimizer().OptimizeStatement(queryContext.Statement, _dataContext.MappingSchema, _dataContext.InlineParameters);

					data = LinqServiceSerializer.Serialize(
						_dataContext.SerializationMappingSchema,
						q,
						q.IsParameterDependent ? q.Parameters.ToArray() : queryContext.GetParameters(),
						QueryHints);
				}

				_client = _dataContext.GetClient();

				var ret = await _client.ExecuteReaderAsync(_dataContext.Configuration, data).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

				var result = LinqServiceSerializer.DeserializeResult(_dataContext.SerializationMappingSchema, ret);
				var reader = new ServiceModelDataReader(_dataContext.SerializationMappingSchema, result);

				return new DataReaderAsync(reader);
			}

			public override Task<object> ExecuteScalarAsync(CancellationToken cancellationToken)
			{
				if (_dataContext._batchCounter > 0)
					throw new LinqException("Incompatible batch operation.");

				string data;

				lock (Query)
				{
					SetCommand(true);

					var queryContext = Query.Queries[QueryNumber];

					var q = _dataContext.GetSqlOptimizer().OptimizeStatement(queryContext.Statement, _dataContext.MappingSchema, _dataContext.InlineParameters);

					data = LinqServiceSerializer.Serialize(
						_dataContext.SerializationMappingSchema,
						q,
						q.IsParameterDependent ? q.Parameters.ToArray() : queryContext.GetParameters(), QueryHints);
				}

				_client = _dataContext.GetClient();

				return _client.ExecuteScalarAsync(_dataContext.Configuration, data);
			}

			public override Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
			{
				string data;

				lock (Query)
				{
					SetCommand(true);

					var queryContext = Query.Queries[QueryNumber];

					var q = _dataContext.GetSqlOptimizer().OptimizeStatement(queryContext.Statement, _dataContext.MappingSchema, _dataContext.InlineParameters);
					data = LinqServiceSerializer.Serialize(
						_dataContext.SerializationMappingSchema,
						q,
						q.IsParameterDependent ? q.Parameters.ToArray() : queryContext.GetParameters(),
						QueryHints);
				}

				if (_dataContext._batchCounter > 0)
				{
					_dataContext._queryBatch.Add(data);
					return Task.FromResult(-1);
				}

				_client = _dataContext.GetClient();

				return _client.ExecuteNonQueryAsync(_dataContext.Configuration, data);
			}
		}
	}
}
