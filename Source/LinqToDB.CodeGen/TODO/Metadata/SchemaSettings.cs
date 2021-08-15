using System;
using System.Collections.Generic;
using LinqToDB.CodeGen.Schema;

namespace LinqToDB.CodeGen.Metadata
{
	public class SchemaSettings
	{
		/// <summary>
		/// Gets or sets which database object types to load.
		/// </summary>
		public SchemaObjects Objects { get; set; } = SchemaObjects.Table | SchemaObjects.View | SchemaObjects.ForeignKey//;
			| SchemaObjects.AggregateFunction | SchemaObjects.ScalarFunction | SchemaObjects.TableFunction | SchemaObjects.StoredProcedure;

		/// <summary>
		/// Specify how to treat <see cref="Schemas"/> list - as list of allowed or as list of disallowed schemas.
		/// </summary>
		public bool IncludeSchemas { get; set; } = true;

		/// <summary>
		/// List of schemas/owners to include or exclude (see <see cref="IncludeSchemas"/>) from schema load.
		/// </summary>
		public ISet<string> Schemas { get; } = new HashSet<string>();

		/// <summary>
		/// Specify how to treat <see cref="Catalogs"/> list - as list of allowed or as list of disallowed catalogs.
		/// </summary>
		public bool IncludeCatalogs { get; set; } = true;

		/// <summary>
		/// List of databases/catalogs to include or exclude (see <see cref="IncludeCatalogs"/>) from schema load.
		/// </summary>
		public ISet<string> Catalogs { get; } = new HashSet<string>();

		// TODO: more parameters? e.g. parameters, description, type
		/// <summary>
		/// Specify procedure filter to load procedure schema.
		/// Disabled by default as it requires procedure execution in schema-only mode and not all databases provide
		/// safe schema-only execution or allow procedures to contain code, not safe for schema-only execution.
		/// Also see <seealso cref="UseSafeSchemaLoad"/>.
		/// </summary>
		//public Func<ObjectName, bool> LoadProcedureSchema { get; set; } = _ => false;
		//public Func<ObjectName, bool> LoadTableFunctionSchema { get; set; } = _ => false;
		public Func<ObjectName, bool> LoadProcedureSchema { get; set; } = _ => true;
		public Func<ObjectName, bool> LoadTableFunctionSchema { get; set; } = _ => true;

		public Func<ObjectName, bool> LoadTable { get; set; } = _ => true;
		public Func<ObjectName, bool> LoadView { get; set; } = _ => true;

		/// <summary>
		/// This option supported only for SQL Server and uses sp_describe_first_result_set stored procedure to load
		/// procedure schema.
		/// </summary>
		public bool UseSafeSchemaLoad { get; set; } = true;

		//public bool LoadProceduresSchema { get; set; }
		//public bool LoadTableFunctionsSchema { get; set; }
		public bool LoadProceduresSchema { get; set; } = true;
		public bool LoadTableFunctionsSchema { get; set; } = true;

		// TODO: move options below to metadata generation
		///// <summary>
		///// List of schema names, that should not be omitted from data model metadata.
		///// </summary>
		//public ISet<string> OmitSchemas { get; } = new HashSet<string>();

		// TODO: implement using cusom type mappings in code model builder
		/// <summary>
		/// When set to true, will prefer generation of provider-specific types instead of general types.
		/// </summary>
		public bool PreferProviderSpecificTypes { get; set; }// = true;
		///// <summary>
		///// Should linq2db use <see cref="string"/> for char(1) type or <see cref="char"/>. Default type: <see cref="char"/> (<c>false</c>).
		///// </summary>
		//public bool     GenerateChar1AsString;

		// TODO: obsoleted
		///// <summary>
		///// String comparison logic for <see cref="IncludedSchemas"/>, <see cref="ExcludedSchemas"/>, <see cref="IncludedCatalogs"/> and <see cref="ExcludedCatalogs"/> values.
		///// Default is <see cref="StringComparer.OrdinalIgnoreCase"/>.
		///// </summary>
		//public StringComparer                 StringComparer           = StringComparer.OrdinalIgnoreCase;

		// TODO: obsoleted
		///// <summary>
		///// Optional callback to report procedure metadata load progress. First parameter contains total number of
		///// discovered procedures. Second parameter provides position of currently loaded procedure.
		///// </summary>
		//public Action<int,int>                ProcedureLoadingProgress = (outOf,current) => {};

		// TODO: move to model generation
		///// <summary>
		///// Optional custom name generation logic for association property.
		///// </summary>
		//public Func<ForeignKeySchema,string>? GetAssociationMemberName;


		// TODO: move to model builder step
		///// <summary>
		///// Optinal callback to filter loaded tables. receives object with table details and return boolean flag
		///// to indicate that table should be loaded (<c>true</c>) or skipped (<c>false</c>).
		///// </summary>
		//public Func<LoadTableData, bool>?     LoadTable;

		public bool IncludeDatabaseName { get; set; }
		public string? DatabaseName { get; set; }
		public string? ServerName { get; set; }

		public HashSet<ObjectName> AddReturnParameterToProcedures { get; } = new();

	}
}
