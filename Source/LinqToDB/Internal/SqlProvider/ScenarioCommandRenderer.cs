using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Runtime.InteropServices;
using System.Text;

using LinqToDB.Data;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.DataProvider;
using LinqToDB.Internal.Infrastructure;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Linq.Translation;
using LinqToDB.Mapping;
using LinqToDB.Metrics;

#pragma warning disable MA0048 // CommandWithParameters record grouped with its shared renderer

namespace LinqToDB.Internal.SqlProvider
{
	/// <summary>A rendered physical command: its SQL text and the parameters it carries.</summary>
	sealed record CommandWithParameters(string Command, IReadOnlyList<SqlParameter> SqlParameters);

	/// <summary>One rendered statement in an isolated parameter scope (its own bound parameters), as a DbBatch command
	/// carries it. Names are NOT uniquified against sibling statements (unlike a semicolon-concatenated command).</summary>
	sealed record RenderedStatement(string Sql, DbParameter[]? Parameters);

	/// <summary>
	/// One physical combined-execution command covering one or more scenario steps. <see cref="Statements"/> holds either a
	/// single pre-merged (semicolon-concatenated) statement or, on the DbBatch path, one entry per statement (each its own
	/// parameter scope). <see cref="StepIndexes"/> are the scenario step indices it covers, in order.
	/// </summary>
	sealed record CombinedCommand(IReadOnlyList<RenderedStatement> Statements, IReadOnlyList<int> StepIndexes, IReadOnlyCollection<string>? QueryHints);

	/// <summary>
	/// One physical command's cache slot in a <see cref="PreparedScenario"/>, covering a contiguous run of scenario steps
	/// (<see cref="StepIndexes"/>). <see cref="Concat"/> is the universal semicolon-joined form (runs on any backend as one
	/// DbCommand); <see cref="Batch"/> is the optional per-statement isolated-scope form (DbBatch). DML fills BOTH and picks
	/// at runtime by <c>CanUseDbBatch</c>; eager fills exactly ONE (the backend recorded in <see cref="PreparedScenario.WasBatch"/>).
	/// A null <see cref="Concat"/> or a null <see cref="Batch"/> element means "render fresh this run" (parameter-dependent);
	/// a null <see cref="Batch"/> array means the command is not batch-eligible.
	/// </summary>
	sealed record PreparedCommand(IReadOnlyList<int> StepIndexes, CommandWithParameters? Concat, CommandWithParameters?[]? Batch)
	{
		/// <summary>
		/// Forwarded-parameter slots for this command, resolved once at assembly time. Empty unless a step forwards an
		/// earlier step's result into one of its parameters. Because the position is precomputed, execution needs no
		/// <see cref="SqlParameter"/> AST node — so a non-parameter-dependent cache can drop its statement.
		/// </summary>
		public IReadOnlyList<ForwardedSlot> ForwardedSlots { get; init; } = [];
	}

	/// <summary>
	/// Forwarding resolved to a position: bind the physical command's parameter at <see cref="TargetPosition"/> from the
	/// unified result of scenario step <see cref="SourceStepIndex"/>. Resolved from <c>SqlStepParameterBinding.Target</c>'s
	/// index in the command's parameter list, replacing the execution-time node-identity match.
	/// </summary>
	[StructLayout(LayoutKind.Auto)]
	readonly record struct ForwardedSlot(int SourceStepIndex, int TargetPosition);

	/// <summary>
	/// The render cache for a compiled query's command scenario: the logical <see cref="SqlCommandScenario"/> + its
	/// physical-command <see cref="SqlCommandGroupPlan"/> + the per-command rendered templates. Used
	/// (stored on <c>QueryInfo.EagerCommandCache</c>) by the combined eager-loading executor (the main-query path uses the statement-free <see cref="PreparedQuery"/> instead), binding DbParameters to
	/// cached SQL instead of re-rendering. <see cref="WasBatch"/> records which backend the templates were rendered for so a
	/// run whose <c>CanUseDbBatch</c> differs re-renders instead of binding a wrong-shaped cache (eager); DML fills both
	/// forms and ignores it.
	/// </summary>
	sealed record PreparedScenario(SqlCommandScenario Scenario, SqlCommandGroupPlan Plan, PreparedCommand[] Commands, bool WasBatch);

	/// <summary>
	/// Statement-free per-step facts the scenario interpreter needs at execution (kind, error policy, gate, OUT-parameter
	/// metadata) — the projection of <see cref="SqlCommandStep"/> that carries NO <see cref="SqlStatement"/>. Lets a
	/// <see cref="PreparedQuery"/> drive execution after the statement graph has been dropped.
	/// </summary>
	sealed record ExecutionStep(SqlStepKind Kind, SqlStepOnError OnError, SqlStepCondition? RunIf, string? OutParameterName, DbType OutParameterDbType);

	/// <summary>
	/// The Phase-R render cache: everything the scenario interpreter needs to execute a compiled query, with NO
	/// <see cref="SqlStatement"/> retained. <see cref="Steps"/> are the statement-free step facts; <see cref="Commands"/>
	/// are the rendered physical commands (each covering a contiguous run of steps, replacing the separate
	/// <c>SqlCommandGroupPlan</c>); <see cref="OutcomeSteps"/> are the candidate outcome step indices. Replaces
	/// <see cref="PreparedScenario"/> on the main-query path (<c>QueryInfo.Prepared</c>). The eager path still uses
	/// <see cref="PreparedScenario"/> until it migrates (a later stage).
	/// </summary>
	abstract record PreparedQuery(ExecutionStep[] Steps, IReadOnlyList<int> OutcomeSteps, PreparedCommand[] Commands, bool WasBatch);

	/// <summary>
	/// A fully-rendered <see cref="PreparedQuery"/>: every <see cref="PreparedCommand"/> slot is materialized (concat
	/// always; batch for batch-eligible groups when the connection can batch), so execution never re-renders and needs
	/// no <see cref="SqlStatement"/>. That the statement is gone is guaranteed by the <em>type</em> — a
	/// <see cref="BakedQuery"/> carries no structural graph. A non-parameter-dependent query bakes once and is cached; a
	/// parameter-dependent query bakes per execution (its render is parameter-value specific) and is not cached.
	/// </summary>
	sealed record BakedQuery(ExecutionStep[] Steps, IReadOnlyList<int> OutcomeSteps, PreparedCommand[] Commands, bool WasBatch)
		: PreparedQuery(Steps, OutcomeSteps, Commands, WasBatch);

	/// <summary>
	/// The memoized <b>structural</b> (parameter-independent) artifact for a compiled query, produced once by Phase S
	/// (<see cref="ScenarioCommandRenderer.PrepareStructure"/>): the whole-query optimize+convert (with a null
	/// <see cref="EvaluationContext"/>), aliasing, command-scenario build, synthetic-step finalization and physical
	/// grouping. Immutable and safe to memoize on <c>QueryInfo.Structure</c> and share across executions; the
	/// parameter-value-level build-time convert is deferred to the per-execution render (Phase R). This is distinct from
	/// <see cref="PreparedScenario"/>, which is the <em>rendered</em> (Phase R) physical commands.
	/// <see cref="MainStatement"/> is the optimized+converted main statement (the one <see cref="Aliases"/> is keyed to),
	/// retained so the render can reuse the main statement's aliases instead of re-aliasing it.
	/// </summary>
	sealed record QueryStructure(
		SqlCommandScenario  Scenario,
		SqlCommandGroupPlan Plan,
		SqlStatement        MainStatement,
		AliasesContext      Aliases,
		bool                IsParameterDependent);

#if BUGCHECK
	/// <summary>
	/// Test diagnostics — Release builds don't see this (BUGCHECK-gated). It is <c>public</c> so BUGCHECK tests that have no
	/// <c>InternalsVisibleTo</c> can read it, mirroring <see cref="LinqToDB.Internal.Linq.QueryCache"/>'s BUGCHECK hooks.
	/// </summary>
	public static class RenderDiagnostics
	{
		/// <summary>
		/// Count of top-level statement renders on the current thread (one per <c>ISqlBuilder.BuildSql</c> entry). Used by
		/// EagerRenderCacheTests to prove a non-parameter-dependent command is rendered once and not re-rendered on reuse.
		/// <see cref="ThreadStaticAttribute"/> keeps the count clean under parallel test execution.
		/// </summary>
		[ThreadStatic] public static long BuildSqlCount;
	}
#endif

	/// <summary>
	/// Shared rendering of a <see cref="SqlCommandScenario"/> into physical commands (one per <see cref="SqlCommandGroup"/>).
	/// Used by the direct runner (<c>DataConnection.QueryRunner.GetCommand</c>) and the remote <c>GetSqlText</c> path so
	/// both produce identical grouped commands from the same scenario + plan.
	/// </summary>
	static class ScenarioCommandRenderer
	{
		/// <summary>
		/// Runs provider finalization on synthetic step statements built by the DML service (identity SELECT, the
		/// InsertOrReplace/Upsert UPDATE/INSERT/SELECT emulation) after the main statement was finalized, so per-provider
		/// transforms apply (e.g. SqlCe rewrites its UPDATE into the alias-free form it accepts). Steps reusing the
		/// already-finalized main statement are skipped.
		/// </summary>
		public static SqlCommandScenario FinalizeScenarioSteps(SqlCommandScenario scenario, SqlStatement mainStatement, ISqlOptimizer sqlOptimizer, DataOptions options, MappingSchema mappingSchema)
		{
			SqlCommandStep[]? finalized = null;

			for (var i = 0; i < scenario.Steps.Count; i++)
			{
				var step = scenario.Steps[i];

				// A self-executing step (eager self-executing preamble) carries no rendered statement to finalize.
				if (step.Statement is not { } statement)
					continue;

				if (ReferenceEquals(statement, mainStatement))
					continue;

				var finalStatement = sqlOptimizer.Finalize(mappingSchema, statement, options);

				if (ReferenceEquals(finalStatement, statement))
					continue;

				if (finalized == null)
				{
					finalized = new SqlCommandStep[scenario.Steps.Count];

					for (var j = 0; j < scenario.Steps.Count; j++)
						finalized[j] = scenario.Steps[j];
				}

				finalized[i] = step with { Statement = finalStatement };
			}

			return finalized == null ? scenario : scenario with { Steps = finalized };
		}

		/// <summary>
		/// Projects a scenario's steps to the statement-free <see cref="ExecutionStep"/> facts the interpreter needs, so a
		/// <see cref="BakedQuery"/> can drive execution without retaining the <see cref="SqlStatement"/> graph.
		/// </summary>
		public static ExecutionStep[] ProjectExecutionSteps(SqlCommandScenario scenario)
		{
			var steps  = scenario.Steps;
			var result = new ExecutionStep[steps.Count];

			for (var i = 0; i < steps.Count; i++)
			{
				var s = steps[i];
				result[i] = new ExecutionStep(s.Kind, s.OnError, s.RunIf, s.OutParameterName, s.OutParameterDbType);
			}

			return result;
		}

		/// <summary>
		/// Phase S — the parameter-independent structural prepare, run once per query and memoized on
		/// <c>QueryInfo.Structure</c>. Runs the whole-query optimize+convert through <paramref name="optimizationContext"/>
		/// (the caller supplies a null-<see cref="EvaluationContext"/> structural context for a cacheable query, or the
		/// shared render context for a parameter-dependent one), then aliases the corrected statement, builds the command
		/// scenario, finalizes synthetic steps and plans the physical groups. The whole-query convert must run <b>before</b>
		/// aliasing: a conversion that restructures the AST (e.g. IN-&gt;EXISTS clones its sub-query) creates nodes the
		/// alias context must see, so deferring it corrupts aliases. Parameter-value-level refinements are NOT applied here
		/// (they need real values and would bake this run's values into a memoized structure); for a parameter-dependent
		/// query the per-run whole-query convert (with values) applies them instead.
		/// </summary>
		public static QueryStructure PrepareStructure(
			DataConnection      dataConnection,
			DataOptions         options,
			SqlStatement        statement,
			OptimizationContext optimizationContext,
			bool                isParameterDependent)
		{
			var sqlOptimizer    = dataConnection.DataProvider.GetSqlOptimizer(options);
			var serviceProvider = ((IInfrastructure<IServiceProvider>)dataConnection.DataProvider).Instance;

			var nullability = NullabilityContext.GetContext(statement.SelectQuery);
			statement = optimizationContext.OptimizeAndConvertAll(statement, nullability);

			// correct aliases if needed
			AliasesHelper.PrepareQueryAndAliases(serviceProvider.GetRequiredService<IIdentifierService>(), statement, out var aliases);

			var dmlService = serviceProvider.GetRequiredService<IDmlService>();
			var scenario   = dmlService.BuildCommandScenario(statement, dataConnection.DataProvider.SqlProviderFlags, optimizationContext.Factory);

			// BuildCommandScenario always returns a scenario now (DmlServiceBase supplies a default single-step one);
			// a custom IDmlService returning null is a contract violation.
			if (scenario == null)
				throw new InvalidOperationException($"'{dmlService.GetType().Name}.BuildCommandScenario' returned null; it must always produce a scenario.");

			// The DML service builds synthetic step statements (identity SELECT, the InsertOrReplace/Upsert
			// UPDATE/INSERT/SELECT emulation) after the main statement was finalized, so they miss provider
			// finalization — run it now so per-provider transforms apply (e.g. SqlCe rewrites its UPDATE into the
			// alias-free form it accepts). Steps reusing the already-finalized main statement are skipped.
			scenario = FinalizeScenarioSteps(scenario, statement, sqlOptimizer, options, dataConnection.MappingSchema);

			// Plan: group the scenario's steps into physical commands (round-trips), size-aware.
			var plan = dmlService.PlanScenario(scenario, dataConnection.DataProvider.SqlProviderFlags);

			return new QueryStructure(scenario, plan, statement, aliases, isParameterDependent);
		}

		public static AliasesContext PrepareStepAliases(IServiceProvider serviceProvider, SqlStatement statement)
		{
			AliasesHelper.PrepareQueryAndAliases(serviceProvider.GetRequiredService<IIdentifierService>(), statement, out var aliases);
			return aliases;
		}

		/// <summary>
		/// A per-statement / per-command render scope: a fresh parameter normalizer (names uniquified within this scope
		/// only) over already build-optimized statements (no whole-query optimize/convert pass). Shared by
		/// <c>RenderStatementTemplates</c> (one scope per DbBatch statement) and <c>RenderCombinedBatchTemplates</c> (one scope per
		/// length-split concatenated command); the caller sets <see cref="OptimizationContext.ShareParametersByAccessor"/>
		/// when several statements share one scope.
		/// </summary>
		internal static OptimizationContext CreateRenderContext(DataConnection dataConnection, ISqlOptimizer sqlOptimizer, ISqlExpressionFactory factory, IReadOnlyParameterValues? parameterValues, bool optimizeAndConvertAll) =>
			new(
				// Converting-all upfront must NOT pass parameter values (baking this run's values into a cached statement
				// would make a re-run reuse them). The parameter-dependent path keeps the values so render-time evaluation
				// works — e.g. a VALUES table's BuildRows enumerating its source collection. Mirrors DataConnection.QueryRunner.GetCommand.
				new EvaluationContext(optimizeAndConvertAll ? null : parameterValues),
				dataConnection.Options,
				dataConnection.DataProvider.SqlProviderFlags,
				dataConnection.MappingSchema,
				// ALWAYS Transform (non-mutating) mode, even when converting all upfront. Unlike GetCommand — which runs its
				// upfront convert once under Monitor.Enter(query) and caches the result, so it can safely use in-place Modify
				// mode — the eager render cache is populated by several concurrent first-run threads over the SAME shared
				// scenario statement. A Modify-mode convert would mutate that shared statement in place on multiple threads at
				// once ("Collection was modified; enumeration operation may not execute"). Transform produces a fresh converted
				// statement per render, leaving the shared one untouched; aliasing then runs over that fresh copy.
				sqlOptimizer.CreateOptimizerVisitor(false),
				sqlOptimizer.CreateConvertVisitor(false),
				factory,
				dataConnection.DataProvider.SqlProviderFlags.IsParameterOrderDependent,
				parametersNormalizerFactory    : dataConnection.DataProvider.GetQueryParameterNormalizer);

		/// <summary>
		/// Binds a rendered scope's <paramref name="sqlParameters"/> into <see cref="DbParameter"/>s (<see langword="null"/>
		/// when there are none). Every parameter is created on the connection's shared command instance (used only as a
		/// <see cref="DbParameter"/> factory), matching the single-command path.
		/// </summary>
		internal static DbParameter[]? MaterializeDbParameters(DataConnection dataConnection, IReadOnlyList<SqlParameter> sqlParameters, IReadOnlyParameterValues? parameterValues)
		{
			if (sqlParameters.Count == 0)
				return null;

			var dbCommand    = dataConnection.GetOrCreateCommand();
			var dbParameters = new DbParameter[sqlParameters.Count];

			for (var p = 0; p < sqlParameters.Count; p++)
			{
				var sqlp = sqlParameters[p];
				dbParameters[p] = DataConnection.QueryRunner.CreateParameter(dataConnection, dbCommand, sqlp, sqlp.GetParameterValue(parameterValues));
			}

			return dbParameters;
		}

		/// <summary>
		/// Appends one statement to <paramref name="sb"/> as part of a semicolon-concatenated command: writes the separator
		/// (when not the first statement), renders through the shared <paramref name="optimizationContext"/>, then promotes its
		/// parameters into the cross-statement sharing set. Shared by RenderScenarioGroups (compile-time, per plan group) and
		/// RenderCombinedBatchTemplates (execute-time, length-split) so both concatenate statements identically.
		/// </summary>
		internal static void AppendConcatenatedStatement(
			StringBuilder sb, ISqlBuilder sqlBuilder, OptimizationContext optimizationContext,
			SqlStatement statement, AliasesContext aliases, bool first, int startIndent)
		{
			if (!first)
				sb.Append(";\n");

#if BUGCHECK
			RenderDiagnostics.BuildSqlCount++;
#endif
			using (ActivityService.Start(ActivityID.BuildSql))
				sqlBuilder.BuildSql(statement, sb, optimizationContext, aliases, null, startIndent);

			optimizationContext.PromoteParametersForSharing();
		}

		/// <summary>
		/// Renders each physical group as ONE command through the group-scoped shared context: a group's step statements
		/// render into one command text (the shared param normalizer within the group yields unique/deduped names);
		/// parameters are cleared at each group boundary so every command carries only its own params.
		/// </summary>
		public static CommandWithParameters[] RenderScenarioGroups(
			SqlCommandScenario  scenario,
			SqlCommandGroupPlan plan,
			SqlStatement        mainStatement,
			AliasesContext      mainAliases,
			ISqlBuilder         sqlBuilder,
			OptimizationContext optimizationContext,
			IServiceProvider    serviceProvider,
			StringBuilder       sb,
			int                 startIndent)
		{
			var commands = new CommandWithParameters[plan.Groups.Count];

			// A combined group renders as one command; share a parameter across its statements (match the remote
			// separate-command path) instead of minting @p_1. Single-step groups have no prior statement, so no-op.
			optimizationContext.ShareParametersByAccessor = true;

			for (var g = 0; g < plan.Groups.Count; g++)
			{
				var stepIndexes = plan.Groups[g].StepIndexes;

				sb.Length = 0;

				for (var k = 0; k < stepIndexes.Count; k++)
				{
					var step        = scenario.Steps[stepIndexes[k]];
					var statement   = step.Statement!; // combined-group steps always carry a statement (self-executing steps are singletons)
					var stepAliases = ReferenceEquals(statement, mainStatement) ? mainAliases : PrepareStepAliases(serviceProvider, statement);

					AppendConcatenatedStatement(sb, sqlBuilder, optimizationContext, statement, stepAliases, k == 0, startIndent);
				}

				commands[g] = new CommandWithParameters(sb.ToString(), optimizationContext.GetParameters());
				optimizationContext.ClearParameters();
			}

			return commands;
		}

#if SUPPORTS_DBBATCH
		/// <summary>
		/// Renders each statement's SQL and collects its (unbound)
		/// <see cref="SqlParameter"/> list into a <see cref="CommandWithParameters"/> (fresh normalizer per statement, so
		/// names are independent). For a non-parameter-dependent scenario the result is stable and can be cached across
		/// executions; only DbParameter binding (<see cref="MaterializeDbParameters"/>) then repeats per execution.
		/// </summary>
		internal static CommandWithParameters[] RenderStatementTemplates(
			DataConnection dataConnection, IReadOnlyList<SqlStatement> statements, IReadOnlyParameterValues? parameterValues)
		{
			var options      = dataConnection.Options;
			var sqlOptimizer = dataConnection.DataProvider.GetSqlOptimizer (options);
			var sqlBuilder   = dataConnection.DataProvider.CreateSqlBuilder(dataConnection.MappingSchema, options);
			var factory      = sqlOptimizer.CreateSqlExpressionFactory(dataConnection.MappingSchema, options);

			var serviceProvider = ((IInfrastructure<IServiceProvider>)dataConnection.DataProvider).Instance;

			var result = new CommandWithParameters[statements.Count];

			using var sb = Pools.StringBuilder.Allocate();

			for (var i = 0; i < statements.Count; i++)
			{
				var statement = statements[i];

				// Convert+alias upfront (then render with no build-time re-convert) only when the statement is NOT
				// parameter-dependent, so the AliasesContext is built over the final nodes and references (e.g. an
				// eager-grouping JOIN key) resolve. Run the parameter-dependence WALK, not just the flag: the flag is
				// only set by GetCommand for the main query, so an eager child (e.g. one carrying a Rows==null VALUES
				// table, whose rows are enumerated at render) would otherwise look non-dependent and lose its values.
				// Each DbBatch statement is its own command/scope, so the decision is per statement.
				var convertAll = !statement.IsParameterDependent
					&& !sqlOptimizer.IsParameterDependent(NullabilityContext.NonQuery, dataConnection.MappingSchema, statement, options);

				// Fresh normalizer per statement => each statement's parameters are named independently.
				var optimizationContext = CreateRenderContext(dataConnection, sqlOptimizer, factory, parameterValues, convertAll);

				sb.Value.Length = 0;

				// Always convert upfront (parameter-dependent statements too) so the builder never re-converts per element.
				statement = optimizationContext.OptimizeAndConvertAll(statement, NullabilityContext.GetContext(statement.SelectQuery));

				var aliases = PrepareStepAliases(serviceProvider, statement);

#if BUGCHECK
				RenderDiagnostics.BuildSqlCount++;
#endif
				using (ActivityService.Start(ActivityID.BuildSql))
					sqlBuilder.BuildSql(statement, sb.Value, optimizationContext, aliases, null, 0);

				result[i] = new CommandWithParameters(sb.Value.ToString(), optimizationContext.GetParameters());
			}

			return result;
		}
#endif
	}
}
