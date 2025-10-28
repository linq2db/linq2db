using System;

using LinqToDB.SqlQuery;

namespace LinqToDB
{
	// IMPORTANT: don't add new paramerters here. This record only used to override CreateTableOptions defaults for temporary tables creation
	/// <summary>
	/// Options for creating a temporary table.
	/// </summary>
	/// <param name="TableName">Optional name of temporary table. If not specified, value from mapping will be used.</param>
	/// <param name="DatabaseName">Optional name of table's database. If not specified, value from mapping will be used.</param>
	/// <param name="SchemaName">Optional name of table schema/owner. If not specified, value from mapping will be used.</param>
	/// <param name="ServerName">Optional name of linked server. If not specified, value from mapping will be used.</param>
	/// <param name="StatementHeader">Replacement for <c>"CREATE TABLE table_name"</c> header. Header is a template with <c>{0}</c> parameter for table name.</param>
	/// <param name="StatementFooter">SQL appended to the generated create table statement.</param>
	/// <param name="TableOptions">Optional Table options. Default is <see cref="TableOptions.IsTemporary"/>.</param>
	/// <param name="DefaultNullable">Defines how columns nullability flag should be generated:
	/// <para> - <see cref="DefaultNullable.Null"/> - generate only <c>NOT NULL</c> for non-nullable fields. Missing nullability information treated as <c>NULL</c> by database.</para>
	/// <para> - <see cref="DefaultNullable.NotNull"/> - generate only <c>NULL</c> for nullable fields. Missing nullability information treated as <c>NOT NULL</c> by database.</para>
	/// <para> - <see cref="DefaultNullable.None"/> - explicitly generate <c>NULL</c> and <c>NOT NULL</c> for all columns.</para>
	/// Default value: <see cref="DefaultNullable.None"/>.
	/// </param>
	public sealed record CreateTempTableOptions(
			string?         TableName       = default,
			string?         DatabaseName    = default,
			string?         SchemaName      = default,
			string?         ServerName      = default,
			string?         StatementHeader = default,
			string?         StatementFooter = default,
			TableOptions    TableOptions    = TableOptions.IsTemporary,
			DefaultNullable DefaultNullable = DefaultNullable.None)
		: CreateTableOptions(
			TableName      : TableName,
			DatabaseName   : DatabaseName,
			SchemaName     : SchemaName,
			ServerName     : ServerName,
			StatementHeader: StatementHeader,
			StatementFooter: StatementFooter,
			TableOptions   : TableOptions,
			DefaultNullable: DefaultNullable);
}
