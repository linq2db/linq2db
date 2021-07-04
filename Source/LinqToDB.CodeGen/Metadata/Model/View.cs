using System.Collections.Generic;

namespace LinqToDB.CodeGen.Metadata
{
	public record View(
		ObjectName Name,
		string? Description,
		bool IsSystem,
		IReadOnlyCollection<Column> Columns,
		PrimaryKey? PrimaryKey,
		Identity? Identity)
		: TableBase(Name, Description, IsSystem, Columns, PrimaryKey, Identity);
}
