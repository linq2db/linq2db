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
using LinqToDB.Mapping;
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

			// Cached in query.Context on first compile so a re-run reuses both the rendered commands and the logical
			// scenario (the interpreter needs the real scenario for combined grouping / forwarding — the bridge can't
			// re-infer those).
			private sealed record CachedScenario(CommandWithParameters[] Commands, SqlCommandScenario? Scenario, SqlCommandGroupPlan? Plan);

			private sealed record PreparedQuery(CommandWithParameters[] Commands, SqlStatement Statement, IReadOnlyCollection<string>? QueryHints, SqlCommandScenario? Scenario, SqlCommandGroupPlan? Plan);

			private sealed record ExecutionPreparedQuery(PreparedQuery PreparedQuery, DbParameter[]?[] CommandsParameters, IReadOnlyParameterValues? ParameterValues);

			ExecutionPreparedQuery? _executionQuery;

			static ExecutionPreparedQuery CreateExecutionQuery(
				DataConnection            dataConnection,
				IQueryContext             context,
				IReadOnlyParameterValues? parameterValues,
				bool                      forGetSqlText)
			{
				var preparedQuery      = GetCommand(dataConnection, context, parameterValues, forGetSqlText);
				var commandsParameters = GetParameters(dataConnection, preparedQuery, parameterValues);
				var executionQuery     = new ExecutionPreparedQuery(preparedQuery, commandsParameters, parameterValues);
				return executionQuery;
			}

			// Finalizes the DML service's synthetic scenario steps (identity SELECT, InsertOrReplace/Upsert emulation).
			// They are built from the already-finalized main statement, so they never went through provider finalization
			// (FinalizeStatement) — most importantly the UPDATE rewrite some providers need (e.g. SqlCe cannot parse a
			// table alias in UPDATE). Steps that reuse the main statement (e.g. truncate reseed) are already finalized and
			// are skipped by reference; the scenario is rebuilt only if a step actually changed.
			static PreparedQuery GetCommand(DataConnection dataConnection, IQueryContext query, IReadOnlyParameterValues? parameterValues, bool forGetSqlText, int startIndent = 0)
			{
				bool aquiredLock = false;
				try
				{
					Monitor.Enter(query, ref aquiredLock);

					var statement = query.Statement;
					var options   = query.DataOptions ?? dataConnection.Options;

					if (query.Context is CachedScenario cached)
					{
						return new PreparedQuery(cached.Commands, statement, dataConnection.GetNextCommandHints(!forGetSqlText), cached.Scenario, cached.Plan);
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
					SqlCommandGroupPlan?    plan;

					if (scenario != null)
					{
						// The DML service builds synthetic step statements (identity SELECT, the InsertOrReplace/Upsert
						// UPDATE/INSERT/SELECT emulation) after the main statement was finalized, so they miss provider
						// finalization — run it now so per-provider transforms apply (e.g. SqlCe rewrites its UPDATE into the
						// alias-free form it accepts). Steps reusing the already-finalized main statement are skipped.
						scenario = ScenarioCommandRenderer.FinalizeScenarioSteps(scenario, statement, sqlOptimizer, options, dataConnection.MappingSchema);

						// Plan FIRST: group the scenario's steps into physical commands (round-trips), size-aware.
						plan = dmlService.PlanScenario(scenario, dataConnection.DataProvider.SqlProviderFlags);

						// Render each group as one command (shared with the remote GetSqlText path).
						commands = ScenarioCommandRenderer.RenderScenarioGroups(scenario, plan, statement, aliases, sqlBuilder, optimizationContext, serviceProvider, sb.Value, startIndent);
					}
					else
					{
						// BuildCommandScenario always returns a scenario now (DmlServiceBase supplies a default single-step
						// one), so this is unreachable — a custom IDmlService returning null is a contract violation.
						throw new InvalidOperationException($"'{dmlService.GetType().Name}.BuildCommandScenario' returned null; it must always produce a scenario.");
					}

					if (optimizeAndConvertAll)
					{
						query.Context = new CachedScenario(commands, scenario, plan);

						// clear aliases, they are not needed after SQL generation.
						//
						query.Aliases = null;
					}

					query.IsContinuousRun = true;

					return new PreparedQuery(commands, statement, dataConnection.GetNextCommandHints(!forGetSqlText), scenario, plan);
				}
				finally
				{
					if (aquiredLock)
						Monitor.Exit(query);
				}
			}

			// Adds an output parameter to the current command for a step that produces its value via an OUT parameter
			// (Oracle/Firebird identity). The step's context result becomes the parameter's value after execution.
			static DbParameter AddOutputParameter(DataConnection dataConnection, SqlCommandStep step)
			{
				var p = dataConnection.CurrentCommand!.CreateParameter();

				p.ParameterName = step.OutParameterName!;
				p.Direction     = ParameterDirection.Output;
				p.DbType        = step.OutParameterDbType;

				dataConnection.CurrentCommand!.Parameters.Add(p);

				return p;
			}

			// Forwarding: bind each of the step's forwarded parameters (its value comes from an earlier step's result,
			// not the client) on the already-initialized command. The SqlParameter AST node is immutable/shared, so we
			// locate its per-execution DbParameter by position (its index in the command's SqlParameter list) rather
			// than by name (which providers may normalize).
			static void ApplyForwardedParameters(ExecutionPreparedQuery executionQuery, int commandIndex, SqlCommandStep step, SqlCommandExecutionContext context)
			{
				if (step.ParameterBindings.Count == 0)
					return;

				var sqlParameters = executionQuery.PreparedQuery.Commands[commandIndex].SqlParameters;
				var dbParameters  = executionQuery.CommandsParameters[commandIndex];

				if (dbParameters == null)
					return;

				foreach (var binding in step.ParameterBindings)
				{
					var index = -1;

					for (var k = 0; k < sqlParameters.Count; k++)
					{
						if (ReferenceEquals(sqlParameters[k], binding.Target))
						{
							index = k;
							break;
						}
					}

					if (index >= 0 && index < dbParameters.Length)
						dbParameters[index].Value = context.GetResult(binding.SourceStepIndex) ?? DBNull.Value;
				}
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

			internal static DbParameter CreateParameter(DataConnection dataConnection, DbCommand command, SqlParameter parameter, SqlParameterValue parmValue)
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

			// Renders the given (already build-optimized) statements into ONE OR MORE combined multi-statement commands,
			// starting a new command whenever the accumulated SQL would exceed the provider's command-length cap — so a large
			// eager-load group splits across several round-trips instead of one oversized command. Within each command the
			// statements render through a SINGLE shared OptimizationContext (dialect-converted during BuildSql, which
			// never mutates the shared cached statement) whose parameter normalizer uniquifies/dedups names ACROSS that
			// command; each command therefore carries its OWN parameter scope. Returns, per command: the SQL, the bound
			// parameters, and how many of the input statements it covers (contiguous, in order).
			internal static IReadOnlyList<(string Sql, DbParameter[]? Parameters, int StatementCount)> RenderCombinedBatches(
				DataConnection dataConnection, IReadOnlyList<SqlStatement> statements, IReadOnlyParameterValues? parameterValues)
			{
				var options      = dataConnection.Options;
				var sqlOptimizer = dataConnection.DataProvider.GetSqlOptimizer (options);
				var sqlBuilder   = dataConnection.DataProvider.CreateSqlBuilder(dataConnection.MappingSchema, options);
				var factory      = sqlOptimizer.CreateSqlExpressionFactory(dataConnection.MappingSchema, options);

				var serviceProvider = ((IInfrastructure<IServiceProvider>)dataConnection.DataProvider).Instance;

				// Upper bound on ONE combined command's rendered SQL length — provider-dependent (packet / batch / command
				// limit; see SqlProviderFlags.MaxCombinedCommandLength). Soft cap: a command may overshoot by the last
				// statement appended, and a single statement is never split; a group past it splits across round-trips
				// (statement COUNT is separately bounded by PlanScenario's MaxStatementsPerCombinedGroup).
				var maxCommandLength = dataConnection.DataProvider.SqlProviderFlags.MaxCombinedCommandLength;

				var result = new List<(string, DbParameter[]?, int)>();

				using var sb = Pools.StringBuilder.Allocate();

				var i = 0;

				while (i < statements.Count)
				{
					// Each command has its OWN parameter scope (fresh normalizer), so names are uniquified per command.
					var optimizationContext = new OptimizationContext(
						new EvaluationContext(parameterValues),
						options,
						dataConnection.DataProvider.SqlProviderFlags,
						dataConnection.MappingSchema,
						sqlOptimizer.CreateOptimizerVisitor(false),
						sqlOptimizer.CreateConvertVisitor(false),
						factory,
						dataConnection.DataProvider.SqlProviderFlags.IsParameterOrderDependent,
						isAlreadyOptimizedAndConverted : false,
						parametersNormalizerFactory    : dataConnection.DataProvider.GetQueryParameterNormalizer);

					optimizationContext.ShareParametersByAccessor = true;

					sb.Value.Length = 0;

					var count = 0;

					// Always at least one statement per command; then keep appending until the SQL reaches the length cap.
					while (i < statements.Count && (count == 0 || sb.Value.Length < maxCommandLength))
					{
						var aliases = ScenarioCommandRenderer.PrepareStepAliases(serviceProvider, statements[i]);

						ScenarioCommandRenderer.AppendConcatenatedStatement(sb.Value, sqlBuilder, optimizationContext, statements[i], aliases, count == 0, 0);

						count++;
						i++;
					}

					var sqlParameters = optimizationContext.GetParameters();

					DbParameter[]? dbParameters = null;

					if (sqlParameters.Count > 0)
					{
						var dbCommand = dataConnection.GetOrCreateCommand();

						dbParameters = new DbParameter[sqlParameters.Count];

						for (var p = 0; p < sqlParameters.Count; p++)
						{
							var sqlp = sqlParameters[p];
							dbParameters[p] = CreateParameter(dataConnection, dbCommand, sqlp, sqlp.GetParameterValue(parameterValues));
						}
					}

					result.Add((sb.Value.ToString(), dbParameters, count));
				}

				return result;
			}

			// Plans the physical command groups for an assembled eager-loading scenario, size-aware (a large child
			// fan-out splits into several round-trips). Resolves the provider's IDmlService so eager loading reuses the
			// same PlanScenario grouping authority as DML combining.
			internal static SqlCommandGroupPlan PlanEagerScenario(DataConnection dataConnection, SqlCommandScenario scenario)
			{
				var dmlService = ((IInfrastructure<IServiceProvider>)dataConnection.DataProvider).Instance.GetRequiredService<IDmlService>();

				return dmlService.PlanScenario(scenario, dataConnection.DataProvider.SqlProviderFlags);
			}

			// The single execution seam for one combined-eager command. A single (pre-merged / concatenated) statement runs
			// as one DbCommand; a multi-statement command runs as a DbBatch (each statement its own parameter scope),
			// attached to the reader wrapper so the batch is released when the reader is disposed. Returns the reader
			// positioned at the first result set: the caller harvests each buffered result set, then for the command holding
			// the main query streams the last result set — and owns the returned reader for the stream's lifetime.
			internal static DataReaderWrapper ExecuteCombined(DataConnection dataConnection, CombinedCommand command, CommandBehavior commandBehavior)
			{
				var statements = command.Statements;

				if (statements.Count == 1)
				{
					var statement = statements[0];

					InitCommand(dataConnection, new CommandWithParameters(statement.Sql, []), statement.Parameters, command.QueryHints);

					return dataConnection.ExecuteDataReader(commandBehavior);
				}

#if SUPPORTS_DBBATCH
				if (dataConnection.CanUseDbBatch)
				{
					var batch = dataConnection.CreateBatch(statements);

					try
					{
						var reader = dataConnection.ExecuteBatchDataReader(batch, commandBehavior);

						reader.AdditionalDisposable = batch;

						return reader;
					}
					catch
					{
						// ExecuteBatchDataReader threw before ownership passed to the reader wrapper: release the batch here.
						batch.Dispose();
						throw;
					}
				}
#endif

				// A multi-statement command carries isolated (non-uniquified) parameter scopes, so it can only run as a
				// DbBatch; the eager builder renders a semicolon-concatenated single statement when DbBatch is unavailable,
				// so this path is unreachable.
				throw new InvalidOperationException("A multi-statement combined command requires DbBatch support.");
			}

			// In case of change the logic of this method, DO NOT FORGET to change the sibling method.
			internal static async Task<DataReaderWrapper> ExecuteCombinedAsync(DataConnection dataConnection, CombinedCommand command, CommandBehavior commandBehavior, CancellationToken cancellationToken)
			{
				var statements = command.Statements;

				if (statements.Count == 1)
				{
					var statement = statements[0];

					InitCommand(dataConnection, new CommandWithParameters(statement.Sql, []), statement.Parameters, command.QueryHints);

					return await dataConnection.ExecuteDataReaderAsync(commandBehavior, cancellationToken).ConfigureAwait(false);
				}

#if SUPPORTS_DBBATCH
				if (dataConnection.CanUseDbBatch)
				{
					var batch = dataConnection.CreateBatch(statements);

					try
					{
						var reader = await dataConnection.ExecuteBatchDataReaderAsync(batch, commandBehavior, cancellationToken).ConfigureAwait(false);

						reader.AdditionalDisposable = batch;

						return reader;
					}
					catch
					{
						// ExecuteBatchDataReaderAsync threw before ownership passed to the reader wrapper: release the batch here.
						batch.Dispose();
						throw;
					}
				}
#endif

				throw new InvalidOperationException("A multi-statement combined command requires DbBatch support.");
			}

			#region Scenario interpreter

			// One unified value returned by the sequential scenario interpreter; the public execute methods project the
			// field they need (rows-affected for ExecuteNonQuery, scalar for ExecuteScalar).
			readonly record struct ScenarioOutcome(int RowsAffected, object? Scalar);

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

			// Executes one step as its own command. stepIndex indexes the logical step (context / gate / result);
			// commandIndex indexes the pre-rendered physical command (Commands[commandIndex]) — the two differ once steps
			// are grouped (for the legacy bridge they are equal).
			static void ExecuteSingleStep(DataConnection dataConnection, ExecutionPreparedQuery executionQuery, IReadOnlyList<SqlCommandStep> steps, int stepIndex, int commandIndex, SqlCommandExecutionContext context)
			{
				var step = steps[stepIndex];

				if (step.RunIf is { } gate && !EvaluateGate(gate, context))
					return;

				InitCommand(dataConnection, executionQuery, commandIndex);

				ApplyForwardedParameters(executionQuery, commandIndex, step, context);

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

					return;
				}

				switch (step.Kind)
				{
					case SqlStepKind.NonQuery:
					{
						var outParam = step.OutParameterName is null ? null : AddOutputParameter(dataConnection, step);
						var n        = dataConnection.ExecuteNonQuery();
						context.SetResult(stepIndex, outParam != null ? outParam.Value : n);
						break;
					}
					case SqlStepKind.Scalar:
						context.SetResult(stepIndex, dataConnection.ExecuteScalar());
						break;
				}
			}

			// Shared combined-command harvest walk (sync): over one open reader, invokes the per-step harvest callback for
			// each step in order, then advances to the next result set for every result-producing step (a pure non-query
			// step yields no result set, so no advance). DML harvests scalar / rows-affected into the execution context;
			// eager loading plugs its own materializer into the same walk.
			static void WalkCombinedResultSets(DbDataReader dr, IReadOnlyList<int> stepIndexes, IReadOnlyList<SqlCommandStep> steps, Action<int, DbDataReader> harvest)
			{
				foreach (var i in stepIndexes)
				{
					harvest(i, dr);

					if (steps[i].Kind != SqlStepKind.NonQuery)
						dr.NextResult();
				}
			}

			// Executes a combined group as ONE pre-rendered command (Commands[commandIndex]) and harvests its result sets in
			// step order: a Scalar step reads the first cell of the current result set; a pure non-query step yields no
			// result set (RecordsAffected only).
#if SUPPORTS_DBBATCH
			// A combined group can run as a DbBatch (isolated per-statement parameter scopes) only when no step needs
			// cross-statement binding the batch can't do: an OUT parameter (Oracle/Firebird identity) or a forwarded
			// ParameterBinding must stay in the single semicolon-concatenated command, and query hints (command 0 only) can't be
			// applied to a DbBatch. Otherwise it is DbBatch-eligible when the provider supports it (CanUseDbBatch).
			static bool IsGroupBatchEligible(DataConnection dataConnection, ExecutionPreparedQuery executionQuery, IReadOnlyList<SqlCommandStep> steps, SqlCommandGroup group, int commandIndex)
			{
				if (!dataConnection.CanUseDbBatch)
					return false;

				if (commandIndex == 0 && executionQuery.PreparedQuery.QueryHints != null)
					return false;

				foreach (var i in group.StepIndexes)
				{
					var step = steps[i];

					if (step.OutParameterName != null || step.ParameterBindings.Count > 0)
						return false;
				}

				return true;
			}
#endif

			// Builds the physical command for a combined group: a DbBatch of isolated per-statement scopes when eligible (rendered
			// fresh from the group's statements), otherwise the single pre-rendered semicolon-concatenated command
			// (Commands[commandIndex]) executed as one DbCommand. Both flow through the same ExecuteCombined seam.
			static CombinedCommand BuildCombinedGroupCommand(DataConnection dataConnection, ExecutionPreparedQuery executionQuery, IReadOnlyList<SqlCommandStep> steps, SqlCommandGroup group, int commandIndex)
			{
				var stepIndexes = group.StepIndexes;

#if SUPPORTS_DBBATCH
				if (IsGroupBatchEligible(dataConnection, executionQuery, steps, group, commandIndex))
				{
					var groupStatements = new SqlStatement[stepIndexes.Count];

					for (var k = 0; k < stepIndexes.Count; k++)
						groupStatements[k] = steps[stepIndexes[k]].Statement;

					var rendered = ScenarioCommandRenderer.RenderStatements(dataConnection, groupStatements, executionQuery.ParameterValues);

					return new CombinedCommand(rendered, stepIndexes, null);
				}
#endif

				var pq = executionQuery.PreparedQuery;

				return new CombinedCommand(
					[new RenderedStatement(pq.Commands[commandIndex].Command, executionQuery.CommandsParameters[commandIndex])],
					stepIndexes,
					commandIndex == 0 ? pq.QueryHints : null);
			}

			static void ExecuteCombinedGroup(DataConnection dataConnection, ExecutionPreparedQuery executionQuery, IReadOnlyList<SqlCommandStep> steps, SqlCommandGroup group, int commandIndex, SqlCommandExecutionContext context)
			{
				var command = BuildCombinedGroupCommand(dataConnection, executionQuery, steps, group, commandIndex);

				using var rd = ExecuteCombined(dataConnection, command, CommandBehavior.Default);

				WalkCombinedResultSets(rd.DataReader!, group.StepIndexes, steps, (i, dr) =>
				{
					switch (steps[i].Kind)
					{
						case SqlStepKind.Scalar:
							context.SetResult(i, dr.Read() ? dr.GetValue(0) : null);
							break;
						case SqlStepKind.NonQuery:
							context.SetResult(i, dr.RecordsAffected);
							break;
					}
				});
			}

			// Interpreter (sync). Walks the group plan (from PreparedQuery.Plan): a singleton group runs as its own
			// command; a combined group runs as one command whose result sets are harvested per step. When there is no
			// plan (the legacy bridge), every step is its own command. Owns all InitCommand calls.
			static ScenarioOutcome ExecuteScenario(DataConnection dataConnection, ExecutionPreparedQuery executionQuery, SqlCommandScenario scenario)
			{
				var steps   = scenario.Steps;
				var context = new SqlCommandExecutionContext(steps.Count);
				var groups  = executionQuery.PreparedQuery.Plan?.Groups;

				if (groups == null)
				{
					// Legacy bridge: each step is its own command (Commands are per-command, 1:1 with steps).
					for (var i = 0; i < steps.Count; i++)
						ExecuteSingleStep(dataConnection, executionQuery, steps, i, i, context);
				}
				else
				{
					// Per-group: each group is one pre-rendered command (Commands[g]).
					for (var g = 0; g < groups.Count; g++)
					{
						var group = groups[g];

						if (group.StepIndexes.Count == 1)
							ExecuteSingleStep(dataConnection, executionQuery, steps, group.StepIndexes[0], g, context);
						else
							ExecuteCombinedGroup(dataConnection, executionQuery, steps, group, g, context);
					}
				}

				// ExecuteNonQuery's rows-affected is the last-executed outcome branch's count (the UPDATE or INSERT that
				// actually ran in the InsertOrReplace/Upsert emulation; step 0 for a plain single statement; 0 when a
				// gate skipped every outcome branch, e.g. SELECT-exists found the row). See ResolveOutcome.
				var resolved = ResolveOutcome(scenario, context);
				return new ScenarioOutcome(resolved is int rowsAffected ? rowsAffected : 0, resolved);
			}

			// In case of change the logic of this method, DO NOT FORGET to change the sibling method.
			static async Task ExecuteSingleStepAsync(DataConnection dataConnection, ExecutionPreparedQuery executionQuery, IReadOnlyList<SqlCommandStep> steps, int stepIndex, int commandIndex, SqlCommandExecutionContext context, CancellationToken cancellationToken)
			{
				var step = steps[stepIndex];

				if (step.RunIf is { } gate && !EvaluateGate(gate, context))
					return;

				InitCommand(dataConnection, executionQuery, commandIndex);

				ApplyForwardedParameters(executionQuery, commandIndex, step, context);

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

					return;
				}

				switch (step.Kind)
				{
					case SqlStepKind.NonQuery:
					{
						var outParam = step.OutParameterName is null ? null : AddOutputParameter(dataConnection, step);
						var n        = await dataConnection.ExecuteNonQueryDataAsync(cancellationToken).ConfigureAwait(false);
						context.SetResult(stepIndex, outParam != null ? outParam.Value : n);
						break;
					}
					case SqlStepKind.Scalar:
						context.SetResult(stepIndex, await dataConnection.ExecuteScalarDataAsync(cancellationToken).ConfigureAwait(false));
						break;
				}
			}

			// Shared combined-command harvest walk (async sibling). In case of change the logic of this method, DO NOT
			// FORGET to change the sibling method.
			static async Task WalkCombinedResultSetsAsync(DbDataReader dr, IReadOnlyList<int> stepIndexes, IReadOnlyList<SqlCommandStep> steps, Func<int, DbDataReader, Task> harvest, CancellationToken cancellationToken)
			{
				foreach (var i in stepIndexes)
				{
					await harvest(i, dr).ConfigureAwait(false);

					if (steps[i].Kind != SqlStepKind.NonQuery)
						await dr.NextResultAsync(cancellationToken).ConfigureAwait(false);
				}
			}

			// In case of change the logic of this method, DO NOT FORGET to change the sibling method.
			static async Task ExecuteCombinedGroupAsync(DataConnection dataConnection, ExecutionPreparedQuery executionQuery, IReadOnlyList<SqlCommandStep> steps, SqlCommandGroup group, int commandIndex, SqlCommandExecutionContext context, CancellationToken cancellationToken)
			{
				var command = BuildCombinedGroupCommand(dataConnection, executionQuery, steps, group, commandIndex);

				var rd = await ExecuteCombinedAsync(dataConnection, command, CommandBehavior.Default, cancellationToken).ConfigureAwait(false);
				await using var _ = rd.ConfigureAwait(false);

				await WalkCombinedResultSetsAsync(rd.DataReader!, group.StepIndexes, steps, async (i, dr) =>
				{
					switch (steps[i].Kind)
					{
						case SqlStepKind.Scalar:
							context.SetResult(i, await dr.ReadAsync(cancellationToken).ConfigureAwait(false) ? dr.GetValue(0) : null);
							break;
						case SqlStepKind.NonQuery:
							context.SetResult(i, dr.RecordsAffected);
							break;
					}
				}, cancellationToken).ConfigureAwait(false);
			}

			// Interpreter (async sibling). In case of change the logic of this method, DO NOT FORGET to change the sibling method.
			static async Task<ScenarioOutcome> ExecuteScenarioAsync(DataConnection dataConnection, ExecutionPreparedQuery executionQuery, SqlCommandScenario scenario, CancellationToken cancellationToken)
			{
				var steps   = scenario.Steps;
				var context = new SqlCommandExecutionContext(steps.Count);
				var groups  = executionQuery.PreparedQuery.Plan?.Groups;

				if (groups == null)
				{
					// Legacy bridge: each step is its own command (Commands are per-command, 1:1 with steps).
					for (var i = 0; i < steps.Count; i++)
						await ExecuteSingleStepAsync(dataConnection, executionQuery, steps, i, i, context, cancellationToken).ConfigureAwait(false);
				}
				else
				{
					// Per-group: each group is one pre-rendered command (Commands[g]).
					for (var g = 0; g < groups.Count; g++)
					{
						var group = groups[g];

						if (group.StepIndexes.Count == 1)
							await ExecuteSingleStepAsync(dataConnection, executionQuery, steps, group.StepIndexes[0], g, context, cancellationToken).ConfigureAwait(false);
						else
							await ExecuteCombinedGroupAsync(dataConnection, executionQuery, steps, group, g, context, cancellationToken).ConfigureAwait(false);
					}
				}

				// ExecuteNonQuery's rows-affected is the last-executed outcome branch's count (the UPDATE or INSERT that
				// actually ran in the InsertOrReplace/Upsert emulation; step 0 for a plain single statement; 0 when a
				// gate skipped every outcome branch, e.g. SELECT-exists found the row). See ResolveOutcome.
				var resolved = ResolveOutcome(scenario, context);
				return new ScenarioOutcome(resolved is int rowsAffected ? rowsAffected : 0, resolved);
			}

			#endregion

			#region ExecuteNonQuery

			// In case of change the logic of this method, DO NOT FORGET to change the sibling method.
			static async Task<int> ExecuteNonQueryImplAsync(
				DataConnection         dataConnection,
				ExecutionPreparedQuery executionQuery,
				CancellationToken      cancellationToken)
			{
				if (IsSingleSimpleCommand(executionQuery))
				{
					InitFirstCommand(dataConnection, executionQuery);

					return await dataConnection.ExecuteNonQueryDataAsync(cancellationToken)
						.ConfigureAwait(false);
				}

				return (await ExecuteScenarioAsync(dataConnection, executionQuery, executionQuery.PreparedQuery.Scenario!, cancellationToken).ConfigureAwait(false)).RowsAffected;
			}

			// In case of change the logic of this method, DO NOT FORGET to change the sibling method.
			static int ExecuteNonQueryImpl(DataConnection dataConnection, ExecutionPreparedQuery executionQuery)
			{
				if (IsSingleSimpleCommand(executionQuery))
				{
					InitFirstCommand(dataConnection, executionQuery);

					return dataConnection.ExecuteNonQuery();
				}

				return ExecuteScenario(dataConnection, executionQuery, executionQuery.PreparedQuery.Scenario!).RowsAffected;
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
				var executionQuery     = new ExecutionPreparedQuery(preparedQuery, commandsParameters, parameterValues);

				return await ExecuteNonQueryImplAsync(dataConnection, executionQuery, cancellationToken)
					.ConfigureAwait(false);
			}

			// In case of change the logic of this method, DO NOT FORGET to change the sibling method.
			public static int ExecuteNonQuery(DataConnection dataConnection, IQueryContext context, IReadOnlyParameterValues? parameterValues)
			{
				var preparedQuery      = GetCommand(dataConnection, context, parameterValues, false);
				var commandsParameters = GetParameters(dataConnection, preparedQuery, parameterValues);
				var executionQuery     = new ExecutionPreparedQuery(preparedQuery, commandsParameters, parameterValues);

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
				if (IsSingleSimpleCommand(executionQuery))
				{
					InitFirstCommand(dataConnection, executionQuery);

					return await dataConnection.ExecuteScalarDataAsync(cancellationToken).ConfigureAwait(false);
				}

				return (await ExecuteScenarioAsync(dataConnection, executionQuery, executionQuery.PreparedQuery.Scenario!, cancellationToken).ConfigureAwait(false)).Scalar;
			}

			// In case of change the logic of this method, DO NOT FORGET to change the sibling method.
			static object? ExecuteScalarImpl(DataConnection dataConnection, ExecutionPreparedQuery executionQuery)
			{
				if (IsSingleSimpleCommand(executionQuery))
				{
					InitFirstCommand(dataConnection, executionQuery);

					return dataConnection.ExecuteScalar();
				}

				return ExecuteScenario(dataConnection, executionQuery, executionQuery.PreparedQuery.Scenario!).Scalar;
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
				var executionQuery     = new ExecutionPreparedQuery(preparedQuery, commandsParameters, parameterValues);

				return ExecuteScalarImplAsync(dataConnection, executionQuery, cancellationToken);
			}

			// In case of change the logic of this method, DO NOT FORGET to change the sibling method.
			public static object? ExecuteScalar(DataConnection dataConnection, IQueryContext context, IReadOnlyParameterValues? parameterValues)
			{
				var preparedQuery      = GetCommand(dataConnection, context, parameterValues, false);
				var commandsParameters = GetParameters(dataConnection, preparedQuery, parameterValues);
				var executionQuery     = new ExecutionPreparedQuery(preparedQuery, commandsParameters, parameterValues);

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
			// A single physical command that is NOT a combined (multi-statement) group can be executed directly (the fast
			// path). A lone COMBINED group must instead go through the scenario interpreter so its result sets are read via
			// the NextResult walk — ExecuteScalar/ExecuteNonQuery over a batch is the cross-provider footgun the walk avoids.
			static bool IsSingleSimpleCommand(ExecutionPreparedQuery executionQuery)
			{
				var pq = executionQuery.PreparedQuery;

				if (pq.Commands.Length != 1 || (pq.Plan != null && pq.Plan.Groups[0].StepIndexes.Count != 1))
					return false;

				// A lone OUT-parameter step (Firebird/Oracle identity built as a scenario) must run through the interpreter
				// so AddOutputParameter creates the output parameter; the fast path would execute the command directly with
				// no output parameter. Every other single-command scenario takes the fast path.
				if (pq.Scenario is { Steps: [{ OutParameterName: not null }] })
					return false;

				return true;
			}

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

				if (IsSingleSimpleCommand(_executionQuery!))
				{
					InitFirstCommand(_dataConnection, _executionQuery!);

					return await _dataConnection.ExecuteNonQueryDataAsync(cancellationToken).ConfigureAwait(false);
				}

				return (await ExecuteScenarioAsync(_dataConnection, _executionQuery!, _executionQuery!.PreparedQuery.Scenario!, cancellationToken).ConfigureAwait(false)).RowsAffected;
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
