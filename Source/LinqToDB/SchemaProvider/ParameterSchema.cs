using System;

namespace LinqToDB.SchemaProvider
{
	/// <summary>
	/// Describes database procedure or function parameter.
	/// </summary>
	public class ParameterSchema
	{
		/// <summary>
		/// Gets parameter's name.
		/// </summary>
		public string   SchemaName           { get; set; }

		/// <summary>
		/// Gets database-specific parameter type.
		/// </summary>
		public string   SchemaType           { get; set; }

		/// <summary>
		/// Gets flag indicating that it is input parameter.
		/// </summary>
		public bool     IsIn                 { get; set; }

		/// <summary>
		/// Gets flag indicating that it is output parameter.
		/// </summary>
		public bool     IsOut                { get; set; }

		/// <summary>
		/// Gets flag indicating that it is return value parameter.
		/// </summary>
		public bool     IsResult             { get; set; }

		/// <summary>
		/// Gets parameter type size.
		/// </summary>
		public long?    Size                 { get; set; }


		/// <summary>
		/// Gets C#-friendly parameter name.
		/// </summary>
		public string   ParameterName        { get; set; }
		/// <summary>
		/// Gets .net type for parameter as string.
		/// </summary>
		public string   ParameterType        { get; set; }

		/// <summary>
		/// Gets .net type for parameter.
		/// </summary>
		public Type     SystemType           { get; set; }
		/// <summary>
		/// Gets parameter type as <see cref="DataType"/> enumeration value.
		/// </summary>
		public DataType DataType             { get; set; }

		/// <summary>
		/// Gets provider-specific .net parameter type as a string.
		/// </summary>
		public string   ProviderSpecificType { get; set; }
	}
}
