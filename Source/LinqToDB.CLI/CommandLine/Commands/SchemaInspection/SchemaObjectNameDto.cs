using System;

namespace LinqToDB.CommandLine.Commands.SchemaInspection
{
	internal sealed record SchemaObjectNameDto(
		string? Catalog,
		string? Schema,
		string  Name,
		string  Kind);
}
