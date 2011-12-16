using System;

namespace LinqToDB.SqlBuilder
{
	public interface ISqlExpression : IQueryElement, IEquatable<ISqlExpression>, ISqlExpressionWalkable, ICloneableElement
	{
		bool CanBeNull();
		int  Precedence { get; }
		Type SystemType { get; }
	}
}
