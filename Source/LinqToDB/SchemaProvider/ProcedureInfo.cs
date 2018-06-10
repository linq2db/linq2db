using System;

namespace LinqToDB.SchemaProvider
{
	/// <summary>
	/// Database procedure or function description.
	/// </summary>
	public class ProcedureInfo
	{
		/// <summary>
		/// Gets or sets unique procedure identifier.
		/// NOTE: this is not fully-qualified procedure name (even if it used right now for some providers as procedure identifier).
		/// </summary>
		public string ProcedureID;
		/// <summary>
		/// Gets or sets database name for procedure.
		/// </summary>
		public string CatalogName;
		/// <summary>
		/// Gets or sets schema/owner name for procedure.
		/// </summary>
		public string SchemaName;
		/// <summary>
		/// Gets or sets procedure name.
		/// </summary>
		public string ProcedureName;
		/// <summary>
		/// Gets or sets flag to distinguish function from procedure.
		/// </summary>
		public bool   IsFunction;
		/// <summary>
		/// Gets or sets flag to distinguish table function from aggregate and scalar functions.
		/// </summary>
		public bool   IsTableFunction;
		/// <summary>
		/// Gets or sets flag to distinguish aggregate function from table and scalar functions.
		/// </summary>
		public bool   IsAggregateFunction;
		/// <summary>
		/// Get or sets glag, indicating that procedure belongs to default schema/owner.
		/// </summary>
		public bool   IsDefaultSchema;
		/// <summary>
		/// Gets or sets procedure source code.
		/// </summary>
		public string ProcedureDefinition;
	}
}
