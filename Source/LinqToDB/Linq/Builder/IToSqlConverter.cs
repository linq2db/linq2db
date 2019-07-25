using System;
using System.Linq.Expressions;

using LinqToDB.SqlQuery;

namespace LinqToDB.Linq.Builder
{
	public interface IToSqlConverter
	{
		ISqlExpression ToSql(Expression expression);
	}
}
