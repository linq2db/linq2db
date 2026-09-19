using System;

namespace LinqToDB.CommandLine.Commands.SchemaInspection
{
	internal sealed record SchemaObjectNamesDto(
		string                           Provider,
		string                           Dialect,
		string?                          Database,
		SchemaInspectionEffectiveOptions Options,
		SchemaObjectNameDto[]            Objects,
		string[]                         Warnings);
}
