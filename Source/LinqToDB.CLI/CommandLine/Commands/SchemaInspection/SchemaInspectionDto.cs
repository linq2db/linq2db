using System;

namespace LinqToDB.CommandLine.Commands.SchemaInspection
{
	internal sealed record SchemaInspectionDto(
		string Provider,
		string Dialect,
		string? Database,
		SchemaInspectionEffectiveOptions Options,
		SchemaTableDto[] Tables,
		string[] Warnings);
}
