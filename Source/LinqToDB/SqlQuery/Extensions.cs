using System;

namespace LinqToDB.SqlQuery
{
	public static class Extensions
	{
		public static SqlFromClause.Join InnerJoin    (this ISqlTableSource table,               params SqlFromClause.Join[] joins) { return SelectQuery.InnerJoin    (table,        joins); }
		public static SqlFromClause.Join InnerJoin    (this ISqlTableSource table, string alias, params SqlFromClause.Join[] joins) { return SelectQuery.InnerJoin    (table, alias, joins); }
		public static SqlFromClause.Join LeftJoin     (this ISqlTableSource table,               params SqlFromClause.Join[] joins) { return SelectQuery.LeftJoin     (table,        joins); }
		public static SqlFromClause.Join LeftJoin     (this ISqlTableSource table, string alias, params SqlFromClause.Join[] joins) { return SelectQuery.LeftJoin     (table, alias, joins); }
		public static SqlFromClause.Join Join         (this ISqlTableSource table,               params SqlFromClause.Join[] joins) { return SelectQuery.Join         (table,        joins); }
		public static SqlFromClause.Join Join         (this ISqlTableSource table, string alias, params SqlFromClause.Join[] joins) { return SelectQuery.Join         (table, alias, joins); }
		public static SqlFromClause.Join CrossApply   (this ISqlTableSource table,               params SqlFromClause.Join[] joins) { return SelectQuery.CrossApply   (table,        joins); }
		public static SqlFromClause.Join CrossApply   (this ISqlTableSource table, string alias, params SqlFromClause.Join[] joins) { return SelectQuery.CrossApply   (table, alias, joins); }
		public static SqlFromClause.Join OuterApply   (this ISqlTableSource table,               params SqlFromClause.Join[] joins) { return SelectQuery.OuterApply   (table,        joins); }
		public static SqlFromClause.Join OuterApply   (this ISqlTableSource table, string alias, params SqlFromClause.Join[] joins) { return SelectQuery.OuterApply   (table, alias, joins); }

		public static SqlFromClause.Join WeakInnerJoin(this ISqlTableSource table,               params SqlFromClause.Join[] joins) { return SelectQuery.WeakInnerJoin(table,        joins); }
		public static SqlFromClause.Join WeakInnerJoin(this ISqlTableSource table, string alias, params SqlFromClause.Join[] joins) { return SelectQuery.WeakInnerJoin(table, alias, joins); }
		public static SqlFromClause.Join WeakLeftJoin (this ISqlTableSource table,               params SqlFromClause.Join[] joins) { return SelectQuery.WeakLeftJoin (table,        joins); }
		public static SqlFromClause.Join WeakLeftJoin (this ISqlTableSource table, string alias, params SqlFromClause.Join[] joins) { return SelectQuery.WeakLeftJoin (table, alias, joins); }
		public static SqlFromClause.Join WeakJoin     (this ISqlTableSource table,               params SqlFromClause.Join[] joins) { return SelectQuery.WeakJoin     (table,        joins); }
		public static SqlFromClause.Join WeakJoin     (this ISqlTableSource table, string alias, params SqlFromClause.Join[] joins) { return SelectQuery.WeakJoin     (table, alias, joins); }
	}
}
