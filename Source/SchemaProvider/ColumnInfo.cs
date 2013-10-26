using System;
using System.Diagnostics;

namespace LinqToDB.SchemaProvider
{
	[DebuggerDisplay("TableID = {TableID}, Name = {Name}, Ordinal = {Ordinal}")]
	public class ColumnInfo
	{
		public string TableID;
		public string Name;
		public bool   IsNullable;
		public int    Ordinal;
		public string DataType;
		public string ColumnType;
		public int    Length;
		public int    Precision;
		public int    Scale;
		public string Description;
		public bool   IsIdentity;
		public bool   SkipOnInsert;
		public bool   SkipOnUpdate;
	}
}
