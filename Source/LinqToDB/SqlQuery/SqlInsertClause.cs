using System;

namespace LinqToDB.SqlQuery
{
	public class SqlInsertClause : IQueryElement, ISqlExpressionWalkable
	{
		public SqlInsertClause()
		{
			Items        = new List<SqlSetExpression>();
		}

		public List<SqlSetExpression> Items        { get; private set; }
		public SqlTable?              Into         { get; set; }
		public bool                   WithIdentity { get; set; }

		public void Modify(SqlTable? into, List<SqlSetExpression> items)
		{
			Into  = into;
			Items = items;
		}

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
			((ISqlExpressionWalkable?)Into)?.Walk(options, context, func);

			foreach (var t in Items)
				((ISqlExpressionWalkable)t).Walk(options, context, func);

			return null;
		}

		#endregion

		#region IQueryElement Members

		public QueryElementType ElementType => QueryElementType.InsertClause;

		QueryElementTextWriter IQueryElement.ToString(QueryElementTextWriter writer)
		{
			writer
				.Append("VALUES ")
				.AppendElement(Into)
				.AppendLine();

			using(writer.WithScope())
				for (var index = 0; index < Items.Count; index++)
				{
					var e = Items[index];
					writer.AppendElement(e);
					if (index < Items.Count - 1)
						writer.AppendLine();
				}

			return writer;
		}

		#endregion
	}
}
