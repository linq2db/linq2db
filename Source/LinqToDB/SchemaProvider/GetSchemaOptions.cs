using System;

namespace LinqToDB.SchemaProvider
{
	/// <summary>
	/// Defines schema load options.
	/// </summary>
	public class GetSchemaOptions
	{
		/// <summary>
		/// Enable or disable read of table schema. Default - enabled (<c>true</c>).
		/// </summary>
		public bool     GetTables             = true;
		/// <summary>
		/// Enable or disable read of procedures and functions metadata. Default - enabled (<c>true</c>).
		/// </summary>
		public bool     GetProcedures         = true;
		/// <summary>
		/// Should linq2db use <see cref="string"/> for char(1) type or <see cref="char"/>. Default type: <see cref="char"/> (<c>false</c>).
		/// </summary>
		public bool     GenerateChar1AsString = false;
		/// <summary>
		/// List of allowed schemas/owners.
		/// </summary>
		public string[] IncludedSchemas;
		/// <summary>
		/// List of disallowed schemas/owners.
		/// </summary>
		public string[] ExcludedSchemas;
		/// <summary>
		/// List of allowed databases/catalogs.
		/// </summary>
		public string[] IncludedCatalogs;
		/// <summary>
		/// List of disallowed databases/catalogs.
		/// </summary>
		public string[] ExcludedCatalogs;

		/// <summary>
		/// String comparison logic for <see cref="IncludedSchemas"/>, <see cref="ExcludedSchemas"/>, <see cref="IncludedCatalogs"/> and <see cref="ExcludedCatalogs"/> values.
		/// Default is <see cref="StringComparer.OrdinalIgnoreCase"/>.
		/// </summary>
		public StringComparer                StringComparer           = StringComparer.OrdinalIgnoreCase;
		/// <summary>
		/// Optional procedure metadata load filter. By default all procedures loaded.
		/// </summary>
		public Func<ProcedureSchema,bool>    LoadProcedure            = _ => true;
		/// <summary>
		/// Optional custom name generation logic for association property.
		/// </summary>
		public Func<ForeignKeySchema,string> GetAssociationMemberName = null;
		/// <summary>
		/// Optional callback to report procedure metadata load progress. First parameter contains total number of
		/// discovered procedires. Second parameter provides position of currently loaded procedure.
		/// </summary>
		public Action<int,int>               ProcedureLoadingProgress = (outOf,current) => {};
	}
}
