using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToDB.SqlQuery
{
	public class SqlWhereClause : ClauseBase<SqlWhereClause,SqlWhereClause.Next>, IQueryElement, ISqlExpressionWalkable
	{
		public class Next : ClauseBase
		{
			internal Next(SqlWhereClause parent) : base(parent.SelectQuery)
			{
				_parent = parent;
			}

			readonly SqlWhereClause _parent;

			public SqlWhereClause Or  => _parent.SetOr(true);
			public SqlWhereClause And => _parent.SetOr(false);
		}

		internal SqlWhereClause(SelectQuery selectQuery) : base(selectQuery)
		{
			SearchCondition = new SqlSearchCondition();
		}

		internal SqlWhereClause(
			SelectQuery selectQuery,
			SqlWhereClause clone,
			Dictionary<ICloneableElement,ICloneableElement> objectTree,
			Predicate<ICloneableElement> doClone)
			: base(selectQuery)
		{
			SearchCondition = (SqlSearchCondition)clone.SearchCondition.Clone(objectTree, doClone);
		}

		internal SqlWhereClause(SqlSearchCondition searchCondition) : base(null)
		{
			SearchCondition = searchCondition;
		}

		public SqlSearchCondition SearchCondition { get; private set; }

		public bool IsEmpty => SearchCondition.Conditions.Count == 0;

		protected override SqlSearchCondition Search => SearchCondition;

		protected override Next GetNext()
		{
			return new Next(this);
		}

#if OVERRIDETOSTRING

			public override string ToString()
			{
				return ((IQueryElement)this).ToString(new StringBuilder(), new Dictionary<IQueryElement,IQueryElement>()).ToString();
			}

#endif

		#region ISqlExpressionWalkable Members

		ISqlExpression ISqlExpressionWalkable.Walk(bool skipColumns, Func<ISqlExpression,ISqlExpression> action)
		{
			SearchCondition = (SqlSearchCondition)((ISqlExpressionWalkable)SearchCondition).Walk(skipColumns, action);
			return null;
		}

		#endregion

		#region IQueryElement Members

		public QueryElementType ElementType => QueryElementType.WhereClause;

		StringBuilder IQueryElement.ToString(StringBuilder sb, Dictionary<IQueryElement,IQueryElement> dic)
		{
			if (Search.Conditions.Count == 0)
				return sb;

			sb.Append("\nWHERE\n\t");
			return ((IQueryElement)Search).ToString(sb, dic);
		}

		#endregion
	}
}
