using System;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

#if !NET6_0_OR_GREATER
using System.Text;
#endif

namespace LinqToDB.Remote
{
	using Common.Internal;
	using Data;
	using Linq;
	using DataProvider;
	using SqlProvider;
	using SqlQuery;

	public abstract partial class RemoteDataContextBase
	{
		IQueryRunner IDataContext.GetQueryRunner(Query query, IDataContext parametersContext, int queryNumber, Expression expression, object?[]? parameters, object?[]? preambles)
		{
			ThrowOnDisposed();
			return new QueryRunner(query, queryNumber, this, parametersContext, expression, parameters, preambles);
		}

		sealed class QueryRunner : QueryRunnerBase
		{
			public QueryRunner(Query query, int queryNumber, RemoteDataContextBase dataContext, IDataContext parametersContext, Expression expression, object?[]? parameters, object?[]? preambles)
				: base(query, queryNumber, dataContext, parametersContext, expression, parameters, preambles)
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

				for (var i = 0; i < cc; i++)
				{
					AliasesHelper.PrepareQueryAndAliases(new IdentifierServiceSimple(128), query.Statement, query.Aliases, out var aliases);

					var optimizationContext = new OptimizationContext(
						_evaluationContext,
						DataContext.Options,
						DataContext.SqlProviderFlags,
						DataContext.MappingSchema,
						sqlOptimizer.CreateOptimizerVisitor(false),
						sqlOptimizer.CreateConvertVisitor(false),
						isParameterOrderDepended : false,
						isAlreadyOptimizedAndConverted : true,
						static () => NoopQueryParametersNormalizer.Instance);

					var statement = sqlOptimizer.PrepareStatementForSql(query.Statement, DataContext.MappingSchema, DataContext.Options, optimizationContext);

					sqlBuilder.BuildSql(i, statement, sqlStringBuilder.Value, optimizationContext, aliases);

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
						var sqlParameters = optimizationContext.GetParameters();
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

							value = value switch
							{
								string str => FormattableString.Invariant($"'{str.Replace("'", "''")}'"),
								char chr   => FormattableString.Invariant($"'{(chr == '\'' ? "''" : chr)}'"),
								_ => value
							};

							sb.Value.AppendLine(CultureInfo.InvariantCulture, $"-- SET {p.Name} = {value}");
						}

						sb.Value.AppendLine();
					}

#if NET6_0_OR_GREATER
					sb.Value.Append(sqlStringBuilder.Value);
#else
					sb.Value.Append(sqlStringBuilder.Value.ToString());
#endif
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

			public override async ValueTask DisposeAsync()
			{
				if (_client is IAsyncDisposable asyncDisposable)
					await asyncDisposable.DisposeAsync().ConfigureAwait(false);
				else if (_client is IDisposable disposable)
					disposable.Dispose();

				await base.DisposeAsync().ConfigureAwait(false);
			}

			public override int ExecuteNonQuery()
			{
				SetCommand(false);

				var queryContext = Query.Queries[QueryNumber];

				var q = _dataContext.GetSqlOptimizer(_dataContext.Options).PrepareStatementForRemoting(
					queryContext.Statement, _dataContext.MappingSchema, _dataContext.Options, _evaluationContext);

				var data = LinqServiceSerializer.Serialize(
					_dataContext.SerializationMappingSchema,
					q,
					_evaluationContext.ParameterValues,
					_dataContext.GetNextCommandHints(true),
					_dataContext.Options);

				if (_dataContext._batchCounter > 0)
				{
					_dataContext._queryBatch!.Add(data);
					return -1;
				}

				_client = _dataContext.GetClient();

				return _client.ExecuteNonQuery(_dataContext.ConfigurationString, data);
			}

			public override object? ExecuteScalar()
			{
				if (_dataContext._batchCounter > 0)
					throw new LinqException("Incompatible batch operation.");

				SetCommand(false);

				var queryContext = Query.Queries[QueryNumber];

				var sqlOptimizer = _dataContext.GetSqlOptimizer(_dataContext.Options);
				var q            = sqlOptimizer.PrepareStatementForRemoting(queryContext.Statement, _dataContext.MappingSchema, _dataContext.Options, _evaluationContext);

				var data = LinqServiceSerializer.Serialize(
					_dataContext.SerializationMappingSchema,
					q,
					_evaluationContext.ParameterValues,
					_dataContext.GetNextCommandHints(true),
					_dataContext.Options);

				_client = _dataContext.GetClient();

				var ret = _client.ExecuteScalar(_dataContext.ConfigurationString, data);

				object? result = null;
				if (ret != null)
				{
					var lsr = LinqServiceSerializer.DeserializeResult(_dataContext.SerializationMappingSchema, _dataContext.MappingSchema, _dataContext.Options, ret);
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

				var q = _dataContext.GetSqlOptimizer(_dataContext.Options).PrepareStatementForRemoting(queryContext.Statement, _dataContext.MappingSchema, _dataContext.Options, _evaluationContext);

				var data = LinqServiceSerializer.Serialize(
					_dataContext.SerializationMappingSchema,
					q,
					_evaluationContext.ParameterValues,
					_dataContext.GetNextCommandHints(true),
					_dataContext.Options);

				_client = _dataContext.GetClient();

				var ret = _client.ExecuteReader(_dataContext.ConfigurationString, data);

				var result = LinqServiceSerializer.DeserializeResult(_dataContext.SerializationMappingSchema, _dataContext.MappingSchema, _dataContext.Options, ret);

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
					cancellationToken.ThrowIfCancellationRequested();

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

				public ValueTask DisposeAsync()
				{
					DataReader.Dispose();
					return default;
				}
			}

			public override async Task<IDataReaderAsync> ExecuteReaderAsync(CancellationToken cancellationToken)
			{
				if (_dataContext._batchCounter > 0)
					throw new LinqException("Incompatible batch operation.");

				// preload _configurationInfo asynchronously if needed
				await _dataContext.GetConfigurationInfoAsync(cancellationToken).ConfigureAwait(false);

				SetCommand(false);

				var queryContext = Query.Queries[QueryNumber];

				var q = _dataContext.GetSqlOptimizer(_dataContext.Options).PrepareStatementForRemoting(queryContext.Statement, _dataContext.MappingSchema, _dataContext.Options, _evaluationContext);

				var data = LinqServiceSerializer.Serialize(
					_dataContext.SerializationMappingSchema,
					q,
					_evaluationContext.ParameterValues,
					_dataContext.GetNextCommandHints(true),
					_dataContext.Options);

				_client = _dataContext.GetClient();

				var ret = await _client.ExecuteReaderAsync(_dataContext.ConfigurationString, data, cancellationToken).ConfigureAwait(false);

				var result = LinqServiceSerializer.DeserializeResult(_dataContext.SerializationMappingSchema, _dataContext.MappingSchema, _dataContext.Options, ret);
				var reader = new RemoteDataReader(_dataContext.SerializationMappingSchema, result);

				return new DataReaderAsync(reader);
			}

			public override async Task<object?> ExecuteScalarAsync(CancellationToken cancellationToken)
			{
				if (_dataContext._batchCounter > 0)
					throw new LinqException("Incompatible batch operation.");

				// preload _configurationInfo asynchronously if needed
				await _dataContext.GetConfigurationInfoAsync(cancellationToken).ConfigureAwait(false);

				SetCommand(false);

				var queryContext = Query.Queries[QueryNumber];

				var q = _dataContext.GetSqlOptimizer(_dataContext.Options).PrepareStatementForRemoting(queryContext.Statement, _dataContext.MappingSchema, _dataContext.Options, _evaluationContext);

				var data = LinqServiceSerializer.Serialize(
					_dataContext.SerializationMappingSchema,
					q,
					_evaluationContext.ParameterValues,
					_dataContext.GetNextCommandHints(true),
					_dataContext.Options);

				_client = _dataContext.GetClient();

				var ret = await _client.ExecuteScalarAsync(_dataContext.ConfigurationString, data, cancellationToken)
					.ConfigureAwait(false);

				object? result = null;
				if (ret != null)
				{
					var lsr = LinqServiceSerializer.DeserializeResult(_dataContext.SerializationMappingSchema, _dataContext.MappingSchema, _dataContext.Options, ret);
					var value = lsr.Data[0][0];
					result = SerializationConverter.Deserialize(_dataContext.SerializationMappingSchema, lsr.FieldTypes[0], value);
				}

				return result;
			}

			public override async Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
			{
				// preload _configurationInfo asynchronously if needed
				await _dataContext.GetConfigurationInfoAsync(cancellationToken).ConfigureAwait(false);

				SetCommand(false);

				var queryContext = Query.Queries[QueryNumber];

				var q = _dataContext.GetSqlOptimizer(_dataContext.Options).PrepareStatementForRemoting(queryContext.Statement, _dataContext.MappingSchema, _dataContext.Options, _evaluationContext);

				var data = LinqServiceSerializer.Serialize(
					_dataContext.SerializationMappingSchema,
					q,
					_evaluationContext.ParameterValues,
					_dataContext.GetNextCommandHints(true),
					_dataContext.Options);

				if (_dataContext._batchCounter > 0)
				{
					_dataContext._queryBatch!.Add(data);
					return -1;
				}

				_client = _dataContext.GetClient();

				return await _client.ExecuteNonQueryAsync(_dataContext.ConfigurationString, data, cancellationToken)
					.ConfigureAwait(false);
			}
		}
	}
}
