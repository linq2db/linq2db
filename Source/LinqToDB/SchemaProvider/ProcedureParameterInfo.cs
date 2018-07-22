using System;

namespace LinqToDB.SchemaProvider
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
		public string ProcedureID;
		/// <summary>
		/// Gets or sets parameter position.
		/// </summary>
		public int    Ordinal;
		/// <summary>
		/// Gets or sets parameter name.
		/// </summary>
		public string ParameterName;
		/// <summary>
		/// Get or sets database type for parameter.
		/// </summary>
		public string DataType;
		/// <summary>
		/// Gets or sets parameter type length attribute.
		/// </summary>
		public long?  Length;
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
	}
}
