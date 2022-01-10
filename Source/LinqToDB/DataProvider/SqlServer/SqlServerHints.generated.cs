// Generated.
//
using System;
using System.Linq.Expressions;

namespace LinqToDB.DataProvider.SqlServer
{
	public static partial class SqlServerHints
	{
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

	}
}
