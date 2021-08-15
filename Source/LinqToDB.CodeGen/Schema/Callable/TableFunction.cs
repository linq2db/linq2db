using System;
using System.Collections.Generic;

namespace LinqToDB.CodeGen.Schema
{
	/// <summary>
	/// Table function descriptor.
	/// </summary>
	/// <param name="Name">Function name.</param>
	/// <param name="Description">Optional function description.</param>
	/// <param name="IsSystem">Flag indicating that function is predefined system function or user-defined one.</param>
	/// <param name="Parameters">Ordered list of parameters.</param>
	/// <param name="SchemaError">If <paramref name="Result" /> schema failed to load, contains generated exception.</param>
	/// <param name="Result">Result set schema or <c>null</c> if schema load failed.</param>
	public record TableFunction(
		ObjectName Name,
		string? Description,
		bool IsSystem,
		IReadOnlyCollection<Parameter> Parameters,
		Exception? SchemaError,
		IReadOnlyCollection<ResultColumn>? Result)
		: CallableObject(CallableKind.TableFunction, Name, Description, IsSystem, Parameters);
}
