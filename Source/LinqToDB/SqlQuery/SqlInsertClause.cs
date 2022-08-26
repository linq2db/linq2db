using System.Text;

namespace LinqToDB.SqlQuery
{
	public class SqlInsertClause : IQueryElement, ISqlExpressionWalkable
	{
		public SqlInsertClause()
		{
			Items        = new List<SqlSetExpression>();
			DefaultItems = new List<SqlSetExpression>();
		}

		public List<SqlSetExpression> Items        { get; }
		public List<SqlSetExpression> DefaultItems { get; }
		public SqlTable?              Into         { get; set; }
		public bool                   WithIdentity { get; set; }

		#region Overrides

#if OVERRIDETOSTRING

			public override string ToString()
			{
				return ((IQueryElement)this).ToString(new StringBuilder(), new Dictionary<IQueryElement,IQueryElement>()).ToString();
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

		StringBuilder IQueryElement.ToString(StringBuilder sb, Dictionary<IQueryElement,IQueryElement> dic)
		{
			sb.Append("VALUES ");

			((IQueryElement?)Into)?.ToString(sb, dic);

			sb.AppendLine();

			var items = Items;
			if (items.Count == 0)
				items = DefaultItems;

			foreach (var e in items)
			{
				sb.Append('\t');
				((IQueryElement)e).ToString(sb, dic);
				sb.AppendLine();
			}

			return sb;
		}

		#endregion
	}
}
