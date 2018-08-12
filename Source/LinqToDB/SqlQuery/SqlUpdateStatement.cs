using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LinqToDB.SqlQuery
{
	public class SqlUpdateStatement : SqlStatementWithQueryBase
	{
		public override QueryType QueryType          => QueryType.Update;
		public override QueryElementType ElementType => QueryElementType.UpdateStatement;

		private SqlUpdateClause _update;

		public SqlUpdateClause Update
		{
			get => _update ?? (_update = new SqlUpdateClause());
			set => _update = value;
		}

		public SqlUpdateStatement(SelectQuery selectQuery) : base(selectQuery)
		{
		}

		public override StringBuilder ToString(StringBuilder sb, Dictionary<IQueryElement, IQueryElement> dic)
		{
			((IQueryElement)Update).ToString(sb, dic);
			return sb;
		}

		public override ISqlExpression Walk(bool skipColumns, Func<ISqlExpression, ISqlExpression> func)
		{
			((ISqlExpressionWalkable)_update)?.Walk(skipColumns, func);

			SelectQuery = (SelectQuery)SelectQuery.Walk(skipColumns, func);

			return null;
		}

		public override ICloneableElement Clone(Dictionary<ICloneableElement, ICloneableElement> objectTree, Predicate<ICloneableElement> doClone)
		{
			var clone = new SqlUpdateStatement((SelectQuery)SelectQuery.Clone(objectTree, doClone));

			if (_update != null)
				clone._update = (SqlUpdateClause)_update.Clone(objectTree, doClone);
			
			clone.Parameters.AddRange(Parameters.Select(p => (SqlParameter)p.Clone(objectTree, doClone)));

			objectTree.Add(this, clone);

			return clone;
		}

		public override IEnumerable<IQueryElement> EnumClauses()
		{
			if (_update != null)
				yield return _update;
		}

		public override ISqlTableSource GetTableSource(ISqlTableSource table)
		{
			var result = SelectQuery.GetTableSource(table);

			if (result == null)
				return _update?.Table;

			//if (result == null)
			//{
			//	var updateTable = _update?.Table;
			//	if (updateTable != null)
			//	{
			//		if (updateTable == table)
			//			return table;

			//		if (table is SelectQuery query)
			//		{
			//			if (QueryHelper.IsEqualTables(updateTable, updateTable))
			//				var result = query.GetTableSource(table);
			//			if (result != null)
			//				return result;
			//		}
			//	}
			//}


			//if (updateTable != null)
			//{

			//}


			return SelectQuery.GetTableSource(table);
		}

		public override bool IsDependedOn(SqlTable table)
		{
			// do not allow to optimize out Update table
			if (Update == null)
				return false;
			return null != QueryVisitor.Find(Update, e =>
			{
				switch (e)
				{
					case SqlTable t: return QueryHelper.IsEqualTables(t, table);
					case SqlField f: return QueryHelper.IsEqualTables(f.Table as SqlTable, table);
				}

				return false;
			});
		}

	}
}
