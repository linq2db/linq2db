using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToDB.SqlQuery
{
	public class SqlUpdateClause : IQueryElement, ISqlExpressionWalkable
	{
		public SqlUpdateClause()
		{
			Items = new List<SqlSetExpression>();
			Keys  = new List<SqlSetExpression>();
		}

		public List<SqlSetExpression> Items { get; }
		public List<SqlSetExpression> Keys  { get; }
		public SqlTable?              Table { get; set; }

		#region Overrides

#if OVERRIDETOSTRING

		public override string ToString()
		{
			return ((IQueryElement)this).ToString(new StringBuilder(), new Dictionary<IQueryElement,IQueryElement>()).ToString();
		}

#endif

		#endregion

		#region ISqlExpressionWalkable Members

		ISqlExpression? ISqlExpressionWalkable.Walk(WalkOptions options, Func<ISqlExpression,ISqlExpression> func)
		{
			if (Table != null)
				((ISqlExpressionWalkable)Table).Walk(options, func);

			foreach (var t in Items)
				((ISqlExpressionWalkable)t).Walk(options, func);

			foreach (var t in Keys)
				((ISqlExpressionWalkable)t).Walk(options, func);

			return null;
		}

		#endregion

		#region IQueryElement Members

		public QueryElementType ElementType => QueryElementType.UpdateClause;

		StringBuilder IQueryElement.ToString(StringBuilder sb, Dictionary<IQueryElement,IQueryElement> dic)
		{
			sb.Append("SET ");

			((IQueryElement?)Table)?.ToString(sb, dic);

			sb.AppendLine();

			foreach (var e in Items)
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
