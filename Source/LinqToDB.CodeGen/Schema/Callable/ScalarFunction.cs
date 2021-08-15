using System.Collections.Generic;

namespace LinqToDB.CodeGen.Schema
{
	/// <summary>
	/// Scalar function descriptor.
	/// </summary>
	/// <param name="Name">Function name.</param>
	/// <param name="Description">Optional function description.</param>
	/// <param name="IsSystem">Flag indicating that function is predefined system function or user-defined one.</param>
	/// <param name="Parameters">Ordered list of parameters.</param>
	/// <param name="Result">Function return value descriptor.</param>
	public record ScalarFunction(
		ObjectName Name,
		string? Description,
		bool IsSystem,
		IReadOnlyCollection<Parameter> Parameters,
		Result Result)
		: CallableObject(CallableKind.ScalarFunction, Name, Description, IsSystem, Parameters);
}
