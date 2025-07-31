using System.Diagnostics;

namespace LinqToDB.Internal.SchemaProvider
{
	[DebuggerDisplay("TableID = {TableID}, PrimaryKeyName = {PrimaryKeyName}, ColumnName = {ColumnName}, Ordinal = {Ordinal}")]
	public sealed class PrimaryKeyInfo
	{
		public string  TableID    = null!;
		public string? PrimaryKeyName;
		public string  ColumnName = null!;
		public int     Ordinal;
	}
}
