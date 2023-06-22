using System;
using System.Collections.Generic;

namespace LinqToDB.SqlQuery
{
	using Remote;

	public class SqlJoinedTable : IQueryElement, ISqlExpressionWalkable, IQueryExtendible
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

		public JoinType                 JoinType           { get; set; }
		public SqlTableSource           Table              { get; set; }
		public SqlSearchCondition       Condition          { get; internal set; }
		public bool                     IsWeak             { get; set; }
		public bool                     CanConvertApply    { get; set; }
		public List<SqlQueryExtension>? SqlQueryExtensions { get; set; }
		public SourceCardinality        Cardinality        { get; set; }

#if OVERRIDETOSTRING

		public override string ToString()
		{
			return this.ToDebugString();
		}

#endif

		#region ISqlExpressionWalkable Members

		public ISqlExpression? Walk<TContext>(WalkOptions options, TContext context, Func<TContext, ISqlExpression, ISqlExpression> func)
		{
			Condition = (SqlSearchCondition)((ISqlExpressionWalkable)Condition).Walk(options, context, func)!;

			Table.Walk(options, context, func);

			if (SqlQueryExtensions != null)
				foreach (var e in SqlQueryExtensions)
					e.Walk(options, context, func);

			return null;
		}

		#endregion

		#region IQueryElement Members

#if DEBUG
		public string DebugText => this.ToDebugString();
#endif

		public QueryElementType ElementType => QueryElementType.JoinedTable;

		QueryElementTextWriter IQueryElement.ToString(QueryElementTextWriter writer)
		{
			if (!writer.AddVisited(this))
				return writer.Append("...");

			if (IsWeak)
				writer.Append("WEAK ");

			switch (JoinType)
			{
				case JoinType.Inner      : writer.Append("INNER JOIN ");  break;
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

			writer
				.AppendElement(Table)
				.Append(" ON ");

			var localWriter = writer.WithInnerSource(Table.Source);
			localWriter.AppendElement(Condition);

			writer.RemoveVisited(this);

			return writer;
		}

		#endregion
	}
}
