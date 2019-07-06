using System;

namespace LinqToDB.SqlQuery
{
	public interface ISqlPredicate : IQueryElement, ISqlExpressionWalkable, ICloneableElement, IEquatable<ISqlPredicate>
	{
		bool CanBeNull  { get; }
		int  Precedence { get; }
	}
}
