using System;

namespace LinqToDB.SqlQuery
{
	public static class Extensions
	{
		public static SqlFromClause.Join InnerJoin(this ISqlTableSource table, params SqlFromClause.Join[] joins)
		{
			return new SqlFromClause.Join(JoinType.Inner, table, null,  false, joins);
		}

		public static SqlFromClause.Join InnerJoin(this ISqlTableSource table, string alias, params SqlFromClause.Join[] joins)
		{
			return new SqlFromClause.Join(JoinType.Inner, table, alias, false, joins);
		}

		public static SqlFromClause.Join LeftJoin(this ISqlTableSource table, params SqlFromClause.Join[] joins)
		{
			return new SqlFromClause.Join(JoinType.Left, table, null, false, joins);
		}

		public static SqlFromClause.Join LeftJoin(this ISqlTableSource table, string alias, params SqlFromClause.Join[] joins)
		{
			return new SqlFromClause.Join(JoinType.Left, table, alias, false, joins);
		}

		public static SqlFromClause.Join Join(this ISqlTableSource table, params SqlFromClause.Join[] joins)
		{
			return new SqlFromClause.Join(JoinType.Auto, table, null,  false, joins);
		}

		public static SqlFromClause.Join Join(this ISqlTableSource table, string alias, params SqlFromClause.Join[] joins)
		{
			return new SqlFromClause.Join(JoinType.Auto, table, alias, false, joins);
		}

		public static SqlFromClause.Join CrossApply(this ISqlTableSource table, params SqlFromClause.Join[] joins)
		{
			return new SqlFromClause.Join(JoinType.CrossApply, table, null, false, joins);
		}

		public static SqlFromClause.Join CrossApply(this ISqlTableSource table, string alias, params SqlFromClause.Join[] joins)
		{
			return new SqlFromClause.Join(JoinType.CrossApply, table, alias, false, joins);
		}

		public static SqlFromClause.Join OuterApply(this ISqlTableSource table, params SqlFromClause.Join[] joins)
		{
			return new SqlFromClause.Join(JoinType.OuterApply, table, null, false, joins);
		}

		public static SqlFromClause.Join OuterApply(this ISqlTableSource table, string alias, params SqlFromClause.Join[] joins)
		{
			return new SqlFromClause.Join(JoinType.OuterApply, table, alias, false, joins);
		}

		public static SqlFromClause.Join WeakInnerJoin(this ISqlTableSource table, params SqlFromClause.Join[] joins)
		{
			return new SqlFromClause.Join(JoinType.Inner, table, null, true, joins);
		}

		public static SqlFromClause.Join WeakInnerJoin(this ISqlTableSource table, string alias, params SqlFromClause.Join[] joins)
		{
			return new SqlFromClause.Join(JoinType.Inner, table, alias, true, joins);
		}

		public static SqlFromClause.Join WeakLeftJoin (this ISqlTableSource table, params SqlFromClause.Join[] joins)
		{
			return new SqlFromClause.Join(JoinType.Left, table, null,  true,  joins);
		}

		public static SqlFromClause.Join WeakLeftJoin (this ISqlTableSource table, string alias, params SqlFromClause.Join[] joins)
		{
			return new SqlFromClause.Join(JoinType.Left, table, alias, true,  joins);
		}

		public static SqlFromClause.Join WeakJoin(this ISqlTableSource table, params SqlFromClause.Join[] joins)
		{
			return new SqlFromClause.Join(JoinType.Auto, table, null, true, joins);
		}

		public static SqlFromClause.Join WeakJoin(this ISqlTableSource table, string alias, params SqlFromClause.Join[] joins)
		{
			return new SqlFromClause.Join(JoinType.Auto, table, alias, true, joins);
		}

		public static SqlFromClause.Join RightJoin(this ISqlTableSource table, params SqlFromClause.Join[] joins)
		{
			return new SqlFromClause.Join(JoinType.Right, table, null,  false, joins);
		}

		public static SqlFromClause.Join RightJoin(this ISqlTableSource table, string alias, params SqlFromClause.Join[] joins)
		{
			return new SqlFromClause.Join(JoinType.Right, table, alias, false, joins);
		}

		public static SqlFromClause.Join FullJoin(this ISqlTableSource table, params SqlFromClause.Join[] joins)
		{
			return new SqlFromClause.Join(JoinType.Full, table, null, false, joins);
		}

		public static SqlFromClause.Join FullJoin(this ISqlTableSource table, string alias, params SqlFromClause.Join[] joins)
		{
			return new SqlFromClause.Join(JoinType.Full, table, alias, false, joins);
		}
	}
}
