using System.Diagnostics;

namespace LinqToDB.SchemaProvider
{
	[DebuggerDisplay("CatalogName = {CatalogName}, SchemaName = {SchemaName}, TableName = {TableName}, IsDefaultSchema = {IsDefaultSchema}, IsView = {IsView}, Description = {Description}")]
	public class TableInfo
	{
		public string  TableID = null!;
		public string? CatalogName;
		public string  SchemaName = null!;
		public string  TableName = null!;
		public string? Description;
		public bool    IsDefaultSchema;
		public bool    IsView;
		public bool    IsProviderSpecific;
	}
}
