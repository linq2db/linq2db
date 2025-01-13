using System;

namespace LinqToDB.SchemaProvider
{
	/// <summary>
	/// Defines schema load options.
	/// </summary>
	public class GetSchemaOptions
	{
		/// <summary>
		/// When set to true, will prefer generation of provider-specific types instead of general types.
		/// </summary>
		public bool     PreferProviderSpecificTypes;

		/// <summary>
		/// Enable or disable read of table schema. Default - enabled (<c>true</c>).
		/// </summary>
		public bool     GetTables      = true;
		/// <summary>
		/// Enable or disable read of foreign keys. Default - enabled (<c>true</c>).
		/// Disabe could be useful at least for Access, as it could <a href="https://github.com/linq2db/linq2db.LINQPad/issues/23">crash</a> on some database files.
		/// </summary>
		public bool     GetForeignKeys = true;
		/// <summary>
		/// Enable or disable read of procedures and functions metadata. Default - enabled (<c>true</c>).
		/// </summary>
		public bool     GetProcedures  = true;
		/// <summary>
		/// Should linq2db use <see cref="string"/> for char(1) type or <see cref="char"/>. Default type: <see cref="char"/> (<c>false</c>).
		/// </summary>
		public bool     GenerateChar1AsString;
		/// <summary>
		/// Only for SQL Server. Doesn't return history table schema for temporal tables.
		/// </summary>
		public bool     IgnoreSystemHistoryTables;

		/// <summary>
		/// Default Schema name.
		/// </summary>
		public string?  DefaultSchema;

		/// <summary>
		/// List of allowed schemas/owners.
		/// </summary>
		public string?[]? IncludedSchemas;
		/// <summary>
		/// List of disallowed schemas/owners.
		/// </summary>
		public string?[]? ExcludedSchemas;
		/// <summary>
		/// List of allowed databases/catalogs.
		/// </summary>
		public string?[]? IncludedCatalogs;
		/// <summary>
		/// List of disallowed databases/catalogs.
		/// </summary>
		public string?[]? ExcludedCatalogs;

		/// <summary>
		/// String comparison logic for <see cref="IncludedSchemas"/>, <see cref="ExcludedSchemas"/>, <see cref="IncludedCatalogs"/> and <see cref="ExcludedCatalogs"/> values.
		/// Default is <see cref="StringComparer.OrdinalIgnoreCase"/>.
		/// </summary>
		public StringComparer                 StringComparer           = StringComparer.OrdinalIgnoreCase;
		/// <summary>
		/// Optional procedure metadata load filter. By default all procedures loaded.
		/// </summary>
		public Func<ProcedureSchema,bool>     LoadProcedure            = _ => true;
		/// <summary>
		/// Optional custom name generation logic for association property.
		/// </summary>
		public Func<ForeignKeySchema,string>? GetAssociationMemberName;
		/// <summary>
		/// Optional callback to report procedure metadata load progress. First parameter contains total number of
		/// discovered procedures. Second parameter provides position of currently loaded procedure.
		/// </summary>
		public Action<int,int>                ProcedureLoadingProgress = (outOf,current) => {};

		/// <summary>
		/// Optinal callback to filter loaded tables. receives object with table details and return boolean flag
		/// to indicate that table should be loaded (<c>true</c>) or skipped (<c>false</c>).
		/// </summary>
		public Func<LoadTableData, bool>?     LoadTable;

		/// <summary>
		/// if set to true, SchemaProvider uses <see cref="System.Data.CommandBehavior.SchemaOnly"/> to get SqlServer metadata.
		/// Otherwise the sp_describe_first_result_set sproc is used.
		/// </summary>
		public bool                           UseSchemaOnly = Common.Configuration.SqlServer.UseSchemaOnlyToGetSchema;
	}
}
