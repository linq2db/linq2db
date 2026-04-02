using LinqToDB.Data;

namespace LinqToDB.SchemaProvider
{
	/// <summary>
	/// Database schema provider.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Retrieves structural metadata about the database: tables, views, columns, foreign keys,
	/// stored procedures, and other schema objects.
	/// </para>
	/// <para>
	/// Obtain an implementation via <see cref="DataConnection.DataProvider"/>'s
	/// <c>GetSchemaProvider()</c> method.
	/// </para>
	/// <para>
	/// <b>Transaction note:</b> call <see cref="GetSchema"/> outside of an active transaction.
	/// Several providers (MySQL, SQL Server, Sybase, DB2) do not support or behave incorrectly
	/// when schema queries are issued inside a transaction.
	/// </para>
	/// <para>
	/// AI-Tags: Group=Schema; Execution=Immediate; Composability=Terminal; Affects=SchemaResult; Pipeline=SqlText; Provider=ProviderDefined;
	/// </para>
	/// </remarks>
	public interface ISchemaProvider
	{
		/// <summary>
		/// Returns database schema.
		/// </summary>
		/// <param name="dataConnection">Data connection to use to read schema from.</param>
		/// <param name="options">Schema read configuration options.</param>
		/// <returns>Returns database schema information.</returns>
		/// <remarks>
		/// Note that it is recommended to call this method outside of transaction as some providers do not support it
		/// or behave incorrectly.
		/// At least following providers shouldn't be called in transaction:
		/// - MySQL;
		/// - Microsoft SQL Server;
		/// - Sybase;
		/// - DB2.
		/// <para>
		/// AI-Tags: Group=Schema; Execution=Immediate; Composability=Terminal; Affects=SchemaResult; Pipeline=SqlText; Provider=ProviderDefined;
		/// </para>
		/// </remarks>
		DatabaseSchema GetSchema(DataConnection dataConnection, GetSchemaOptions? options = null);
	}
}
