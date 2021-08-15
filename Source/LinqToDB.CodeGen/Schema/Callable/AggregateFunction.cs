using System.Collections.Generic;

namespace LinqToDB.CodeGen.Schema
{
	/// <summary>
	/// Aggregate function descriptor.
	/// </summary>
	/// <param name="Name">Function name.</param>
	/// <param name="Description">Optional function description.</param>
	/// <param name="IsSystem">Flag indicating that function is predefined system function or user-defined one.</param>
	/// <param name="Parameters">Ordered list of parameters.</param>
	/// <param name="Result">Function return value descriptor.</param>
	public record AggregateFunction(
		ObjectName Name,
		string? Description,
		bool IsSystem,
		IReadOnlyCollection<Parameter> Parameters,
		ScalarResult Result)
		: CallableObject(CallableKind.AggregateFunction, Name, Description, IsSystem, Parameters);
}
