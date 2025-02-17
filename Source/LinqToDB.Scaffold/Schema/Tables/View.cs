using System.Collections.Generic;

using LinqToDB.Internal.SqlQuery;

namespace LinqToDB.Schema
{
	/// <summary>
	/// View descriptor.
	/// </summary>
	/// <param name="Name">Name of view.</param>
	/// <param name="Description">Optional description, associated with view.</param>
	/// <param name="Columns">Ordered (by ordinal) list of view columns.</param>
	/// <param name="Identity">Optional identity column descriptor.</param>
	/// <param name="PrimaryKey">Optional primary key descriptor.</param>
	public sealed record View(
		SqlObjectName               Name,
		string?                     Description,
		IReadOnlyCollection<Column> Columns,
		Identity?                   Identity,
		PrimaryKey?                 PrimaryKey)
		: TableLikeObject(Name, Description, Columns, Identity, PrimaryKey);
}
