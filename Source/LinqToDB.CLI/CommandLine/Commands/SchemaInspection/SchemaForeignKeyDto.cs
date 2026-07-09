using System;

namespace LinqToDB.CommandLine.Commands.SchemaInspection
{
	internal sealed record SchemaForeignKeyDto(
		string Name,
		string[] Columns,
		SchemaObjectRefDto ReferencedTable,
		string[] ReferencedColumns,
		bool Nullable);
}
