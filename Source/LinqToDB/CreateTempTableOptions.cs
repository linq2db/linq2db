using System;

namespace LinqToDB
{
	/// <summary>
	/// Options for creating a temporary table.
	/// </summary>
	public sealed class CreateTempTableOptions
	{
		/// <summary>
		/// Name of the temporary table. If not specified, value from mapping will be used.
		/// </summary>
		public string?      TableName       { get; set; }

		/// <summary>
		/// Name of the table's database. If not specified, value from mapping will be used.
		/// </summary>
		public string?      DatabaseName    { get; set; }

		/// <summary>
		/// Name of table's schema/owner. If not specified, value from mapping will be used.
		/// </summary>
		public string?      SchemaName      { get; set; }

		/// <summary>
		/// Name of linked server. If not specified, value from mapping will be used.
		/// </summary>
		public string?      ServerName      { get; set; }

		/// <summary>
		/// Replacement for <c>"CREATE TABLE table_name"</c> header. Header is a template with <c>{0}</c> parameter for table name.
		/// </summary>
		public string?      StatementHeader { get; set; }

		/// <summary>
		/// SQL appended to the generated create table statement.
		/// </summary>
		public string?      StatementFooter { get; set; }

		/// <summary>
		/// Table Options.
		/// </summary>
		public TableOptions TableOptions    { get; set; }

		/// <summary>
		/// Creates a new options instance.
		/// </summary>
		/// <param name="tableName">Optional name of temporary table. If not specified, value from mapping will be used.</param>
		/// <param name="databaseName">Optional name of table's database. If not specified, value from mapping will be used.</param>
		/// <param name="schemaName">Optional name of table schema/owner. If not specified, value from mapping will be used.</param>
		/// <param name="serverName">Optional name of linked server. If not specified, value from mapping will be used.</param>
		/// <param name="statementHeader"></param>
		/// <param name="statementFooter"></param>
		/// <param name="tableOptions">Optional Table options. Default is <see cref="TableOptions.IsTemporary"/>.</param>
		public CreateTempTableOptions(
			string?      tableName       = default,
			string?      databaseName    = default,
			string?      schemaName      = default,
			string?      serverName      = default,
			string?      statementHeader = default,
			string?      statementFooter = default,
			TableOptions tableOptions    = TableOptions.IsTemporary)
		{
			TableName       = tableName;
			DatabaseName    = databaseName;
			SchemaName      = schemaName;
			ServerName      = serverName;
			StatementHeader = statementHeader;
			StatementFooter = statementFooter;
			TableOptions    = tableOptions;
		}
	}
}
