using System;

namespace LinqToDB.SqlQuery
{
	public interface ISqlExpression : IQueryElement, IEquatable<ISqlExpression>, ISqlExpressionWalkable
	{
		bool Equals   (ISqlExpression other, Func<ISqlExpression,ISqlExpression,bool> comparer);

		bool  CanBeNull  { get; }
		int   Precedence { get; }
		// TODO: v4 refactoring: replace with DbDataType and eradicate nullability. Should remove need for GetExpressionType method
		Type? SystemType { get; }
	}
}
