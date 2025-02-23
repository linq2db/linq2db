#nullable enable
// Generated.
//
using System;
using System.Linq.Expressions;

using LinqToDB.Mapping;

namespace LinqToDB.DataProvider.SqlCe
{
	public static partial class SqlCeHints
	{
		[ExpressionMethod(ProviderName.SqlCe, nameof(WithHoldLockTableImpl))]
		public static ISqlCeSpecificTable<TSource> WithHoldLock<TSource>(this ISqlCeSpecificTable<TSource> table)
			where TSource : notnull
		{
			return TableHint(table, Table.HoldLock);
		}

		static Expression<Func<ISqlCeSpecificTable<TSource>,ISqlCeSpecificTable<TSource>>> WithHoldLockTableImpl<TSource>()
			where TSource : notnull
		{
			return table => TableHint(table, Table.HoldLock);
		}

		[ExpressionMethod(ProviderName.SqlCe, nameof(WithHoldLockQueryImpl))]
		public static ISqlCeSpecificQueryable<TSource> WithHoldLockInScope<TSource>(this ISqlCeSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return TablesInScopeHint(query, Table.HoldLock);
		}

		static Expression<Func<ISqlCeSpecificQueryable<TSource>,ISqlCeSpecificQueryable<TSource>>> WithHoldLockQueryImpl<TSource>()
			where TSource : notnull
		{
			return query => TablesInScopeHint(query, Table.HoldLock);
		}

		[ExpressionMethod(ProviderName.SqlCe, nameof(WithNoLockTableImpl))]
		public static ISqlCeSpecificTable<TSource> WithNoLock<TSource>(this ISqlCeSpecificTable<TSource> table)
			where TSource : notnull
		{
			return TableHint(table, Table.NoLock);
		}

		static Expression<Func<ISqlCeSpecificTable<TSource>,ISqlCeSpecificTable<TSource>>> WithNoLockTableImpl<TSource>()
			where TSource : notnull
		{
			return table => TableHint(table, Table.NoLock);
		}

		[ExpressionMethod(ProviderName.SqlCe, nameof(WithNoLockQueryImpl))]
		public static ISqlCeSpecificQueryable<TSource> WithNoLockInScope<TSource>(this ISqlCeSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return TablesInScopeHint(query, Table.NoLock);
		}

		static Expression<Func<ISqlCeSpecificQueryable<TSource>,ISqlCeSpecificQueryable<TSource>>> WithNoLockQueryImpl<TSource>()
			where TSource : notnull
		{
			return query => TablesInScopeHint(query, Table.NoLock);
		}

		[ExpressionMethod(ProviderName.SqlCe, nameof(WithPagLockTableImpl))]
		public static ISqlCeSpecificTable<TSource> WithPagLock<TSource>(this ISqlCeSpecificTable<TSource> table)
			where TSource : notnull
		{
			return TableHint(table, Table.PagLock);
		}

		static Expression<Func<ISqlCeSpecificTable<TSource>,ISqlCeSpecificTable<TSource>>> WithPagLockTableImpl<TSource>()
			where TSource : notnull
		{
			return table => TableHint(table, Table.PagLock);
		}

		[ExpressionMethod(ProviderName.SqlCe, nameof(WithPagLockQueryImpl))]
		public static ISqlCeSpecificQueryable<TSource> WithPagLockInScope<TSource>(this ISqlCeSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return TablesInScopeHint(query, Table.PagLock);
		}

		static Expression<Func<ISqlCeSpecificQueryable<TSource>,ISqlCeSpecificQueryable<TSource>>> WithPagLockQueryImpl<TSource>()
			where TSource : notnull
		{
			return query => TablesInScopeHint(query, Table.PagLock);
		}

		[ExpressionMethod(ProviderName.SqlCe, nameof(WithRowLockTableImpl))]
		public static ISqlCeSpecificTable<TSource> WithRowLock<TSource>(this ISqlCeSpecificTable<TSource> table)
			where TSource : notnull
		{
			return TableHint(table, Table.RowLock);
		}

		static Expression<Func<ISqlCeSpecificTable<TSource>,ISqlCeSpecificTable<TSource>>> WithRowLockTableImpl<TSource>()
			where TSource : notnull
		{
			return table => TableHint(table, Table.RowLock);
		}

		[ExpressionMethod(ProviderName.SqlCe, nameof(WithRowLockQueryImpl))]
		public static ISqlCeSpecificQueryable<TSource> WithRowLockInScope<TSource>(this ISqlCeSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return TablesInScopeHint(query, Table.RowLock);
		}

		static Expression<Func<ISqlCeSpecificQueryable<TSource>,ISqlCeSpecificQueryable<TSource>>> WithRowLockQueryImpl<TSource>()
			where TSource : notnull
		{
			return query => TablesInScopeHint(query, Table.RowLock);
		}

		[ExpressionMethod(ProviderName.SqlCe, nameof(WithTabLockTableImpl))]
		public static ISqlCeSpecificTable<TSource> WithTabLock<TSource>(this ISqlCeSpecificTable<TSource> table)
			where TSource : notnull
		{
			return TableHint(table, Table.TabLock);
		}

		static Expression<Func<ISqlCeSpecificTable<TSource>,ISqlCeSpecificTable<TSource>>> WithTabLockTableImpl<TSource>()
			where TSource : notnull
		{
			return table => TableHint(table, Table.TabLock);
		}

		[ExpressionMethod(ProviderName.SqlCe, nameof(WithTabLockQueryImpl))]
		public static ISqlCeSpecificQueryable<TSource> WithTabLockInScope<TSource>(this ISqlCeSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return TablesInScopeHint(query, Table.TabLock);
		}

		static Expression<Func<ISqlCeSpecificQueryable<TSource>,ISqlCeSpecificQueryable<TSource>>> WithTabLockQueryImpl<TSource>()
			where TSource : notnull
		{
			return query => TablesInScopeHint(query, Table.TabLock);
		}

		[ExpressionMethod(ProviderName.SqlCe, nameof(WithUpdLockTableImpl))]
		public static ISqlCeSpecificTable<TSource> WithUpdLock<TSource>(this ISqlCeSpecificTable<TSource> table)
			where TSource : notnull
		{
			return TableHint(table, Table.UpdLock);
		}

		static Expression<Func<ISqlCeSpecificTable<TSource>,ISqlCeSpecificTable<TSource>>> WithUpdLockTableImpl<TSource>()
			where TSource : notnull
		{
			return table => TableHint(table, Table.UpdLock);
		}

		[ExpressionMethod(ProviderName.SqlCe, nameof(WithUpdLockQueryImpl))]
		public static ISqlCeSpecificQueryable<TSource> WithUpdLockInScope<TSource>(this ISqlCeSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return TablesInScopeHint(query, Table.UpdLock);
		}

		static Expression<Func<ISqlCeSpecificQueryable<TSource>,ISqlCeSpecificQueryable<TSource>>> WithUpdLockQueryImpl<TSource>()
			where TSource : notnull
		{
			return query => TablesInScopeHint(query, Table.UpdLock);
		}

		[ExpressionMethod(ProviderName.SqlCe, nameof(WithXLockTableImpl))]
		public static ISqlCeSpecificTable<TSource> WithXLock<TSource>(this ISqlCeSpecificTable<TSource> table)
			where TSource : notnull
		{
			return TableHint(table, Table.XLock);
		}

		static Expression<Func<ISqlCeSpecificTable<TSource>,ISqlCeSpecificTable<TSource>>> WithXLockTableImpl<TSource>()
			where TSource : notnull
		{
			return table => TableHint(table, Table.XLock);
		}

		[ExpressionMethod(ProviderName.SqlCe, nameof(WithXLockQueryImpl))]
		public static ISqlCeSpecificQueryable<TSource> WithXLockInScope<TSource>(this ISqlCeSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return TablesInScopeHint(query, Table.XLock);
		}

		static Expression<Func<ISqlCeSpecificQueryable<TSource>,ISqlCeSpecificQueryable<TSource>>> WithXLockQueryImpl<TSource>()
			where TSource : notnull
		{
			return query => TablesInScopeHint(query, Table.XLock);
		}

	}
}
