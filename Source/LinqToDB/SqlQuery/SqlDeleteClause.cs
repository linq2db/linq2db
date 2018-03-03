using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToDB.SqlQuery
{
	public class SqlDeleteClause2 : IQueryElement, ISqlExpressionWalkable, ICloneableElement
	{
		public SqlTable Table { get; set; }

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

			var clone = new SqlDeleteClause2();

			if (Table != null)
				clone.Table = (SqlTable)Table.Clone(objectTree, doClone);

			objectTree.Add(this, clone);

			return clone;
		}

		#endregion

		#region ISqlExpressionWalkable Members

		[Obsolete]
		ISqlExpression ISqlExpressionWalkable.Walk(bool skipColumns, Func<ISqlExpression,ISqlExpression> func)
		{
			if (Table != null)
				((ISqlExpressionWalkable)Table).Walk(skipColumns, func);

			return null;
		}

		#endregion

		#region IQueryElement Members

		public QueryElementType ElementType => QueryElementType.Column;

		StringBuilder IQueryElement.ToString(StringBuilder sb, Dictionary<IQueryElement,IQueryElement> dic)
		{
			sb.Append("DELETE FROM ");

			if (Table != null)
				((IQueryElement)Table).ToString(sb, dic);

			sb.AppendLine();

			return sb;
		}

		#endregion
	}
}
