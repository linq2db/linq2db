using System;
using System.Collections.Generic;
using System.Data;

using LinqToDB.Schema;
using LinqToDB.SqlQuery;

namespace LinqToDB.Scaffold
{
	public sealed class SchemaOptions
	{
		internal SchemaOptions() { }

		#region General
		/// <summary>
		/// Gets or sets flags, that specify which database objects to load from database schema.
		/// <list type="bullet">
		/// <item>Default: <see cref="SchemaObjects.Table"/> | <see cref="SchemaObjects.View"/> | <see cref="SchemaObjects.ForeignKey"/></item>
		/// <item>In T4 compability mode: <see cref="SchemaObjects.Table"/> | <see cref="SchemaObjects.View"/> | <see cref="SchemaObjects.ForeignKey"/> | <see cref="SchemaObjects.StoredProcedure"/> | <see cref="SchemaObjects.ScalarFunction"/> | <see cref="SchemaObjects.TableFunction"/> | <see cref="SchemaObjects.AggregateFunction"/></item>
		/// </list>
		/// </summary>
		public SchemaObjects LoadedObjects { get; set; } = SchemaObjects.Table | SchemaObjects.View | SchemaObjects.ForeignKey;
		/// <summary>
		/// When set to <c>true</c>, will prefer generation of provider-specific types instead of general types in mappings (for columns and procedure/function parameters).
		/// <list type="bullet">
		/// <item>Default: <c>false</c></item>
		/// <item>In T4 compability mode: <c>false</c></item>
		/// </list>
		/// </summary>
		public bool PreferProviderSpecificTypes { get; set; }
		#endregion

		#region Tables/Views
		/// <summary>
		/// Delegate to filter loaded tables and views by database name (only name and schema provided). Returns <c>true</c>, if table/view should be loaded.
		/// Second parameter (isView) in delegate provides <c>true</c> for view and <c>false</c> for table.
		/// <list type="bullet">
		/// <item>Default: all tables and views allowed.</item>
		/// <item>In T4 compability mode: all tables and views allowed.</item>
		/// </list>
		/// </summary>
		public Func<SqlObjectName, bool, bool> LoadTableOrView { get; set; } = (_, _) => true;

		/// <summary>
		/// This option applied only to SQL Server 2016+ and, when enabled, removes history tables information for temporal tables from schema load results.
		/// <list type="bullet">
		/// <item>Default: <c>false</c></item>
		/// <item>In T4 compability mode: <c>false</c></item>
		/// </list>
		/// </summary>
		public bool IgnoreSystemHistoryTables { get; set; }
		#endregion

		#region Foreign Keys
		/// <summary>
		/// Specify that schema load procedure should ignore duplicate foreign keys (keys with different names but same set of columns).
		/// If ignore mode enabled, only one instance of foreign key will be loaded (in order, returned from database).
		/// <list type="bullet">
		/// <item>Default: <c>true</c></item>
		/// <item>In T4 compability mode: <c>false</c></item>
		/// </list>
		/// </summary>
		public bool IgnoreDuplicateForeignKeys { get; set; } = true;
		#endregion

		#region Schemas/Catalogs
		/// <summary>
		/// Specify how to treat <see cref="Schemas"/> list, when it is not empty.
		/// When <c>true</c>, load only specified schemas, otherwise load all schemas except specififed in <see cref="Schemas"/>.
		/// <list type="bullet">
		/// <item>Default: <c>true</c></item>
		/// <item>In T4 compability mode: <c>true</c></item>
		/// </list>
		/// </summary>
		public bool IncludeSchemas { get; set; } = true;
		/// <summary>
		/// List of schemas(owners) to include or exclude (see <see cref="IncludeSchemas"/>) from schema load.
		/// <list type="bullet">
		/// <item>Default: empty</item>
		/// <item>In T4 compability mode: empty</item>
		/// </list>
		/// </summary>
		public ISet<string> Schemas { get; } = new HashSet<string>();
		/// <summary>
		/// List of default schemas. When <c>null</c>, use default schema information from database schema provider.
		/// <list type="bullet">
		/// <item>Default: <c>null</c></item>
		/// <item>In T4 compability mode: <c>null</c></item>
		/// </list>
		/// </summary>
		public ISet<string>? DefaultSchemas { get; set; }
		/// <summary>
		/// Specify how to treat <see cref="Catalogs"/> list, when it is not empty.
		/// When <c>true</c>, load only specified catalogs, otherwise all load catalogs except specififed in <see cref="Catalogs"/>.
		/// <list type="bullet">
		/// <item>Default: <c>true</c></item>
		/// <item>In T4 compability mode: <c>true</c></item>
		/// </list>
		/// </summary>
		public bool IncludeCatalogs { get; set; } = true;
		/// <summary>
		/// List of catalogs(databases) to include or exclude (see <see cref="IncludeCatalogs"/>) from schema load.
		/// <list type="bullet">
		/// <item>Default: empty</item>
		/// <item>In T4 compability mode: empty</item>
		/// </list>
		/// </summary>
		public ISet<string> Catalogs { get; } = new HashSet<string>();
		#endregion

		#region Procedures/Functions
		/// <summary>
		/// Specify stored procedure or table function schema load mode.
		/// When <c>false</c>, procedure or function will be executed with <see cref="CommandBehavior.SchemaOnly"/> option.
		/// Otherwise more safe approach will be used (currently supported only by SQL Server and uses sp_describe_first_result_set stored procedure).
		/// If safe-load mode not supported by database, schema load will be disabled.
		/// While <see cref="CommandBehavior.SchemaOnly"/> is safe in most of cases, it could create issues, when executed procedure contains
		/// non-transactional logic.
		/// <list type="bullet">
		/// <item>Default: <c>true</c></item>
		/// <item>In T4 compability mode: <c>false</c></item>
		/// </list>
		/// </summary>
		public bool UseSafeSchemaLoad { get; set; } = true;
		/// <summary>
		/// Include database name component into db object name in schema.
		/// <list type="bullet">
		/// <item>Default: <c>false</c></item>
		/// <item>In T4 compability mode: <c>false</c></item>
		/// </list>
		/// </summary>
		public bool LoadDatabaseName { get; set; }
		/// <summary>
		/// Specify stored procedures schema load mode: with result schema or without.
		/// <list type="bullet">
		/// <item>Default: <c>false</c></item>
		/// <item>In T4 compability mode: <c>true</c></item>
		/// </list>
		/// </summary>
		public bool LoadProceduresSchema { get; set; }
		/// <summary>
		/// Delegate to specify if schema should be loaded for procedure by procedure's database name (only name and schema provided). Returns <c>true</c>, if procedure schema should be loaded.
		/// <list type="bullet">
		/// <item>Default: all procedures allowed.</item>
		/// <item>In T4 compability mode: all procedures allowed</item>
		/// </list>
		/// </summary>
		public Func<SqlObjectName, bool> LoadProcedureSchema { get; set; } = _ => true;
		/// <summary>
		/// Delegate to filter loaded stored procedured by database name (only name and schema provided). Returns <c>true</c>, if stored procedure should be loaded.
		/// <list type="bullet">
		/// <item>Default: all stored procedures allowed.</item>
		/// <item>In T4 compability mode: all stored procedures allowed</item>
		/// </list>
		/// </summary>
		public Func<SqlObjectName, bool> LoadStoredProcedure { get; set; } = _ => true;
		/// <summary>
		/// Delegate to filter loaded table functions by database name (only name and schema provided). Returns <c>true</c>, if table function should be loaded.
		/// <list type="bullet">
		/// <item>Default: all table functions allowed.</item>
		/// <item>In T4 compability mode: all table functions allowed</item>
		/// </list>
		/// </summary>
		public Func<SqlObjectName, bool> LoadTableFunction { get; set; } = _ => true;
		/// <summary>
		/// Delegate to filter loaded scalar functions by database name (only name and schema provided). Returns <c>true</c>, if scalar function should be loaded.
		/// <list type="bullet">
		/// <item>Default: all scalar functions allowed.</item>
		/// <item>In T4 compability mode: all scalar functions allowed</item>
		/// </list>
		/// </summary>
		public Func<SqlObjectName, bool> LoadScalarFunction { get; set; } = _ => true;
		/// <summary>
		/// Delegate to filter loaded aggregate functions by database name (only name and schema provided). Returns <c>true</c>, if aggregate function should be loaded.
		/// <list type="bullet">
		/// <item>Default: all aggregate functions allowed.</item>
		/// <item>In T4 compability mode: all aggregate functions allowed</item>
		/// </list>
		/// </summary>
		public Func<SqlObjectName, bool> LoadAggregateFunction { get; set; } = _ => true;

		/// <summary>
		/// Generate RETURN_VALUE stored procedure parameter for SQL Server.
		/// <list type="bullet">
		/// <item>Default: <c>false</c></item>
		/// <item>In T4 compability mode: <c>false</c></item>
		/// </list>
		/// </summary>
		public bool EnableSqlServerReturnValue { get; set; }
		#endregion
	}
}
