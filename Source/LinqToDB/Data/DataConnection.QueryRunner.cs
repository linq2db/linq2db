using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Internal;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.DataProvider;
using LinqToDB.Internal.Infrastructure;
using LinqToDB.Internal.Linq;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Metrics;

namespace LinqToDB.Data
{
	public partial class DataConnection
	{
		IQueryRunner IDataContext.GetQueryRunner(
			Query query,
			IDataContext parametersContext,
			int queryNumber,
			IQueryExpressions expressions,
			object?[]? parameters,
			object?[]? preambles)
		{
			CheckAndThrowOnDisposed();

			return new QueryRunner(query, queryNumber, this, parametersContext, expressions, parameters, preambles);
		}

		internal sealed class QueryRunner : QueryRunnerBase
		{
			public QueryRunner(Query query, int queryNumber, DataConnection dataConnection, IDataContext parametersContext, IQueryExpressions expressions, object?[]? parameters, object?[]? preambles)
				: base(query, queryNumber, dataConnection, parametersContext, expressions, parameters, preambles)
			{
				_dataConnection    = dataConnection;
				_executionScope    = _dataConnection.DataProvider.ExecuteScope(_dataConnection);
			}

			readonly IExecutionScope? _executionScope;
			readonly DataConnection   _dataConnection;
			readonly DateTime         _startedOn = DateTime.UtcNow;
			readonly Stopwatch        _stopwatch = Stopwatch.StartNew();

			bool        _isAsync;

			public override Expression? MapperExpression
			{
				get;
				set
				{
					field = value;

					if (value != null && DataContext.Options.LinqOptions.TraceMapperExpression && _dataConnection.TraceSwitchConnection.TraceInfo)
					{
						_dataConnection.OnTraceConnection(new TraceInfo(_dataConnection, TraceInfoStep.MapperCreated, TraceOperation.BuildMapping, _isAsync)
						{
							TraceLevel       = TraceLevel.Info,
							MapperExpression = MapperExpression,
							StartTime        = _startedOn,
							ExecutionTime    = _stopwatch.Elapsed,
						});
					}
				}
			}

			public override IReadOnlyList<QuerySql> GetSqlText()
			{
				SetCommand(true);

				return GetSqlTextImpl();
			}

			private IReadOnlyList<QuerySql> GetSqlTextImpl()
			{
				var queries = new QuerySql[_executionQuery!.PreparedQuery.Commands.Length];

				for (var index = 0; index < _executionQuery!.PreparedQuery.Commands.Length; index++)
				{
					var queryCommand    = _executionQuery.PreparedQuery.Commands[index];
					var queryParameters = _executionQuery.CommandsParameters[index];

					var parameters = queryParameters == null || queryParameters.Length == 0
						? Array.Empty<DataParameter>()
						: new DataParameter[queryParameters.Length];

					if (queryParameters != null)
					{
						for (var i = 0; i < queryParameters.Length; i++)
						{
							var p            = queryParameters[i];
							var sqlParameter = queryCommand.SqlParameters[i];

							parameters[i] = new DataParameter(p.ParameterName, p.Value, sqlParameter.Type);
						}
					}

					var sql = queryCommand.Command;

					if (index == 0 && _executionQuery.PreparedQuery.QueryHints != null)
					{
						var sqlBuilder = _dataConnection.DataProvider.CreateSqlBuilder(_dataConnection.MappingSchema, _dataConnection.Options);
						sql = sqlBuilder.ApplyQueryHints(sql, _executionQuery.PreparedQuery.QueryHints);
					}

					queries[index] = new QuerySql(sql, parameters);
				}

				return queries;
			}

			public override void Dispose()
			{
				if (_executionScope != null)
					_executionScope.Dispose();

				if (_dataConnection.TraceSwitchConnection.TraceInfo)
				{
					_dataConnection.OnTraceConnection(new TraceInfo(_dataConnection, TraceInfoStep.Completed, TraceOperation.DisposeQuery, _isAsync)
					{
						TraceLevel       = TraceLevel.Info,
						Command          = _dataConnection.CurrentCommand,
						MapperExpression = MapperExpression,
						StartTime        = _startedOn,
						ExecutionTime    = _stopwatch.Elapsed,
						RecordsAffected  = RowsCount,
					});
				}

				base.Dispose();
			}

			public override async ValueTask DisposeAsync()
			{
				if (_executionScope != null)
					await _executionScope.DisposeAsync().ConfigureAwait(false);

				if (_dataConnection.TraceSwitchConnection.TraceInfo)
				{
					_dataConnection.OnTraceConnection(new TraceInfo(_dataConnection, TraceInfoStep.Completed, TraceOperation.DisposeQuery, _isAsync)
					{
						TraceLevel       = TraceLevel.Info,
						Command          = _dataConnection.CurrentCommand,
						MapperExpression = MapperExpression,
						StartTime        = _startedOn,
						ExecutionTime    = _stopwatch.Elapsed,
						RecordsAffected  = RowsCount,
					});
				}

				await base.DisposeAsync().ConfigureAwait(false);
			}

			private sealed record CommandWithParameters(string Command, IReadOnlyList<SqlParameter> SqlParameters);

			private sealed record PreparedQuery(CommandWithParameters[] Commands, SqlStatement Statement, IReadOnlyCollection<string>? QueryHints);

			private sealed record ExecutionPreparedQuery(PreparedQuery PreparedQuery, DbParameter[]?[] CommandsParameters);

			ExecutionPreparedQuery? _executionQuery;

			static ExecutionPreparedQuery CreateExecutionQuery(
				DataConnection            dataConnection,
				IQueryContext             context,
				IReadOnlyParameterValues? parameterValues,
				bool                      forGetSqlText)
			{
				var preparedQuery      = GetCommand(dataConnection, context, parameterValues, forGetSqlText);
				var commandsParameters = GetParameters(dataConnection, preparedQuery, parameterValues);
				var executionQuery     = new ExecutionPreparedQuery(preparedQuery, commandsParameters);
				return executionQuery;
			}

			static PreparedQuery GetCommand(DataConnection dataConnection, IQueryContext query, IReadOnlyParameterValues? parameterValues, bool forGetSqlText, int startIndent = 0)
			{
				bool aquiredLock = false;
				try
				{
					Monitor.Enter(query, ref aquiredLock);

					var statement = query.Statement;
					var options   = query.DataOptions ?? dataConnection.Options;

					if (query.Context is CommandWithParameters[] context)
					{
						return new PreparedQuery(context, statement, dataConnection.GetNextCommandHints(!forGetSqlText));
					}

					var continuousRun = query.IsContinuousRun;

					if (continuousRun)
					{
						// query will not modify statement, release lock
						Monitor.Exit(query);
						aquiredLock = false;
					}

					var sqlOptimizer = dataConnection.DataProvider.GetSqlOptimizer (options);
					var sqlBuilder   = dataConnection.DataProvider.CreateSqlBuilder(dataConnection.MappingSchema, options);
					var factory      = sqlOptimizer.CreateSqlExpressionFactory(dataConnection.MappingSchema, options);

					// custom query handling
					var preprocessContext = new EvaluationContext(parameterValues);
					var newSql            = dataConnection.ProcessQuery(statement, preprocessContext);

					if (!ReferenceEquals(statement, newSql))
					{
						statement                      = newSql;
						statement.IsParameterDependent = true;
					}

					if (!continuousRun)
					{
						if (!statement.IsParameterDependent)
						{
							if (sqlOptimizer.IsParameterDependent(NullabilityContext.NonQuery, dataConnection.MappingSchema, statement, options))
								statement.IsParameterDependent = true;
						}
					}

					using var sb = Pools.StringBuilder.Allocate();

					var optimizeAndConvertAll = !continuousRun && !statement.IsParameterDependent;
					// We can optimize and convert all queries at once, because they are not parameter dependent.

					var optimizeVisitor = sqlOptimizer.CreateOptimizerVisitor(optimizeAndConvertAll);
					var convertVisitor  = sqlOptimizer.CreateConvertVisitor(optimizeAndConvertAll);

					// do not pass parameter values to the evaluation context when optimising whole query.
					var evaluationContext = new EvaluationContext(optimizeAndConvertAll ? null: parameterValues);

					var optimizationContext = new OptimizationContext(evaluationContext, options,
						dataConnection.DataProvider.SqlProviderFlags,
						dataConnection.MappingSchema,
						optimizeVisitor,
						convertVisitor,
						factory,
						dataConnection.DataProvider.SqlProviderFlags.IsParameterOrderDependent,
						isAlreadyOptimizedAndConverted : optimizeAndConvertAll,
						parametersNormalizerFactory : dataConnection.DataProvider.GetQueryParameterNormalizer);

					if (optimizeAndConvertAll)
					{
						var nullability = NullabilityContext.GetContext(statement.SelectQuery);
						statement = optimizationContext.OptimizeAndConvertAll(statement, nullability);
					}

					// correct aliases if needed
					var serviceProvider = ((IInfrastructure<IServiceProvider>)dataConnection.DataProvider).Instance;
					AliasesHelper.PrepareQueryAndAliases(serviceProvider.GetRequiredService<IIdentifierService>(), statement, query.Aliases, out var aliases);

					query.Aliases = aliases;

					var dmlService = serviceProvider.GetRequiredService<IDmlService>();
					var scenario   = dmlService.BuildCommandScenario(statement, dataConnection.DataProvider.SqlProviderFlags, factory);

					CommandWithParameters[] commands;

					if (scenario != null)
					{
						// Provider supplied an explicit scenario: render each step's statement (step 0 is usually the main
						// statement, already optimized/aliased above; synthetic steps get their own aliases).
						commands = new CommandWithParameters[scenario.Steps.Count];

						for (var i = 0; i < scenario.Steps.Count; i++)
						{
							var step        = scenario.Steps[i];
							var stepAliases = ReferenceEquals(step.Statement, statement) ? aliases : PrepareStepAliases(serviceProvider, step.Statement);

							sb.Value.Length = 0;

							using (ActivityService.Start(ActivityID.BuildSql))
							{
								if (step.Fragment == SqlCommandFragment.None)
									sqlBuilder.BuildSql(0, step.Statement, sb.Value, optimizationContext, stepAliases, null, startIndent);
								else
									sqlBuilder.BuildCommandFragment(step.Fragment, step.FragmentFieldIndex, step.Statement, sb.Value, optimizationContext, stepAliases, null, startIndent);
							}

							commands[i] = new CommandWithParameters(sb.Value.ToString(), optimizationContext.GetParameters());
							optimizationContext.ClearParameters();
						}
					}
					else
					{
						// Legacy command-splitting: CommandCount + BuildSql(commandNumber) render N commands from one statement.
						var cc = sqlBuilder.CommandCount(statement);

						commands = new CommandWithParameters[cc];

						for (var i = 0; i < cc; i++)
						{
							sb.Value.Length = 0;

							using (ActivityService.Start(ActivityID.BuildSql))
								sqlBuilder.BuildSql(i, statement, sb.Value, optimizationContext, aliases, null, startIndent);

							commands[i] = new CommandWithParameters(sb.Value.ToString(), optimizationContext.GetParameters());
							optimizationContext.ClearParameters();
						}
					}

					if (optimizeAndConvertAll)
					{
						query.Context = commands;

						// clear aliases, they are not needed after SQL generation.
						//
						query.Aliases = null;
					}

					query.IsContinuousRun = true;

					return new PreparedQuery(commands, statement, dataConnection.GetNextCommandHints(!forGetSqlText));
				}
				finally
				{
					if (aquiredLock)
						Monitor.Exit(query);
				}
			}

			static AliasesContext PrepareStepAliases(IServiceProvider serviceProvider, SqlStatement statement)
			{
				AliasesHelper.PrepareQueryAndAliases(serviceProvider.GetRequiredService<IIdentifierService>(), statement, null, out var aliases);
				return aliases;
			}

			static DbParameter[]?[] GetParameters(DataConnection dataConnection, PreparedQuery pq, IReadOnlyParameterValues? parameterValues)
			{
				var result = new DbParameter[pq.Commands.Length][];

				DbCommand? dbCommand = null;

				for (var index = 0; index < pq.Commands.Length; index++)
				{
					var command = pq.Commands[index];
					if (command.SqlParameters.Count == 0)
						continue;

					var parms = new DbParameter[command.SqlParameters.Count];

					for (var i = 0; i < command.SqlParameters.Count; i++)
					{
						var sqlp = command.SqlParameters[i];

						dbCommand ??= dataConnection.GetOrCreateCommand();

						parms[i] = CreateParameter(dataConnection, dbCommand, sqlp, sqlp.GetParameterValue(parameterValues));
					}

					result[index] = parms;
				}

				return result;
			}

			static DbParameter CreateParameter(DataConnection dataConnection, DbCommand command, SqlParameter parameter, SqlParameterValue parmValue)
			{
				var p          = command.CreateParameter();
				var dbDataType = parmValue.DbDataType;
				var paramValue = parameter.CorrectParameterValue(parmValue.ProviderValue);

				if (dbDataType.DataType == DataType.Undefined)
				{
					var newDataType = dbDataType.SystemType != typeof(object)
							? dataConnection.MappingSchema.GetDbDataType(dbDataType.SystemType).DataType
							: DataType.Undefined;

					if (newDataType == DataType.Undefined && paramValue != null)
						newDataType = dataConnection.MappingSchema.GetDbDataType(paramValue.GetType()).DataType;

					dbDataType = dbDataType.WithDataType(newDataType);
				}

				dataConnection.DataProvider.SetParameter(dataConnection, p, parameter.Name!, dbDataType, paramValue);
				// some providers (e.g. managed sybase provider) could change parameter name
				// which breaks parameters rebind logic
				parameter.Name = p.ParameterName;

				return p;
			}

			protected override void SetQuery(IReadOnlyParameterValues parameterValues, bool forGetSqlText)
			{
				_executionQuery = CreateExecutionQuery(_dataConnection, Query.Queries[QueryNumber], parameterValues, forGetSqlText);
			}

			#region Scenario interpreter

			// One unified value returned by the sequential scenario interpreter; the public execute methods project the
			// field they need (rows-affected for ExecuteNonQuery, scalar for ExecuteScalar).
			readonly record struct ScenarioOutcome(int RowsAffected, object? Scalar);

			// TEMPORARY bridge: builds a sequential scenario from the already-rendered commands (CommandCount/BuildSql).
			// Step i maps 1:1 to command i; the last step is a Scalar when the terminal is a scalar query, all others are
			// non-queries; a non-last command starting with "DROP" is marked Ignore (the historical run-and-swallow).
			// Superseded by the per-provider IDmlService.BuildCommandScenario once providers move off CommandCount/BuildCommand.
			static SqlCommandScenario BuildSequentialScenario(ExecutionPreparedQuery executionQuery, SqlStepKind terminalKind)
			{
				var commands  = executionQuery.PreparedQuery.Commands;
				var count     = commands.Length;
				var statement = executionQuery.PreparedQuery.Statement;
				var steps     = new SqlCommandStep[count];

				for (var i = 0; i < count; i++)
				{
					var isLast = i == count - 1;
					var ignore = !isLast && commands[i].Command.StartsWith("DROP", StringComparison.Ordinal);
					var kind   = terminalKind == SqlStepKind.Scalar && isLast ? SqlStepKind.Scalar : SqlStepKind.NonQuery;

					steps[i] = new SqlCommandStep
					{
						Statement = statement,
						Kind      = kind,
						OnError   = ignore ? SqlStepOnError.Ignore : SqlStepOnError.Throw,
					};
				}

				return new SqlCommandScenario
				{
					Steps        = steps,
					OutcomeSteps = terminalKind == SqlStepKind.Scalar ? new[] { count - 1 } : new[] { 0 },
				};
			}

			static bool EvaluateGate(SqlStepCondition condition, SqlCommandExecutionContext context)
			{
				var value = context.GetResult(condition.SourceStepIndex);

				return condition.Kind switch
				{
					SqlStepConditionKind.ResultIsNull    => value is null,
					SqlStepConditionKind.ResultIsNotNull => value is not null,
					SqlStepConditionKind.ResultIsZero    => value is 0,
					SqlStepConditionKind.ResultIsNonZero => value is not null and not 0,
					_                                    => throw new InvalidOperationException($"Unexpected {nameof(SqlStepConditionKind)}: {condition.Kind}"),
				};
			}

			static object? ResolveOutcome(SqlCommandScenario scenario, SqlCommandExecutionContext context)
			{
				object? outcome = null;

				foreach (var index in scenario.OutcomeSteps)
				{
					if (context.WasExecuted(index))
						outcome = context.GetResult(index);
				}

				return outcome;
			}

			// Sequential interpreter (sync): each step becomes its own command (one round-trip), executed in order; gates
			// pull earlier results from the execution context; the outcome is the last executed candidate. Owns all
			// InitCommand calls.
			static ScenarioOutcome ExecuteScenario(DataConnection dataConnection, ExecutionPreparedQuery executionQuery, SqlCommandScenario scenario)
			{
				var steps        = scenario.Steps;
				var context      = new SqlCommandExecutionContext(steps.Count);
				var rowsAffected = -1;

				for (var i = 0; i < steps.Count; i++)
				{
					var step = steps[i];

					if (step.RunIf is { } gate && !EvaluateGate(gate, context))
						continue;

					InitCommand(dataConnection, executionQuery, i);

					if (step.OnError == SqlStepOnError.Ignore)
					{
						try
						{
							dataConnection.ExecuteNonQuery();
						}
						catch
						{
							// ignore
						}

						continue;
					}

					switch (step.Kind)
					{
						case SqlStepKind.NonQuery:
						{
							var n = dataConnection.ExecuteNonQuery();
							context.SetResult(i, n);
							if (i == 0)
								rowsAffected = n;
							break;
						}
						case SqlStepKind.Scalar:
							context.SetResult(i, dataConnection.ExecuteScalar());
							break;
					}
				}

				return new ScenarioOutcome(rowsAffected, ResolveOutcome(scenario, context));
			}

			// Sequential interpreter (async sibling). In case of change the logic of this method, DO NOT FORGET to change the sibling method.
			static async Task<ScenarioOutcome> ExecuteScenarioAsync(DataConnection dataConnection, ExecutionPreparedQuery executionQuery, SqlCommandScenario scenario, CancellationToken cancellationToken)
			{
				var steps        = scenario.Steps;
				var context      = new SqlCommandExecutionContext(steps.Count);
				var rowsAffected = -1;

				for (var i = 0; i < steps.Count; i++)
				{
					var step = steps[i];

					if (step.RunIf is { } gate && !EvaluateGate(gate, context))
						continue;

					InitCommand(dataConnection, executionQuery, i);

					if (step.OnError == SqlStepOnError.Ignore)
					{
						try
						{
							await dataConnection.ExecuteNonQueryDataAsync(cancellationToken).ConfigureAwait(false);
						}
						catch
						{
							// ignore
						}

						continue;
					}

					switch (step.Kind)
					{
						case SqlStepKind.NonQuery:
						{
							var n = await dataConnection.ExecuteNonQueryDataAsync(cancellationToken).ConfigureAwait(false);
							context.SetResult(i, n);
							if (i == 0)
								rowsAffected = n;
							break;
						}
						case SqlStepKind.Scalar:
							context.SetResult(i, await dataConnection.ExecuteScalarDataAsync(cancellationToken).ConfigureAwait(false));
							break;
					}
				}

				return new ScenarioOutcome(rowsAffected, ResolveOutcome(scenario, context));
			}

			#endregion

			#region ExecuteNonQuery

			// In case of change the logic of this method, DO NOT FORGET to change the sibling method.
			static async Task<int> ExecuteNonQueryImplAsync(
				DataConnection         dataConnection,
				ExecutionPreparedQuery executionQuery,
				CancellationToken      cancellationToken)
			{
				if (executionQuery.PreparedQuery.Commands.Length == 1)
				{
					InitFirstCommand(dataConnection, executionQuery);

					return await dataConnection.ExecuteNonQueryDataAsync(cancellationToken)
						.ConfigureAwait(false);
				}

				return (await ExecuteScenarioAsync(dataConnection, executionQuery, BuildSequentialScenario(executionQuery, SqlStepKind.NonQuery), cancellationToken).ConfigureAwait(false)).RowsAffected;
			}

			// In case of change the logic of this method, DO NOT FORGET to change the sibling method.
			static int ExecuteNonQueryImpl(DataConnection dataConnection, ExecutionPreparedQuery executionQuery)
			{
				if (executionQuery.PreparedQuery.Commands.Length == 1)
				{
					InitFirstCommand(dataConnection, executionQuery);

					return dataConnection.ExecuteNonQuery();
				}

				return ExecuteScenario(dataConnection, executionQuery, BuildSequentialScenario(executionQuery, SqlStepKind.NonQuery)).RowsAffected;
			}

			public override int ExecuteNonQuery()
			{
				SetCommand(false);
				return ExecuteNonQueryImpl(_dataConnection, _executionQuery!);
			}

			// In case of change the logic of this method, DO NOT FORGET to change the sibling method.
			public static async Task<int> ExecuteNonQueryAsync(
				DataConnection            dataConnection,
				IQueryContext             context,
				IReadOnlyParameterValues? parameterValues,
				CancellationToken         cancellationToken)
			{
				var preparedQuery      = GetCommand(dataConnection, context, parameterValues, false);
				var commandsParameters = GetParameters(dataConnection, preparedQuery, parameterValues);
				var executionQuery     = new ExecutionPreparedQuery(preparedQuery, commandsParameters);

				return await ExecuteNonQueryImplAsync(dataConnection, executionQuery, cancellationToken)
					.ConfigureAwait(false);
			}

			// In case of change the logic of this method, DO NOT FORGET to change the sibling method.
			public static int ExecuteNonQuery(DataConnection dataConnection, IQueryContext context, IReadOnlyParameterValues? parameterValues)
			{
				var preparedQuery      = GetCommand(dataConnection, context, parameterValues, false);
				var commandsParameters = GetParameters(dataConnection, preparedQuery, parameterValues);
				var executionQuery     = new ExecutionPreparedQuery(preparedQuery, commandsParameters);

				return ExecuteNonQueryImpl(dataConnection, executionQuery);
			}

			#endregion

			#region ExecuteScalar

			// In case of change the logic of this method, DO NOT FORGET to change the sibling method.
			static async Task<object?> ExecuteScalarImplAsync(
				DataConnection         dataConnection,
				ExecutionPreparedQuery executionQuery,
				CancellationToken      cancellationToken)
			{
				if (executionQuery.PreparedQuery.Commands.Length == 1)
				{
					InitFirstCommand(dataConnection, executionQuery);

					var idParam = GetIdentityParameter(dataConnection, executionQuery);

					if (idParam != null)
					{
						// This is because the firebird provider does not return any parameters via ExecuteReader
						// the rest of the providers must support this mode
						await dataConnection.ExecuteNonQueryDataAsync(cancellationToken).ConfigureAwait(false);

						return idParam.Value;
					}

					return await dataConnection.ExecuteScalarDataAsync(cancellationToken).ConfigureAwait(false);
				}

				return (await ExecuteScenarioAsync(dataConnection, executionQuery, BuildSequentialScenario(executionQuery, SqlStepKind.Scalar), cancellationToken).ConfigureAwait(false)).Scalar;
			}

			// In case of change the logic of this method, DO NOT FORGET to change the sibling method.
			static object? ExecuteScalarImpl(DataConnection dataConnection, ExecutionPreparedQuery executionQuery)
			{
				if (executionQuery.PreparedQuery.Commands.Length == 1)
				{
					InitFirstCommand(dataConnection, executionQuery);

					var idParam = GetIdentityParameter(dataConnection, executionQuery);

					if (idParam != null)
					{
						// This is because the firebird provider does not return any parameters via ExecuteReader
						// the rest of the providers must support this mode
						dataConnection.ExecuteNonQuery();

						return idParam.Value;
					}

					return dataConnection.ExecuteScalar();
				}

				return ExecuteScenario(dataConnection, executionQuery, BuildSequentialScenario(executionQuery, SqlStepKind.Scalar)).Scalar;
			}

			private static DbParameter? GetIdentityParameter(DataConnection dataConnection, ExecutionPreparedQuery executionQuery)
			{
				DbParameter? idParam = null;

				if (dataConnection.DataProvider.SqlProviderFlags.IsIdentityParameterRequired)
				{
					if (executionQuery.PreparedQuery.Statement.NeedsIdentity)
					{
						idParam = dataConnection.CurrentCommand!.CreateParameter();

						idParam.ParameterName = "IDENTITY_PARAMETER";
						idParam.Direction     = ParameterDirection.Output;
						idParam.DbType        = DbType.Decimal;

						dataConnection.CurrentCommand!.Parameters.Add(idParam);
					}
				}

				return idParam;
			}

			// In case of change the logic of this method, DO NOT FORGET to change the sibling method.
			public static Task<object?> ExecuteScalarAsync(
				DataConnection            dataConnection,
				IQueryContext             context,
				IReadOnlyParameterValues? parameterValues,
				CancellationToken         cancellationToken)
			{
				var preparedQuery      = GetCommand(dataConnection, context, parameterValues, false);
				var commandsParameters = GetParameters(dataConnection, preparedQuery, parameterValues);
				var executionQuery     = new ExecutionPreparedQuery(preparedQuery, commandsParameters);

				return ExecuteScalarImplAsync(dataConnection, executionQuery, cancellationToken);
			}

			// In case of change the logic of this method, DO NOT FORGET to change the sibling method.
			public static object? ExecuteScalar(DataConnection dataConnection, IQueryContext context, IReadOnlyParameterValues? parameterValues)
			{
				var preparedQuery      = GetCommand(dataConnection, context, parameterValues, false);
				var commandsParameters = GetParameters(dataConnection, preparedQuery, parameterValues);
				var executionQuery     = new ExecutionPreparedQuery(preparedQuery, commandsParameters);

				return ExecuteScalarImpl(dataConnection, executionQuery);
			}

			public override object? ExecuteScalar()
			{
				SetCommand(false);
				return ExecuteScalarImpl(_dataConnection, _executionQuery!);
			}

			#endregion

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			static void InitFirstCommand(DataConnection dataConnection, ExecutionPreparedQuery executionQuery)
			{
				InitCommand(dataConnection, executionQuery, 0);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			static void InitCommand(DataConnection dataConnection, ExecutionPreparedQuery executionQuery, int index)
			{
				InitCommand(dataConnection,
					executionQuery.PreparedQuery.Commands[index],
					executionQuery.CommandsParameters[index],
					index == 0 ? executionQuery.PreparedQuery.QueryHints : null);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			static void InitCommand(DataConnection dataConnection, CommandWithParameters queryCommand, DbParameter[]? dbParameters, IReadOnlyCollection<string>? queryHints)
			{
				var hasParameters = dbParameters?.Length > 0;

				dataConnection.InitCommand(CommandType.Text, queryCommand.Command, null, queryHints, hasParameters);

				if (hasParameters)
				{
					foreach (var p in dbParameters!)
						dataConnection.CurrentCommand!.Parameters.Add(p);
				}

				dataConnection.CommitCommandInit();
			}

			#region ExecuteReader

			// In case of change the logic of this method, DO NOT FORGET to change the sibling method.
			public static Task<DataReaderWrapper> ExecuteReaderAsync(
				DataConnection            dataConnection,
				IQueryContext             context,
				IReadOnlyParameterValues? parameterValues,
				CancellationToken         cancellationToken)
			{
				var executionQuery = CreateExecutionQuery(dataConnection, context, parameterValues, false);

				InitFirstCommand(dataConnection, executionQuery);

				return dataConnection.ExecuteDataReaderAsync(CommandBehavior.Default, cancellationToken);
			}

			// In case of change the logic of this method, DO NOT FORGET to change the sibling method.
			public static DataReaderWrapper ExecuteReader(DataConnection dataConnection, IQueryContext context, IReadOnlyParameterValues? parameterValues)
			{
				var executionQuery = CreateExecutionQuery(dataConnection, context, parameterValues, false);

				InitFirstCommand(dataConnection, executionQuery);

				return dataConnection.ExecuteDataReader(CommandBehavior.Default);
			}

			public override IDataReaderAsync ExecuteReader()
			{
				SetCommand(false);

				InitFirstCommand(_dataConnection, _executionQuery!);

				var dataReader = _dataConnection.ExecuteDataReader(CommandBehavior.Default);

				return new DataReaderAsync(dataReader);
			}

			#endregion

			sealed class DataReaderAsync : IDataReaderAsync
			{
				readonly DataReaderWrapper _dataReader;

				public DataReaderAsync(DataReaderWrapper dataReader)
				{
					_dataReader = dataReader;
				}

				DbDataReader IDataReaderAsync.DataReader => _dataReader.DataReader!;

				bool IDataReaderAsync.Read() => _dataReader.DataReader!.Read();

				Task<bool> IDataReaderAsync.ReadAsync(CancellationToken cancellationToken) => _dataReader.DataReader!.ReadAsync(cancellationToken);

				void IDisposable.Dispose() => _dataReader.Dispose();

				ValueTask IAsyncDisposable.DisposeAsync() => _dataReader.DisposeAsync();
			}

			public override async Task<IDataReaderAsync> ExecuteReaderAsync(CancellationToken cancellationToken)
			{
				_isAsync = true;

				SetCommand(false);

				InitFirstCommand(_dataConnection, _executionQuery!);

				var dataReader = await _dataConnection.ExecuteDataReaderAsync(CommandBehavior.Default, cancellationToken).ConfigureAwait(false);

				return new DataReaderAsync(dataReader);
			}

			public override async Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
			{
				_isAsync = true;

				SetCommand(false);

				if (_executionQuery!.PreparedQuery.Commands.Length == 1)
				{
					InitFirstCommand(_dataConnection, _executionQuery);

					return await _dataConnection.ExecuteNonQueryDataAsync(cancellationToken).ConfigureAwait(false);
				}

				return (await ExecuteScenarioAsync(_dataConnection, _executionQuery!, BuildSequentialScenario(_executionQuery!, SqlStepKind.NonQuery), cancellationToken).ConfigureAwait(false)).RowsAffected;
			}

			public override async Task<object?> ExecuteScalarAsync(CancellationToken cancellationToken)
			{
				_isAsync = true;

				SetCommand(false);

				return await ExecuteScalarImplAsync(_dataConnection, _executionQuery!, cancellationToken).ConfigureAwait(false);
			}
		}
	}
}
