using System;

namespace LinqToDB.CommandLine.Commands.SchemaInspection
{
	internal sealed record SchemaObjectRefDto(string? Catalog, string? Schema, string? Name);
}
