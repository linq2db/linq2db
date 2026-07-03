using System;
using System.Collections.Generic;
using System.Text;

using LinqToDB.Internal.DataProvider;
using LinqToDB.Internal.Infrastructure;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;
using LinqToDB.Metrics;

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
						sqlBuilder.BuildSql(0, step.Statement, sb, optimizationContext, stepAliases, null, startIndent);
				}

				commands[g] = new CommandWithParameters(sb.ToString(), optimizationContext.GetParameters());
				optimizationContext.ClearParameters();
			}

			return commands;
		}
	}
}
