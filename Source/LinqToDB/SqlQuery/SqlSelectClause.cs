using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LinqToDB.SqlQuery
{
	public class SqlSelectClause : ClauseBase, IQueryElement, ISqlExpressionWalkable
	{
		#region Init

		internal SqlSelectClause(SelectQuery selectQuery) : base(selectQuery)
		{
		}

		internal SqlSelectClause(
			SelectQuery     selectQuery,
			SqlSelectClause clone,
			Dictionary<ICloneableElement,ICloneableElement> objectTree,
			Predicate<ICloneableElement> doClone)
			: base(selectQuery)
		{
			Columns.AddRange(clone.Columns.Select(c => (SqlColumn)c.Clone(objectTree, doClone)));
			IsDistinct = clone.IsDistinct;
			TakeValue  = (ISqlExpression)clone.TakeValue?.Clone(objectTree, doClone);
			SkipValue  = (ISqlExpression)clone.SkipValue?.Clone(objectTree, doClone);
		}

		internal SqlSelectClause(bool isDistinct, ISqlExpression takeValue, ISqlExpression skipValue, IEnumerable<SqlColumn> columns)
			: base(null)
		{
			IsDistinct = isDistinct;
			TakeValue  = takeValue;
			SkipValue  = skipValue;
			Columns.AddRange(columns);
		}

		#endregion

		#region Columns

		public SqlSelectClause Field(SqlField field)
		{
			AddOrFindColumn(new SqlColumn(SelectQuery, field));
			return this;
		}

		public SqlSelectClause Field(SqlField field, string alias)
		{
			AddOrFindColumn(new SqlColumn(SelectQuery, field, alias));
			return this;
		}

		public SqlSelectClause SubQuery(SelectQuery subQuery)
		{
			if (subQuery.ParentSelect != null && subQuery.ParentSelect != SelectQuery)
				throw new ArgumentException("SqlQuery already used as subquery");

			subQuery.ParentSelect = SelectQuery;

			AddOrFindColumn(new SqlColumn(SelectQuery, subQuery));
			return this;
		}

		public SqlSelectClause SubQuery(SelectQuery selectQuery, string alias)
		{
			if (selectQuery.ParentSelect != null && selectQuery.ParentSelect != SelectQuery)
				throw new ArgumentException("SqlQuery already used as subquery");

			selectQuery.ParentSelect = SelectQuery;

			AddOrFindColumn(new SqlColumn(SelectQuery, selectQuery, alias));
			return this;
		}

		public SqlSelectClause Expr(ISqlExpression expr)
		{
			AddOrFindColumn(new SqlColumn(SelectQuery, expr));
			return this;
		}

		public SqlSelectClause Expr(ISqlExpression expr, string alias)
		{
			AddOrFindColumn(new SqlColumn(SelectQuery, expr, alias));
			return this;
		}

		public SqlSelectClause Expr(string expr, params ISqlExpression[] values)
		{
			AddOrFindColumn(new SqlColumn(SelectQuery, new SqlExpression(null, expr, values)));
			return this;
		}

		public SqlSelectClause Expr(Type systemType, string expr, params ISqlExpression[] values)
		{
			AddOrFindColumn(new SqlColumn(SelectQuery, new SqlExpression(systemType, expr, values)));
			return this;
		}

		public SqlSelectClause Expr(string expr, int priority, params ISqlExpression[] values)
		{
			AddOrFindColumn(new SqlColumn(SelectQuery, new SqlExpression(null, expr, priority, values)));
			return this;
		}

		public SqlSelectClause Expr(Type systemType, string expr, int priority, params ISqlExpression[] values)
		{
			AddOrFindColumn(new SqlColumn(SelectQuery, new SqlExpression(systemType, expr, priority, values)));
			return this;
		}

		public SqlSelectClause Expr(string alias, string expr, int priority, params ISqlExpression[] values)
		{
			AddOrFindColumn(new SqlColumn(SelectQuery, new SqlExpression(null, expr, priority, values)));
			return this;
		}

		public SqlSelectClause Expr(Type systemType, string alias, string expr, int priority, params ISqlExpression[] values)
		{
			AddOrFindColumn(new SqlColumn(SelectQuery, new SqlExpression(systemType, expr, priority, values)));
			return this;
		}

		public SqlSelectClause Expr<T>(ISqlExpression expr1, string operation, ISqlExpression expr2)
		{
			AddOrFindColumn(new SqlColumn(SelectQuery, new SqlBinaryExpression(typeof(T), expr1, operation, expr2)));
			return this;
		}

		public SqlSelectClause Expr<T>(ISqlExpression expr1, string operation, ISqlExpression expr2, int priority)
		{
			AddOrFindColumn(new SqlColumn(SelectQuery, new SqlBinaryExpression(typeof(T), expr1, operation, expr2, priority)));
			return this;
		}

		public SqlSelectClause Expr<T>(string alias, ISqlExpression expr1, string operation, ISqlExpression expr2, int priority)
		{
			AddOrFindColumn(new SqlColumn(SelectQuery, new SqlBinaryExpression(typeof(T), expr1, operation, expr2, priority), alias));
			return this;
		}

		public int Add(ISqlExpression expr)
		{
			if (expr is SqlColumn && ((SqlColumn)expr).Parent == SelectQuery)
				throw new InvalidOperationException();

			return AddOrFindColumn(new SqlColumn(SelectQuery, expr));
		}

		public int AddNew(ISqlExpression expr)
		{
			if (expr is SqlColumn && ((SqlColumn)expr).Parent == SelectQuery)
				throw new InvalidOperationException();

			Columns.Add(new SqlColumn(SelectQuery, expr));
			return Columns.Count - 1;
		}

		public int Add(ISqlExpression expr, string alias)
		{
			return AddOrFindColumn(new SqlColumn(SelectQuery, expr, alias));
		}

		/// <summary>
		/// Adds column if it is not added yet.
		/// </summary>
		/// <returns>Returns index of column in Columns list.</returns>
		int AddOrFindColumn(SqlColumn col)
		{
			for (var i = 0; i < Columns.Count; i++)
			{
				if (Columns[i].Equals(col))
				{
					return i;
				}
			}

#if DEBUG

			switch (col.Expression.ElementType)
			{
				case QueryElementType.SqlField :
					{
						var table = ((SqlField)col.Expression).Table;

						//if (SqlQuery.From.GetFromTables().Any(_ => _ == table))
						//	throw new InvalidOperationException("Wrong field usage.");

						break;
					}

				case QueryElementType.Column :
					{
						var query = ((SqlColumn)col.Expression).Parent;

						//if (!SqlQuery.From.GetFromQueries().Any(_ => _ == query))
						//	throw new InvalidOperationException("Wrong column usage.");

						if (SelectQuery.HasUnion)
						{
							if (SelectQuery.Unions.Any(u => u.SelectQuery == query))
							{

							}
						}

						break;
					}

				case QueryElementType.SqlQuery :
					{
						if (col.Expression == SelectQuery)
							throw new InvalidOperationException("Wrong query usage.");
						break;
					}
			}

#endif
			Columns.Add(col);

			return Columns.Count - 1;
		}

		public List<SqlColumn> Columns { get; } = new List<SqlColumn>();

		#endregion

		#region HasModifier

		public bool HasModifier => IsDistinct || SkipValue != null || TakeValue != null;

		#endregion

		#region Distinct

		public SqlSelectClause Distinct
		{
			get { IsDistinct = true; return this; }
		}

		public bool IsDistinct { get; set; }

		#endregion

		#region Take

		public SqlSelectClause Take(int value, TakeHints? hints)
		{
			TakeValue = new SqlValue(value);
			TakeHints = hints;
			return this;
		}

		public SqlSelectClause Take(ISqlExpression value, TakeHints? hints)
		{
			TakeHints = hints;
			TakeValue = value;
			return this;
		}

		public ISqlExpression TakeValue { get; private set; }
		public TakeHints?     TakeHints { get; private set; }

		#endregion

		#region Skip

		public SqlSelectClause Skip(int value)
		{
			SkipValue = new SqlValue(value);
			return this;
		}

		public SqlSelectClause Skip(ISqlExpression value)
		{
			SkipValue = value;
			return this;
		}

		public ISqlExpression SkipValue { get; set; }

		#endregion

		#region Overrides

#if OVERRIDETOSTRING

		public override string ToString()
		{
			return ((IQueryElement)this).ToString(new StringBuilder(), new Dictionary<IQueryElement,IQueryElement>()).ToString();
		}

#endif

		#endregion

		#region ISqlExpressionWalkable Members

		ISqlExpression ISqlExpressionWalkable.Walk(bool skipColumns, Func<ISqlExpression,ISqlExpression> func)
		{
			for (var i = 0; i < Columns.Count; i++)
			{
				var col  = Columns[i];
				var expr = col.Walk(skipColumns, func);

				if (expr is SqlColumn column)
					Columns[i] = column;
				else
					Columns[i] = new SqlColumn(col.Parent, expr, col.Alias);
			}

			TakeValue = TakeValue?.Walk(skipColumns, func);
			SkipValue = SkipValue?.Walk(skipColumns, func);

			return null;
		}

		#endregion

		#region IQueryElement Members

		public QueryElementType ElementType => QueryElementType.SelectClause;

		StringBuilder IQueryElement.ToString(StringBuilder sb, Dictionary<IQueryElement,IQueryElement> dic)
		{
			if (dic.ContainsKey(this))
				return sb.Append("...");

			dic.Add(this, this);

			sb.Append("SELECT ");

			if (IsDistinct) sb.Append("DISTINCT ");

			if (SkipValue != null)
			{
				sb.Append("SKIP ");
				SkipValue.ToString(sb, dic);
				sb.Append(" ");
			}

			if (TakeValue != null)
			{
				sb.Append("TAKE ");
				TakeValue.ToString(sb, dic);
				sb.Append(" ");
			}

			sb.AppendLine();

			if (Columns.Count == 0)
				sb.Append("\t*, \n");
			else
				for (var i = 0; i < Columns.Count; i++)
				{
					var c = Columns[i];
					sb.Append("\t");
					((IQueryElement)c).ToString(sb, dic);
					sb
						.Append(" as ")
						.Append(c.Alias ?? "c" + (i + 1))
						.Append(", \n");
				}

			sb.Length -= 3;

			dic.Remove(this);

			return sb;
		}

		#endregion
	}
}
