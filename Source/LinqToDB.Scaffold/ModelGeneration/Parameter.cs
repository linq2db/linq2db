using System;

namespace LinqToDB.Tools.ModelGeneration
{
	/// <summary>
	/// For internal use.
	/// </summary>
	public class Parameter
	{
		public string? SchemaName    { get; set; }
		public string? SchemaType    { get; set; }
		public bool    IsIn          { get; set; }
		public bool    IsOut         { get; set; }
		public bool    IsResult      { get; set; }
		public int?    Size          { get; set; }
		public string? ParameterName { get; set; }
		public string? ParameterType { get; set; }
		public bool    IsNullable    { get; set; }
		public Type?   SystemType    { get; set; }
		public string? DataType      { get; set; }
		public string? Description   { get; set; }

		public ModelType Type => new (ParameterType!, !ModelGenerator.IsValueType(ParameterType!), IsNullable);
	}
}
