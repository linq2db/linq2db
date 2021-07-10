using System.Collections.Generic;

namespace LinqToDB.CodeGen.Metadata
{
	public record ForeignKey(
		string Name,
		TableBase Source,
		TableBase Target,
		IReadOnlyList<(Column sourceColumn, Column targetColumn)> OrderedColumns);
}
