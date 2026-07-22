using System;

namespace LinqToDB.CommandLine.Commands.SchemaInspection
{
	internal sealed record SchemaTableDto(
		string?               Catalog,
		string?               Schema,
		string?               Name,
		string                Kind,
		string?               Description,
		SchemaColumnDto[]     Columns,
		SchemaPrimaryKeyDto?  PrimaryKey,
		SchemaForeignKeyDto[] ForeignKeys);
}
