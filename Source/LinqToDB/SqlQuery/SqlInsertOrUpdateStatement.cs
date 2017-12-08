using System;
using System.Collections.Generic;
using System.Text;
using LinqToDB.Mapping;

namespace LinqToDB.SqlQuery
{
	public class SqlInsertOrUpdateStatement: SqlStatementWithQueryBase

	{
		public override QueryType QueryType          => QueryType.InsertOrUpdate;
		public override QueryElementType ElementType => QueryElementType.InsertOrUpdateStatement;

		private SqlInsertClause _insert;
		public  SqlInsertClause  Insert
		{
			get => _insert ?? (_insert = new SqlInsertClause());
			set => _insert = value;
		}

		private SqlUpdateClause _update;
		public  SqlUpdateClause  Update
		{
			get => _update ?? (_update = new SqlUpdateClause());
			set => _update = value;
		}

		public SqlInsertOrUpdateStatement(SelectQuery selectQuery) : base(selectQuery)
		{
		}

		public override StringBuilder ToString(StringBuilder sb, Dictionary<IQueryElement, IQueryElement> dic)
		{
			((IQueryElement)Insert).ToString(sb, dic);
			((IQueryElement)Update).ToString(sb, dic);
			return sb;
		}

		public override ISqlExpression Walk(bool skipColumns, Func<ISqlExpression, ISqlExpression> func)
		{
			((ISqlExpressionWalkable)_insert)?.Walk(skipColumns, func);
			((ISqlExpressionWalkable)_update)?.Walk(skipColumns, func);

			SelectQuery = (SelectQuery)SelectQuery.Walk(skipColumns, func);

			return null;
		}

		public override ICloneableElement Clone(Dictionary<ICloneableElement, ICloneableElement> objectTree, Predicate<ICloneableElement> doClone)
		{
			var clone = new SqlInsertOrUpdateStatement((SelectQuery)SelectQuery.Clone(objectTree, doClone));

			if (_insert != null)
				clone._insert = (SqlInsertClause)_insert.Clone(objectTree, doClone);

			if (_update != null)
				clone._update = (SqlUpdateClause)_update.Clone(objectTree, doClone);
			
			objectTree.Add(this, clone);

			return clone;
		}

		public override IEnumerable<IQueryElement> EnumClauses()
		{
			if (_insert != null)
				yield return _insert;
			if (_update != null)
				yield return _update;
		}

		public override ISqlTableSource GetTableSource(ISqlTableSource table)
		{
			if (_update?.Table == table)
				return table;
			if (_insert?.Into == table)
				return table;

			return SelectQuery.GetTableSource(table);
		}

		public override SqlStatement ProcessParameters(MappingSchema mappingSchema)
		{
			var newQuery = SelectQuery.ProcessParameters(mappingSchema);
			if (!ReferenceEquals(newQuery, SelectQuery))
				return new SqlInsertOrUpdateStatement(newQuery);
			return this;
		}
	}
}
