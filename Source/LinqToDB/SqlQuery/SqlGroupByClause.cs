using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LinqToDB.SqlQuery
{
	public class SqlGroupByClause : ClauseBase, IQueryElement, ISqlExpressionWalkable
	{
		internal SqlGroupByClause(SelectQuery selectQuery) : base(selectQuery)
		{
		}

		internal SqlGroupByClause(
			SelectQuery   selectQuery,
			SqlGroupByClause clone,
			Dictionary<ICloneableElement,ICloneableElement> objectTree,
			Predicate<ICloneableElement> doClone)
			: base(selectQuery)
		{
			Items.AddRange(clone.Items.Select(e => (ISqlExpression)e.Clone(objectTree, doClone)));
		}

		internal SqlGroupByClause(IEnumerable<ISqlExpression> items) : base(null)
		{
			Items.AddRange(items);
		}

		public SqlGroupByClause Expr(ISqlExpression expr)
		{
			Add(expr);
			return this;
		}

		public SqlGroupByClause Field(SqlField field)
		{
			return Expr(field);
		}

		void Add(ISqlExpression expr)
		{
			foreach (var e in Items)
				if (e.Equals(expr))
					return;

			Items.Add(expr);
		}

		public List<ISqlExpression> Items { get; } = new List<ISqlExpression>();

		public bool IsEmpty => Items.Count == 0;

#if OVERRIDETOSTRING

			public override string ToString()
			{
				return ((IQueryElement)this).ToString(new StringBuilder(), new Dictionary<IQueryElement,IQueryElement>()).ToString();
			}

#endif

		#region ISqlExpressionWalkable Members

		ISqlExpression ISqlExpressionWalkable.Walk(bool skipColumns, Func<ISqlExpression,ISqlExpression> func)
		{
			for (var i = 0; i < Items.Count; i++)
				Items[i] = Items[i].Walk(skipColumns, func);

			return null;
		}

		#endregion

		#region IQueryElement Members

		public QueryElementType ElementType => QueryElementType.GroupByClause;

		StringBuilder IQueryElement.ToString(StringBuilder sb, Dictionary<IQueryElement,IQueryElement> dic)
		{
			if (Items.Count == 0)
				return sb;

			sb.Append(" \nGROUP BY \n");

			foreach (var item in Items)
			{
				sb.Append('\t');
				item.ToString(sb, dic);
				sb.Append(",");
			}

			sb.Length--;

			return sb;
		}

		#endregion
	}
}
