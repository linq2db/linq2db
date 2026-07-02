using System;
using System.Collections.Generic;

using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Linq.Translation;

namespace LinqToDB.Internal.DataProvider
{
	/// <summary>
	/// Base class for provider-specific <see cref="IDmlService"/> implementations. By default
	/// <see cref="BuildCommandScenario"/> returns <see langword="null"/> (the runner falls back to the legacy
	/// <see cref="ISqlBuilder.CommandCount"/> / <c>BuildCommand</c> splitting); providers override it for identity,
	/// truncate-reset, etc. <see cref="PlanScenario"/> defaults to all-singleton (sequential) groups.
	/// </summary>
	public abstract class DmlServiceBase : IDmlService
	{
		/// <summary>
		/// Returns <see langword="null"/> by default — the runner uses the legacy command-splitting path
		/// (<see cref="ISqlBuilder.CommandCount"/> / <c>BuildCommand</c>). Providers override to build an explicit
		/// scenario (identity retrieval, per-field truncate reset, etc.), using <paramref name="factory"/> to
		/// construct any synthetic statements (e.g. an identity <c>SELECT</c>).
		/// </summary>
		public virtual SqlCommandScenario? BuildCommandScenario(SqlStatement statement, SqlProviderFlags flags, ISqlExpressionFactory factory)
		{
			return null;
		}

		/// <summary>
		/// Plans physical command groups. Without <see cref="SqlProviderFlags.IsMultiStatementBatchSupported"/> — or
		/// around a gated step (which must be evaluated client-side before it runs) — every step is its own group
		/// (today's sequential behavior). Otherwise a maximal run of contiguous non-gated steps is combined; a single
		/// combined command may harvest more than one result-producing step only when
		/// <see cref="SqlProviderFlags.IsMultipleResultSetsSupported"/> is set — otherwise the run is split so each
		/// combined group holds at most one result-producing step.
		/// </summary>
		// Upper bound on statements merged into one combined command (see PlanScenario). Keeps a large eager-load
		// fan-out from producing one oversized multi-statement command; 32 SELECTs is a few KB of SQL, well under
		// provider batch / parameter limits, while still collapsing the common N+1 case to a single round-trip.
		const int MaxStatementsPerCombinedGroup = 32;

		public virtual SqlCommandGroupPlan PlanScenario(SqlCommandScenario scenario, SqlProviderFlags flags)
		{
			var steps = scenario.Steps;

			if (!flags.IsMultiStatementBatchSupported)
			{
				var singletons = new SqlCommandGroup[steps.Count];

				for (var i = 0; i < steps.Count; i++)
					singletons[i] = new SqlCommandGroup { StepIndexes = [i] };

				return new SqlCommandGroupPlan { Groups = singletons };
			}

			var multipleResultSets = flags.IsMultipleResultSetsSupported;

			var groups = new List<SqlCommandGroup>();
			var run    = new List<int>();

			// Emits one physical group for a contiguous slice: combined into a single command when it has a
			// result-producing step (Scalar/Reader) and more than one step; otherwise one group per step, so a pure
			// non-query run (e.g. truncate + reset) keeps its per-step rows-affected semantics.
			void EmitGroup(List<int> slice)
			{
				var hasResult = false;

				foreach (var idx in slice)
				{
					if (steps[idx].Kind != SqlStepKind.NonQuery)
					{
						hasResult = true;
						break;
					}
				}

				if (hasResult && slice.Count > 1)
				{
					groups.Add(new SqlCommandGroup { StepIndexes = slice.ToArray() });
				}
				else
				{
					foreach (var idx in slice)
						groups.Add(new SqlCommandGroup { StepIndexes = [idx] });
				}
			}

			// A run of contiguous non-gated steps forms one combined command. One command can harvest more than one
			// result-producing step only when the provider returns multiple result sets (NextResult); without that the
			// run is split so each combined group holds at most one result-producing step (e.g. identity's INSERT +
			// SELECT), and any additional readers fall into their own commands.
			void FlushRun()
			{
				if (run.Count == 0)
					return;

				if (multipleResultSets)
				{
					// Size-aware: cap how many statements merge into one command so a large fan-out (e.g. an eager load
					// with many child collections) splits across several round-trips instead of one oversized command.
					// DML scenarios are tiny (<= a few steps) so this never changes their grouping.
					if (run.Count <= MaxStatementsPerCombinedGroup)
					{
						EmitGroup(run);
					}
					else
					{
						for (var start = 0; start < run.Count; start += MaxStatementsPerCombinedGroup)
							EmitGroup(run.GetRange(start, Math.Min(MaxStatementsPerCombinedGroup, run.Count - start)));
					}
				}
				else
				{
					var slice     = new List<int>();
					var sawResult = false;

					foreach (var idx in run)
					{
						var isResult = steps[idx].Kind != SqlStepKind.NonQuery;

						if (isResult && sawResult)
						{
							EmitGroup(slice);
							slice     = new List<int>();
							sawResult = false;
						}

						slice.Add(idx);

						if (isResult)
							sawResult = true;
					}

					EmitGroup(slice);
				}

				run.Clear();
			}

			for (var i = 0; i < steps.Count; i++)
			{
				if (steps[i].RunIf != null)
				{
					FlushRun();
					groups.Add(new SqlCommandGroup { StepIndexes = [i] });
				}
				else
				{
					run.Add(i);
				}
			}

			FlushRun();

			return new SqlCommandGroupPlan { Groups = groups.ToArray() };
		}

		public bool IsTableNotFoundException(Exception exception)
		{
			ArgumentNullException.ThrowIfNull(exception);

			for (var current = exception; current != null; current = current.InnerException)
			{
				if (IsTableNotFoundExceptionCore(current))
					return true;

				if (current is AggregateException agg)
				{
					foreach (var inner in agg.Flatten().InnerExceptions)
						if (IsTableNotFoundException(inner))
							return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Detects a provider-specific "table not found" exception.
		/// Called for every link of the inner-exception chain.
		/// </summary>
		protected abstract bool IsTableNotFoundExceptionCore(Exception exception);

		/// <summary>
		/// Matches <paramref name="marker"/> against the exception's type name or its message.
		/// The message check is needed for remote (gRPC / HTTP) data contexts where the original
		/// provider exception is wrapped — the type name survives only as text inside the
		/// wrapping exception's <see cref="Exception.Message"/> (populated via <see cref="Exception.ToString"/>).
		/// </summary>
		protected static bool TypeOrMessageContains(Exception exception, string marker)
		{
			var typeName = exception.GetType().FullName;

			return (typeName != null && typeName.Contains(marker, StringComparison.Ordinal))
				|| exception.Message.Contains(marker, StringComparison.Ordinal);
		}

		/// <summary>
		/// True if <paramref name="exception"/>'s <see cref="Exception.HResult"/> matches
		/// <paramref name="hResult"/>, or the remote-transport message wrapper contains the
		/// canonical hex form ("0x1234ABCD").
		/// </summary>
		protected static bool HResultMatches(Exception exception, int hResult)
		{
			if (exception.HResult == hResult)
				return true;

			var hex = "0x" + hResult.ToString("X8", System.Globalization.CultureInfo.InvariantCulture);
			return exception.Message.Contains(hex, StringComparison.OrdinalIgnoreCase);
		}
	}
}
