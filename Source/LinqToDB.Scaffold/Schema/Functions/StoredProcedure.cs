using System;
using System.Collections.Generic;
using LinqToDB.SqlQuery;

namespace LinqToDB.Schema
{
	/// <summary>
	/// Stored procedure descriptor.
	/// </summary>
	/// <param name="Name">Procedure name.</param>
	/// <param name="Description">Optional procedure description.</param>
	/// <param name="Parameters">Ordered list of parameters.</param>
	/// <param name="SchemaError">If <paramref name="ResultSets"/> schema failed to load, contains generated exception.</param>
	/// <param name="ResultSets">Result sets schema or <c>null</c> if schema load failed.</param>
	/// <param name="Result">Procedure scalar return value descriptor.</param>
	public sealed record StoredProcedure(
		SqlObjectName                               Name,
		string?                                     Description,
		IReadOnlyCollection<Parameter>              Parameters,
		Exception?                                  SchemaError,
		IReadOnlyList<IReadOnlyList<ResultColumn>>? ResultSets,
		Result                                      Result)
		: CallableObject(CallableKind.StoredProcedure, Name, Description, Parameters);
}
