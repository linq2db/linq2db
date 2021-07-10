using System.Collections.Generic;

namespace LinqToDB.CodeGen.Metadata
{
	public record ScalarFunctionBase(
		ObjectName Name,
		string? Description,
		IReadOnlyCollection<Parameter> Parameters)
		: FunctionBase(Name, Description, Parameters);
}
