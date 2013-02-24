using System;
using System.Collections.Generic;

namespace LinqToDB.DataProvider.SchemaProvider
{
	public class TableSchema
	{
		public string             CatalogName { get; set; }
		public string             SchemaName  { get; set; }
		public string             TableName   { get; set; }
		public string             Description { get; set; }
		public List<ColumnSchema> Columns     { get; set; }
	}
}
