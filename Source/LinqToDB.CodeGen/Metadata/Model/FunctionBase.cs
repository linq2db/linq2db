using System.Collections.Generic;

namespace LinqToDB.CodeGen.Metadata
{
	public abstract record FunctionBase(
		ObjectName Name,
		string? Description,
		IReadOnlyCollection<Parameter> Parameters);
}
