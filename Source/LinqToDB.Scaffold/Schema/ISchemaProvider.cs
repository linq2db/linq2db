using System.Collections.Generic;
using System.Data.Common;

namespace LinqToDB.Schema
{
	// TODO: add async overloads in future, when we will have schema provider with async support
	/// <summary>
	/// Database schema provider.
	/// </summary>
	public interface ISchemaProvider
	{
		/// <summary>
		/// Returns schema information for all tables.
		/// </summary>
		/// <returns>Tables schema collection.</returns>
		IEnumerable<Table> GetTables();

		/// <summary>
		/// Returns schema information for all views.
		/// </summary>
		/// <returns>Views schema collection.</returns>
		IEnumerable<View> GetViews();

		/// <summary>
		/// Returns schema information for all foreign keys.
		/// </summary>
		/// <returns>Foreign keys schema collection.</returns>
		IEnumerable<ForeignKey> GetForeignKeys();

		/// <summary>
		/// Returns schema information for all stored procedures.
		/// </summary>
		/// <param name="withSchema">Try to load result records schema.</param>
		/// <param name="safeSchemaOnly">When <paramref name="withSchema"/> is <see langword="true" />, specify record schema load method:
		/// <list type="bullet">
		/// <item><see langword="true" />: read record metadata (not supported by most of databases)</item>
		/// <item><see langword="false"/>: execute procedure in schema-only mode. Could lead to unwanted side-effects if procedure contains non-transactional functionality</item>
		/// </list>
		/// </param>
		/// <returns>Stored procedures schema collection.</returns>
		IEnumerable<StoredProcedure> GetProcedures(bool withSchema, bool safeSchemaOnly);

		/// <summary>
		/// Returns schema information for all table functions.
		/// </summary>
		/// <list type="bullet">
		/// <item><see langword="true" />: read record metadata (not supported by most of databases)</item>
		/// <item><see langword="false"/>: execute function in schema-only mode. Could lead to unwanted side-effects if function contains non-transactional functionality</item>
		/// </list>
		/// <returns>Table functions schema collection.</returns>
		IEnumerable<TableFunction> GetTableFunctions();

		/// <summary>
		/// Returns schema information for all scalar functions.
		/// </summary>
		/// <returns>Scalar functions schema collection.</returns>
		IEnumerable<ScalarFunction> GetScalarFunctions();

		/// <summary>
		/// Returns schema information for all aggregate functions.
		/// </summary>
		/// <returns>Aggregate functions schema collection.</returns>
		IEnumerable<AggregateFunction> GetAggregateFunctions();

		/// <summary>
		/// Gets list of default database schemas.
		/// </summary>
		/// <returns>List of default schemas.</returns>
		ISet<string> GetDefaultSchemas();

		/// <summary>
		/// Gets current database name.
		/// </summary>
		string? DatabaseName            { get; }
		/// <summary>
		/// Gets current server name.
		/// </summary>
		string? ServerVersion           { get; }
		/// <summary>
		/// Gets value of <see cref="DbConnection.DataSource"/> property.
		/// Returned value is implementation-specific to used ADO.NET provider.
		/// </summary>
		string? DataSource              { get; }
		/// <summary>
		/// Database-specific options, useful for scaffold process.
		/// </summary>
		DatabaseOptions DatabaseOptions { get; }
	}
}
