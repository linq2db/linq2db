using System.Collections.Generic;

namespace LinqToDB.CodeGen.Metadata
{
	public record ScalarFunction(
		ObjectName Name,
		string? Description,
		IReadOnlyCollection<Parameter> Parameters,
		ReturnValue[] Result,
		bool IsDynamicResult)
		: ScalarFunctionBase(Name, Description, Parameters);
}
