using System;
using System.Collections.Generic;

namespace LinqToDB.CodeGen.Metadata
{
	public record TableFunctionBase(
		ObjectName Name,
		string? Description,
		IReadOnlyCollection<Parameter> Parameters,
		Exception? SchemaError)
		: FunctionBase(Name, Description, Parameters);
}
