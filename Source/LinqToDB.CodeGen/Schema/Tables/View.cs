using System.Collections.Generic;

namespace LinqToDB.CodeGen.Schema
{
	/// <summary>
	/// View descriptor.
	/// </summary>
	/// <param name="Name">Name of view.</param>
	/// <param name="Description">Optional description, associated with view.</param>
	/// <param name="IsSystem">Flag indicating that this is predefined system view or user-defined one.</param>
	/// <param name="Columns">Ordered (by ordinal) list of view columns.</param>
	/// <param name="Identity">Optional identity column descriptor.</param>
	public record View(
		ObjectName Name,
		string? Description,
		bool IsSystem,
		IReadOnlyCollection<Column> Columns,
		Identity? Identity) : TableLikeObject(Name, Description, IsSystem, Columns, Identity);
}
