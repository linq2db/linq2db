using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToDB.SqlQuery
{
	public class SqlInsertClause : IQueryElement, ISqlExpressionWalkable, ICloneableElement
	{
		public SqlInsertClause()
		{
			Items = new List<SqlSetExpression>();
		}

		public List<SqlSetExpression> Items        { get; }
		public SqlTable               Into         { get; set; }
		public bool                   WithIdentity { get; set; }

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

			var clone = new SqlInsertClause { WithIdentity = WithIdentity };

			if (Into != null)
				clone.Into = (SqlTable)Into.Clone(objectTree, doClone);

			foreach (var item in Items)
				clone.Items.Add((SqlSetExpression)item.Clone(objectTree, doClone));

			objectTree.Add(this, clone);

			return clone;
		}

		#endregion

		#region ISqlExpressionWalkable Members

		ISqlExpression ISqlExpressionWalkable.Walk(bool skipColumns, Func<ISqlExpression,ISqlExpression> func)
		{
			((ISqlExpressionWalkable)Into)?.Walk(skipColumns, func);

			foreach (var t in Items)
				((ISqlExpressionWalkable)t).Walk(skipColumns, func);

			return null;
		}

		#endregion

		#region IQueryElement Members

		public QueryElementType ElementType => QueryElementType.InsertClause;

		StringBuilder IQueryElement.ToString(StringBuilder sb, Dictionary<IQueryElement,IQueryElement> dic)
		{
			sb.Append("VALUES ");

			((IQueryElement)Into)?.ToString(sb, dic);

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
