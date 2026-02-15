using System;
using System.Collections.Generic;

namespace LinqToDB.SchemaProvider
{
	/// <summary>
	/// Describes database procedure or function.
	/// </summary>
	public class ProcedureSchema
	{
		/// <summary>
		/// Name of database, that contains current procedure.
		/// </summary>
		public string? CatalogName         { get; set; }

		/// <summary>
		/// Name of procedure schema/owner.
		/// </summary>
		public string? SchemaName          { get; set; }
		/// <summary>
		/// Name of procedure package/library/module.
		/// </summary>
		public string? PackageName         { get; set; }

		/// <summary>
		/// Procedure or function name.
		/// </summary>
		public string ProcedureName       { get; set; } = null!;

		/// <summary>
		/// C#-friendly name.
		/// </summary>
		public string MemberName          { get; set; } = null!;

		/// <summary>
		/// <see langword="true"/> for function and <see langword="false"/> for procedure.
		/// </summary>
		public bool   IsFunction          { get; set; }

		/// <summary>
		/// Gets flag indicating that it is scalar or table function.
		/// </summary>
		public bool   IsTableFunction     { get; set; }

		/// <summary>
		/// Get or sets flag, indicating that procedure returns dynamic (generic) result.
		/// </summary>
		public bool IsResultDynamic       { get; set; }

		/// <summary>
		/// Gets flag indicating that it is aggregate function or not.
		/// </summary>
		public bool   IsAggregateFunction { get; set; }

		/// <summary>
		/// Gets flag indicating that procedure defined with default owner/schema or not.
		/// </summary>
		public bool   IsDefaultSchema     { get; set; }

		/// <summary>
		/// Gets procedure or function description.
		/// </summary>
		public string? Description { get; set; }

		/// <summary>
		/// Gets flag indicating that procedure tabl result schema loaded. If it is <see langword="false"/>, procedure doesn't return
		/// table-like results or schema loading failed. In latter case check <see cref="ResultException"/> property for
		/// error.
		/// </summary>
		public bool   IsLoaded            { get; set; }

		/// <summary>
		/// Gets table result schema for procedure to table function.
		/// </summary>
		public TableSchema?          ResultTable     { get; set; }

		/// <summary>
		/// Contains exception, generated during schema load.
		/// </summary>
		public Exception?            ResultException { get; set; }

		/// <summary>
		/// List of tables with the same schema as schema in <see cref="ResultTable"/>.
		/// </summary>
		public List<TableSchema>?    SimilarTables   { get; set; }

		/// <summary>
		/// Gets list of procedure parameters.
		/// </summary>
		public List<ParameterSchema> Parameters      { get; set; } = null!;
	}
}
