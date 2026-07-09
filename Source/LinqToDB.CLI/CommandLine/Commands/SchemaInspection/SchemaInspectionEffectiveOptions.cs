using System;

namespace LinqToDB.CommandLine.Commands.SchemaInspection
{
	/// <summary>
	/// Effective declarative schema provider options returned in command/tool output.
	/// </summary>
	internal sealed record SchemaInspectionEffectiveOptions(
		bool     PreferProviderSpecificTypes,
		bool     GetTables,
		bool     GetForeignKeys,
		bool     GetProcedures,
		bool     GenerateChar1AsString,
		bool     IgnoreSystemHistoryTables,
		string?  DefaultSchema,
		string[] FilterSchemas,
		string[] FilterCatalogs,
		string[] FilterTables);
}
