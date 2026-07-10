using System;

namespace LinqToDB.CommandLine.Commands.SchemaInspection
{
	internal sealed record SchemaColumnDto(
		string  Name,
		int?    Ordinal,
		string? DatabaseType,
		string  DataType,
		string? SystemType,
		string? ProviderSpecificType,
		bool    Nullable,
		bool    Identity,
		bool    PrimaryKey,
		int?    PrimaryKeyOrder,
		int?    Length,
		int?    Precision,
		int?    Scale,
		string? Description);
}
