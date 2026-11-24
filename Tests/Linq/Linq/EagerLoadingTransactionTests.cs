using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Async;
using LinqToDB.Data;
using LinqToDB.Internal.Async;

using NUnit.Framework;

using Shouldly;

using Tests.Model;

namespace Tests.Linq
{
	// The read-consistency transaction an eager load opens: LinqOptions.UseEagerLoadingTransaction turns it off,
	// LinqOptions.EagerLoadingIsolationLevel replaces the provider's level. Each test runs both values of the option and
	// expects the same rows from both.
	//
	// The tests run on SQLite: the transaction is decided in Query.GetTransactionOwner before any provider code runs,
	// SQLite supports transactions, and its default level (Serializable) differs from the override used here
	// (ReadCommitted). Two of them also run on SQL Server, whose default level (RepeatableRead) differs from SQLite's, so
	// the fallback to the provider's level is told apart from a fixed level. Remote contexts are left out because they
	// never open the transaction and trace on the server.
	[TestFixture]
	public class EagerLoadingTransactionTests : TestBase
	{
		sealed class OperationTrace
		{
			public readonly List<TraceOperation> Operations = new();
			public readonly List<string?>        Begins     = new();

			public void Clear()
			{
				Operations.Clear();
				Begins.Clear();
			}
		}

		sealed class ParentChildIds
		{
			public int       ParentID;
			public List<int> ChildIDs = null!;
		}

		static Func<DataOptions, DataOptions> Traced(OperationTrace trace, bool useTransaction, IsolationLevel? isolationLevel = null) => options => options
			.UseEagerLoadingTransaction(useTransaction)
			.UseEagerLoadingIsolationLevel(isolationLevel)
			.UseTracing(TraceLevel.Info, ti =>
			{
				if (ti.TraceInfoStep != TraceInfoStep.BeforeExecute)
					return;

				if (ti.Operation is TraceOperation.ExecuteReader or TraceOperation.ExecuteNonQuery or TraceOperation.ExecuteScalar
					or TraceOperation.BeginTransaction or TraceOperation.CommitTransaction or TraceOperation.RollbackTransaction)
				{
					trace.Operations.Add(ti.Operation);

					if (ti.Operation == TraceOperation.BeginTransaction)
						trace.Begins.Add(ti.CommandText);
				}
			});

		static DataContext CreateDataContext(string context, OperationTrace trace, bool useTransaction, IsolationLevel? isolationLevel = null)
		{
			var options = new DataOptions()
				.UseConfiguration(context);

			return new DataContext(Traced(trace, useTransaction, isolationLevel)(options));
		}

		// The main query and one child query, inside the transaction when it is opened.
		static void AssertTrace(OperationTrace trace, bool useTransaction, bool isAsync, IsolationLevel isolationLevel)
		{
			TraceOperation[] readers = [TraceOperation.ExecuteReader, TraceOperation.ExecuteReader];

			trace.Operations.ShouldBe(useTransaction
				? [TraceOperation.BeginTransaction, .. readers, TraceOperation.CommitTransaction]
				: readers);

			string?[] begins = useTransaction
				? [isAsync ? $"BeginTransactionAsync({isolationLevel})" : $"BeginTransaction({isolationLevel})"]
				: [];

			trace.Begins.ShouldBe(begins);
		}

		static string Format(int parentId, IEnumerable<int> childIds)
		{
			var ids = childIds
				.OrderBy(id => id);

			return $"{parentId}:{string.Join(",", ids)}";
		}

		static string FormatParent(Parent parent)
		{
			var childIds = parent.Children
				.Select(c => c.ChildID);

			return Format(parent.ParentID, childIds);
		}

		static string[] Format(IEnumerable<Parent> parents)
		{
			return parents
				.OrderBy(p => p.ParentID)
				.Select(FormatParent)
				.ToArray();
		}

		string[] ExpectedParents()
		{
			return Parent
				.OrderBy(p => p.ParentID)
				.Select(p => ExpectedParent(p.ParentID))
				.ToArray();
		}

		string ExpectedParent(int parentId)
		{
			var childIds = Child
				.Where(c => c.ParentID == parentId)
				.Select(c => c.ChildID);

			return Format(parentId, childIds);
		}

		// SQLite and SQL Server: SQL Server's default level (RepeatableRead) differs from SQLite's (Serializable), so the
		// fallback to the provider's level is checked too (see the fixture comment).
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4053")]
		public async Task EnumerableOnDataConnection([IncludeDataSources(false, TestProvName.AllSQLite, TestProvName.AllSqlServer)] string context, [Values] bool useTransaction, [Values] bool isAsync)
		{
			var trace = new OperationTrace();

			using var db = GetDataContext(context, Traced(trace, useTransaction));

			trace.Clear();

			var query = db.Parent
				.LoadWith(p => p.Children);

			var parents = isAsync
				? await query.ToListAsync()
				: query.ToList();

			Format(parents)
				.ShouldBe(ExpectedParents());

			AssertTrace(trace, useTransaction, isAsync, db.SqlProviderFlags.DefaultMultiQueryIsolationLevel);
		}

		// SQLite only: the transaction is decided before any provider code runs (see the fixture comment).
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4053")]
		public async Task ElementOnDataConnection([IncludeDataSources(false, TestProvName.AllSQLite)] string context, [Values] bool useTransaction, [Values] bool isAsync)
		{
			var trace = new OperationTrace();

			using var db = GetDataContext(context, Traced(trace, useTransaction));

			trace.Clear();

			var query = db.Parent
				.Select(p => new
				{
					p.ParentID,
					ChildIDs = p.Children
						.Select(c => c.ChildID)
						.ToList(),
				});

			var parent = isAsync
				? await query.FirstOrDefaultAsync(p => p.ParentID == 1)
				: query.FirstOrDefault(p => p.ParentID == 1);

			parent
				.ShouldNotBeNull();

			Format(parent.ParentID, parent.ChildIDs)
				.ShouldBe(ExpectedParent(1));

			AssertTrace(trace, useTransaction, isAsync, db.SqlProviderFlags.DefaultMultiQueryIsolationLevel);
		}

		// SQLite only: the transaction is decided before any provider code runs (see the fixture comment).
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4053")]
		public void ElementThroughNonGenericExecute([IncludeDataSources(false, TestProvName.AllSQLite)] string context, [Values] bool useTransaction)
		{
			var trace = new OperationTrace();

			using var db = GetDataContext(context, Traced(trace, useTransaction));

			trace.Clear();

			var query = db.Parent
				.Where(p => p.ParentID == 1)
				.Select(p => new ParentChildIds
				{
					ParentID = p.ParentID,
					ChildIDs = p.Children
						.Select(c => c.ChildID)
						.ToList(),
				});

			var firstOrDefault = Expression.Call(
				typeof(Queryable),
				nameof(Queryable.FirstOrDefault),
				[query.ElementType],
				query.Expression);

			var parent = (ParentChildIds?)query.Provider.Execute(firstOrDefault);

			parent
				.ShouldNotBeNull();

			Format(parent.ParentID, parent.ChildIDs)
				.ShouldBe(ExpectedParent(1));

			AssertTrace(trace, useTransaction, isAsync: false, db.SqlProviderFlags.DefaultMultiQueryIsolationLevel);
		}

		// SQLite only: the transaction is decided before any provider code runs (see the fixture comment).
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4053")]
		public async Task AwaitForeach([IncludeDataSources(false, TestProvName.AllSQLite)] string context, [Values] bool useTransaction)
		{
			var trace = new OperationTrace();

			using var db = GetDataContext(context, Traced(trace, useTransaction));

			trace.Clear();

			var parents = new List<Parent>();

			var query = db.Parent
				.LoadWith(p => p.Children)
				.AsAsyncEnumerable();

			await foreach (var parent in query)
				parents.Add(parent);

			Format(parents)
				.ShouldBe(ExpectedParents());

			AssertTrace(trace, useTransaction, isAsync: true, db.SqlProviderFlags.DefaultMultiQueryIsolationLevel);
		}

		// SQLite only: the transaction is decided before any provider code runs (see the fixture comment).
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4053")]
		public async Task ExecuteAsyncEnumerable([IncludeDataSources(false, TestProvName.AllSQLite)] string context, [Values] bool useTransaction)
		{
			var trace = new OperationTrace();

			using var db = GetDataContext(context, Traced(trace, useTransaction));

			trace.Clear();

			var query = db.Parent
				.LoadWith(p => p.Children);

			var provider = (IQueryProviderAsync)query.Provider;

			var enumerable = await provider.ExecuteAsyncEnumerable<Parent>(query.Expression, CancellationToken.None);

			var parents = new List<Parent>();

			await foreach (var parent in enumerable)
				parents.Add(parent);

			Format(parents)
				.ShouldBe(ExpectedParents());

			AssertTrace(trace, useTransaction, isAsync: true, db.SqlProviderFlags.DefaultMultiQueryIsolationLevel);
		}

		// SQLite only: the transaction is decided before any provider code runs (see the fixture comment).
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4053")]
		public async Task CompiledElement([IncludeDataSources(false, TestProvName.AllSQLite)] string context, [Values] bool useTransaction, [Values] bool isAsync)
		{
			var element = CompiledQuery.Compile<ITestDataContext, int, Parent>(static (db, id) =>
				db.Parent
					.Where(p => p.ParentID == id)
					.LoadWith(p => p.Children)
					.First());

			var elementAsync = CompiledQuery.Compile<ITestDataContext, int, CancellationToken, Task<Parent>>(static (db, id, token) =>
				db.Parent
					.Where(p => p.ParentID == id)
					.LoadWith(p => p.Children)
					.FirstAsync(token));

			var trace = new OperationTrace();

			using var db = GetDataContext(context, Traced(trace, useTransaction));

			trace.Clear();

			var parent = isAsync
				? await elementAsync(db, 1, CancellationToken.None)
				: element(db, 1);

			FormatParent(parent)
				.ShouldBe(ExpectedParent(1));

			AssertTrace(trace, useTransaction, isAsync, db.SqlProviderFlags.DefaultMultiQueryIsolationLevel);
		}

		// SQLite only: the transaction is decided before any provider code runs (see the fixture comment).
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4053")]
		public async Task EnumerableOnDataContext([IncludeDataSources(false, TestProvName.AllSQLite)] string context, [Values] bool useTransaction, [Values] bool isAsync)
		{
			var trace = new OperationTrace();

			using var db = CreateDataContext(context, trace, useTransaction);

			var query = db.GetTable<Parent>()
				.LoadWith(p => p.Children);

			var parents = isAsync
				? await query.ToListAsync()
				: query.ToList();

			Format(parents)
				.ShouldBe(ExpectedParents());

			AssertTrace(trace, useTransaction, isAsync, db.DataProvider.SqlProviderFlags.DefaultMultiQueryIsolationLevel);
		}

		// With the transaction on, it is opened at the override; with it off, the override opens nothing.
		// SQLite and SQL Server: the override differs from both providers' defaults (see the fixture comment).
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4053")]
		public async Task IsolationLevelOnDataConnection([IncludeDataSources(false, TestProvName.AllSQLite, TestProvName.AllSqlServer)] string context, [Values] bool useTransaction, [Values] bool isAsync)
		{
			var trace = new OperationTrace();

			using var db = GetDataContext(context, Traced(trace, useTransaction, IsolationLevel.ReadCommitted));

			db.SqlProviderFlags.DefaultMultiQueryIsolationLevel
				.ShouldNotBe(IsolationLevel.ReadCommitted);

			trace.Clear();

			var query = db.Parent
				.LoadWith(p => p.Children);

			var parents = isAsync
				? await query.ToListAsync()
				: query.ToList();

			Format(parents)
				.ShouldBe(ExpectedParents());

			AssertTrace(trace, useTransaction, isAsync, IsolationLevel.ReadCommitted);
		}

		// SQLite only: the transaction is decided before any provider code runs (see the fixture comment).
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4053")]
		public async Task IsolationLevelOnDataContext([IncludeDataSources(false, TestProvName.AllSQLite)] string context, [Values] bool useTransaction, [Values] bool isAsync)
		{
			var trace = new OperationTrace();

			using var db = CreateDataContext(context, trace, useTransaction, IsolationLevel.ReadCommitted);

			db.DataProvider.SqlProviderFlags.DefaultMultiQueryIsolationLevel
				.ShouldNotBe(IsolationLevel.ReadCommitted);

			var query = db.GetTable<Parent>()
				.LoadWith(p => p.Children);

			var parents = isAsync
				? await query.ToListAsync()
				: query.ToList();

			Format(parents)
				.ShouldBe(ExpectedParents());

			AssertTrace(trace, useTransaction, isAsync, IsolationLevel.ReadCommitted);
		}
	}
}
