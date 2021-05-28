﻿using System;
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

		internal SqlSelectClause(bool isDistinct, ISqlExpression? takeValue, TakeHints? takeHints, ISqlExpression? skipValue, IEnumerable<SqlColumn> columns)
			: base(null)
		{
			IsDistinct = isDistinct;
			TakeValue  = takeValue;
			TakeHints  = takeHints;
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

		public SqlSelectClause ExprNew(ISqlExpression expr)
		{
			Columns.Add(new SqlColumn(SelectQuery, expr));
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
			if (expr is SqlColumn column && column.Parent == SelectQuery)
				throw new InvalidOperationException();

			return AddOrFindColumn(new SqlColumn(SelectQuery, expr));
		}

		public SqlColumn AddColumn(ISqlExpression expr)
		{
			return SelectQuery.Select.Columns[Add(expr)];
		}

		public int AddNew(ISqlExpression expr, string? alias = default)
		{
			if (expr is SqlColumn column && column.Parent == SelectQuery)
				throw new InvalidOperationException();

			Columns.Add(new SqlColumn(SelectQuery, expr, alias));
			return Columns.Count - 1;
		}

		public SqlColumn AddNewColumn(ISqlExpression expr)
		{
			return Columns[AddNew(expr)];
		}

		public int Add(ISqlExpression expr, string? alias)
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
				var expr1 = Columns[i].Expression;
				var expr2 = col.Expression;
				if (expr1.CanBeNull == expr2.CanBeNull && QueryHelper.UnwrapExpression(expr1).Equals(QueryHelper.UnwrapExpression(expr2)))
				{
					return i;
				}

				if (Columns[i].UnderlyingExpression().Equals(col.UnderlyingExpression()))
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

						if (SelectQuery.HasSetOperators)
						{
							if (SelectQuery.SetOperators.Any(u => u.SelectQuery == query))
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

		public bool IsDistinct       { get; set; }
		public bool OptimizeDistinct { get; set; }

		#endregion

		#region Take

		public SqlSelectClause Take(int value, TakeHints? hints)
		{
			TakeValue = new SqlValue(value);
			TakeHints = hints;
			return this;
		}

		public SqlSelectClause Take(ISqlExpression? value, TakeHints? hints)
		{
			TakeHints = hints;
			TakeValue = value;
			return this;
		}

		public ISqlExpression? TakeValue { get; internal set; }
		public TakeHints?      TakeHints { get; private set; }

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

		public ISqlExpression? SkipValue { get; set; }

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

		ISqlExpression? ISqlExpressionWalkable.Walk(WalkOptions options, Func<ISqlExpression,ISqlExpression> func)
		{
			if (!options.SkipColumnDeclaration)
			{
				for (var i = 0; i < Columns.Count; i++)
				{
					var col = Columns[i];
					var expr = col.Walk(options, func);

					if (expr is SqlColumn column)
						Columns[i] = column;
					else
						Columns[i] = new SqlColumn(col.Parent, expr, col.Alias);
				}
			}

			TakeValue = TakeValue?.Walk(options, func);
			SkipValue = SkipValue?.Walk(options, func);

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
				sb.Append(' ');
			}

			if (TakeValue != null)
			{
				sb.Append("TAKE ");
				TakeValue.ToString(sb, dic);
				sb.Append(' ');
			}

			sb.AppendLine();

			if (Columns.Count == 0)
				sb.Append("\t*, \n");
			else
			{
				var columnNames = new List<string>();
				var csb         = new StringBuilder();
				var maxLength   = 0;
				for (var i = 0; i < Columns.Count; i++)
				{
					csb.Length = 0;
					var c = Columns[i];
					csb.Append('\t');

					csb
						.Append('t')
						.Append(c.Parent?.SourceID ?? -1)
#if DEBUG
						.Append('[').Append(c.ColumnNumber).Append(']')
#endif
						.Append('.')
						.Append(c.Alias ?? "c" + (i + 1));

					var columnName = csb.ToString();
					columnNames.Add(columnName);
					maxLength = Math.Max(maxLength, columnName.Length);
				}

				for (var i = 0; i < Columns.Count; i++)
				{
					var c          = Columns[i];
					var columnName = columnNames[i];
					sb.Append(columnName)
						.Append(' ', maxLength - columnName.Length)
						.Append(" = ");

					csb.Length = 0;
					c.Expression.ToString(csb, dic);

					var expressionText = csb.ToString();
					if (expressionText.Contains("\n"))
					{
						var ident = "\t" + new string(' ', maxLength + 2);
						expressionText = expressionText.Replace("\n", "\n" + ident);
					}

					sb
						.Append(expressionText)
						.Append(", \n");
				}
			}

			sb.Length -= 3;

			dic.Remove(this);

			return sb;
		}

		#endregion
	}
}
