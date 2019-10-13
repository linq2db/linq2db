using System;
using System.Diagnostics;

namespace LinqToDB.SchemaProvider
{
	[DebuggerDisplay("TableID = {TableID}, Name = {Name}, DataType = {DataType}, Length = {Length}, Precision = {Precision}, Scale = {Scale}")]
	public class ColumnInfo
	{
		private string _dataType;
		private string _columnType;

		public string TableID;
		public string Name;
		public bool   IsNullable;
		public int    Ordinal;
		public string DataType      { get => _dataType;   set => _dataType   = value.Trim(); }
		public string ColumnType    { get => _columnType; set => _columnType = value.Trim(); }
		public long?  Length;
		public int?   Precision;
		public int?   Scale;
		public string Description;
		public bool   IsIdentity;
		public bool   SkipOnInsert;
		public bool   SkipOnUpdate;
	}
}
