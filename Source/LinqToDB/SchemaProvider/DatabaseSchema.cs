using System.Collections.Generic;
using System.Data;

using JetBrains.Annotations;

namespace LinqToDB.SchemaProvider
{
	[PublicAPI]
	public class DatabaseSchema
	{
		public string                DataSource                    { get; set; } = null!;
		public string                Database                      { get; set; } = null!;
		public string                ServerVersion                 { get; set; } = null!;
		public List<TableSchema>     Tables                        { get; set; } = null!;
		public List<ProcedureSchema> Procedures                    { get; set; } = null!;
		public DataTable?            DataTypesSchema               { get; set; }
		public string?               ProviderSpecificTypeNamespace { get; set; }
	}
}
