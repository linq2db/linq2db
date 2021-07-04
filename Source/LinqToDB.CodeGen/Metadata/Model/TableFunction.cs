using System;
using System.Collections.Generic;

namespace LinqToDB.CodeGen.Metadata
{
	public record TableFunction(
		ObjectName Name,
		string? Description,
		IReadOnlyCollection<Parameter> Parameters,
		Exception? SchemaError,
		IReadOnlyCollection<Column> Table)
		: TableFunctionBase(Name, Description, Parameters, SchemaError);
}
