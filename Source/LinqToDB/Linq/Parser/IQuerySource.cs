using System;
using System.Linq.Expressions;
using System.Reflection;
using LinqToDB.SqlQuery;

namespace LinqToDB.Linq.Parser
{
	public interface IQuerySource
	{
		int QuerySourceId { get; }
		Type ItemType { get; }
		string ItemName { get; }

		bool DoesContainMember(MemberInfo memberInfo);
		ISqlExpression ConvertToSql(ISqlTableSource tableSource, Expression expression);
	}
}
