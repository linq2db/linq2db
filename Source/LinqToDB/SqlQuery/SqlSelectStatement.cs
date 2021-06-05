﻿using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToDB.SqlQuery
{
	public class SqlSelectStatement : SqlStatementWithQueryBase
	{
		public SqlSelectStatement(SelectQuery selectQuery) : base(selectQuery)
		{
		}

		public SqlSelectStatement() : base(null)
		{
		}

		public override QueryType          QueryType  => QueryType.Select;
		public override QueryElementType   ElementType => QueryElementType.SelectStatement;

		public override StringBuilder ToString(StringBuilder sb, Dictionary<IQueryElement, IQueryElement> dic)
		{
			if (With?.Clauses.Count > 0)
			{
				With?.ToString(sb, dic);
				sb.AppendLine("--------------------------");
			}

			return SelectQuery.ToString(sb, dic);
		}

		public override ISqlExpression? Walk(WalkOptions options, Func<ISqlExpression, ISqlExpression> func)
		{
			With?.Walk(options, func);
			var newQuery = SelectQuery.Walk(options, func);
			if (!ReferenceEquals(newQuery, SelectQuery))
				SelectQuery = (SelectQuery)newQuery;
			return null;
		}
	}
}
