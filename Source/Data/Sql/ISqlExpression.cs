using System;

namespace LinqToDB.Data.Sql
{
	public interface ISqlExpression : IQueryElement, IEquatable<ISqlExpression>, ISqlExpressionWalkable, ICloneableElement
	{
		bool CanBeNull();
		int  Precedence { get; }
		Type SystemType { get; }
	}
}
