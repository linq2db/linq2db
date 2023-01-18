using System;

namespace LinqToDB.SqlQuery
{
	public class SqlUpdateClause : IQueryElement, ISqlExpressionWalkable
	{
		public SqlUpdateClause()
		{
			Items = new List<SqlSetExpression>();
			Keys  = new List<SqlSetExpression>();
		}

		public List<SqlSetExpression> Items       { get; }
		public List<SqlSetExpression> Keys        { get; }
		public SqlTable?              Table       { get; set; }
		public SqlTableSource?        TableSource { get; set; }

		#region Overrides

#if OVERRIDETOSTRING

		public override string ToString()
		{
			return this.ToDebugString();
		}

#endif

		#endregion

		#region ISqlExpressionWalkable Members

		ISqlExpression? ISqlExpressionWalkable.Walk<TContext>(WalkOptions options, TContext context, Func<TContext, ISqlExpression, ISqlExpression> func)
		{
			if (Table != null)
				((ISqlExpressionWalkable)Table).Walk(options, context, func);

			foreach (var t in Items)
				((ISqlExpressionWalkable)t).Walk(options, context, func);

			foreach (var t in Keys)
				((ISqlExpressionWalkable)t).Walk(options, context, func);

			return null;
		}

		#endregion

		#region IQueryElement Members

		public QueryElementType ElementType => QueryElementType.UpdateClause;

		QueryElementTextWriter IQueryElement.ToString(QueryElementTextWriter writer)
		{
			writer
				.Append('\t')
				.AppendElement(Table)
				.AppendLine()
				.Append("SET ")
				.AppendLine();

			using (writer.WithScope())
				foreach (var e in Items)
				{
					writer
						.AppendElement(e)
						.AppendLine();
				}

			return writer;
		}

		#endregion
	}
}
