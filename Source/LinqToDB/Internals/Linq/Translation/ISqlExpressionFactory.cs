using System;

using LinqToDB.Common;
using LinqToDB.Internals.SqlQuery;

namespace LinqToDB.Internals.Linq.Translation
{
	public interface ISqlExpressionFactory
	{
		DataOptions DataOptions { get; }
		DbDataType GetDbDataType(ISqlExpression expression);
		DbDataType GetDbDataType(Type type);
	}
}
