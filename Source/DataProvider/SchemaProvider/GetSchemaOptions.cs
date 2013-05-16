using System;

namespace LinqToDB.DataProvider.SchemaProvider
{
	public class GetSchemaOptions
	{
		public bool     GetTables     = true;
		public bool     GetProcedures = true;
		public string[] IncludedSchemas;
		public string[] ExcludedSchemas;

		public Func<ProcedureSchema,bool> LoadProcedure            = _ => true;
		public Action<int,int>            ProcedureLoadingProgress = (outOf,current) => {};
	}
}
