using System;
using System.Collections.Generic;

namespace LinqToDB.SchemaProvider
{
	public class TableSchema
	{
		public string ID                { get; set; }
		public string CatalogName       { get; set; }
		public string SchemaName        { get; set; }
		public string TableName         { get; set; }
		public string Description       { get; set; }
		public bool   IsDefaultSchema   { get; set; }
		public bool   IsView            { get; set; }
		public bool   IsProcedureResult { get; set; }
		public string TypeName          { get; set; }

		public List<ColumnSchema>     Columns     { get; set; }
		public List<ForeignKeySchema> ForeignKeys { get; set; }
	}
}
