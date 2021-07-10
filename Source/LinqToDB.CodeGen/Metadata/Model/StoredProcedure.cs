using System;
using System.Collections.Generic;

namespace LinqToDB.CodeGen.Metadata
{
	public record StoredProcedure(
		ObjectName Name,
		string? Description,
		IReadOnlyCollection<Parameter> Parameters,
		Exception? SchemaError,
		IReadOnlyList<IReadOnlyCollection<Column>> ResultSets,
		ReturnValue? ReturnParameter)
		: TableFunctionBase(Name, Description, Parameters, SchemaError);
}
