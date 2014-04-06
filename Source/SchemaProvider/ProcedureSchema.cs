using System;
using System.Collections.Generic;

namespace LinqToDB.SchemaProvider
{
	public class ProcedureSchema
	{
		public string CatalogName         { get; set; }
		public string SchemaName          { get; set; }
		public string ProcedureName       { get; set; }
		public string MemberName          { get; set; }
		public bool   IsFunction          { get; set; }
		public bool   IsTableFunction     { get; set; }
          public bool   IsAggregateFunction { get; set; }
          public bool   IsDefaultSchema     { get; set; }

		public TableSchema           ResultTable     { get; set; }
		public Exception             ResultException { get; set; }
		public List<TableSchema>     SimilarTables   { get; set; }
		public List<ParameterSchema> Parameters      { get; set; }
	}
}
