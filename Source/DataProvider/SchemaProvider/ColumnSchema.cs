using System;

namespace LinqToDB.DataProvider.SchemaProvider
{
	public class ColumnSchema
	{
		public string   ColumnName { get; set; }
		public Type     SystemType { get; set; }
		public bool     IsNullable { get; set; }
		public DataType DataType   { get; set; }
		public string   DbType     { get; set; }
	}
}
