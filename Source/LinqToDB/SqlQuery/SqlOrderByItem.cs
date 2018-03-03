using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToDB.SqlQuery
{
	public class SqlOrderByItem : IQueryElement, ICloneableElement
	{
		public SqlOrderByItem(ISqlExpression expression, bool isDescending)
		{
			Expression   = expression;
			IsDescending = isDescending;
		}

		public ISqlExpression Expression   { get; internal set; }
		public bool           IsDescending { get; }

		internal void Walk(bool skipColumns, Func<ISqlExpression,ISqlExpression> func)
		{
			Expression = Expression.Walk(skipColumns, func);
		}

		public ICloneableElement Clone(Dictionary<ICloneableElement, ICloneableElement> objectTree, Predicate<ICloneableElement> doClone)
		{
			if (!doClone(this))
				return this;

			if (!objectTree.TryGetValue(this, out var clone))
				objectTree.Add(this, clone = new SqlOrderByItem((ISqlExpression)Expression.Clone(objectTree, doClone), IsDescending));

			return clone;
		}

		#region Overrides

#if OVERRIDETOSTRING

		public override string ToString()
		{
			return ((IQueryElement)this).ToString(new StringBuilder(), new Dictionary<IQueryElement,IQueryElement>()).ToString();
		}

#endif

		#endregion

		#region IQueryElement Members

		public QueryElementType ElementType => QueryElementType.OrderByItem;

		StringBuilder IQueryElement.ToString(StringBuilder sb, Dictionary<IQueryElement,IQueryElement> dic)
		{
			Expression.ToString(sb, dic);

			if (IsDescending)
				sb.Append(" DESC");

			return sb;
		}

		#endregion
	}
}
