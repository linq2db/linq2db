using System.Collections.Generic;

namespace LinqToDB.SqlQuery
{
	public class SqlJoinedTable : QueryElement
	{
		public SqlJoinedTable(JoinType joinType, SqlTableSource table, bool isWeak, SqlSearchCondition searchCondition)
		{
			JoinType             = joinType;
			Table                = table;
			IsWeak               = isWeak;
			Condition            = searchCondition;
			IsSubqueryExpression = false;
		}

		public SqlJoinedTable(JoinType joinType, SqlTableSource table, bool isWeak)
			: this(joinType, table, isWeak, new SqlSearchCondition())
		{
		}

		public SqlJoinedTable(JoinType joinType, ISqlTableSource table, string? alias, bool isWeak)
			: this(joinType, new SqlTableSource(table, alias), isWeak)
		{
		}

		public JoinType                 JoinType             { get; set; }
		public SqlTableSource           Table                { get; set; }
		public SqlSearchCondition       Condition            { get; internal set; }
		public bool                     IsWeak               { get; set; }
		public bool                     IsSubqueryExpression { get; set; }
		public List<SqlQueryExtension>? SqlQueryExtensions   { get; set; }
		public SourceCardinality        Cardinality          { get; set; }

		public override QueryElementType ElementType => QueryElementType.JoinedTable;

		public override QueryElementTextWriter ToString(QueryElementTextWriter writer)
		{
			if (!writer.AddVisited(this))
				return writer.Append("...");

			writer.DebugAppendUniqueId(this);

			if (IsWeak)
				writer.Append("WEAK ");

			switch (JoinType)
			{
				case JoinType.Inner      : writer.Append("INNER JOIN ");  break;
				case JoinType.Cross      : writer.Append("CROSS JOIN ");  break;
				case JoinType.Left       : writer.Append("LEFT JOIN ");   break;
				case JoinType.CrossApply : writer.Append("CROSS APPLY "); break;
				case JoinType.OuterApply : writer.Append("OUTER APPLY "); break;
				case JoinType.Right      : writer.Append("RIGHT JOIN ");  break;
				case JoinType.Full       : writer.Append("FULL JOIN ");   break;
				case JoinType.FullApply  : writer.Append("FULL APPLY ");  break;
				case JoinType.RightApply : writer.Append("RIGHT APPLY "); break;
				default                  : writer.Append("SOME JOIN ");   break;
			}

			if (Cardinality != SourceCardinality.Unknown)
				writer.Append(" (" + Cardinality + ") ");

			if (Table.Joins.Count > 0)
			{
				writer
					.Append("(")
					.AppendLine();

				using (writer.IndentScope())
				{
					writer
						.AppendElement(Table);
				}

				writer
					.AppendLine()
					.Append(") ");
			}
			else
			{
				writer
					.AppendElement(Table);
			}

			if (JoinType != JoinType.Cross)
			{
				writer
					.Append(" ON ")
					.AppendElement(Condition);
			}

			writer.RemoveVisited(this);

			return writer;
		}
	}
}
