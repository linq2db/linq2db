using System;

namespace LinqToDB.SqlBuilder
{
	public interface ISqlPredicate : IQueryElement, ISqlExpressionWalkable, ICloneableElement
	{
		bool CanBeNull();
		int  Precedence { get; }
	}
}
