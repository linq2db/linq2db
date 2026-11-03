using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Async;
using LinqToDB.Data;
using LinqToDB.Data.RetryPolicy;
using LinqToDB.Tools.Comparers;

using NUnit.Framework;

using Shouldly;

namespace Tests.Linq
{
	// Eager loads that force UseCombinedCommands on: in the parameterized EagerLoadingTests the (False) instance would only
	// skip them.
	[TestFixture]
	public class CombinedEagerLoadTests : TestBase
	{
		// Regression for the combined eager engine (Source/LinqToDB/Internal/Linq/QueryRunner.cs) merging
		// HETEROGENEOUS combinable children in one query: a KEYED correlated child (CorrelatedChildren,
		// FK-correlated -> Harvester<TKey,T>) and a NON-KEYED detached child (no master dependency ->
		// DetachedHarvester<T>) merge into a single multi-result-set command, and each still materializes
		// correctly for every master row (the correlated per-master, the detached shared across all).
		[Test]
		public void CorrelatedAndDetachedChildren_CombineIntoSingleCommand(
			[IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var (masters, correlated, detached) = EagerLoadingTests.MixedEagerData.Generate();

			// This test exists to assert the combined engine merges heterogeneous children, so it opts into the policy
			// switch explicitly rather than relying on the default (off in 6.x).
			using var db = GetDataContext(context, o => o.UseCombinedCommands(true));

			var counter = new EagerLoadingTests.CombinedCommandCounter();
			db.AddInterceptor(counter);

			using var tMaster     = db.CreateLocalTable(masters);
			using var tCorrelated = db.CreateLocalTable(correlated);
			using var tDetached   = db.CreateLocalTable(detached);

			counter.Count = 0;

			var query =
				from m in tMaster
				orderby m.Id
				select new
				{
					m.Id,
					m.Name,
					Correlated = tCorrelated
						.Where(c => c.MasterId == m.Id)
						.OrderBy(c => c.Id)
						.ToList(),
					Detached = tDetached
						.OrderBy(d => d.Id)
						.ToList(),
				};

			var result = query.ToList();

			var expected = masters
				.OrderBy(m => m.Id)
				.Select(m => new
				{
					m.Id,
					m.Name,
					Correlated = correlated
						.Where(c => c.MasterId == m.Id)
						.OrderBy(c => c.Id)
						.ToList(),
					Detached = detached
						.OrderBy(d => d.Id)
						.ToList(),
				})
				.ToList();

			AreEqual(expected, result, ComparerBuilder.GetEqualityComparer(expected));

			// Every master must see its own correlated rows (never another master's) and the full detached set.
			var expectedDetached = detached
				.Select(d => d.Id)
				.OrderBy(id => id)
				.ToList();

			foreach (var m in result)
			{
				var expectedCorrelated = correlated
					.Where(c => c.MasterId == m.Id)
					.Select(c => c.Id)
					.OrderBy(id => id)
					.ToList();

				var correlatedIds = m.Correlated
					.Select(c => c.Id)
					.OrderBy(id => id)
					.ToList();

				var detachedIds = m.Detached
					.Select(d => d.Id)
					.OrderBy(id => id)
					.ToList();

				correlatedIds.ShouldBe(expectedCorrelated);
				detachedIds.ShouldBe(expectedDetached);
			}

			// Both children are combinable, so the detached SELECT, the correlated child, and the main query
			// all merge into ONE combined multi-result-set command (a single round-trip).
			counter.Count.ShouldBe(1);
		}

		// Runs the same combined eager load down BOTH execution backends: the ADO.NET DbBatch (UseDbBatch on, the
		// default) and the semicolon-concatenated single command it falls back to. Providers here are the batch-capable
		// ones on net8.0+, so the axis is a real fork rather than two identical runs.
		//
		// This test registers NO interceptor on purpose. DataConnection.CanUseDbBatch requires Interceptor == null, so
		// any interceptor-based assertion would itself force the concat path and collapse the axis — which is also why
		// the UseDbBatch=false branch had no option-driven coverage before: it was reachable only through a registered
		// ICommandInterceptor or on net462. The backend is read from tracing instead, which is interceptor-free.
		[Test]
		public void CombinedEagerLoad_HonoursDbBatchBackend(
			[IncludeDataSources(TestProvName.AllSqlServer, TestProvName.AllPostgreSQL, TestProvName.AllMySql)] string context,
			[Values] bool useDbBatch)
		{
			var (masters, correlated, detached) = EagerLoadingTests.MixedEagerData.Generate();
			var commands                        = new List<string>();

			using var db = GetDataContext(context, o => o
				.UseCombinedCommands(true)
				.UseDbBatch(useDbBatch)
				.UseTracing(TraceLevel.Info, info =>
				{
					if (info.TraceInfoStep == TraceInfoStep.BeforeExecute)
						commands.Add(info.SqlText);
				}));

			using var tMaster     = db.CreateLocalTable(masters);
			using var tCorrelated = db.CreateLocalTable(correlated);
			using var tDetached   = db.CreateLocalTable(detached);

			var query =
				from m in tMaster
				orderby m.Id
				select new
				{
					m.Id,
					Correlated = tCorrelated
						.Where(c => c.MasterId == m.Id)
						.OrderBy(c => c.Id)
						.ToList(),
					Detached = tDetached
						.OrderBy(d => d.Id)
						.ToList(),
				};

			commands.Clear();

			var result = query.ToList();

			result.Count.ShouldBe(masters.Length);

			var expectedDetached = detached
				.Select(d => d.Id)
				.OrderBy(id => id)
				.ToList();

			// A batch binds each statement's parameters in its own scope while the concat form uniquifies them across
			// statements, so identical materialization on both backends is the property worth pinning.
			foreach (var m in result)
			{
				var expectedCorrelated = correlated
					.Where(c => c.MasterId == m.Id)
					.Select(c => c.Id)
					.OrderBy(id => id)
					.ToList();

				m.Correlated
					.Select(c => c.Id)
					.ShouldBe(expectedCorrelated);

				m.Detached
					.Select(d => d.Id)
					.ShouldBe(expectedDetached);
			}

#if SUPPORTS_DBBATCH
			// The harness registers a command interceptor on the sequential-access configurations, which forces the
			// concatenated backend.
			var batch = useDbBatch
				&& !context.IsAnyOf(TestProvName.AllSqlServerSequentialAccess)
				&& ((DataConnection)db).OpenDbConnection().CanCreateBatch;
#else
			var batch = false;
#endif

			commands
				.Any(c => c.Contains("-- Command 1", StringComparison.Ordinal))
				.ShouldBe(batch);
		}

		// The DbBatch is created on the provider connection beneath RetryingDbConnection, so the retry policy wraps it only
		// when the batch execution calls the policy itself.
		[Test]
		public void CombinedEagerLoad_ExecutesThroughRetryPolicy(
			[IncludeDataSources(TestProvName.AllSqlServer, TestProvName.AllPostgreSQL, TestProvName.AllMySql)] string context)
		{
			var (masters, correlated, _) = EagerLoadingTests.MixedEagerData.Generate();
			var policy                   = new ReaderCountingRetryPolicy();
			var commands                 = new List<string>();

			using var db = GetDataContext(context, o => o
				.UseCombinedCommands(true)
				.UseRetryPolicy(policy)
				.UseTracing(TraceLevel.Info, info =>
				{
					if (info.TraceInfoStep == TraceInfoStep.BeforeExecute && info.Operation == TraceOperation.ExecuteReader)
						commands.Add(info.SqlText);
				}));

			using var tMaster     = db.CreateLocalTable(masters);
			using var tCorrelated = db.CreateLocalTable(correlated);

			var query =
				from m in tMaster
				orderby m.Id
				select new
				{
					m.Id,
					Correlated = tCorrelated
						.Where(c => c.MasterId == m.Id)
						.ToList(),
				};

			commands.Clear();
			policy.Readers = 0;

			var result = query.ToList();

			result.Count.ShouldBe(masters.Length);

			AssertSingleCommandUnderPolicy(context, db, commands, policy);
		}

		[Test]
		public async Task CombinedEagerLoad_ExecutesThroughRetryPolicyAsync(
			[IncludeDataSources(TestProvName.AllSqlServer, TestProvName.AllPostgreSQL, TestProvName.AllMySql)] string context)
		{
			var (masters, correlated, _) = EagerLoadingTests.MixedEagerData.Generate();
			var policy                   = new ReaderCountingRetryPolicy();
			var commands                 = new List<string>();

			await using var db = GetDataContext(context, o => o
				.UseCombinedCommands(true)
				.UseRetryPolicy(policy)
				.UseTracing(TraceLevel.Info, info =>
				{
					if (info.TraceInfoStep == TraceInfoStep.BeforeExecute && info.Operation == TraceOperation.ExecuteReader)
						commands.Add(info.SqlText);
				}));

			await using var tMaster     = db.CreateLocalTable(masters);
			await using var tCorrelated = db.CreateLocalTable(correlated);

			var query =
				from m in tMaster
				orderby m.Id
				select new
				{
					m.Id,
					Correlated = tCorrelated
						.Where(c => c.MasterId == m.Id)
						.ToList(),
				};

			commands.Clear();
			policy.Readers = 0;

			var result = await query.ToListAsync();

			result.Count.ShouldBe(masters.Length);

			AssertSingleCommandUnderPolicy(context, db, commands, policy);
		}

		static void AssertSingleCommandUnderPolicy(string context, IDataContext db, List<string> commands, ReaderCountingRetryPolicy policy)
		{
#if SUPPORTS_DBBATCH
			var batch = !context.IsAnyOf(TestProvName.AllSqlServerSequentialAccess)
				&& ((DataConnection)db).OpenDbConnection().CanCreateBatch;
#else
			var batch = false;
#endif

			commands.Count.ShouldBe(1);
			commands[0]
				.Contains("-- Command 1", StringComparison.Ordinal)
				.ShouldBe(batch);
			policy.Readers.ShouldBe(1);
		}

		sealed class ReaderCountingRetryPolicy : IRetryPolicy
		{
			public int Readers { get; set; }

			public TResult Execute<TResult>(Func<TResult> operation)
			{
				if (typeof(TResult) == typeof(DbDataReader))
					Readers++;

				return operation();
			}

			public void Execute(Action operation) => operation();

			public Task<TResult> ExecuteAsync<TResult>(Func<CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken = default)
			{
				if (typeof(TResult) == typeof(DbDataReader))
					Readers++;

				return operation(cancellationToken);
			}

			public Task ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default) => operation(cancellationToken);
		}
	}
}
