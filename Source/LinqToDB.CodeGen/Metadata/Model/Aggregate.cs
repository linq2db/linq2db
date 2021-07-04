using System.Collections.Generic;

namespace LinqToDB.CodeGen.Metadata
{
	public record Aggregate(
		ObjectName Name,
		string? Description,
		IReadOnlyCollection<Parameter> Parameters,
		ReturnValue Result)
		: ScalarFunctionBase(Name, Description, Parameters);
}
