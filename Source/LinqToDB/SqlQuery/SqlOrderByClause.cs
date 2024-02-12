﻿using System;
using System.Collections.Generic;

namespace LinqToDB.SqlQuery
{
	public class SqlOrderByClause : ClauseBase, IQueryElement
	{
		internal SqlOrderByClause(SelectQuery selectQuery) : base(selectQuery)
		{
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
				if (item.Expression.Equals(expr, (x, y) => !(x is SqlColumn col) || !col.Parent!.HasSetOperators || x == y))
					return;

			Items.Add(new SqlOrderByItem(expr, isDescending));
		}

		public List<SqlOrderByItem>  Items { get; private set; } = new();

		public bool IsEmpty => Items.Count == 0;

		#region QueryElement overrides

		public override QueryElementType ElementType => QueryElementType.OrderByClause;

		public override QueryElementTextWriter ToString(QueryElementTextWriter writer)
		{
			if (Items.Count == 0)
				return writer;

			writer
				.AppendLine()
				.AppendLine("ORDER BY");

			using(writer.WithScope())
				for (var index = 0; index < Items.Count; index++)
				{
					var item = Items[index];
					writer.AppendElement(item);
					if (index < Items.Count - 1)
						writer.AppendLine(',');
				}

			return writer;
		}

		#endregion

		public void Cleanup()
		{
			Items.Clear();
		}
	}
}
