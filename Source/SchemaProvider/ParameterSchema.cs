using System;

namespace LinqToDB.SchemaProvider
{
	public class ParameterSchema
	{
		public string   SchemaName    { get; set; }
		public string   SchemaType    { get; set; }
		public bool     IsIn          { get; set; }
		public bool     IsOut         { get; set; }
		public bool     IsResult      { get; set; }
		public long?    Size          { get; set; }

		public string   ParameterName { get; set; }
		public string   ParameterType { get; set; }
		public Type     SystemType    { get; set; }
		public DataType DataType      { get; set; }
	}
}
