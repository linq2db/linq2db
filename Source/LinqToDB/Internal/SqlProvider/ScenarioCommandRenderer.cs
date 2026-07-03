using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;

using LinqToDB.Data;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.DataProvider;
using LinqToDB.Internal.Infrastructure;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;
using LinqToDB.Metrics;

#pragma warning disable MA0048 // CommandWithParameters record grouped with its shared renderer

namespace LinqToDB.Internal.SqlProvider
{
	/// <summary>A rendered physical command: its SQL text and the parameters it carries.</summary>
	sealed record CommandWithParameters(string Command, IReadOnlyList<SqlParameter> SqlParameters);

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

				if (ReferenceEquals(step.Statement, mainStatement))
					continue;

				var finalStatement = sqlOptimizer.Finalize(mappingSchema, step.Statement, options);

				if (ReferenceEquals(finalStatement, step.Statement))
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

		public static AliasesContext PrepareStepAliases(IServiceProvider serviceProvider, SqlStatement statement)
		{
			AliasesHelper.PrepareQueryAndAliases(serviceProvider.GetRequiredService<IIdentifierService>(), statement, null, out var aliases);
			return aliases;
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

			for (var g = 0; g < plan.Groups.Count; g++)
			{
				var stepIndexes = plan.Groups[g].StepIndexes;

				sb.Length = 0;

				for (var k = 0; k < stepIndexes.Count; k++)
				{
					var step        = scenario.Steps[stepIndexes[k]];
					var stepAliases = ReferenceEquals(step.Statement, mainStatement) ? mainAliases : PrepareStepAliases(serviceProvider, step.Statement);

					if (k > 0)
						sb.Append(";\n");

					using (ActivityService.Start(ActivityID.BuildSql))
						sqlBuilder.BuildSql(step.Statement, sb, optimizationContext, stepAliases, null, startIndent);
				}

				commands[g] = new CommandWithParameters(sb.ToString(), optimizationContext.GetParameters());
				optimizationContext.ClearParameters();
			}

			return commands;
		}

#if SUPPORTS_DBBATCH
		// Renders each statement into its OWN parameter scope (fresh normalizer per statement) for DbBatch execution: the
		// group becomes one DbBatch whose DbBatchCommands each carry only their own parameters (NO cross-statement name
		// uniquification, unlike RenderCombinedBatches which merges a group into one semicolon-concatenated command). No
		// SQL-length splitting - DbBatch sends statements structurally, so the only bound is PlanScenario per-group count.
		internal static IReadOnlyList<(string Sql, DbParameter[]? Parameters)> RenderBatchStatements(
			DataConnection dataConnection, IReadOnlyList<SqlStatement> statements, IReadOnlyParameterValues? parameterValues)
		{
			var options      = dataConnection.Options;
			var sqlOptimizer = dataConnection.DataProvider.GetSqlOptimizer (options);
			var sqlBuilder   = dataConnection.DataProvider.CreateSqlBuilder(dataConnection.MappingSchema, options);
			var factory      = sqlOptimizer.CreateSqlExpressionFactory(dataConnection.MappingSchema, options);

			var serviceProvider = ((IInfrastructure<IServiceProvider>)dataConnection.DataProvider).Instance;

			var result = new (string, DbParameter[]?)[statements.Count];

			using var sb = Pools.StringBuilder.Allocate();

			for (var i = 0; i < statements.Count; i++)
			{
				// Fresh normalizer per statement => each statement's parameters are named independently.
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

				sb.Value.Length = 0;

				var aliases = PrepareStepAliases(serviceProvider, statements[i]);

				using (ActivityService.Start(ActivityID.BuildSql))
					sqlBuilder.BuildSql(statements[i], sb.Value, optimizationContext, aliases, null, 0);

				var sqlParameters = optimizationContext.GetParameters();

				DbParameter[]? dbParameters = null;

				if (sqlParameters.Count > 0)
				{
					var dbCommand = dataConnection.GetOrCreateCommand();

					dbParameters = new DbParameter[sqlParameters.Count];

					for (var p = 0; p < sqlParameters.Count; p++)
					{
						var sqlp = sqlParameters[p];
						dbParameters[p] = DataConnection.QueryRunner.CreateParameter(dataConnection, dbCommand, sqlp, sqlp.GetParameterValue(parameterValues));
					}
				}

				result[i] = (sb.Value.ToString(), dbParameters);
			}

			return result;
		}
#endif
	}
}
