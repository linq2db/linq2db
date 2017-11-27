using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToDB.SqlQuery
{
	public class SqlUpdateClause : IQueryElement, ISqlExpressionWalkable, ICloneableElement
	{
		public SqlUpdateClause()
		{
			Items = new List<SqlSetExpression>();
			Keys  = new List<SqlSetExpression>();
		}

		public List<SqlSetExpression> Items { get; }
		public List<SqlSetExpression> Keys  { get; }
		public SqlTable               Table { get; set; }

		#region Overrides

#if OVERRIDETOSTRING

			public override string ToString()
			{
				return ((IQueryElement)this).ToString(new StringBuilder(), new Dictionary<IQueryElement,IQueryElement>()).ToString();
			}

#endif

		#endregion

		#region ICloneableElement Members

		public ICloneableElement Clone(Dictionary<ICloneableElement, ICloneableElement> objectTree, Predicate<ICloneableElement> doClone)
		{
			if (!doClone(this))
				return this;

			var clone = new SqlUpdateClause();

			if (Table != null)
				clone.Table = (SqlTable)Table.Clone(objectTree, doClone);

			foreach (var item in Items)
				clone.Items.Add((SqlSetExpression)item.Clone(objectTree, doClone));

			foreach (var item in Keys)
				clone.Keys.Add((SqlSetExpression)item.Clone(objectTree, doClone));

			objectTree.Add(this, clone);

			return clone;
		}

		#endregion

		#region ISqlExpressionWalkable Members

		ISqlExpression ISqlExpressionWalkable.Walk(bool skipColumns, Func<ISqlExpression,ISqlExpression> func)
		{
			if (Table != null)
				((ISqlExpressionWalkable)Table).Walk(skipColumns, func);

			foreach (var t in Items)
				((ISqlExpressionWalkable)t).Walk(skipColumns, func);

			foreach (var t in Keys)
				((ISqlExpressionWalkable)t).Walk(skipColumns, func);

			return null;
		}

		#endregion

		#region IQueryElement Members

		public QueryElementType ElementType => QueryElementType.UpdateClause;

		StringBuilder IQueryElement.ToString(StringBuilder sb, Dictionary<IQueryElement,IQueryElement> dic)
		{
			sb.Append("SET ");

			((IQueryElement)Table)?.ToString(sb, dic);

			sb.AppendLine();

			foreach (var e in Items)
			{
				sb.Append("\t");
				((IQueryElement)e).ToString(sb, dic);
				sb.AppendLine();
			}

			return sb;
		}

		#endregion
	}
}
