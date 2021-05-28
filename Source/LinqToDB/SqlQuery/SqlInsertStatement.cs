﻿using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToDB.SqlQuery
{
	public class SqlInsertStatement : SqlStatementWithQueryBase
	{

		public SqlInsertStatement() : base(null)
		{
		}

		public SqlInsertStatement(SelectQuery selectQuery) : base(selectQuery)
		{
		}

		public override QueryType          QueryType   => QueryType.Insert;
		public override QueryElementType   ElementType => QueryElementType.InsertStatement;

		#region InsertClause

		private SqlInsertClause? _insert;
		public  SqlInsertClause   Insert
		{
			get => _insert ??= new SqlInsertClause();
			set => _insert = value;
		}

		internal bool HasInsert => _insert != null;

		#endregion

		#region Output

		public  SqlOutputClause?  Output { get; set; }

		#endregion

		public override StringBuilder ToString(StringBuilder sb, Dictionary<IQueryElement, IQueryElement> dic)
		{
			((IQueryElement?)_insert)?.ToString(sb, dic);
			return sb;
		}

		public override ISqlExpression? Walk(WalkOptions options, Func<ISqlExpression, ISqlExpression> func)
		{
			With?.Walk(options, func);
			((ISqlExpressionWalkable?)_insert)?.Walk(options, func);

			SelectQuery = (SelectQuery)SelectQuery.Walk(options, func);

			return null;
		}

		public override IEnumerable<IQueryElement> EnumClauses()
		{
			if (_insert != null)
				yield return _insert;
		}

		public override ISqlTableSource? GetTableSource(ISqlTableSource table)
		{
			if (_insert?.Into == table)
				return table;

			return SelectQuery!.GetTableSource(table);
		}
	}
}
