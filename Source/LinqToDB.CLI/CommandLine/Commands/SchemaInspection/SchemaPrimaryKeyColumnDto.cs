using System;

namespace LinqToDB.CommandLine.Commands.SchemaInspection
{
	internal sealed record SchemaPrimaryKeyColumnDto(string Name, int Order);
}
