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

		public SqlOrderByClause Expr(ISqlExpression expr, bool isDescending, bool isPositioned)
		{
			Add(expr, isDescending, isPositioned);
			return this;
		}

		public SqlOrderByClause Expr     (ISqlExpression expr, bool isPositioned = false) => Expr(expr, false, isPositioned);
		public SqlOrderByClause ExprAsc  (ISqlExpression expr, bool isPositioned = false) => Expr(expr, false, isPositioned);
		public SqlOrderByClause ExprDesc(ISqlExpression  expr, bool isPositioned = false) => Expr(expr, true, isPositioned);

		public SqlOrderByClause Field(SqlField     field, bool isDescending, bool isPositioned) => Expr(field, isDescending, isPositioned);
		public SqlOrderByClause Field(SqlField     field, bool isPositioned = false) => Expr(field, false, isPositioned);
		public SqlOrderByClause FieldAsc (SqlField field, bool isPositioned = false) => Expr(field, false, isPositioned);
		public SqlOrderByClause FieldDesc(SqlField field, bool isPositioned = false) => Expr(field, true, isPositioned);

		void Add(ISqlExpression expr, bool isDescending, bool isPositioned)
		{
			foreach (var item in Items)
				if (item.Expression.Equals(expr, (x, y) => !(x is SqlColumn col) || !col.Parent!.HasSetOperators || x == y))
					return;

			Items.Add(new SqlOrderByItem(expr, isDescending, isPositioned));
		}

		public List<SqlOrderByItem> Items { get; } = [];

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

			using(writer.IndentScope())
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
