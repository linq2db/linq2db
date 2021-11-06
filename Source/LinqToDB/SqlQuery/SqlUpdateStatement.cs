using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToDB.SqlQuery
{
	public class SqlUpdateStatement : SqlStatementWithQueryBase
	{
		public override QueryType QueryType          => QueryType.Update;
		public override QueryElementType ElementType => QueryElementType.UpdateStatement;

		public SqlOutputClause? Output { get; set; }

		private SqlUpdateClause? _update;

		public SqlUpdateClause Update
		{
			get => _update ??= new SqlUpdateClause();
			set => _update = value;
		}

		internal bool HasUpdate => _update != null;

		public SqlUpdateStatement(SelectQuery selectQuery) : base(selectQuery)
		{
		}

		public override StringBuilder ToString(StringBuilder sb, Dictionary<IQueryElement, IQueryElement> dic)
		{
			sb.AppendLine("UPDATE");

			((IQueryElement)Update).ToString(sb, dic);

			sb.AppendLine();

			SelectQuery.ToString(sb, dic);

			return sb;
		}

		public override ISqlExpression? Walk<TContext>(WalkOptions options, TContext context, Func<TContext, ISqlExpression, ISqlExpression> func)
		{
			With?.Walk(options, context, func);
			((ISqlExpressionWalkable?)_update)?.Walk(options, context, func);
			((ISqlExpressionWalkable?)Output)?.Walk(options, context, func);

			SelectQuery = (SelectQuery)SelectQuery.Walk(options, context, func);

			return null;
		}

		public override void WalkQueries<TContext>(TContext context, Func<TContext, SelectQuery, SelectQuery> func)
		{
			base.WalkQueries(context, func);

			foreach (var setItem in Update.Items) {
				if (setItem.Expression is SelectQuery q) {
					setItem.Expression = func(context, q);
				}
			}
		}

		public override ISqlTableSource? GetTableSource(ISqlTableSource table)
		{
			var result = SelectQuery.GetTableSource(table);

			if (result != null)
				return result;

			if (table == _update?.Table)
				return _update.Table;

			if (Update != null)
			{
				foreach (var item in Update.Items)
				{
					if (item.Expression is SelectQuery q)
					{
						result = q.GetTableSource(table);
						if (result != null)
							return result;
					}
				}
			}

			return result;
		}

		public override bool IsDependedOn(SqlTable table)
		{
			// do not allow to optimize out Update table
			if (Update == null)
				return false;

			return null != Update.Find(table, static (table, e) =>
			{
				return e switch
				{
					SqlTable t => QueryHelper.IsEqualTables(t, table),
					SqlField f => QueryHelper.IsEqualTables(f.Table as SqlTable, table),
					_          => false,
				};
			});
		}

	}
}
