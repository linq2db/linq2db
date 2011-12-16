using System;

namespace LinqToDB.SqlBuilder
{
	public static class Extensions
	{
		public static SqlQuery.FromClause.Join InnerJoin    (this ISqlTableSource table,               params SqlQuery.FromClause.Join[] joins) { return SqlQuery.InnerJoin    (table,        joins); }
		public static SqlQuery.FromClause.Join InnerJoin    (this ISqlTableSource table, string alias, params SqlQuery.FromClause.Join[] joins) { return SqlQuery.InnerJoin    (table, alias, joins); }
		public static SqlQuery.FromClause.Join LeftJoin     (this ISqlTableSource table,               params SqlQuery.FromClause.Join[] joins) { return SqlQuery.LeftJoin     (table,        joins); }
		public static SqlQuery.FromClause.Join LeftJoin     (this ISqlTableSource table, string alias, params SqlQuery.FromClause.Join[] joins) { return SqlQuery.LeftJoin     (table, alias, joins); }
		public static SqlQuery.FromClause.Join Join         (this ISqlTableSource table,               params SqlQuery.FromClause.Join[] joins) { return SqlQuery.Join         (table,        joins); }
		public static SqlQuery.FromClause.Join Join         (this ISqlTableSource table, string alias, params SqlQuery.FromClause.Join[] joins) { return SqlQuery.Join         (table, alias, joins); }
		public static SqlQuery.FromClause.Join CrossApply   (this ISqlTableSource table,               params SqlQuery.FromClause.Join[] joins) { return SqlQuery.CrossApply   (table,        joins); }
		public static SqlQuery.FromClause.Join CrossApply   (this ISqlTableSource table, string alias, params SqlQuery.FromClause.Join[] joins) { return SqlQuery.CrossApply   (table, alias, joins); }
		public static SqlQuery.FromClause.Join OuterApply   (this ISqlTableSource table,               params SqlQuery.FromClause.Join[] joins) { return SqlQuery.OuterApply   (table,        joins); }
		public static SqlQuery.FromClause.Join OuterApply   (this ISqlTableSource table, string alias, params SqlQuery.FromClause.Join[] joins) { return SqlQuery.OuterApply   (table, alias, joins); }

		public static SqlQuery.FromClause.Join WeakInnerJoin(this ISqlTableSource table,               params SqlQuery.FromClause.Join[] joins) { return SqlQuery.WeakInnerJoin(table,        joins); }
		public static SqlQuery.FromClause.Join WeakInnerJoin(this ISqlTableSource table, string alias, params SqlQuery.FromClause.Join[] joins) { return SqlQuery.WeakInnerJoin(table, alias, joins); }
		public static SqlQuery.FromClause.Join WeakLeftJoin (this ISqlTableSource table,               params SqlQuery.FromClause.Join[] joins) { return SqlQuery.WeakLeftJoin (table,        joins); }
		public static SqlQuery.FromClause.Join WeakLeftJoin (this ISqlTableSource table, string alias, params SqlQuery.FromClause.Join[] joins) { return SqlQuery.WeakLeftJoin (table, alias, joins); }
		public static SqlQuery.FromClause.Join WeakJoin     (this ISqlTableSource table,               params SqlQuery.FromClause.Join[] joins) { return SqlQuery.WeakJoin     (table,        joins); }
		public static SqlQuery.FromClause.Join WeakJoin     (this ISqlTableSource table, string alias, params SqlQuery.FromClause.Join[] joins) { return SqlQuery.WeakJoin     (table, alias, joins); }
	}
}
