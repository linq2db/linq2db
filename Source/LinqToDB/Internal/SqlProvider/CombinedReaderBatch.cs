using System;
using System.Collections.Generic;
using System.Globalization;

using LinqToDB.Data;
using LinqToDB.Internal.SqlQuery;

#pragma warning disable MA0048 // CombinedReaderStep grouped with the batch that consumes it

namespace LinqToDB.Internal.SqlProvider
{
	/// <summary>
	/// One reader statement participating in a <see cref="CombinedReaderBatch"/>, paired with the
	/// <see cref="SqlCommandExecutionContext"/> slot its result lands in.
	/// </summary>
	/// <param name="ContextSlot">
	/// Assigned by the OWNER, not by position in the batch: combined eager loading uses the harvester's build index — so a
	/// batch covers a <em>sparse</em> set, because self-executing harvesters occupy slots in between — while a future
	/// batch (#1404) would use registration order. Keeping it explicit is what lets one engine serve both.
	/// </param>
	/// <param name="Statement">The statement to render. Must be a real statement; a slot with no SQL never joins a batch.</param>
	readonly record struct CombinedReaderStep(int ContextSlot, SqlStatement Statement);

	/// <summary>
	/// Turns a set of reader statements into physical combined commands bound to one execution's parameter values.
	/// <para>
	/// This is the <em>mechanical</em> half of combined execution: chunking by
	/// <see cref="SqlProviderFlags.MaxStatementsPerCombinedGroup"/>, the DbBatch / semicolon-concat split, per-step
	/// parameter-dependence, the template cache handshake and DbParameter binding. It deliberately does NOT own
	/// materialization, group ordering or result streaming — those differ per consumer (combined eager loading runs
	/// self-executing preambles first and streams its last result set; a future batch materializes every slot), so they
	/// stay with the caller.
	/// </para>
	/// <para>
	/// Extracted so the second consumer — deferred / future batches, see issue #1404 — reuses the split and binding rather
	/// than copying them.
	/// </para>
	/// </summary>
	/// <remarks>
	/// A <see langword="readonly struct"/>: it is created per execution and lives only for the <see cref="Bind"/> call, so
	/// there is no reason to put it on the heap.
	/// </remarks>
	readonly struct CombinedReaderBatch(DataConnection dataConnection, IReadOnlyList<CombinedReaderStep> steps)
	{
		/// <summary>
		/// Produces the commands to run for this execution.
		/// <para>
		/// With <paramref name="cachedTemplates"/> (already checked by the caller to match this run's backend) nothing is
		/// re-rendered: the templates are bound to <paramref name="values"/>, and a parameter-dependent DbBatch slot — a null
		/// entry — is re-rendered on its own. Without them the batch renders cold.
		/// </para>
		/// </summary>
		/// <param name="values">This execution's parameter values, collected by the caller from its participants.</param>
		/// <param name="useBatch">Whether the connection can run a DbBatch this execution (<c>DataConnection.CanUseDbBatch</c>).</param>
		/// <param name="cachedTemplates">The parameter-independent templates from a previous run, or <see langword="null"/> for a cold render.</param>
		/// <param name="templatesToCache">
		/// What the caller should store for reuse, or <see langword="null"/> when there is nothing new to store — either the
		/// warm path ran (the caller already holds them) or the render is not cacheable.
		/// </param>
		public List<CombinedCommand> Bind(
			SqlParameterValues     values,
			bool                   useBatch,
			PreparedCommand[]?     cachedTemplates,
			out PreparedCommand[]? templatesToCache)
		{
			if (cachedTemplates != null)
			{
				var bound = new List<CombinedCommand>(cachedTemplates.Length);

				foreach (var template in cachedTemplates)
					bound.Add(BindTemplate(template, useBatch, values));

				templatesToCache = null;

				return bound;
			}

			return RenderCold(values, useBatch, out templatesToCache);
		}

#if SUPPORTS_DBBATCH
		/// <summary>
		/// The statement for a context slot, fetched from the batch's own participant list. Lets a warm execution re-render a
		/// parameter-dependent DbBatch slot without rebuilding any scenario just to read one statement out of it. The scan is
		/// over the batch's participants (a handful), so a map would cost more than it saves.
		/// <para>
		/// Only a DbBatch slot is ever re-rendered on its own, so on a target framework without the DbBatch API this has no
		/// call site — hence the guard, which also keeps the unused-member analyzer quiet there.
		/// </para>
		/// </summary>
		SqlStatement StatementForSlot(int contextSlot)
		{
			foreach (var step in steps)
				if (step.ContextSlot == contextSlot)
					return step.Statement;

			throw new InvalidOperationException(
				"Context slot " + contextSlot.ToString(CultureInfo.InvariantCulture) + " is not part of this combined reader batch.");
		}
#endif

		/// <summary>
		/// Binds one template to this execution's values. On the DbBatch backend a null slot is a parameter-dependent
		/// statement re-rendered here in its own isolated scope; concat carries a single, always-present merged command.
		/// </summary>
		CombinedCommand BindTemplate(PreparedCommand command, bool useBatch, SqlParameterValues values)
		{
			var stepIndexes = command.StepIndexes;

			if (useBatch)
			{
				var slots    = command.Batch!;
				var rendered = new RenderedStatement[slots.Length];

				for (var i = 0; i < slots.Length; i++)
				{
					var slot = slots[i];

					if (slot is null)
					{
#if SUPPORTS_DBBATCH
						slot = ScenarioCommandRenderer.RenderStatementTemplates(
							dataConnection, [StatementForSlot(stepIndexes[i])], values)[0];
#else
						throw new InvalidOperationException("Combined batch cache slot is null on a non-DbBatch build.");
#endif
					}

					rendered[i] = new RenderedStatement(
						slot.Command,
						ScenarioCommandRenderer.MaterializeDbParameters(dataConnection, slot.SqlParameters, values));
				}

				return new CombinedCommand(rendered, stepIndexes, null);
			}

			var concat = command.Concat!;

			return new CombinedCommand(
				[new RenderedStatement(concat.Command, ScenarioCommandRenderer.MaterializeDbParameters(dataConnection, concat.SqlParameters, values))],
				stepIndexes, null);
		}

		/// <summary>
		/// Per-participant parameter-dependence: a statement whose SQL varies with parameter values cannot have its render
		/// cached. Finer than an all-or-nothing check — a stable main query is cached while a <c>Contains</c>-filtered
		/// (<c>LIKE</c>) detail re-renders. Indexed by position in the participant list, not by context slot.
		/// </summary>
		bool[] ComputeVolatility()
		{
			var options      = dataConnection.Options;
			var sqlOptimizer = dataConnection.DataProvider.GetSqlOptimizer(options);
			var result       = new bool[steps.Count];

			for (var i = 0; i < steps.Count; i++)
			{
				var statement = steps[i].Statement;

				result[i] = statement.IsParameterDependent
					|| sqlOptimizer.IsParameterDependent(NullabilityContext.NonQuery, dataConnection.MappingSchema, statement, options);
			}

			return result;
		}

		/// <summary>
		/// Cold render: the participants are chunked by the provider's per-command statement cap, then each chunk renders
		/// into one command (DbBatch — one isolated-scope statement per step) or into one or more commands bounded by SQL
		/// length (concat). Each command records the context slots it covers, in order, so the caller knows which result set
		/// belongs to which participant.
		/// </summary>
		List<CombinedCommand> RenderCold(SqlParameterValues values, bool useBatch, out PreparedCommand[]? templatesToCache)
		{
			var volatility  = ComputeVolatility();
			var anyVolatile = false;

			foreach (var v in volatility)
			{
				if (v)
				{
					anyVolatile = true;
					break;
				}
			}

			var maxPerGroup = Math.Max(1, dataConnection.DataProvider.SqlProviderFlags.MaxStatementsPerCombinedGroup);

			var commands  = new List<CombinedCommand>();
			var templates = new List<PreparedCommand>();

			for (var start = 0; start < steps.Count; start += maxPerGroup)
			{
				var count           = Math.Min(maxPerGroup, steps.Count - start);
				var groupStatements = new SqlStatement[count];
				var groupSlots      = new int[count];

				for (var k = 0; k < count; k++)
				{
					groupStatements[k] = steps[start + k].Statement;
					groupSlots     [k] = steps[start + k].ContextSlot;
				}

#if SUPPORTS_DBBATCH
				// DbBatch: the whole chunk is one command carrying one isolated-scope statement per step (each its own
				// DbBatchCommand + parameter scope); no SQL-length split, so it covers every step of the chunk in order.
				if (useBatch)
				{
					var rendered = ScenarioCommandRenderer.RenderStatementTemplates(dataConnection, groupStatements, values);

					// Run this execution off the freshly-rendered statements (bind all, no re-render).
					commands.Add(BindTemplate(new PreparedCommand(groupSlots, null, rendered), useBatch, values));

					// Cache with the parameter-dependent slots nulled so a later run re-renders only those.
					var cacheSlots = new CommandWithParameters?[rendered.Length];

					for (var k = 0; k < rendered.Length; k++)
						cacheSlots[k] = volatility[start + k] ? null : rendered[k];

					templates.Add(new PreparedCommand(groupSlots, null, cacheSlots));

					continue;
				}
#endif

				// Concat: each length-split batch is one pre-merged (semicolon-concatenated) command.
				var batches = DataConnection.QueryRunner.RenderCombinedBatchTemplates(dataConnection, groupStatements, values);

				var offset = 0;

				foreach (var (sql, sqlParameters, statementCount) in batches)
				{
					var slots = new int[statementCount];

					for (var k = 0; k < statementCount; k++)
						slots[k] = groupSlots[offset + k];

					var command = new PreparedCommand(slots, new CommandWithParameters(sql, sqlParameters), null);

					templates.Add(command);
					commands .Add(BindTemplate(command, useBatch, values));

					offset += statementCount;
				}
			}

			// Batch caches per-statement (null slots for parameter-dependent steps, re-rendered per run); concat is
			// all-or-nothing, so it is not cacheable at all when any step is volatile — its length-split slices share one
			// stateful parameter normalizer. That fallback re-renders on every execution.
			templatesToCache = useBatch || !anyVolatile ? templates.ToArray() : null;

			return commands;
		}
	}
}
