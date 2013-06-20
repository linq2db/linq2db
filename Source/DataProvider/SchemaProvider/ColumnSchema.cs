using System;

namespace LinqToDB.DataProvider.SchemaProvider
{
	public class ColumnSchema
	{
		public string      ColumnName      { get; set; }
		public string      ColumnType      { get; set; }
		public bool        IsNullable      { get; set; }
		public bool        IsIdentity      { get; set; }
		public bool        IsPrimaryKey    { get; set; }
		public int         PrimaryKeyOrder { get; set; }
		public string      Description     { get; set; }

		public string      MemberName      { get; set; }
		public string      MemberType      { get; set; }
		public Type        SystemType      { get; set; }
		public DataType    DataType        { get; set; }
		public bool        SkipOnInsert    { get; set; }
		public bool        SkipOnUpdate    { get; set; }

		public TableSchema Table;
	}
}
