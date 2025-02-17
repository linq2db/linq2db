using System.Collections.Generic;

using LinqToDB.Internal.SqlQuery;

namespace LinqToDB.Schema
{
	/// <summary>
	/// Aggregate function descriptor.
	/// </summary>
	/// <param name="Name">Function name.</param>
	/// <param name="Description">Optional function description.</param>
	/// <param name="Parameters">Ordered list of parameters.</param>
	/// <param name="Result">Function return value descriptor.</param>
	public sealed record AggregateFunction(
		SqlObjectName                  Name,
		string?                        Description,
		IReadOnlyCollection<Parameter> Parameters,
		ScalarResult                   Result)
		: CallableObject(CallableKind.AggregateFunction, Name, Description, Parameters);
}
