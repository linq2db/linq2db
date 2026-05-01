#nullable enable
// Generated.
//
using System;
using System.Linq.Expressions;

using LinqToDB.Mapping;

namespace LinqToDB.DataProvider.SqlServer
{
	public static partial class SqlServerHints
	{
		/// <summary>
		/// Adds a SQL Server <c>FORCESCAN</c> table hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Table; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.SqlServer, nameof(WithForceScanTableImpl))]
		public static ISqlServerSpecificTable<TSource> WithForceScan<TSource>(this ISqlServerSpecificTable<TSource> table)
			where TSource : notnull
		{
			return table.TableHint2012Plus(Table.ForceScan);
		}

		static Expression<Func<ISqlServerSpecificTable<TSource>,ISqlServerSpecificTable<TSource>>> WithForceScanTableImpl<TSource>()
			where TSource : notnull
		{
			return table => table.TableHint2012Plus(Table.ForceScan);
		}

		/// <summary>
		/// Adds a SQL Server <c>FORCESCAN</c> table hint to tables in the current query scope.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=TablesInScope; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.SqlServer, nameof(WithForceScanQueryImpl))]
		public static ISqlServerSpecificQueryable<TSource> WithForceScanInScope<TSource>(this ISqlServerSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return query.TablesInScopeHint2012Plus(Table.ForceScan);
		}

		static Expression<Func<ISqlServerSpecificQueryable<TSource>,ISqlServerSpecificQueryable<TSource>>> WithForceScanQueryImpl<TSource>()
			where TSource : notnull
		{
			return query => query.TablesInScopeHint2012Plus(Table.ForceScan);
		}

		/// <summary>
		/// Adds a SQL Server <c>FORCESEEK</c> table hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Table; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.SqlServer, nameof(WithForceSeekTableImpl))]
		public static ISqlServerSpecificTable<TSource> WithForceSeek<TSource>(this ISqlServerSpecificTable<TSource> table)
			where TSource : notnull
		{
			return table.TableHint(Table.ForceSeek);
		}

		static Expression<Func<ISqlServerSpecificTable<TSource>,ISqlServerSpecificTable<TSource>>> WithForceSeekTableImpl<TSource>()
			where TSource : notnull
		{
			return table => table.TableHint(Table.ForceSeek);
		}

		/// <summary>
		/// Adds a SQL Server <c>FORCESEEK</c> table hint to tables in the current query scope.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=TablesInScope; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.SqlServer, nameof(WithForceSeekQueryImpl))]
		public static ISqlServerSpecificQueryable<TSource> WithForceSeekInScope<TSource>(this ISqlServerSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return query.TablesInScopeHint(Table.ForceSeek);
		}

		static Expression<Func<ISqlServerSpecificQueryable<TSource>,ISqlServerSpecificQueryable<TSource>>> WithForceSeekQueryImpl<TSource>()
			where TSource : notnull
		{
			return query => query.TablesInScopeHint(Table.ForceSeek);
		}

		/// <summary>
		/// Adds a SQL Server <c>HOLDLOCK</c> table hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Table; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.SqlServer, nameof(WithHoldLockTableImpl))]
		public static ISqlServerSpecificTable<TSource> WithHoldLock<TSource>(this ISqlServerSpecificTable<TSource> table)
			where TSource : notnull
		{
			return table.TableHint(Table.HoldLock);
		}

		static Expression<Func<ISqlServerSpecificTable<TSource>,ISqlServerSpecificTable<TSource>>> WithHoldLockTableImpl<TSource>()
			where TSource : notnull
		{
			return table => table.TableHint(Table.HoldLock);
		}

		/// <summary>
		/// Adds a SQL Server <c>HOLDLOCK</c> table hint to tables in the current query scope.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=TablesInScope; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.SqlServer, nameof(WithHoldLockQueryImpl))]
		public static ISqlServerSpecificQueryable<TSource> WithHoldLockInScope<TSource>(this ISqlServerSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return query.TablesInScopeHint(Table.HoldLock);
		}

		static Expression<Func<ISqlServerSpecificQueryable<TSource>,ISqlServerSpecificQueryable<TSource>>> WithHoldLockQueryImpl<TSource>()
			where TSource : notnull
		{
			return query => query.TablesInScopeHint(Table.HoldLock);
		}

		/// <summary>
		/// Adds a SQL Server <c>NOLOCK</c> table hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Table; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.SqlServer, nameof(WithNoLockTableImpl))]
		public static ISqlServerSpecificTable<TSource> WithNoLock<TSource>(this ISqlServerSpecificTable<TSource> table)
			where TSource : notnull
		{
			return table.TableHint(Table.NoLock);
		}

		static Expression<Func<ISqlServerSpecificTable<TSource>,ISqlServerSpecificTable<TSource>>> WithNoLockTableImpl<TSource>()
			where TSource : notnull
		{
			return table => table.TableHint(Table.NoLock);
		}

		/// <summary>
		/// Adds a SQL Server <c>NOLOCK</c> table hint to tables in the current query scope.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=TablesInScope; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.SqlServer, nameof(WithNoLockQueryImpl))]
		public static ISqlServerSpecificQueryable<TSource> WithNoLockInScope<TSource>(this ISqlServerSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return query.TablesInScopeHint(Table.NoLock);
		}

		static Expression<Func<ISqlServerSpecificQueryable<TSource>,ISqlServerSpecificQueryable<TSource>>> WithNoLockQueryImpl<TSource>()
			where TSource : notnull
		{
			return query => query.TablesInScopeHint(Table.NoLock);
		}

		/// <summary>
		/// Adds a SQL Server <c>NOWAIT</c> table hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Table; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.SqlServer, nameof(WithNoWaitTableImpl))]
		public static ISqlServerSpecificTable<TSource> WithNoWait<TSource>(this ISqlServerSpecificTable<TSource> table)
			where TSource : notnull
		{
			return table.TableHint(Table.NoWait);
		}

		static Expression<Func<ISqlServerSpecificTable<TSource>,ISqlServerSpecificTable<TSource>>> WithNoWaitTableImpl<TSource>()
			where TSource : notnull
		{
			return table => table.TableHint(Table.NoWait);
		}

		/// <summary>
		/// Adds a SQL Server <c>NOWAIT</c> table hint to tables in the current query scope.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=TablesInScope; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.SqlServer, nameof(WithNoWaitQueryImpl))]
		public static ISqlServerSpecificQueryable<TSource> WithNoWaitInScope<TSource>(this ISqlServerSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return query.TablesInScopeHint(Table.NoWait);
		}

		static Expression<Func<ISqlServerSpecificQueryable<TSource>,ISqlServerSpecificQueryable<TSource>>> WithNoWaitQueryImpl<TSource>()
			where TSource : notnull
		{
			return query => query.TablesInScopeHint(Table.NoWait);
		}

		/// <summary>
		/// Adds a SQL Server <c>PAGLOCK</c> table hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Table; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.SqlServer, nameof(WithPagLockTableImpl))]
		public static ISqlServerSpecificTable<TSource> WithPagLock<TSource>(this ISqlServerSpecificTable<TSource> table)
			where TSource : notnull
		{
			return table.TableHint(Table.PagLock);
		}

		static Expression<Func<ISqlServerSpecificTable<TSource>,ISqlServerSpecificTable<TSource>>> WithPagLockTableImpl<TSource>()
			where TSource : notnull
		{
			return table => table.TableHint(Table.PagLock);
		}

		/// <summary>
		/// Adds a SQL Server <c>PAGLOCK</c> table hint to tables in the current query scope.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=TablesInScope; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.SqlServer, nameof(WithPagLockQueryImpl))]
		public static ISqlServerSpecificQueryable<TSource> WithPagLockInScope<TSource>(this ISqlServerSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return query.TablesInScopeHint(Table.PagLock);
		}

		static Expression<Func<ISqlServerSpecificQueryable<TSource>,ISqlServerSpecificQueryable<TSource>>> WithPagLockQueryImpl<TSource>()
			where TSource : notnull
		{
			return query => query.TablesInScopeHint(Table.PagLock);
		}

		/// <summary>
		/// Adds a SQL Server <c>READCOMMITTED</c> table hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Table; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.SqlServer, nameof(WithReadCommittedTableImpl))]
		public static ISqlServerSpecificTable<TSource> WithReadCommitted<TSource>(this ISqlServerSpecificTable<TSource> table)
			where TSource : notnull
		{
			return table.TableHint(Table.ReadCommitted);
		}

		static Expression<Func<ISqlServerSpecificTable<TSource>,ISqlServerSpecificTable<TSource>>> WithReadCommittedTableImpl<TSource>()
			where TSource : notnull
		{
			return table => table.TableHint(Table.ReadCommitted);
		}

		/// <summary>
		/// Adds a SQL Server <c>READCOMMITTED</c> table hint to tables in the current query scope.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=TablesInScope; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.SqlServer, nameof(WithReadCommittedQueryImpl))]
		public static ISqlServerSpecificQueryable<TSource> WithReadCommittedInScope<TSource>(this ISqlServerSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return query.TablesInScopeHint(Table.ReadCommitted);
		}

		static Expression<Func<ISqlServerSpecificQueryable<TSource>,ISqlServerSpecificQueryable<TSource>>> WithReadCommittedQueryImpl<TSource>()
			where TSource : notnull
		{
			return query => query.TablesInScopeHint(Table.ReadCommitted);
		}

		/// <summary>
		/// Adds a SQL Server <c>READCOMMITTEDLOCK</c> table hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Table; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.SqlServer, nameof(WithReadCommittedLockTableImpl))]
		public static ISqlServerSpecificTable<TSource> WithReadCommittedLock<TSource>(this ISqlServerSpecificTable<TSource> table)
			where TSource : notnull
		{
			return table.TableHint(Table.ReadCommittedLock);
		}

		static Expression<Func<ISqlServerSpecificTable<TSource>,ISqlServerSpecificTable<TSource>>> WithReadCommittedLockTableImpl<TSource>()
			where TSource : notnull
		{
			return table => table.TableHint(Table.ReadCommittedLock);
		}

		/// <summary>
		/// Adds a SQL Server <c>READCOMMITTEDLOCK</c> table hint to tables in the current query scope.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=TablesInScope; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.SqlServer, nameof(WithReadCommittedLockQueryImpl))]
		public static ISqlServerSpecificQueryable<TSource> WithReadCommittedLockInScope<TSource>(this ISqlServerSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return query.TablesInScopeHint(Table.ReadCommittedLock);
		}

		static Expression<Func<ISqlServerSpecificQueryable<TSource>,ISqlServerSpecificQueryable<TSource>>> WithReadCommittedLockQueryImpl<TSource>()
			where TSource : notnull
		{
			return query => query.TablesInScopeHint(Table.ReadCommittedLock);
		}

		/// <summary>
		/// Adds a SQL Server <c>READPAST</c> table hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Table; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.SqlServer, nameof(WithReadPastTableImpl))]
		public static ISqlServerSpecificTable<TSource> WithReadPast<TSource>(this ISqlServerSpecificTable<TSource> table)
			where TSource : notnull
		{
			return table.TableHint(Table.ReadPast);
		}

		static Expression<Func<ISqlServerSpecificTable<TSource>,ISqlServerSpecificTable<TSource>>> WithReadPastTableImpl<TSource>()
			where TSource : notnull
		{
			return table => table.TableHint(Table.ReadPast);
		}

		/// <summary>
		/// Adds a SQL Server <c>READPAST</c> table hint to tables in the current query scope.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=TablesInScope; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.SqlServer, nameof(WithReadPastQueryImpl))]
		public static ISqlServerSpecificQueryable<TSource> WithReadPastInScope<TSource>(this ISqlServerSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return query.TablesInScopeHint(Table.ReadPast);
		}

		static Expression<Func<ISqlServerSpecificQueryable<TSource>,ISqlServerSpecificQueryable<TSource>>> WithReadPastQueryImpl<TSource>()
			where TSource : notnull
		{
			return query => query.TablesInScopeHint(Table.ReadPast);
		}

		/// <summary>
		/// Adds a SQL Server <c>READUNCOMMITTED</c> table hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Table; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.SqlServer, nameof(WithReadUncommittedTableImpl))]
		public static ISqlServerSpecificTable<TSource> WithReadUncommitted<TSource>(this ISqlServerSpecificTable<TSource> table)
			where TSource : notnull
		{
			return table.TableHint(Table.ReadUncommitted);
		}

		static Expression<Func<ISqlServerSpecificTable<TSource>,ISqlServerSpecificTable<TSource>>> WithReadUncommittedTableImpl<TSource>()
			where TSource : notnull
		{
			return table => table.TableHint(Table.ReadUncommitted);
		}

		/// <summary>
		/// Adds a SQL Server <c>READUNCOMMITTED</c> table hint to tables in the current query scope.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=TablesInScope; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.SqlServer, nameof(WithReadUncommittedQueryImpl))]
		public static ISqlServerSpecificQueryable<TSource> WithReadUncommittedInScope<TSource>(this ISqlServerSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return query.TablesInScopeHint(Table.ReadUncommitted);
		}

		static Expression<Func<ISqlServerSpecificQueryable<TSource>,ISqlServerSpecificQueryable<TSource>>> WithReadUncommittedQueryImpl<TSource>()
			where TSource : notnull
		{
			return query => query.TablesInScopeHint(Table.ReadUncommitted);
		}

		/// <summary>
		/// Adds a SQL Server <c>REPEATABLEREAD</c> table hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Table; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.SqlServer, nameof(WithRepeatableReadTableImpl))]
		public static ISqlServerSpecificTable<TSource> WithRepeatableRead<TSource>(this ISqlServerSpecificTable<TSource> table)
			where TSource : notnull
		{
			return table.TableHint(Table.RepeatableRead);
		}

		static Expression<Func<ISqlServerSpecificTable<TSource>,ISqlServerSpecificTable<TSource>>> WithRepeatableReadTableImpl<TSource>()
			where TSource : notnull
		{
			return table => table.TableHint(Table.RepeatableRead);
		}

		/// <summary>
		/// Adds a SQL Server <c>REPEATABLEREAD</c> table hint to tables in the current query scope.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=TablesInScope; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.SqlServer, nameof(WithRepeatableReadQueryImpl))]
		public static ISqlServerSpecificQueryable<TSource> WithRepeatableReadInScope<TSource>(this ISqlServerSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return query.TablesInScopeHint(Table.RepeatableRead);
		}

		static Expression<Func<ISqlServerSpecificQueryable<TSource>,ISqlServerSpecificQueryable<TSource>>> WithRepeatableReadQueryImpl<TSource>()
			where TSource : notnull
		{
			return query => query.TablesInScopeHint(Table.RepeatableRead);
		}

		/// <summary>
		/// Adds a SQL Server <c>ROWLOCK</c> table hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Table; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.SqlServer, nameof(WithRowLockTableImpl))]
		public static ISqlServerSpecificTable<TSource> WithRowLock<TSource>(this ISqlServerSpecificTable<TSource> table)
			where TSource : notnull
		{
			return table.TableHint(Table.RowLock);
		}

		static Expression<Func<ISqlServerSpecificTable<TSource>,ISqlServerSpecificTable<TSource>>> WithRowLockTableImpl<TSource>()
			where TSource : notnull
		{
			return table => table.TableHint(Table.RowLock);
		}

		/// <summary>
		/// Adds a SQL Server <c>ROWLOCK</c> table hint to tables in the current query scope.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=TablesInScope; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.SqlServer, nameof(WithRowLockQueryImpl))]
		public static ISqlServerSpecificQueryable<TSource> WithRowLockInScope<TSource>(this ISqlServerSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return query.TablesInScopeHint(Table.RowLock);
		}

		static Expression<Func<ISqlServerSpecificQueryable<TSource>,ISqlServerSpecificQueryable<TSource>>> WithRowLockQueryImpl<TSource>()
			where TSource : notnull
		{
			return query => query.TablesInScopeHint(Table.RowLock);
		}

		/// <summary>
		/// Adds a SQL Server <c>SERIALIZABLE</c> table hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Table; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.SqlServer, nameof(WithSerializableTableImpl))]
		public static ISqlServerSpecificTable<TSource> WithSerializable<TSource>(this ISqlServerSpecificTable<TSource> table)
			where TSource : notnull
		{
			return table.TableHint(Table.Serializable);
		}

		static Expression<Func<ISqlServerSpecificTable<TSource>,ISqlServerSpecificTable<TSource>>> WithSerializableTableImpl<TSource>()
			where TSource : notnull
		{
			return table => table.TableHint(Table.Serializable);
		}

		/// <summary>
		/// Adds a SQL Server <c>SERIALIZABLE</c> table hint to tables in the current query scope.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=TablesInScope; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.SqlServer, nameof(WithSerializableQueryImpl))]
		public static ISqlServerSpecificQueryable<TSource> WithSerializableInScope<TSource>(this ISqlServerSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return query.TablesInScopeHint(Table.Serializable);
		}

		static Expression<Func<ISqlServerSpecificQueryable<TSource>,ISqlServerSpecificQueryable<TSource>>> WithSerializableQueryImpl<TSource>()
			where TSource : notnull
		{
			return query => query.TablesInScopeHint(Table.Serializable);
		}

		/// <summary>
		/// Adds a SQL Server <c>SNAPSHOT</c> table hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Table; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.SqlServer, nameof(WithSnapshotTableImpl))]
		public static ISqlServerSpecificTable<TSource> WithSnapshot<TSource>(this ISqlServerSpecificTable<TSource> table)
			where TSource : notnull
		{
			return table.TableHint2014Plus(Table.Snapshot);
		}

		static Expression<Func<ISqlServerSpecificTable<TSource>,ISqlServerSpecificTable<TSource>>> WithSnapshotTableImpl<TSource>()
			where TSource : notnull
		{
			return table => table.TableHint2014Plus(Table.Snapshot);
		}

		/// <summary>
		/// Adds a SQL Server <c>SNAPSHOT</c> table hint to tables in the current query scope.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=TablesInScope; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.SqlServer, nameof(WithSnapshotQueryImpl))]
		public static ISqlServerSpecificQueryable<TSource> WithSnapshotInScope<TSource>(this ISqlServerSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return query.TablesInScopeHint2014Plus(Table.Snapshot);
		}

		static Expression<Func<ISqlServerSpecificQueryable<TSource>,ISqlServerSpecificQueryable<TSource>>> WithSnapshotQueryImpl<TSource>()
			where TSource : notnull
		{
			return query => query.TablesInScopeHint2014Plus(Table.Snapshot);
		}

		/// <summary>
		/// Adds a SQL Server <c>TABLOCK</c> table hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Table; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.SqlServer, nameof(WithTabLockTableImpl))]
		public static ISqlServerSpecificTable<TSource> WithTabLock<TSource>(this ISqlServerSpecificTable<TSource> table)
			where TSource : notnull
		{
			return table.TableHint(Table.TabLock);
		}

		static Expression<Func<ISqlServerSpecificTable<TSource>,ISqlServerSpecificTable<TSource>>> WithTabLockTableImpl<TSource>()
			where TSource : notnull
		{
			return table => table.TableHint(Table.TabLock);
		}

		/// <summary>
		/// Adds a SQL Server <c>TABLOCK</c> table hint to tables in the current query scope.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=TablesInScope; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.SqlServer, nameof(WithTabLockQueryImpl))]
		public static ISqlServerSpecificQueryable<TSource> WithTabLockInScope<TSource>(this ISqlServerSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return query.TablesInScopeHint(Table.TabLock);
		}

		static Expression<Func<ISqlServerSpecificQueryable<TSource>,ISqlServerSpecificQueryable<TSource>>> WithTabLockQueryImpl<TSource>()
			where TSource : notnull
		{
			return query => query.TablesInScopeHint(Table.TabLock);
		}

		/// <summary>
		/// Adds a SQL Server <c>TABLOCKX</c> table hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Table; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.SqlServer, nameof(WithTabLockXTableImpl))]
		public static ISqlServerSpecificTable<TSource> WithTabLockX<TSource>(this ISqlServerSpecificTable<TSource> table)
			where TSource : notnull
		{
			return table.TableHint(Table.TabLockX);
		}

		static Expression<Func<ISqlServerSpecificTable<TSource>,ISqlServerSpecificTable<TSource>>> WithTabLockXTableImpl<TSource>()
			where TSource : notnull
		{
			return table => table.TableHint(Table.TabLockX);
		}

		/// <summary>
		/// Adds a SQL Server <c>TABLOCKX</c> table hint to tables in the current query scope.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=TablesInScope; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.SqlServer, nameof(WithTabLockXQueryImpl))]
		public static ISqlServerSpecificQueryable<TSource> WithTabLockXInScope<TSource>(this ISqlServerSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return query.TablesInScopeHint(Table.TabLockX);
		}

		static Expression<Func<ISqlServerSpecificQueryable<TSource>,ISqlServerSpecificQueryable<TSource>>> WithTabLockXQueryImpl<TSource>()
			where TSource : notnull
		{
			return query => query.TablesInScopeHint(Table.TabLockX);
		}

		/// <summary>
		/// Adds a SQL Server <c>UPDLOCK</c> table hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Table; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.SqlServer, nameof(WithUpdLockTableImpl))]
		public static ISqlServerSpecificTable<TSource> WithUpdLock<TSource>(this ISqlServerSpecificTable<TSource> table)
			where TSource : notnull
		{
			return table.TableHint(Table.UpdLock);
		}

		static Expression<Func<ISqlServerSpecificTable<TSource>,ISqlServerSpecificTable<TSource>>> WithUpdLockTableImpl<TSource>()
			where TSource : notnull
		{
			return table => table.TableHint(Table.UpdLock);
		}

		/// <summary>
		/// Adds a SQL Server <c>UPDLOCK</c> table hint to tables in the current query scope.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=TablesInScope; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.SqlServer, nameof(WithUpdLockQueryImpl))]
		public static ISqlServerSpecificQueryable<TSource> WithUpdLockInScope<TSource>(this ISqlServerSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return query.TablesInScopeHint(Table.UpdLock);
		}

		static Expression<Func<ISqlServerSpecificQueryable<TSource>,ISqlServerSpecificQueryable<TSource>>> WithUpdLockQueryImpl<TSource>()
			where TSource : notnull
		{
			return query => query.TablesInScopeHint(Table.UpdLock);
		}

		/// <summary>
		/// Adds a SQL Server <c>XLOCK</c> table hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Table; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.SqlServer, nameof(WithXLockTableImpl))]
		public static ISqlServerSpecificTable<TSource> WithXLock<TSource>(this ISqlServerSpecificTable<TSource> table)
			where TSource : notnull
		{
			return table.TableHint(Table.XLock);
		}

		static Expression<Func<ISqlServerSpecificTable<TSource>,ISqlServerSpecificTable<TSource>>> WithXLockTableImpl<TSource>()
			where TSource : notnull
		{
			return table => table.TableHint(Table.XLock);
		}

		/// <summary>
		/// Adds a SQL Server <c>XLOCK</c> table hint to tables in the current query scope.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=TablesInScope; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(ProviderName.SqlServer, nameof(WithXLockQueryImpl))]
		public static ISqlServerSpecificQueryable<TSource> WithXLockInScope<TSource>(this ISqlServerSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return query.TablesInScopeHint(Table.XLock);
		}

		static Expression<Func<ISqlServerSpecificQueryable<TSource>,ISqlServerSpecificQueryable<TSource>>> WithXLockQueryImpl<TSource>()
			where TSource : notnull
		{
			return query => query.TablesInScopeHint(Table.XLock);
		}

		/// <summary>
		/// Adds a SQL Server <c>LOOP</c> join hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Join; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(JoinLoopHintImpl))]
		public static ISqlServerSpecificQueryable<TSource> JoinLoopHint<TSource>(this ISqlServerSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return query.JoinHint(Join.Loop);
		}

		static Expression<Func<ISqlServerSpecificQueryable<TSource>,ISqlServerSpecificQueryable<TSource>>> JoinLoopHintImpl<TSource>()
			where TSource : notnull
		{
			return query => query.JoinHint(Join.Loop);
		}

		/// <summary>
		/// Adds a SQL Server <c>LOOP</c> join hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Join; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(JoinLoopTableHintImpl))]
		public static ISqlServerSpecificTable<TSource> JoinLoopHint<TSource>(this ISqlServerSpecificTable<TSource> table)
			where TSource : notnull
		{
			return table.JoinHint(Join.Loop);
		}

		static Expression<Func<ISqlServerSpecificTable<TSource>,ISqlServerSpecificTable<TSource>>> JoinLoopTableHintImpl<TSource>()
			where TSource : notnull
		{
			return table => table.JoinHint(Join.Loop);
		}

		/// <summary>
		/// Adds a SQL Server <c>HASH</c> join hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Join; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(JoinHashHintImpl))]
		public static ISqlServerSpecificQueryable<TSource> JoinHashHint<TSource>(this ISqlServerSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return query.JoinHint(Join.Hash);
		}

		static Expression<Func<ISqlServerSpecificQueryable<TSource>,ISqlServerSpecificQueryable<TSource>>> JoinHashHintImpl<TSource>()
			where TSource : notnull
		{
			return query => query.JoinHint(Join.Hash);
		}

		/// <summary>
		/// Adds a SQL Server <c>HASH</c> join hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Join; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(JoinHashTableHintImpl))]
		public static ISqlServerSpecificTable<TSource> JoinHashHint<TSource>(this ISqlServerSpecificTable<TSource> table)
			where TSource : notnull
		{
			return table.JoinHint(Join.Hash);
		}

		static Expression<Func<ISqlServerSpecificTable<TSource>,ISqlServerSpecificTable<TSource>>> JoinHashTableHintImpl<TSource>()
			where TSource : notnull
		{
			return table => table.JoinHint(Join.Hash);
		}

		/// <summary>
		/// Adds a SQL Server <c>MERGE</c> join hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Join; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(JoinMergeHintImpl))]
		public static ISqlServerSpecificQueryable<TSource> JoinMergeHint<TSource>(this ISqlServerSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return query.JoinHint(Join.Merge);
		}

		static Expression<Func<ISqlServerSpecificQueryable<TSource>,ISqlServerSpecificQueryable<TSource>>> JoinMergeHintImpl<TSource>()
			where TSource : notnull
		{
			return query => query.JoinHint(Join.Merge);
		}

		/// <summary>
		/// Adds a SQL Server <c>MERGE</c> join hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Join; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(JoinMergeTableHintImpl))]
		public static ISqlServerSpecificTable<TSource> JoinMergeHint<TSource>(this ISqlServerSpecificTable<TSource> table)
			where TSource : notnull
		{
			return table.JoinHint(Join.Merge);
		}

		static Expression<Func<ISqlServerSpecificTable<TSource>,ISqlServerSpecificTable<TSource>>> JoinMergeTableHintImpl<TSource>()
			where TSource : notnull
		{
			return table => table.JoinHint(Join.Merge);
		}

		/// <summary>
		/// Adds a SQL Server <c>REMOTE</c> join hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Join; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(JoinRemoteHintImpl))]
		public static ISqlServerSpecificQueryable<TSource> JoinRemoteHint<TSource>(this ISqlServerSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return query.JoinHint(Join.Remote);
		}

		static Expression<Func<ISqlServerSpecificQueryable<TSource>,ISqlServerSpecificQueryable<TSource>>> JoinRemoteHintImpl<TSource>()
			where TSource : notnull
		{
			return query => query.JoinHint(Join.Remote);
		}

		/// <summary>
		/// Adds a SQL Server <c>REMOTE</c> join hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Join; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(JoinRemoteTableHintImpl))]
		public static ISqlServerSpecificTable<TSource> JoinRemoteHint<TSource>(this ISqlServerSpecificTable<TSource> table)
			where TSource : notnull
		{
			return table.JoinHint(Join.Remote);
		}

		static Expression<Func<ISqlServerSpecificTable<TSource>,ISqlServerSpecificTable<TSource>>> JoinRemoteTableHintImpl<TSource>()
			where TSource : notnull
		{
			return table => table.JoinHint(Join.Remote);
		}

		/// <summary>
		/// Adds a SQL Server <c>HASH GROUP</c> query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(OptionHashGroupImpl))]
		public static ISqlServerSpecificQueryable<TSource> OptionHashGroup<TSource>(this ISqlServerSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return query.QueryHint(Query.HashGroup);
		}

		static Expression<Func<ISqlServerSpecificQueryable<TSource>,ISqlServerSpecificQueryable<TSource>>> OptionHashGroupImpl<TSource>()
			where TSource : notnull
		{
			return query => query.QueryHint(Query.HashGroup);
		}

		/// <summary>
		/// Adds a SQL Server <c>ORDER GROUP</c> query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(OptionOrderGroupImpl))]
		public static ISqlServerSpecificQueryable<TSource> OptionOrderGroup<TSource>(this ISqlServerSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return query.QueryHint(Query.OrderGroup);
		}

		static Expression<Func<ISqlServerSpecificQueryable<TSource>,ISqlServerSpecificQueryable<TSource>>> OptionOrderGroupImpl<TSource>()
			where TSource : notnull
		{
			return query => query.QueryHint(Query.OrderGroup);
		}

		/// <summary>
		/// Adds a SQL Server <c>CONCAT UNION</c> query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(OptionConcatUnionImpl))]
		public static ISqlServerSpecificQueryable<TSource> OptionConcatUnion<TSource>(this ISqlServerSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return query.QueryHint(Query.ConcatUnion);
		}

		static Expression<Func<ISqlServerSpecificQueryable<TSource>,ISqlServerSpecificQueryable<TSource>>> OptionConcatUnionImpl<TSource>()
			where TSource : notnull
		{
			return query => query.QueryHint(Query.ConcatUnion);
		}

		/// <summary>
		/// Adds a SQL Server <c>HASH UNION</c> query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(OptionHashUnionImpl))]
		public static ISqlServerSpecificQueryable<TSource> OptionHashUnion<TSource>(this ISqlServerSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return query.QueryHint(Query.HashUnion);
		}

		static Expression<Func<ISqlServerSpecificQueryable<TSource>,ISqlServerSpecificQueryable<TSource>>> OptionHashUnionImpl<TSource>()
			where TSource : notnull
		{
			return query => query.QueryHint(Query.HashUnion);
		}

		/// <summary>
		/// Adds a SQL Server <c>MERGE UNION</c> query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(OptionMergeUnionImpl))]
		public static ISqlServerSpecificQueryable<TSource> OptionMergeUnion<TSource>(this ISqlServerSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return query.QueryHint(Query.MergeUnion);
		}

		static Expression<Func<ISqlServerSpecificQueryable<TSource>,ISqlServerSpecificQueryable<TSource>>> OptionMergeUnionImpl<TSource>()
			where TSource : notnull
		{
			return query => query.QueryHint(Query.MergeUnion);
		}

		/// <summary>
		/// Adds a SQL Server <c>LOOP JOIN</c> query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(OptionLoopJoinImpl))]
		public static ISqlServerSpecificQueryable<TSource> OptionLoopJoin<TSource>(this ISqlServerSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return query.QueryHint(Query.LoopJoin);
		}

		static Expression<Func<ISqlServerSpecificQueryable<TSource>,ISqlServerSpecificQueryable<TSource>>> OptionLoopJoinImpl<TSource>()
			where TSource : notnull
		{
			return query => query.QueryHint(Query.LoopJoin);
		}

		/// <summary>
		/// Adds a SQL Server <c>HASH JOIN</c> query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(OptionHashJoinImpl))]
		public static ISqlServerSpecificQueryable<TSource> OptionHashJoin<TSource>(this ISqlServerSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return query.QueryHint(Query.HashJoin);
		}

		static Expression<Func<ISqlServerSpecificQueryable<TSource>,ISqlServerSpecificQueryable<TSource>>> OptionHashJoinImpl<TSource>()
			where TSource : notnull
		{
			return query => query.QueryHint(Query.HashJoin);
		}

		/// <summary>
		/// Adds a SQL Server <c>MERGE JOIN</c> query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(OptionMergeJoinImpl))]
		public static ISqlServerSpecificQueryable<TSource> OptionMergeJoin<TSource>(this ISqlServerSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return query.QueryHint(Query.MergeJoin);
		}

		static Expression<Func<ISqlServerSpecificQueryable<TSource>,ISqlServerSpecificQueryable<TSource>>> OptionMergeJoinImpl<TSource>()
			where TSource : notnull
		{
			return query => query.QueryHint(Query.MergeJoin);
		}

		/// <summary>
		/// Adds a SQL Server <c>EXPAND VIEWS</c> query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(OptionExpandViewsImpl))]
		public static ISqlServerSpecificQueryable<TSource> OptionExpandViews<TSource>(this ISqlServerSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return query.QueryHint(Query.ExpandViews);
		}

		static Expression<Func<ISqlServerSpecificQueryable<TSource>,ISqlServerSpecificQueryable<TSource>>> OptionExpandViewsImpl<TSource>()
			where TSource : notnull
		{
			return query => query.QueryHint(Query.ExpandViews);
		}

		/// <summary>
		/// Adds a SQL Server <c>FAST</c> query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(OptionFastImpl))]
		public static ISqlServerSpecificQueryable<TSource> OptionFast<TSource>(this ISqlServerSpecificQueryable<TSource> query, int value)
			where TSource : notnull
		{
			return query.QueryHint(Query.Fast(value));
		}

		static Expression<Func<ISqlServerSpecificQueryable<TSource>,int,ISqlServerSpecificQueryable<TSource>>> OptionFastImpl<TSource>()
			where TSource : notnull
		{
			return (query, value) => query.QueryHint(Query.Fast(value));
		}

		/// <summary>
		/// Adds a SQL Server <c>FORCE ORDER</c> query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(OptionForceOrderImpl))]
		public static ISqlServerSpecificQueryable<TSource> OptionForceOrder<TSource>(this ISqlServerSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return query.QueryHint(Query.ForceOrder);
		}

		static Expression<Func<ISqlServerSpecificQueryable<TSource>,ISqlServerSpecificQueryable<TSource>>> OptionForceOrderImpl<TSource>()
			where TSource : notnull
		{
			return query => query.QueryHint(Query.ForceOrder);
		}

		/// <summary>
		/// Adds a SQL Server <c>FORCE EXTERNALPUSHDOWN</c> query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(OptionForceExternalPushDownImpl))]
		public static ISqlServerSpecificQueryable<TSource> OptionForceExternalPushDown<TSource>(this ISqlServerSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return query.QueryHint(Query.ForceExternalPushDown);
		}

		static Expression<Func<ISqlServerSpecificQueryable<TSource>,ISqlServerSpecificQueryable<TSource>>> OptionForceExternalPushDownImpl<TSource>()
			where TSource : notnull
		{
			return query => query.QueryHint(Query.ForceExternalPushDown);
		}

		/// <summary>
		/// Adds a SQL Server <c>DISABLE EXTERNALPUSHDOWN</c> query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(OptionDisableExternalPushDownImpl))]
		public static ISqlServerSpecificQueryable<TSource> OptionDisableExternalPushDown<TSource>(this ISqlServerSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return query.QueryHint(Query.DisableExternalPushDown);
		}

		static Expression<Func<ISqlServerSpecificQueryable<TSource>,ISqlServerSpecificQueryable<TSource>>> OptionDisableExternalPushDownImpl<TSource>()
			where TSource : notnull
		{
			return query => query.QueryHint(Query.DisableExternalPushDown);
		}

		/// <summary>
		/// Adds a SQL Server <c>FORCE SCALEOUTEXECUTION</c> query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(OptionForceScaleOutExecutionImpl))]
		public static ISqlServerSpecificQueryable<TSource> OptionForceScaleOutExecution<TSource>(this ISqlServerSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return query.QueryHint2019Plus(Query.ForceScaleOutExecution);
		}

		static Expression<Func<ISqlServerSpecificQueryable<TSource>,ISqlServerSpecificQueryable<TSource>>> OptionForceScaleOutExecutionImpl<TSource>()
			where TSource : notnull
		{
			return query => query.QueryHint2019Plus(Query.ForceScaleOutExecution);
		}

		/// <summary>
		/// Adds a SQL Server <c>DISABLE SCALEOUTEXECUTION</c> query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(OptionDisableScaleOutExecutionImpl))]
		public static ISqlServerSpecificQueryable<TSource> OptionDisableScaleOutExecution<TSource>(this ISqlServerSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return query.QueryHint2019Plus(Query.DisableScaleOutExecution);
		}

		static Expression<Func<ISqlServerSpecificQueryable<TSource>,ISqlServerSpecificQueryable<TSource>>> OptionDisableScaleOutExecutionImpl<TSource>()
			where TSource : notnull
		{
			return query => query.QueryHint2019Plus(Query.DisableScaleOutExecution);
		}

		/// <summary>
		/// Adds a SQL Server <c>IGNORE_NONCLUSTERED_COLUMNSTORE_INDEX</c> query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(OptionIgnoreNonClusteredColumnStoreIndexImpl))]
		public static ISqlServerSpecificQueryable<TSource> OptionIgnoreNonClusteredColumnStoreIndex<TSource>(this ISqlServerSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return query.QueryHint2012Plus(Query.IgnoreNonClusteredColumnStoreIndex);
		}

		static Expression<Func<ISqlServerSpecificQueryable<TSource>,ISqlServerSpecificQueryable<TSource>>> OptionIgnoreNonClusteredColumnStoreIndexImpl<TSource>()
			where TSource : notnull
		{
			return query => query.QueryHint2012Plus(Query.IgnoreNonClusteredColumnStoreIndex);
		}

		/// <summary>
		/// Adds a SQL Server <c>KEEP PLAN</c> query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(OptionKeepPlanImpl))]
		public static ISqlServerSpecificQueryable<TSource> OptionKeepPlan<TSource>(this ISqlServerSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return query.QueryHint(Query.KeepPlan);
		}

		static Expression<Func<ISqlServerSpecificQueryable<TSource>,ISqlServerSpecificQueryable<TSource>>> OptionKeepPlanImpl<TSource>()
			where TSource : notnull
		{
			return query => query.QueryHint(Query.KeepPlan);
		}

		/// <summary>
		/// Adds a SQL Server <c>KEEPFIXED PLAN</c> query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(OptionKeepFixedPlanImpl))]
		public static ISqlServerSpecificQueryable<TSource> OptionKeepFixedPlan<TSource>(this ISqlServerSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return query.QueryHint(Query.KeepFixedPlan);
		}

		static Expression<Func<ISqlServerSpecificQueryable<TSource>,ISqlServerSpecificQueryable<TSource>>> OptionKeepFixedPlanImpl<TSource>()
			where TSource : notnull
		{
			return query => query.QueryHint(Query.KeepFixedPlan);
		}

		/// <summary>
		/// Adds a SQL Server <c>MAX_GRANT_PERCENT</c> query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(OptionMaxGrantPercentImpl))]
		public static ISqlServerSpecificQueryable<TSource> OptionMaxGrantPercent<TSource>(this ISqlServerSpecificQueryable<TSource> query, int value)
			where TSource : notnull
		{
			return query.QueryHint2016Plus(Query.MaxGrantPercent(value));
		}

		static Expression<Func<ISqlServerSpecificQueryable<TSource>,int,ISqlServerSpecificQueryable<TSource>>> OptionMaxGrantPercentImpl<TSource>()
			where TSource : notnull
		{
			return (query, value) => query.QueryHint2016Plus(Query.MaxGrantPercent(value));
		}

		/// <summary>
		/// Adds a SQL Server <c>MIN_GRANT_PERCENT</c> query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(OptionMinGrantPercentImpl))]
		public static ISqlServerSpecificQueryable<TSource> OptionMinGrantPercent<TSource>(this ISqlServerSpecificQueryable<TSource> query, int value)
			where TSource : notnull
		{
			return query.QueryHint2016Plus(Query.MinGrantPercent(value));
		}

		static Expression<Func<ISqlServerSpecificQueryable<TSource>,int,ISqlServerSpecificQueryable<TSource>>> OptionMinGrantPercentImpl<TSource>()
			where TSource : notnull
		{
			return (query, value) => query.QueryHint2016Plus(Query.MinGrantPercent(value));
		}

		/// <summary>
		/// Adds a SQL Server <c>MAXDOP</c> query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(OptionMaxDopImpl))]
		public static ISqlServerSpecificQueryable<TSource> OptionMaxDop<TSource>(this ISqlServerSpecificQueryable<TSource> query, int value)
			where TSource : notnull
		{
			return query.QueryHint(Query.MaxDop(value));
		}

		static Expression<Func<ISqlServerSpecificQueryable<TSource>,int,ISqlServerSpecificQueryable<TSource>>> OptionMaxDopImpl<TSource>()
			where TSource : notnull
		{
			return (query, value) => query.QueryHint(Query.MaxDop(value));
		}

		/// <summary>
		/// Adds a SQL Server <c>MAXRECURSION</c> query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(OptionMaxRecursionImpl))]
		public static ISqlServerSpecificQueryable<TSource> OptionMaxRecursion<TSource>(this ISqlServerSpecificQueryable<TSource> query, int value)
			where TSource : notnull
		{
			return query.QueryHint(Query.MaxRecursion(value));
		}

		static Expression<Func<ISqlServerSpecificQueryable<TSource>,int,ISqlServerSpecificQueryable<TSource>>> OptionMaxRecursionImpl<TSource>()
			where TSource : notnull
		{
			return (query, value) => query.QueryHint(Query.MaxRecursion(value));
		}

		/// <summary>
		/// Adds a SQL Server <c>NO_PERFORMANCE_SPOOL</c> query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(OptionNoPerformanceSpoolImpl))]
		public static ISqlServerSpecificQueryable<TSource> OptionNoPerformanceSpool<TSource>(this ISqlServerSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return query.QueryHint2019Plus(Query.NoPerformanceSpool);
		}

		static Expression<Func<ISqlServerSpecificQueryable<TSource>,ISqlServerSpecificQueryable<TSource>>> OptionNoPerformanceSpoolImpl<TSource>()
			where TSource : notnull
		{
			return query => query.QueryHint2019Plus(Query.NoPerformanceSpool);
		}

		/// <summary>
		/// Adds a SQL Server <c>OPTIMIZE FOR UNKNOWN</c> query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(OptionOptimizeForUnknownImpl))]
		public static ISqlServerSpecificQueryable<TSource> OptionOptimizeForUnknown<TSource>(this ISqlServerSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return query.QueryHint2008Plus(Query.OptimizeForUnknown);
		}

		static Expression<Func<ISqlServerSpecificQueryable<TSource>,ISqlServerSpecificQueryable<TSource>>> OptionOptimizeForUnknownImpl<TSource>()
			where TSource : notnull
		{
			return query => query.QueryHint2008Plus(Query.OptimizeForUnknown);
		}

		/// <summary>
		/// Adds a SQL Server <c>QUERYTRACEON</c> query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(OptionQueryTraceOnImpl))]
		public static ISqlServerSpecificQueryable<TSource> OptionQueryTraceOn<TSource>(this ISqlServerSpecificQueryable<TSource> query, int value)
			where TSource : notnull
		{
			return query.QueryHint(Query.QueryTraceOn(value));
		}

		static Expression<Func<ISqlServerSpecificQueryable<TSource>,int,ISqlServerSpecificQueryable<TSource>>> OptionQueryTraceOnImpl<TSource>()
			where TSource : notnull
		{
			return (query, value) => query.QueryHint(Query.QueryTraceOn(value));
		}

		/// <summary>
		/// Adds a SQL Server <c>RECOMPILE</c> query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(OptionRecompileImpl))]
		public static ISqlServerSpecificQueryable<TSource> OptionRecompile<TSource>(this ISqlServerSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return query.QueryHint(Query.Recompile);
		}

		static Expression<Func<ISqlServerSpecificQueryable<TSource>,ISqlServerSpecificQueryable<TSource>>> OptionRecompileImpl<TSource>()
			where TSource : notnull
		{
			return query => query.QueryHint(Query.Recompile);
		}

		/// <summary>
		/// Adds a SQL Server <c>ROBUST PLAN</c> query hint.
		/// </summary>
		/// <remarks>
		/// AI-Tags: Group=Hints; HintType=Query; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </remarks>
		[ExpressionMethod(nameof(OptionRobustPlanImpl))]
		public static ISqlServerSpecificQueryable<TSource> OptionRobustPlan<TSource>(this ISqlServerSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return query.QueryHint(Query.RobustPlan);
		}

		static Expression<Func<ISqlServerSpecificQueryable<TSource>,ISqlServerSpecificQueryable<TSource>>> OptionRobustPlanImpl<TSource>()
			where TSource : notnull
		{
			return query => query.QueryHint(Query.RobustPlan);
		}

	}
}
