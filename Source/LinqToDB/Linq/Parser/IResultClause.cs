using System;

namespace LinqToDB.Linq.Parser
{
	public interface IResultClause
	{
		Type ResultType { get; }
	}
}
