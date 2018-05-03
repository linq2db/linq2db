using System;
using System.Collections.Generic;
using System.Data;

using JetBrains.Annotations;

namespace LinqToDB.SchemaProvider
{
	[PublicAPI]
	public class DatabaseSchema
	{
		public string                DataSource      { get; set; }
		public string                Database        { get; set; }
		public string                ServerVersion   { get; set; }
		public List<TableSchema>     Tables          { get; set; }
		public List<ProcedureSchema> Procedures      { get; set; }
		public DataTable             DataTypesSchema { get; set; }
		public string  ProviderSpecificTypeNamespace { get; set; }
	}
}
