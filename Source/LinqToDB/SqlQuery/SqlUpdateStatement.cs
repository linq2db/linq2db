﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LinqToDB.SqlQuery
{
	public class SqlUpdateStatement : SqlStatementWithQueryBase
	{
		public override QueryType QueryType          => QueryType.Update;
		public override QueryElementType ElementType => QueryElementType.UpdateStatement;

		private SqlUpdateClause? _update;

		public SqlUpdateClause Update
		{
			get => _update ??= new SqlUpdateClause();
			set => _update = value;
		}

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

		public override ISqlExpression? Walk(WalkOptions options, Func<ISqlExpression, ISqlExpression> func)
		{
			With?.Walk(options, func);
			((ISqlExpressionWalkable?)_update)?.Walk(options, func);

			SelectQuery = (SelectQuery)SelectQuery.Walk(options, func);

			return null;
		}

		public override ICloneableElement Clone(Dictionary<ICloneableElement, ICloneableElement> objectTree, Predicate<ICloneableElement> doClone)
		{
			var clone = new SqlUpdateStatement((SelectQuery)SelectQuery.Clone(objectTree, doClone));

			if (_update != null)
				clone._update = (SqlUpdateClause)_update.Clone(objectTree, doClone);

			if (With != null)
				clone.With = (SqlWithClause)With.Clone(objectTree, doClone);

			clone.Parameters.AddRange(Parameters.Select(p => (SqlParameter)p.Clone(objectTree, doClone)));

			objectTree.Add(this, clone);

			return clone;
		}

		public override IEnumerable<IQueryElement> EnumClauses()
		{
			if (_update != null)
				yield return _update;
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

			return null != new QueryVisitor().Find(Update, e =>
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
