using System;

namespace LinqToDB.Data.Sql
{
	public interface ISqlPredicate : IQueryElement, ISqlExpressionWalkable, ICloneableElement
	{
		bool CanBeNull();
		int  Precedence { get; }
	}
}
