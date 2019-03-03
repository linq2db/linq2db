using System;
using System.Linq.Expressions;
using LinqToDB.SqlQuery;

namespace LinqToDB.Linq.Parser
{
	public interface IQuerySource
	{
		Type ItemType { get; }
		string ItemName { get; }

		ISqlExpression ConvertToSql(ISqlTableSource tableSource, Expression expression);
	}
}
