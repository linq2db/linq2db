using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LinqToDB.SqlQuery
{
	public class SqlOrderByClause : ClauseBase, IQueryElement, ISqlExpressionWalkable
	{
		internal SqlOrderByClause(SelectQuery selectQuery) : base(selectQuery)
		{
		}

		internal SqlOrderByClause(
			SelectQuery   selectQuery,
			SqlOrderByClause clone,
			Dictionary<ICloneableElement,ICloneableElement> objectTree,
			Predicate<ICloneableElement> doClone)
			: base(selectQuery)
		{
			Items.AddRange(clone.Items.Select(item => (SqlOrderByItem)item.Clone(objectTree, doClone)));
		}

		internal SqlOrderByClause(IEnumerable<SqlOrderByItem> items) : base(null)
		{
			Items.AddRange(items);
		}

		public SqlOrderByClause Expr(ISqlExpression expr, bool isDescending)
		{
			Add(expr, isDescending);
			return this;
		}

		public SqlOrderByClause Expr     (ISqlExpression expr)               { return Expr(expr,  false);        }
		public SqlOrderByClause ExprAsc  (ISqlExpression expr)               { return Expr(expr,  false);        }
		public SqlOrderByClause ExprDesc (ISqlExpression expr)               { return Expr(expr,  true);         }
		public SqlOrderByClause Field    (SqlField field, bool isDescending) { return Expr(field, isDescending); }
		public SqlOrderByClause Field    (SqlField field)                    { return Expr(field, false);        }
		public SqlOrderByClause FieldAsc (SqlField field)                    { return Expr(field, false);        }
		public SqlOrderByClause FieldDesc(SqlField field)                    { return Expr(field, true);         }

		void Add(ISqlExpression expr, bool isDescending)
		{
			foreach (var item in Items)
				if (item.Expression.Equals(expr, (x, y) => !(x is SqlColumn col) || !col.Parent.HasUnion || x == y))
					return;

			Items.Add(new SqlOrderByItem(expr, isDescending));
		}

		public List<SqlOrderByItem>  Items { get; } = new List<SqlOrderByItem>();

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
			foreach (var t in Items)
				t.Walk(skipColumns, func);
			return null;
		}

		#endregion

		#region IQueryElement Members

		public QueryElementType ElementType => QueryElementType.OrderByClause;

		StringBuilder IQueryElement.ToString(StringBuilder sb, Dictionary<IQueryElement,IQueryElement> dic)
		{
			if (Items.Count == 0)
				return sb;

			sb.Append(" \nORDER BY \n");

			foreach (IQueryElement item in Items)
			{
				sb.Append('\t');
				item.ToString(sb, dic);
				sb.Append(", ");
			}

			sb.Length -= 2;

			return sb;
		}

		#endregion
	}
}
