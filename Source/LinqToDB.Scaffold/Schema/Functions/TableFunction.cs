using System;
using System.Collections.Generic;

using LinqToDB.Internal.SqlQuery;

namespace LinqToDB.Schema
{
	/// <summary>
	/// Table function descriptor.
	/// </summary>
	/// <param name="Name">Function name.</param>
	/// <param name="Description">Optional function description.</param>
	/// <param name="Parameters">Ordered list of parameters.</param>
	/// <param name="SchemaError">If <paramref name="Result" /> schema failed to load, contains generated exception.</param>
	/// <param name="Result">Result set schema or <c>null</c> if schema load failed.</param>
	public sealed record TableFunction(
		SqlObjectName                      Name,
		string?                            Description,
		IReadOnlyCollection<Parameter>     Parameters,
		Exception?                         SchemaError,
		IReadOnlyCollection<ResultColumn>? Result)
		: CallableObject(CallableKind.TableFunction, Name, Description, Parameters);
}
