using System;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.ServiceModel
{
	using Linq;
	using Common.Internal;
	using SqlQuery;
	using SqlProvider;
	using Tools;
	using LinqToDB.Data;
	using System.Data.Common;

	public abstract partial class RemoteDataContextBase
	{
		IQueryRunner IDataContext.GetQueryRunner(Query query, int queryNumber, Expression expression, object?[]? parameters, object?[]? preambles)
		{
			ThrowOnDisposed();
			return new QueryRunner(query, queryNumber, this, expression, parameters, preambles);
		}

		class QueryRunner : QueryRunnerBase
		{
			public QueryRunner(Query query, int queryNumber, RemoteDataContextBase dataContext, Expression expression, object?[]? parameters, object?[]? preambles)
				: base(query, queryNumber, dataContext, expression, parameters, preambles)
			{
				_dataContext = dataContext;
			}

			readonly RemoteDataContextBase _dataContext;

			ILinqClient? _client;
			EvaluationContext _evaluationContext = null!;

			public override Expression? MapperExpression { get; set; }

			protected override void SetQuery(IReadOnlyParameterValues parameterValues)
			{
				_evaluationContext = new EvaluationContext(parameterValues);
			}

			#region GetSqlText

			public override string GetSqlText()
			{
				SetCommand(false);

				var sb = new StringBuilder();
				var query = Query.Queries[QueryNumber];
				var sqlBuilder   = DataContext.CreateSqlProvider();
				var sqlOptimizer = DataContext.GetSqlOptimizer();
				var sqlStringBuilder = new StringBuilder();
				var cc = sqlBuilder.CommandCount(query.Statement);

				var optimizationContext = new OptimizationContext(_evaluationContext, query.Aliases!, false);

				for (var i = 0; i < cc; i++)
				{
					var statement = sqlOptimizer.PrepareStatementForSql(query.Statement, DataContext.MappingSchema, optimizationContext);
					sqlBuilder.BuildSql(i, statement, sqlStringBuilder, optimizationContext);

					if (i == 0 && query.QueryHints != null && query.QueryHints.Count > 0)
					{
						var sql = sqlStringBuilder.ToString();

						sql = sqlBuilder.ApplyQueryHints(sql, query.QueryHints);

						sqlStringBuilder.Append(sql);
					}

					sb
						.Append("-- ")
						.Append("ServiceModel")
						.Append(' ')
						.Append(DataContext.ContextID)
						.Append(' ')
						.Append(sqlBuilder.Name)
						.AppendLine();

					if (optimizationContext.HasParameters())
					{
						var sqlParameters = optimizationContext.GetParameters().ToList();
						foreach (var p in sqlParameters)
						{
							var parameterValue = p.GetParameterValue(_evaluationContext.ParameterValues);

							var value = parameterValue.Value;

							sb
								.Append("-- DECLARE ")
								.Append(p.Name)
								.Append(' ')
								.Append(value == null ? parameterValue.DbDataType.SystemType.ToString() : value.GetType().Name)
								.AppendLine();
						}

						sb.AppendLine();

						foreach (var p in sqlParameters)
						{
							var parameterValue = p.GetParameterValue(_evaluationContext.ParameterValues);

							var value = parameterValue.Value;

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

					sb.Append(sqlStringBuilder);
					sqlStringBuilder.Length = 0;
				}


				return sb.ToString();
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

				SetCommand(true);

				var queryContext = Query.Queries[QueryNumber];

				var q = _dataContext.GetSqlOptimizer().PrepareStatementForRemoting(queryContext.Statement,
					_dataContext.MappingSchema, queryContext.Aliases!, _evaluationContext);

				data = LinqServiceSerializer.Serialize(
					_dataContext.SerializationMappingSchema,
					q,
					_evaluationContext.ParameterValues,
					QueryHints);

				if (_dataContext._batchCounter > 0)
				{
					_dataContext._queryBatch!.Add(data);
					return -1;
				}

				_client = _dataContext.GetClient();

				return _client.ExecuteNonQuery(_dataContext.Configuration, data);
			}

			public override object? ExecuteScalar()
			{
				if (_dataContext._batchCounter > 0)
					throw new LinqException("Incompatible batch operation.");

				string data;

				SetCommand(true);

				var queryContext = Query.Queries[QueryNumber];

				var sqlOptimizer = _dataContext.GetSqlOptimizer();
				var q = sqlOptimizer.PrepareStatementForRemoting(queryContext.Statement, _dataContext.MappingSchema, queryContext.Aliases!, _evaluationContext);

				data = LinqServiceSerializer.Serialize(
					_dataContext.SerializationMappingSchema,
					q,
					_evaluationContext.ParameterValues, QueryHints);

				_client = _dataContext.GetClient();

				return _client.ExecuteScalar(_dataContext.Configuration, data);
			}

			public override DataReaderWrapper ExecuteReader()
			{
				_dataContext.ThrowOnDisposed();

				if (_dataContext._batchCounter > 0)
					throw new LinqException("Incompatible batch operation.");

				string data;

				SetCommand(true);

				var queryContext = Query.Queries[QueryNumber];

				var q = _dataContext.GetSqlOptimizer().PrepareStatementForRemoting(queryContext.Statement, _dataContext.MappingSchema, queryContext.Aliases!, _evaluationContext);

				data = LinqServiceSerializer.Serialize(
					_dataContext.SerializationMappingSchema,
					q,
					_evaluationContext.ParameterValues,
					QueryHints);

				_client = _dataContext.GetClient();

				var ret = _client.ExecuteReader(_dataContext.Configuration, data);

				var result = LinqServiceSerializer.DeserializeResult(_dataContext.SerializationMappingSchema, ret);

				return new DataReaderWrapper(new ServiceModelDataReader(_dataContext.SerializationMappingSchema, result));
			}

			class DataReaderAsync : IDataReaderAsync
			{
				public DataReaderAsync(ServiceModelDataReader dataReader)
				{
					DataReader = dataReader;
				}

				public DbDataReader DataReader { get; }

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
						return DataReader.Read() ? TaskCache.True : TaskCache.False;
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
#if NETSTANDARD2_1PLUS
				public ValueTask DisposeAsync()
				{
					return DataReader.DisposeAsync();
				}
#elif !NETFRAMEWORK
				public ValueTask DisposeAsync()
				{
					Dispose();
					return new ValueTask(Task.CompletedTask);
				}
#endif
			}

			public override async Task<IDataReaderAsync> ExecuteReaderAsync(CancellationToken cancellationToken)
			{
				if (_dataContext._batchCounter > 0)
					throw new LinqException("Incompatible batch operation.");

				string data;

				SetCommand(true);

				var queryContext = Query.Queries[QueryNumber];

				var q = _dataContext.GetSqlOptimizer().PrepareStatementForRemoting(queryContext.Statement, _dataContext.MappingSchema, queryContext.Aliases!, _evaluationContext);

				data = LinqServiceSerializer.Serialize(
					_dataContext.SerializationMappingSchema,
					q,
					_evaluationContext.ParameterValues,
					QueryHints);

				_client = _dataContext.GetClient();

				var ret = await _client.ExecuteReaderAsync(_dataContext.Configuration, data).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

				var result = LinqServiceSerializer.DeserializeResult(_dataContext.SerializationMappingSchema, ret);
				var reader = new ServiceModelDataReader(_dataContext.SerializationMappingSchema, result);

				return new DataReaderAsync(reader);
			}

			public override Task<object?> ExecuteScalarAsync(CancellationToken cancellationToken)
			{
				if (_dataContext._batchCounter > 0)
					throw new LinqException("Incompatible batch operation.");

				string data;

				SetCommand(true);

				var queryContext = Query.Queries[QueryNumber];

				var q = _dataContext.GetSqlOptimizer().PrepareStatementForRemoting(queryContext.Statement, _dataContext.MappingSchema, queryContext.Aliases!, _evaluationContext);

				data = LinqServiceSerializer.Serialize(
					_dataContext.SerializationMappingSchema,
					q,
					_evaluationContext.ParameterValues,
					QueryHints);

				_client = _dataContext.GetClient();

				return _client.ExecuteScalarAsync(_dataContext.Configuration, data);
			}

			public override Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
			{
				string data;

				SetCommand(true);

				var queryContext = Query.Queries[QueryNumber];

				var q = _dataContext.GetSqlOptimizer().PrepareStatementForRemoting(queryContext.Statement, _dataContext.MappingSchema, queryContext.Aliases!, _evaluationContext);

				data = LinqServiceSerializer.Serialize(
					_dataContext.SerializationMappingSchema,
					q,
					_evaluationContext.ParameterValues,
					QueryHints);

				if (_dataContext._batchCounter > 0)
				{
					_dataContext._queryBatch!.Add(data);
					return TaskCache.MinusOne;
				}

				_client = _dataContext.GetClient();

				return _client.ExecuteNonQueryAsync(_dataContext.Configuration, data);
			}
		}
	}
}
