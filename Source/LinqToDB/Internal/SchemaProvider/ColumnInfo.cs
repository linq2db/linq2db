using System.Diagnostics;

namespace LinqToDB.Internal.SchemaProvider
{
	[DebuggerDisplay("TableID = {TableID}, Name = {Name}, DataType = {DataType}, Length = {Length}, Precision = {Precision}, Scale = {Scale}")]
	public class ColumnInfo
	{
		public string    TableID = null!;
		public string    Name = null!;
		public bool      IsNullable;
		public int       Ordinal;
		public string?   DataType;
		public string?   ColumnType;
		public int?      Length;
		public int?      Precision;
		public int?      Scale;
		public string?   Description;
		public bool      IsIdentity;
		public bool      SkipOnInsert;
		public bool      SkipOnUpdate;
		public DataType? Type;
	}
}
