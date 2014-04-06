using System;

namespace LinqToDB.SqlQuery
{
	public interface ISqlPredicate : IQueryElement, ISqlExpressionWalkable, ICloneableElement
	{
		bool CanBeNull();
		int  Precedence { get; }
	}
}
