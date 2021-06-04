﻿using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToDB.SqlQuery
{
	public class SqlJoinedTable : IQueryElement, ISqlExpressionWalkable
	{
		public SqlJoinedTable(JoinType joinType, SqlTableSource table, bool isWeak, SqlSearchCondition searchCondition)
		{
			JoinType        = joinType;
			Table           = table;
			IsWeak          = isWeak;
			Condition       = searchCondition;
			CanConvertApply = true;
		}

		public SqlJoinedTable(JoinType joinType, SqlTableSource table, bool isWeak)
			: this(joinType, table, isWeak, new SqlSearchCondition())
		{
		}

		public SqlJoinedTable(JoinType joinType, ISqlTableSource table, string? alias, bool isWeak)
			: this(joinType, new SqlTableSource(table, alias), isWeak)
		{
		}

		public JoinType           JoinType        { get; set; }
		public SqlTableSource     Table           { get; set; }
		public SqlSearchCondition Condition       { get; private set; }
		public bool               IsWeak          { get; set; }
		public bool               CanConvertApply { get; set; }

#if OVERRIDETOSTRING

			public override string ToString()
			{
				return ((IQueryElement)this).ToString(new StringBuilder(), new Dictionary<IQueryElement,IQueryElement>()).ToString();
			}

#endif

		#region ISqlExpressionWalkable Members

		public ISqlExpression? Walk(WalkOptions options, Func<ISqlExpression,ISqlExpression> func)
		{
			Condition = (SqlSearchCondition)((ISqlExpressionWalkable)Condition).Walk(options, func)!;

			Table.Walk(options, func);

			return null;
		}

		#endregion

		#region IQueryElement Members

		public QueryElementType ElementType => QueryElementType.JoinedTable;

		StringBuilder IQueryElement.ToString(StringBuilder sb, Dictionary<IQueryElement,IQueryElement> dic)
		{
			if (dic.ContainsKey(this))
				return sb.Append("...");

			dic.Add(this, this);

			if (IsWeak)
				sb.Append("WEAK ");

			switch (JoinType)
			{
				case JoinType.Inner      : sb.Append("INNER JOIN ");  break;
				case JoinType.Left       : sb.Append("LEFT JOIN ");   break;
				case JoinType.CrossApply : sb.Append("CROSS APPLY "); break;
				case JoinType.OuterApply : sb.Append("OUTER APPLY "); break;
				default                  : sb.Append("SOME JOIN "); break;
			}

			((IQueryElement)Table).ToString(sb, dic);
			sb.Append(" ON ");
			((IQueryElement)Condition).ToString(sb, dic);

			dic.Remove(this);

			return sb;
		}

		#endregion
	}
}
