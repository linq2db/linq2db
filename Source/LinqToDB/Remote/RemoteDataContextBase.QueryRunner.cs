﻿using System;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.Remote
{
	using System.Data.Common;
	using Common.Internal;
	using Linq;
	using LinqToDB.Data;
	using SqlProvider;
	using SqlQuery;
#if !NATIVE_ASYNC
	using Tools;
#endif

	public abstract partial class RemoteDataContextBase
	{
		IQueryRunner IDataContext.GetQueryRunner(Query query, int queryNumber, Expression expression, object?[]? parameters, object?[]? preambles)
		{
			ThrowOnDisposed();
			return new QueryRunner(query, queryNumber, this, expression, parameters, preambles);
		}

		sealed class QueryRunner : QueryRunnerBase
		{
			public QueryRunner(Query query, int queryNumber, RemoteDataContextBase dataContext, Expression expression, object?[]? parameters, object?[]? preambles)
				: base(query, queryNumber, dataContext, expression, parameters, preambles)
			{
				_dataContext = dataContext;
			}

			readonly RemoteDataContextBase _dataContext;

			ILinqService?     _client;
			EvaluationContext _evaluationContext = null!;

			public override Expression? MapperExpression { get; set; }

			protected override void SetQuery(IReadOnlyParameterValues parameterValues, bool forGetSqlText)
			{
				_evaluationContext = new EvaluationContext(parameterValues);
			}

#region GetSqlText

			public override string GetSqlText()
			{
				SetCommand(true);

				using var sb               = Pools.StringBuilder.Allocate();
				var query                  = Query.Queries[QueryNumber];
				var sqlBuilder             = DataContext.CreateSqlProvider();
				var sqlOptimizer           = DataContext.GetSqlOptimizer(DataContext.Options);
				using var sqlStringBuilder = Pools.StringBuilder.Allocate();
				var cc                     = sqlBuilder.CommandCount(query.Statement);

				var optimizationContext = new OptimizationContext(_evaluationContext, query.Aliases!, false);

				for (var i = 0; i < cc; i++)
				{
					var statement = sqlOptimizer.PrepareStatementForSql(query.Statement, DataContext.MappingSchema, DataContext.Options, optimizationContext);
					sqlBuilder.BuildSql(i, statement, sqlStringBuilder.Value, optimizationContext);

					if (i == 0)
					{
						var queryHints = DataContext.GetNextCommandHints(false);
						if (queryHints != null)
						{
							var sql = sqlStringBuilder.Value.ToString();

							sql = sqlBuilder.ApplyQueryHints(sql, queryHints);

							sqlStringBuilder.Value.Append(sql);
						}
					}

					sb.Value
						.Append("-- ")
						.Append("ServiceModel")
						.Append(' ')
						.Append(DataContext.ContextName)
						.Append(' ')
						.Append(sqlBuilder.Name)
						.AppendLine();

					if (optimizationContext.HasParameters())
					{
						var sqlParameters = optimizationContext.GetParameters().ToList();
						foreach (var p in sqlParameters)
						{
							var parameterValue = p.GetParameterValue(_evaluationContext.ParameterValues);

							var value = parameterValue.ProviderValue;

							sb.Value
								.Append("-- DECLARE ")
								.Append(p.Name)
								.Append(' ')
								.Append(value == null ? parameterValue.DbDataType.SystemType.ToString() : value.GetType().Name)
								.AppendLine();
						}

						sb.Value.AppendLine();

						foreach (var p in sqlParameters)
						{
							var parameterValue = p.GetParameterValue(_evaluationContext.ParameterValues);

							var value = parameterValue.ProviderValue;

							if(value != null)
								if (value is string || value is char)
									value = "'" + value!.ToString()!.Replace("'", "''") + "'";

							sb.Value
								.Append("-- SET ")
								.Append(p.Name)
								.Append(" = ")
								.Append(value)
								.AppendLine();
						}

						sb.Value.AppendLine();
					}

					sb.Value.Append(sqlStringBuilder.Value);
					sqlStringBuilder.Value.Length = 0;
				}


				return sb.Value.ToString();
			}

#endregion

			public override void Dispose()
			{
				if (_client is IDisposable disposable)
					disposable.Dispose();

				base.Dispose();
			}

#if !NATIVE_ASYNC
			public override async Task DisposeAsync()
			{
				if (_client is IDisposable disposable)
					disposable.Dispose();

				await base.DisposeAsync().ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
			}
#else
			public override async ValueTask DisposeAsync()
			{
				if (_client is IAsyncDisposable asyncDisposable)
					await asyncDisposable.DisposeAsync().ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
				else if (_client is IDisposable disposable)
					disposable.Dispose();

				await base.DisposeAsync().ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
			}
#endif

			public override int ExecuteNonQuery()
			{
				SetCommand(false);

				var queryContext = Query.Queries[QueryNumber];

				var q = _dataContext.GetSqlOptimizer(_dataContext.Options).PrepareStatementForRemoting(
					queryContext.Statement, _dataContext.MappingSchema, _dataContext.Options, queryContext.Aliases!, _evaluationContext);

				var data = LinqServiceSerializer.Serialize(
					_dataContext.SerializationMappingSchema,
					q,
					_evaluationContext.ParameterValues,
					_dataContext.GetNextCommandHints(true));

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

				SetCommand(false);

				var queryContext = Query.Queries[QueryNumber];

				var sqlOptimizer = _dataContext.GetSqlOptimizer(_dataContext.Options);
				var q            = sqlOptimizer.PrepareStatementForRemoting(queryContext.Statement, _dataContext.MappingSchema, _dataContext.Options, queryContext.Aliases!, _evaluationContext);

				var data = LinqServiceSerializer.Serialize(
					_dataContext.SerializationMappingSchema,
					q,
					_evaluationContext.ParameterValues,
					_dataContext.GetNextCommandHints(true));

				_client = _dataContext.GetClient();

				var ret = _client.ExecuteScalar(_dataContext.Configuration, data);

				object? result = null;
				if (ret != null)
				{
					var lsr = LinqServiceSerializer.DeserializeResult(_dataContext.SerializationMappingSchema, ret);
					var value = lsr.Data[0][0];

					if (!string.IsNullOrEmpty(value))
					{
						result = SerializationConverter.Deserialize(_dataContext.SerializationMappingSchema, lsr.FieldTypes[0], value);
					}
				}

				return result;
			}

			public override DataReaderWrapper ExecuteReader()
			{
				_dataContext.ThrowOnDisposed();

				if (_dataContext._batchCounter > 0)
					throw new LinqException("Incompatible batch operation.");

				SetCommand(false);

				var queryContext = Query.Queries[QueryNumber];

				var q = _dataContext.GetSqlOptimizer(_dataContext.Options).PrepareStatementForRemoting(queryContext.Statement, _dataContext.MappingSchema, _dataContext.Options, queryContext.Aliases!, _evaluationContext);

				var data = LinqServiceSerializer.Serialize(
					_dataContext.SerializationMappingSchema,
					q,
					_evaluationContext.ParameterValues,
					_dataContext.GetNextCommandHints(true));

				_client = _dataContext.GetClient();

				var ret = _client.ExecuteReader(_dataContext.Configuration, data);

				var result = LinqServiceSerializer.DeserializeResult(_dataContext.SerializationMappingSchema, ret);

				return new DataReaderWrapper(new RemoteDataReader(_dataContext.SerializationMappingSchema, result));
			}

			sealed class DataReaderAsync : IDataReaderAsync
			{
				public DataReaderAsync(RemoteDataReader dataReader)
				{
					DataReader = dataReader;
				}

				public DbDataReader DataReader { get; }

				public Task<bool> ReadAsync(CancellationToken cancellationToken)
				{
					if (cancellationToken.IsCancellationRequested)
					{
						var task = new TaskCompletionSource<bool>();
#if NET6_0_OR_GREATER
						task.SetCanceled(cancellationToken);
#else
						task.SetCanceled();
#endif
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

#if !NATIVE_ASYNC
				public Task DisposeAsync()
				{
					DataReader.Dispose();
					return TaskEx.CompletedTask;
				}
#else
				public ValueTask DisposeAsync()
				{
					DataReader.Dispose();
					return default;
				}
#endif
			}

			public override async Task<IDataReaderAsync> ExecuteReaderAsync(CancellationToken cancellationToken)
			{
				if (_dataContext._batchCounter > 0)
					throw new LinqException("Incompatible batch operation.");

				// preload _configurationInfo asynchronously if needed
				await _dataContext.GetConfigurationInfoAsync(cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

				SetCommand(false);

				var queryContext = Query.Queries[QueryNumber];

				var q = _dataContext.GetSqlOptimizer(_dataContext.Options).PrepareStatementForRemoting(queryContext.Statement, _dataContext.MappingSchema, _dataContext.Options, queryContext.Aliases!, _evaluationContext);

				var data = LinqServiceSerializer.Serialize(
					_dataContext.SerializationMappingSchema,
					q,
					_evaluationContext.ParameterValues,
					_dataContext.GetNextCommandHints(true));

				_client = _dataContext.GetClient();

				var ret = await _client.ExecuteReaderAsync(_dataContext.Configuration, data, cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

				var result = LinqServiceSerializer.DeserializeResult(_dataContext.SerializationMappingSchema, ret);
				var reader = new RemoteDataReader(_dataContext.SerializationMappingSchema, result);

				return new DataReaderAsync(reader);
			}

			public override async Task<object?> ExecuteScalarAsync(CancellationToken cancellationToken)
			{
				if (_dataContext._batchCounter > 0)
					throw new LinqException("Incompatible batch operation.");

				// preload _configurationInfo asynchronously if needed
				await _dataContext.GetConfigurationInfoAsync(cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

				SetCommand(false);

				var queryContext = Query.Queries[QueryNumber];

				var q = _dataContext.GetSqlOptimizer(_dataContext.Options).PrepareStatementForRemoting(queryContext.Statement, _dataContext.MappingSchema, _dataContext.Options, queryContext.Aliases!, _evaluationContext);

				var data = LinqServiceSerializer.Serialize(
					_dataContext.SerializationMappingSchema,
					q,
					_evaluationContext.ParameterValues,
					_dataContext.GetNextCommandHints(true));

				_client = _dataContext.GetClient();

				var ret = await _client.ExecuteScalarAsync(_dataContext.Configuration, data, cancellationToken)
					.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

				object? result = null;
				if (ret != null)
				{
					var lsr = LinqServiceSerializer.DeserializeResult(_dataContext.SerializationMappingSchema, ret);
					var value = lsr.Data[0][0];
					result = SerializationConverter.Deserialize(_dataContext.SerializationMappingSchema, lsr.FieldTypes[0], value);
				}

				return result;
			}

			public override async Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
			{
				// preload _configurationInfo asynchronously if needed
				await _dataContext.GetConfigurationInfoAsync(cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

				SetCommand(false);

				var queryContext = Query.Queries[QueryNumber];

				var q = _dataContext.GetSqlOptimizer(_dataContext.Options).PrepareStatementForRemoting(queryContext.Statement, _dataContext.MappingSchema, _dataContext.Options, queryContext.Aliases!, _evaluationContext);

				var data = LinqServiceSerializer.Serialize(
					_dataContext.SerializationMappingSchema,
					q,
					_evaluationContext.ParameterValues,
					_dataContext.GetNextCommandHints(true));

				if (_dataContext._batchCounter > 0)
				{
					_dataContext._queryBatch!.Add(data);
					return -1;
				}

				_client = _dataContext.GetClient();

				return await _client.ExecuteNonQueryAsync(_dataContext.Configuration, data, cancellationToken)
					.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
			}
		}
	}
}
