using System.Collections.Generic;

namespace LinqToDB.CodeGen.Schema
{
	/// <summary>
	/// Queryable table-like object descriptor.
	/// </summary>
	/// <param name="Name">Name of object.</param>
	/// <param name="Description">Optional description, associated with current object.</param>
	/// <param name="IsSystem">Flag indicating that object is predefined system object or user-defined one.</param>
	/// <param name="Columns">Ordered (by ordinal) list of columns.</param>
	/// <param name="Identity">Optional identity column descriptor.</param>
	public abstract record TableLikeObject(
		ObjectName Name,
		string? Description,
		bool IsSystem,
		IReadOnlyCollection<Column> Columns,
		Identity? Identity)
	{
		public override string ToString() => Name.ToString();
	}
}
