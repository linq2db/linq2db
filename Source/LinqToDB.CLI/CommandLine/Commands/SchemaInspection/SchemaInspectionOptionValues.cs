using System;

namespace LinqToDB.CommandLine.Commands.SchemaInspection
{
	/// <summary>
	/// Raw schema inspection option values collected by command and MCP adapters.
	/// </summary>
	internal sealed record SchemaInspectionOptionValues(
		string?   Profile,
		bool?     PreferProviderSpecificTypes,
		bool?     GetTables,
		bool?     GetForeignKeys,
		bool?     GenerateChar1AsString,
		bool?     IgnoreSystemHistoryTables,
		string?   DefaultSchema,
		string[]? FilterSchemas,
		string[]? FilterCatalogs,
		string[]? FilterTables,
		string?   Output,
		string?   OutputFile,
		bool      Overwrite);
}
