using System;
using System.Collections.Generic;

namespace LinqToDB.CodeGen.Schema
{
	/// <summary>
	/// Stored procedure descriptor.
	/// </summary>
	/// <param name="Name">Procedure name.</param>
	/// <param name="Description">Optional procedure description.</param>
	/// <param name="IsSystem">Flag indicating that procedure is predefined system procedure or user-defined one.</param>
	/// <param name="Parameters">Ordered list of parameters.</param>
	/// <param name="SchemaError">If <paramref name="ResultSets"/> schema failed to load, contains generated exception.</param>
	/// <param name="ResultSets">Result sets schema or <c>null</c> if schema load failed.</param>
	/// <param name="Result">Procedure scalar return value descriptor.</param>
	public record StoredProcedure(
		ObjectName Name,
		string? Description,
		bool IsSystem,
		IReadOnlyCollection<Parameter> Parameters,
		Exception? SchemaError,
		IReadOnlyList<IReadOnlyCollection<ResultColumn>>? ResultSets,
		Result Result)
		: CallableObject(CallableKind.StoredProcedure, Name, Description, IsSystem, Parameters);
}
