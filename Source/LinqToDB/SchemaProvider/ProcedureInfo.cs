using System;

namespace LinqToDB.SchemaProvider
{
	public class ProcedureInfo
	{
		public string ProcedureID;
		public string CatalogName;
		public string SchemaName;
		public string ProcedureName;
		public bool   IsFunction;
		public bool   IsTableFunction;
		public bool   IsAggregateFunction;
		public bool   IsDefaultSchema;
		public string ProcedureDefinition;
	}
}
