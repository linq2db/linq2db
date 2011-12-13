using System;

namespace LinqToDB.Data.Sql
{
	using FJoin = SqlQuery.FromClause.Join;

	public static class Extensions
	{
		public static FJoin InnerJoin    (this ISqlTableSource table,               params FJoin[] joins) { return SqlQuery.InnerJoin    (table,        joins); }
		public static FJoin InnerJoin    (this ISqlTableSource table, string alias, params FJoin[] joins) { return SqlQuery.InnerJoin    (table, alias, joins); }
		public static FJoin LeftJoin     (this ISqlTableSource table,               params FJoin[] joins) { return SqlQuery.LeftJoin     (table,        joins); }
		public static FJoin LeftJoin     (this ISqlTableSource table, string alias, params FJoin[] joins) { return SqlQuery.LeftJoin     (table, alias, joins); }
		public static FJoin Join         (this ISqlTableSource table,               params FJoin[] joins) { return SqlQuery.Join         (table,        joins); }
		public static FJoin Join         (this ISqlTableSource table, string alias, params FJoin[] joins) { return SqlQuery.Join         (table, alias, joins); }
		public static FJoin CrossApply   (this ISqlTableSource table,               params FJoin[] joins) { return SqlQuery.CrossApply   (table,        joins); }
		public static FJoin CrossApply   (this ISqlTableSource table, string alias, params FJoin[] joins) { return SqlQuery.CrossApply   (table, alias, joins); }
		public static FJoin OuterApply   (this ISqlTableSource table,               params FJoin[] joins) { return SqlQuery.OuterApply   (table,        joins); }
		public static FJoin OuterApply   (this ISqlTableSource table, string alias, params FJoin[] joins) { return SqlQuery.OuterApply   (table, alias, joins); }

		public static FJoin WeakInnerJoin(this ISqlTableSource table,               params FJoin[] joins) { return SqlQuery.WeakInnerJoin(table,        joins); }
		public static FJoin WeakInnerJoin(this ISqlTableSource table, string alias, params FJoin[] joins) { return SqlQuery.WeakInnerJoin(table, alias, joins); }
		public static FJoin WeakLeftJoin (this ISqlTableSource table,               params FJoin[] joins) { return SqlQuery.WeakLeftJoin (table,        joins); }
		public static FJoin WeakLeftJoin (this ISqlTableSource table, string alias, params FJoin[] joins) { return SqlQuery.WeakLeftJoin (table, alias, joins); }
		public static FJoin WeakJoin     (this ISqlTableSource table,               params FJoin[] joins) { return SqlQuery.WeakJoin     (table,        joins); }
		public static FJoin WeakJoin     (this ISqlTableSource table, string alias, params FJoin[] joins) { return SqlQuery.WeakJoin     (table, alias, joins); }
	}
}
