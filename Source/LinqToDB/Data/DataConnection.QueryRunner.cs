using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
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
				var queries = new QuerySql[_executionQuery!.PreparedQuery.Prepared.Commands.Length];

				for (var index = 0; index < _executionQuery!.PreparedQuery.Prepared.Commands.Length; index++)
				{
					var queryCommand    = _executionQuery.PreparedQuery.Prepared.Commands[index].Concat!;
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

			// Per-execution wrapper: the render cache (Prepared, a statement-free PreparedQuery) plus this call's query
			// hints. For a non-parameter-dependent query the PreparedQuery is cached on QueryInfo.Prepared and reused
			// across runs; a parameter-dependent one is rebuilt each run. Each PreparedCommand carries its group's concat
			// form (Concat, always present for DML) and, when batch-eligible, the per-statement DbBatch templates (Batch);
			// the interpreter picks per run by CanUseDbBatch.
			private sealed record RenderedQuery(PreparedQuery Prepared, IReadOnlyCollection<string>? QueryHints);

			private sealed record ExecutionPreparedQuery(RenderedQuery PreparedQuery, DbParameter[]?[] CommandsParameters, IReadOnlyParameterValues? ParameterValues);

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
			static RenderedQuery GetCommand(DataConnection dataConnection, IQueryContext query, IReadOnlyParameterValues? parameterValues, bool forGetSqlText, int startIndent = 0)
			{
				bool aquiredLock = false;
				try
				{
					Monitor.Enter(query, ref aquiredLock);

					var statement = query.Statement;
					var options   = query.DataOptions ?? dataConnection.Options;

					if (query is QueryInfo { Prepared: { } cachedPrepared })
					{
						return new RenderedQuery(cachedPrepared, dataConnection.GetNextCommandHints(!forGetSqlText));
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

					var flags           = dataConnection.DataProvider.SqlProviderFlags;
					var serviceProvider = ((IInfrastructure<IServiceProvider>)dataConnection.DataProvider).Instance;

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

					var optimizeAndConvertAll = !continuousRun && !statement.IsParameterDependent;

					// ── Phase S — structural prepare (parameter-independent), memoized on QueryInfo.Structure ─────
					// The whole-query optimize+convert runs before aliasing (a conversion that restructures the AST,
					// e.g. IN->EXISTS, creates nodes the alias context must see; deferring it is the alias-corruption
					// bug). It does NOT apply parameter-value refinements (e.g. narrowing an InsertOrUpdate SET cast, or
					// provider conversions like ClickHouse bitAnd) - those need real values and would bake this run's
					// values into a memoized structure. Parameter-dependent queries instead apply those refinements on the
					// per-run whole-query convert (the else branch below). See ScenarioCommandRenderer.PrepareStructure.
					//
					// Non-parameter-dependent queries prepare the structure ONCE with a null EvaluationContext in Modify
					// mode, memoize it on QueryInfo.Structure, and render on a SEPARATE null-context (no build-time
					// re-convert). Parameter-dependent / continuous-run queries prepare with parameter values in Transform
					// (non-mutating) mode on ONE context shared with the render, and are NOT memoized - they rebuild per
					// execution (Stage 4 introduces their per-execution BuildSql-only reuse).
					QueryStructure      structure;
					OptimizationContext renderContext;

					if (optimizeAndConvertAll)
					{
						if (query is QueryInfo { Structure: { } cachedStructure })
						{
							structure = cachedStructure;
						}
						else
						{
							// Phase-S structural context: null EvaluationContext (parameter-independent), Modify mode (this
							// statement is the query's own, prepared once under Monitor.Enter). Discarded after Phase S.
							var structureContext = new OptimizationContext(
								new EvaluationContext(null),
								options,
								flags,
								dataConnection.MappingSchema,
								sqlOptimizer.CreateOptimizerVisitor(true),
								sqlOptimizer.CreateConvertVisitor(true),
								factory,
								flags.IsParameterOrderDependent,
								parametersNormalizerFactory    : dataConnection.DataProvider.GetQueryParameterNormalizer);

							structure = ScenarioCommandRenderer.PrepareStructure(dataConnection, options, statement, structureContext, isParameterDependent : false);

							if (query is QueryInfo queryInfoStructure)
								queryInfoStructure.Structure = structure;
						}

						// Phase-R render context: null EvaluationContext, no build-time re-convert (structure is already
						// fully converted). Its own parameter normalizer collects this command's parameters during BuildSql.
						renderContext = ScenarioCommandRenderer.CreateRenderContext(dataConnection, sqlOptimizer, factory, parameterValues, optimizeAndConvertAll : true);
					}
					else
					{
						// POC (decouple builder from optimizer, milestone 1): ONE context drives the whole-query
						// optimize+convert (PrepareStructure → OptimizeAndConvertAll) AND the render.
						// The whole-query convert applies this run's parameter-value refinements (IN-list expansion,
						// null-fold, TAKE/SKIP) up front, so the builder is a pure renderer with no convert of its own.
						// The render context still keeps the parameter values so render-time evaluation works (IN-list /
						// VALUES BuildRows). Transform (non-mutating) visitors keep the shared statement untouched; the
						// per-run structure is not cached (no baking-into-cache risk).
						var singleContext = new OptimizationContext(
							new EvaluationContext(parameterValues),
							options,
							flags,
							dataConnection.MappingSchema,
							sqlOptimizer.CreateOptimizerVisitor(false),
							sqlOptimizer.CreateConvertVisitor(false),
							factory,
							flags.IsParameterOrderDependent,
							parametersNormalizerFactory    : dataConnection.DataProvider.GetQueryParameterNormalizer);

						structure     = ScenarioCommandRenderer.PrepareStructure(dataConnection, options, statement, singleContext, isParameterDependent : true);
						renderContext = singleContext;
					}

					using var sb = Pools.StringBuilder.Allocate();

					// Render each group as one concat command (shared with the remote GetSqlText path).
					var commands = ScenarioCommandRenderer.RenderScenarioGroups(structure.Scenario, structure.Plan, structure.MainStatement, structure.Aliases, sqlBuilder, renderContext, serviceProvider, sb.Value, startIndent);

					CommandWithParameters[]?[]? batchCommands = null;

#if SUPPORTS_DBBATCH
					// Pre-render the per-group DbBatch templates so batch execution binds DbParameters per run instead of
					// re-rendering the SQL each time - only when the connection can actually batch (UseDbBatch on, no command
					// interceptor), else the concat command (Concat) is used. Rendered for BOTH non-parameter-dependent (cached)
					// and parameter-dependent (rebuilt per run) queries so the resulting BakedQuery has every slot materialized
					// and never needs the statement at execution.
					if (dataConnection.CanUseDbBatch)
						batchCommands = RenderBatchTemplates(dataConnection, structure.Scenario, structure.Plan, parameterValues);
#endif

					// Assemble the per-command render list: one PreparedCommand per group, carrying its concat form and, when
					// batch-eligible, the per-statement DbBatch templates.
					var preparedCommands = new PreparedCommand[structure.Plan.Groups.Count];

					for (var g = 0; g < structure.Plan.Groups.Count; g++)
						preparedCommands[g] = new PreparedCommand(structure.Plan.Groups[g].StepIndexes, commands[g], batchCommands?[g])
						{
							ForwardedSlots = ResolveForwardedSlots(structure.Scenario, structure.Plan.Groups[g], commands[g]),
						};

					// Project the statement-free ExecutionStep facts and assemble the BakedQuery: every command slot is
					// materialized, so the scenario statement graph is now unreachable from the cache and collectible (the
					// memory win). WasBatch is eager-only; the DML BakedQuery fills both command forms.
					var prepared = new BakedQuery(
						ScenarioCommandRenderer.ProjectExecutionSteps(structure.Scenario),
						structure.Scenario.OutcomeSteps,
						preparedCommands,
						WasBatch: false);

					// Cache the statement-free BakedQuery for a non-parameter-dependent query; a parameter-dependent one is
					// rebuilt each run (its render is parameter-value specific), so it is not cached and is collected after use.
					if (optimizeAndConvertAll)
					{
						if (query is QueryInfo queryInfo)
						{
							queryInfo.Prepared  = prepared;
							// The BakedQuery is statement-free; drop the memoized structure so the SqlStatement/Aliases graph
							// it holds becomes collectible - the intended memory win. Structure's only reader is shadowed by
							// the Prepared early-return at method entry, so it is dead weight once the query is cached.
							queryInfo.Structure = null;
						}
					}

					query.IsContinuousRun = true;

					return new RenderedQuery(prepared, dataConnection.GetNextCommandHints(!forGetSqlText));
				}
				finally
				{
					if (aquiredLock)
						Monitor.Exit(query);
				}
			}

			// Adds an output parameter to the current command for a step that produces its value via an OUT parameter
			// (Oracle/Firebird identity). The step's context result becomes the parameter's value after execution.
			static DbParameter AddOutputParameter(DataConnection dataConnection, ExecutionStep step)
			{
				var p = dataConnection.CurrentCommand!.CreateParameter();

				p.ParameterName = step.OutParameterName!;
				p.Direction     = ParameterDirection.Output;
				p.DbType        = step.OutParameterDbType;

				dataConnection.CurrentCommand!.Parameters.Add(p);

				return p;
			}

			// Resolves each step's forwarded parameter bindings to positions in the command's parameter list, once at
			// assembly time, so ApplyForwardedParameters consults no SqlParameter AST node at execution (letting a
			// non-parameter-dependent cache drop its statement). Empty unless a step forwards an earlier step's result.
			static IReadOnlyList<ForwardedSlot> ResolveForwardedSlots(SqlCommandScenario scenario, SqlCommandGroup group, CommandWithParameters? concat)
			{
				if (concat == null)
					return [];

				List<ForwardedSlot>? slots = null;

				var sqlParameters = concat.SqlParameters;

				foreach (var stepIndex in group.StepIndexes)
				{
					var bindings = scenario.Steps[stepIndex].ParameterBindings;

					for (var b = 0; b < bindings.Count; b++)
					{
						var binding = bindings[b];

						for (var k = 0; k < sqlParameters.Count; k++)
						{
							if (ReferenceEquals(sqlParameters[k], binding.Target))
							{
								(slots ??= new()).Add(new ForwardedSlot(binding.SourceStepIndex, k));
								break;
							}
						}
					}
				}

				return slots ?? (IReadOnlyList<ForwardedSlot>)[];
			}

			// Forwarding: bind each of the command's forwarded parameters (its value comes from an earlier step's result,
			// not the client) on the already-initialized command. Positions were resolved at assembly by
			// ResolveForwardedSlots, so no SqlParameter AST node is consulted here.
			static void ApplyForwardedParameters(ExecutionPreparedQuery executionQuery, int commandIndex, SqlCommandExecutionContext context)
			{
				var slots = executionQuery.PreparedQuery.Prepared.Commands[commandIndex].ForwardedSlots;

				if (slots.Count == 0)
					return;

				var dbParameters = executionQuery.CommandsParameters[commandIndex];

				if (dbParameters == null)
					return;

				foreach (var slot in slots)
				{
					if (slot.TargetPosition < dbParameters.Length)
						dbParameters[slot.TargetPosition].Value = context.GetResult(slot.SourceStepIndex) ?? DBNull.Value;
				}
			}

			static DbParameter[]?[] GetParameters(DataConnection dataConnection, RenderedQuery pq, IReadOnlyParameterValues? parameterValues)
			{
				DbParameter[]?[] result = new DbParameter[pq.Prepared.Commands.Length][];

				for (var index = 0; index < pq.Prepared.Commands.Length; index++)
					result[index] = ScenarioCommandRenderer.MaterializeDbParameters(dataConnection, pq.Prepared.Commands[index].Concat!.SqlParameters, parameterValues);

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
				_executionQuery = CreateExecutionQuery(_dataConnection, Query.QueryInfo, parameterValues, forGetSqlText);
			}

			// Renders the given (already build-optimized) statements into ONE OR MORE combined multi-statement commands,
			// starting a new command whenever the accumulated SQL would exceed the provider's command-length cap — so a large
			// eager-load group splits across several round-trips instead of one oversized command. Within each command the
			// statements render through a SINGLE shared OptimizationContext (dialect-converted during BuildSql, which
			// never mutates the shared cached statement) whose parameter normalizer uniquifies/dedups names ACROSS that
			// command; each command therefore carries its OWN parameter scope. Returns, per command: the SQL, its unbound
			// SqlParameter list, and how many of the input statements it covers (contiguous, in order) — cacheable for a
			// non-parameter-dependent scenario; DbParameter binding then repeats per execution.
			internal static IReadOnlyList<(string Sql, IReadOnlyList<SqlParameter> SqlParameters, int StatementCount)> RenderCombinedBatchTemplates(
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

				var result = new List<(string, IReadOnlyList<SqlParameter>, int)>();

				// Convert+alias upfront (then render with no build-time re-convert) so the AliasesContext is built over the
				// final nodes and references (e.g. an eager-grouping JOIN key) resolve — but only when EVERY statement in the
				// group is non-parameter-dependent, because a combined command shares one parameter scope/context. Run the
				// parameter-dependence WALK, not just the flag (only GetCommand sets the flag, and only for the main query):
				// a child carrying a Rows==null VALUES table is parameter-dependent and must render with its values.
				var convertAll = statements.All(s => !s.IsParameterDependent
					&& !sqlOptimizer.IsParameterDependent(NullabilityContext.NonQuery, dataConnection.MappingSchema, s, options));

				using var sb = Pools.StringBuilder.Allocate();

				var i = 0;

				while (i < statements.Count)
				{
					// Each command has its OWN parameter scope (fresh normalizer), so names are uniquified per command.
					var optimizationContext = ScenarioCommandRenderer.CreateRenderContext(dataConnection, sqlOptimizer, factory, parameterValues, convertAll);

					optimizationContext.ShareParametersByAccessor = true;

					sb.Value.Length = 0;

					var count = 0;

					// Always at least one statement per command; then keep appending until the SQL reaches the length cap.
					while (i < statements.Count && (count == 0 || sb.Value.Length < maxCommandLength))
					{
						var statement = statements[i];

						// Always convert upfront (parameter-dependent statements too) so the builder never re-converts per element.
						statement = optimizationContext.OptimizeAndConvertAll(statement, NullabilityContext.GetContext(statement.SelectQuery));

						var aliases = ScenarioCommandRenderer.PrepareStepAliases(serviceProvider, statement);

						ScenarioCommandRenderer.AppendConcatenatedStatement(sb.Value, sqlBuilder, optimizationContext, statement, aliases, count == 0, 0);

						count++;
						i++;
					}

					result.Add((sb.Value.ToString(), optimizationContext.GetParameters(), count));
				}

				return result;
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
						await batch.DisposeAsync().ConfigureAwait(false);
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
					SqlStepConditionKind.ResultIsZero    => IsZeroNumeric(value),
					SqlStepConditionKind.ResultIsNonZero => value is not null && !IsZeroNumeric(value),
					_                                    => throw new InvalidOperationException($"Unexpected {nameof(SqlStepConditionKind)}: {condition.Kind}"),
				};
			}

			// Numeric-zero test for a gate value. Today the only ResultIsZero source is the InsertOrReplace/Upsert
			// UPDATE step's rows-affected (always a boxed int), but a future gate over a Scalar step could carry a
			// COUNT boxed as long/decimal, so match any integral/decimal zero rather than only a boxed Int32.
			static bool IsZeroNumeric(object? value) => value switch
			{
				int     i => i == 0,
				long    l => l == 0,
				short   s => s == 0,
				byte    b => b == 0,
				decimal m => m == 0m,
				_         => false,
			};

			static object? ResolveOutcome(IReadOnlyList<int> outcomeSteps, SqlCommandExecutionContext context)
			{
				object? outcome = null;

				foreach (var index in outcomeSteps)
				{
					if (context.WasExecuted(index))
						outcome = context.GetResult(index);
				}

				return outcome;
			}

			// Executes one step as its own command. stepIndex indexes the logical step (context / gate / result);
			// commandIndex indexes the pre-rendered physical command (Commands[commandIndex]) — the two differ once steps
			// are grouped (for the legacy bridge they are equal).
			static void ExecuteSingleStep(DataConnection dataConnection, ExecutionPreparedQuery executionQuery, IReadOnlyList<ExecutionStep> steps, int stepIndex, int commandIndex, SqlCommandExecutionContext context)
			{
				var step = steps[stepIndex];

				if (step.RunIf is { } gate && !EvaluateGate(gate, context))
					return;

				InitCommand(dataConnection, executionQuery, commandIndex);

				ApplyForwardedParameters(executionQuery, commandIndex, context);

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
			internal static void WalkCombinedResultSets(DbDataReader dr, IReadOnlyList<int> stepIndexes, IReadOnlyList<ExecutionStep> steps, Action<int, DbDataReader> harvest)
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
			// The static part of batch-eligibility (independent of the connection / hints): a group can run as a DbBatch (isolated
			// per-statement scopes) only when no step needs cross-statement binding the batch can't do - an OUT parameter
			// (Oracle/Firebird identity) or a forwarded ParameterBinding must stay in the single concatenated command.
			static bool IsGroupStructurallyBatchEligible(IReadOnlyList<SqlCommandStep> steps, SqlCommandGroup group)
			{
				foreach (var i in group.StepIndexes)
				{
					var step = steps[i];

					if (step.OutParameterName != null || step.ParameterBindings.Count > 0)
						return false;
				}

				return true;
			}

			// Renders the per-group DbBatch templates (each statement's isolated-scope SQL + unbound SqlParameter list) for the
			// batch-eligible multi-statement groups of a plan, so a cacheable scenario renders them once; a null slot marks a group
			// that always runs concat (a single step, or a step needing OUT-param / forwarded-binding). Execution binds DbParameters.
			static CommandWithParameters[]?[] RenderBatchTemplates(DataConnection dataConnection, SqlCommandScenario scenario, SqlCommandGroupPlan plan, IReadOnlyParameterValues? parameterValues)
			{
				var steps  = scenario.Steps;
				var result = new CommandWithParameters[plan.Groups.Count][];

				for (var g = 0; g < plan.Groups.Count; g++)
				{
					var group = plan.Groups[g];

					if (group.StepIndexes.Count <= 1 || !IsGroupStructurallyBatchEligible(steps, group))
						continue;

					var groupStatements = new SqlStatement[group.StepIndexes.Count];

					for (var k = 0; k < group.StepIndexes.Count; k++)
						groupStatements[k] = steps[group.StepIndexes[k]].Statement!;

					// Pass the run's parameter values so a parameter-dependent group renders correctly (a non-parameter-dependent
					// group converts with a null context and ignores them). Names stay unbound SqlParameters, bound per execution.
					result[g] = ScenarioCommandRenderer.RenderStatementTemplates(dataConnection, groupStatements, parameterValues);
				}

				return result;
			}

#endif

			// Builds the physical command for a combined group: a DbBatch of isolated per-statement scopes when eligible,
			// otherwise the single pre-rendered semicolon-concatenated command (Concat) as one DbCommand. Both flow through
			// the same ExecuteCombined seam. A BakedQuery pre-renders every slot (Concat always; Batch for batch-eligible
			// groups when the build-time connection could batch), so this never re-renders and needs no statement.
			static CombinedCommand BuildCombinedGroupCommand(DataConnection dataConnection, ExecutionPreparedQuery executionQuery, int commandIndex)
			{
				var pq          = executionQuery.PreparedQuery;
				var command     = pq.Prepared.Commands[commandIndex];
				var stepIndexes = command.StepIndexes;

#if SUPPORTS_DBBATCH
				// Batch (isolated per-statement scopes) when the pre-rendered templates exist for this group (Batch != null
				// means a batch-eligible multi-statement group rendered at build), the connection can batch, and no command-0
				// query hints apply (those can't ride a DbBatch). Else fall back to the always-present concat command.
				if (command.Batch != null
					&& dataConnection.CanUseDbBatch
					&& !(commandIndex == 0 && pq.QueryHints != null))
				{
					var templates = command.Batch;
					var rendered  = new RenderedStatement[templates.Length];

					// DML batch templates are always fully rendered (no per-statement volatility), so every element is non-null.
					for (var k = 0; k < templates.Length; k++)
						rendered[k] = new RenderedStatement(templates[k]!.Command, ScenarioCommandRenderer.MaterializeDbParameters(dataConnection, templates[k]!.SqlParameters, executionQuery.ParameterValues));

					return new CombinedCommand(rendered, stepIndexes, null);
				}
#endif

				return new CombinedCommand(
					[new RenderedStatement(command.Concat!.Command, executionQuery.CommandsParameters[commandIndex])],
					stepIndexes,
					commandIndex == 0 ? pq.QueryHints : null);
			}

			// Executes one combined command and harvests the given result-producing steps into the execution context via the
			// shared WalkCombinedResultSets, returning the open reader for the caller to dispose. A non-terminal group disposes
			// it immediately (DML, eager child-only commands); the eager main-carrying group streams its trailing main result
			// set from it first. Disposes the reader itself only if harvesting throws. The reader-lifecycle seam shared by the
			// DML interpreter and the eager enumerator.
			internal static DataReaderWrapper ExecuteCombinedHarvest(DataConnection dataConnection, CombinedCommand command, IReadOnlyList<ExecutionStep> steps, IReadOnlyList<int> harvestStepIndexes, Action<int, DbDataReader> harvest)
			{
				var rd = ExecuteCombined(dataConnection, command, CommandBehavior.Default);

				try
				{
					WalkCombinedResultSets(rd.DataReader!, harvestStepIndexes, steps, harvest);
				}
				catch
				{
					rd.Dispose();
					throw;
				}

				return rd;
			}

			// Shared group-plan driver for the DML interpreter (this file) and the eager enumerator (Linq/QueryRunner):
			// walks the groups in order and harvests each group's results into the context. A group for which isSingleton holds
			// is dispatched to runSingleton (DML: a one-step group runs NonQuery/Scalar via ExecuteSingleStep; eager: a
			// self-executing preamble runs its own query); every other group runs as one combined command via
			// ExecuteCombinedHarvest with harvestCombined. When a group's last step is terminalStepIndex (the eager main), that
			// group's earlier steps are harvested and the still-open reader is returned for the caller to stream (only the last
			// group carries a terminal); otherwise every reader is disposed here and the method returns null. getCommand supplies
			// each combined group's physical command (DML builds it per group from the prepared query; eager already holds it).
			internal static DataReaderWrapper? RunGroups(
				DataConnection                              dataConnection,
				IReadOnlyList<ExecutionStep>                steps,
				IReadOnlyList<SqlCommandGroup>              groups,
				Func<SqlCommandGroup, int, CombinedCommand> getCommand,
				Func<SqlCommandGroup, bool>                 isSingleton,
				Action<int, int>                            runSingleton,
				Action<int, DbDataReader>                   harvestCombined,
				int                                         terminalStepIndex)
			{
				for (var g = 0; g < groups.Count; g++)
				{
					var group = groups[g];

					if (isSingleton(group))
					{
						runSingleton(group.StepIndexes[0], g);
						continue;
					}

					var stepIndexes     = group.StepIndexes;
					var carriesTerminal = terminalStepIndex >= 0 && stepIndexes[stepIndexes.Count - 1] == terminalStepIndex;
					var harvestIndexes  = carriesTerminal ? stepIndexes.Take(stepIndexes.Count - 1).ToList() : stepIndexes;

					var rd = ExecuteCombinedHarvest(dataConnection, getCommand(group, g), steps, harvestIndexes, harvestCombined);

					if (carriesTerminal)
						return rd;

					rd.Dispose();
				}

				return null;
			}

			// Interpreter (sync). Walks the physical commands (each a group of steps) via the shared RunGroups: a singleton
			// group runs as its own command (ExecuteSingleStep); a combined group runs as one command whose result sets are
			// harvested per step. DML has no streaming terminal, so RunGroups always returns null here.
			static ScenarioOutcome ExecuteScenario(DataConnection dataConnection, ExecutionPreparedQuery executionQuery)
			{
				var prepared = executionQuery.PreparedQuery.Prepared;
				var steps    = prepared.Steps;
				var context  = new SqlCommandExecutionContext(steps.Length);
				var groups   = BuildGroups(prepared.Commands);

				var terminalReader = RunGroups(
					dataConnection, steps, groups,
					(group, commandIndex) => BuildCombinedGroupCommand(dataConnection, executionQuery, commandIndex),
					group => group.StepIndexes.Count == 1,
					(stepIndex, commandIndex) => ExecuteSingleStep(dataConnection, executionQuery, steps, stepIndex, commandIndex, context),
					(i, dr) =>
					{
						switch (steps[i].Kind)
						{
							case SqlStepKind.Scalar:
								context.SetResult(i, dr.Read() ? dr.GetValue(0) : null);
								break;
							// A combined group's NonQuery rows-affected is the reader's cumulative count, reliable only once the
							// reader is fully consumed; no current combined group gates/outcomes on a NonQuery step, so it is
							// stored but unused. A future combined scenario consuming it must harvest per-statement counts.
							case SqlStepKind.NonQuery:
								context.SetResult(i, dr.RecordsAffected);
								break;
						}
					},
					terminalStepIndex: -1);

				// DML scenarios have no streaming terminal (terminalStepIndex = -1), so terminalReader is always null.
				terminalReader?.Dispose();

				// ExecuteNonQuery's rows-affected is the last-executed outcome branch's count (the UPDATE or INSERT that
				// actually ran in the InsertOrReplace/Upsert emulation; step 0 for a plain single statement; 0 when a
				// gate skipped every outcome branch, e.g. SELECT-exists found the row). See ResolveOutcome.
				var resolved = ResolveOutcome(prepared.OutcomeSteps, context);
				return new ScenarioOutcome(resolved is int rowsAffected ? rowsAffected : 0, resolved);
			}

			// Projects the render list's per-command step-index groups into the SqlCommandGroup list the shared RunGroups
			// walks (the physical grouping folded into PreparedCommand replaces the separate SqlCommandGroupPlan).
			static SqlCommandGroup[] BuildGroups(PreparedCommand[] commands)
			{
				var groups = new SqlCommandGroup[commands.Length];

				for (var i = 0; i < commands.Length; i++)
					groups[i] = new SqlCommandGroup { StepIndexes = commands[i].StepIndexes };

				return groups;
			}

			// In case of change the logic of this method, DO NOT FORGET to change the sibling method.
			static async Task ExecuteSingleStepAsync(DataConnection dataConnection, ExecutionPreparedQuery executionQuery, IReadOnlyList<ExecutionStep> steps, int stepIndex, int commandIndex, SqlCommandExecutionContext context, CancellationToken cancellationToken)
			{
				var step = steps[stepIndex];

				if (step.RunIf is { } gate && !EvaluateGate(gate, context))
					return;

				InitCommand(dataConnection, executionQuery, commandIndex);

				ApplyForwardedParameters(executionQuery, commandIndex, context);

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
			internal static async Task WalkCombinedResultSetsAsync(DbDataReader dr, IReadOnlyList<int> stepIndexes, IReadOnlyList<ExecutionStep> steps, Func<int, DbDataReader, Task> harvest, CancellationToken cancellationToken)
			{
				foreach (var i in stepIndexes)
				{
					await harvest(i, dr).ConfigureAwait(false);

					if (steps[i].Kind != SqlStepKind.NonQuery)
						await dr.NextResultAsync(cancellationToken).ConfigureAwait(false);
				}
			}

			// In case of change the logic of this method, DO NOT FORGET to change the sibling method.
			internal static async Task<DataReaderWrapper> ExecuteCombinedHarvestAsync(DataConnection dataConnection, CombinedCommand command, IReadOnlyList<ExecutionStep> steps, IReadOnlyList<int> harvestStepIndexes, Func<int, DbDataReader, Task> harvest, CancellationToken cancellationToken)
			{
				var rd = await ExecuteCombinedAsync(dataConnection, command, CommandBehavior.Default, cancellationToken).ConfigureAwait(false);

				try
				{
					await WalkCombinedResultSetsAsync(rd.DataReader!, harvestStepIndexes, steps, harvest, cancellationToken).ConfigureAwait(false);
				}
				catch
				{
					await rd.DisposeAsync().ConfigureAwait(false);
					throw;
				}

				return rd;
			}

			// In case of change the logic of this method, DO NOT FORGET to change the sibling method.
			internal static async Task<DataReaderWrapper?> RunGroupsAsync(
				DataConnection                              dataConnection,
				IReadOnlyList<ExecutionStep>                steps,
				IReadOnlyList<SqlCommandGroup>              groups,
				Func<SqlCommandGroup, int, CombinedCommand> getCommand,
				Func<SqlCommandGroup, bool>                 isSingleton,
				Func<int, int, Task>                        runSingleton,
				Func<int, DbDataReader, Task>               harvestCombined,
				int                                         terminalStepIndex,
				CancellationToken                           cancellationToken)
			{
				for (var g = 0; g < groups.Count; g++)
				{
					var group = groups[g];

					if (isSingleton(group))
					{
						await runSingleton(group.StepIndexes[0], g).ConfigureAwait(false);
						continue;
					}

					var stepIndexes     = group.StepIndexes;
					var carriesTerminal = terminalStepIndex >= 0 && stepIndexes[stepIndexes.Count - 1] == terminalStepIndex;
					var harvestIndexes  = carriesTerminal ? stepIndexes.Take(stepIndexes.Count - 1).ToList() : stepIndexes;

					var rd = await ExecuteCombinedHarvestAsync(dataConnection, getCommand(group, g), steps, harvestIndexes, harvestCombined, cancellationToken).ConfigureAwait(false);

					if (carriesTerminal)
						return rd;

					await rd.DisposeAsync().ConfigureAwait(false);
				}

				return null;
			}

			// Interpreter (async sibling). In case of change the logic of this method, DO NOT FORGET to change the sibling method.
			static async Task<ScenarioOutcome> ExecuteScenarioAsync(DataConnection dataConnection, ExecutionPreparedQuery executionQuery, CancellationToken cancellationToken)
			{
				var prepared = executionQuery.PreparedQuery.Prepared;
				var steps    = prepared.Steps;
				var context  = new SqlCommandExecutionContext(steps.Length);
				var groups   = BuildGroups(prepared.Commands);

				var terminalReader = await RunGroupsAsync(
					dataConnection, steps, groups,
					(group, commandIndex) => BuildCombinedGroupCommand(dataConnection, executionQuery, commandIndex),
					group => group.StepIndexes.Count == 1,
					(stepIndex, commandIndex) => ExecuteSingleStepAsync(dataConnection, executionQuery, steps, stepIndex, commandIndex, context, cancellationToken),
					async (i, dr) =>
					{
						switch (steps[i].Kind)
						{
							case SqlStepKind.Scalar:
								context.SetResult(i, await dr.ReadAsync(cancellationToken).ConfigureAwait(false) ? dr.GetValue(0) : null);
								break;
							// A combined group's NonQuery rows-affected is the reader's cumulative count, reliable only once the
							// reader is fully consumed; no current combined group gates/outcomes on a NonQuery step, so it is
							// stored but unused. A future combined scenario consuming it must harvest per-statement counts.
							case SqlStepKind.NonQuery:
								context.SetResult(i, dr.RecordsAffected);
								break;
						}
					},
					terminalStepIndex: -1,
					cancellationToken).ConfigureAwait(false);

				// DML scenarios have no streaming terminal (terminalStepIndex = -1), so terminalReader is always null.
				if (terminalReader != null)
					await terminalReader.DisposeAsync().ConfigureAwait(false);

				// ExecuteNonQuery's rows-affected is the last-executed outcome branch's count (the UPDATE or INSERT that
				// actually ran in the InsertOrReplace/Upsert emulation; step 0 for a plain single statement; 0 when a
				// gate skipped every outcome branch, e.g. SELECT-exists found the row). See ResolveOutcome.
				var resolved = ResolveOutcome(prepared.OutcomeSteps, context);
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

				return (await ExecuteScenarioAsync(dataConnection, executionQuery, cancellationToken).ConfigureAwait(false)).RowsAffected;
			}

			// In case of change the logic of this method, DO NOT FORGET to change the sibling method.
			static int ExecuteNonQueryImpl(DataConnection dataConnection, ExecutionPreparedQuery executionQuery)
			{
				if (IsSingleSimpleCommand(executionQuery))
				{
					InitFirstCommand(dataConnection, executionQuery);

					return dataConnection.ExecuteNonQuery();
				}

				return ExecuteScenario(dataConnection, executionQuery).RowsAffected;
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

				return (await ExecuteScenarioAsync(dataConnection, executionQuery, cancellationToken).ConfigureAwait(false)).Scalar;
			}

			// In case of change the logic of this method, DO NOT FORGET to change the sibling method.
			static object? ExecuteScalarImpl(DataConnection dataConnection, ExecutionPreparedQuery executionQuery)
			{
				if (IsSingleSimpleCommand(executionQuery))
				{
					InitFirstCommand(dataConnection, executionQuery);

					return dataConnection.ExecuteScalar();
				}

				return ExecuteScenario(dataConnection, executionQuery).Scalar;
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
				var prepared = executionQuery.PreparedQuery.Prepared;

				if (prepared.Commands.Length != 1 || prepared.Commands[0].StepIndexes.Count != 1)
					return false;

				// A lone OUT-parameter step (Firebird/Oracle identity built as a scenario) must run through the interpreter
				// so AddOutputParameter creates the output parameter; the fast path would execute the command directly with
				// no output parameter. Every other single-command scenario takes the fast path.
				if (prepared.Steps is [{ OutParameterName: not null }])
					return false;

				return true;
			}

			static void InitCommand(DataConnection dataConnection, ExecutionPreparedQuery executionQuery, int index)
			{
				InitCommand(dataConnection,
					executionQuery.PreparedQuery.Prepared.Commands[index].Concat!,
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

				return (await ExecuteScenarioAsync(_dataConnection, _executionQuery!, cancellationToken).ConfigureAwait(false)).RowsAffected;
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
