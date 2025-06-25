using System.Collections.Generic;

using LinqToDB.SqlQuery;

namespace LinqToDB.Schema
{
	/// <summary>
	/// Scalar function descriptor.
	/// </summary>
	/// <param name="Name">Function name.</param>
	/// <param name="Description">Optional function description.</param>
	/// <param name="Parameters">Ordered list of parameters.</param>
	/// <param name="Result">Function return value descriptor.</param>
	public sealed record ScalarFunction(
		SqlObjectName                  Name,
		string?                        Description,
		IReadOnlyCollection<Parameter> Parameters,
		Result                         Result)
		: CallableObject(CallableKind.ScalarFunction, Name, Description, Parameters);
}
