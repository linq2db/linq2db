using System;

namespace LinqToDB.CommandLine.Commands.SchemaInspection
{
	internal sealed record SchemaPrimaryKeyDto(SchemaPrimaryKeyColumnDto[] Columns);
}
