namespace LinqToDB.Internal.SchemaProvider
{
	/// <summary>
	/// Database procedure or function parameter description.
	/// </summary>
	public class ProcedureParameterInfo
	{
		/// <summary>
		/// Gets or sets unique procedure identifier.
		/// NOTE: this is not fully-qualified procedure name (even if it used right now for some providers as procedure identifier).
		/// </summary>
		public string ProcedureID = null!;
		/// <summary>
		/// Gets or sets parameter position.
		/// </summary>
		public int    Ordinal;
		/// <summary>
		/// Gets or sets parameter name.
		/// </summary>
		public string? ParameterName;
		/// <summary>
		/// Get or sets database type for parameter.
		/// </summary>
		public string? DataType;
		/// <summary>
		/// Get or sets exact database type for parameter.
		/// </summary>
		public string? DataTypeExact;
		/// <summary>
		/// Gets or sets parameter type length attribute.
		/// </summary>
		public int?  Length;
		/// <summary>
		/// Gets or sets parameter type precision attribute.
		/// </summary>
		public int?   Precision;
		/// <summary>
		/// Gets or sets parameter type scale attribute.
		/// </summary>
		public int?   Scale;
		/// <summary>
		/// Gets or sets input or input-output parameter flag.
		/// </summary>
		public bool   IsIn;
		/// <summary>
		/// Gets or sets output or input-output parameter flag.
		/// </summary>
		public bool   IsOut;
		/// <summary>
		/// Gets or sets return value parameter flag.
		/// </summary>
		public bool   IsResult;
		/// <summary>
		/// Parameter's user-defined type(UDT) catalog/database.
		/// </summary>
		public string? UDTCatalog;
		/// <summary>
		/// Parameter's user-defined type(UDT) schema/owner.
		/// </summary>
		public string? UDTSchema;
		/// <summary>
		/// Parameter's user-defined type(UDT) name.
		/// </summary>
		public string? UDTName;
		/// <summary>
		/// Gets flag indicating that it is nullable parameter.
		/// </summary>
		public bool IsNullable;
		/// <summary>
		/// Parameter's description.
		/// </summary>
		public string? Description;
	}
}
