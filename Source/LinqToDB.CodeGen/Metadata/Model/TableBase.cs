using System.Collections.Generic;

namespace LinqToDB.CodeGen.Metadata
{
	public abstract record TableBase(
		ObjectName Name,
		string? Description,
		bool IsSystem,
		IReadOnlyCollection<Column> Columns,
		PrimaryKey? PrimaryKey,
		Identity? Identity);
}
