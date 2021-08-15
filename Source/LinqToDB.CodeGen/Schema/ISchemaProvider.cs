using System.Collections.Generic;

namespace LinqToDB.CodeGen.Schema
{
	// TODO: add async overloads
	public interface ISchemaProvider
	{
		IEnumerable<Table> GetTables();
		IEnumerable<View> GetViews();

		IEnumerable<ForeignKey> GetForeignKeys();

		IEnumerable<StoredProcedure> GetProcedures(bool withSchema, bool safeSchemaOnly);
		IEnumerable<TableFunction> GetTableFunctions(bool withSchema, bool safeSchemaOnly);

		IEnumerable<ScalarFunction> GetScalarFunctions();
		IEnumerable<AggregateFunction> GetAggregateFunctions();

		ISet<string> GetDefaultSchemas();

		string? DatabaseName { get; }
		string? ServerVersion { get; }
		string? DataSource { get; }
	}
}
