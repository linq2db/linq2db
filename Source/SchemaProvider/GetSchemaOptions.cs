using System;

namespace LinqToDB.SchemaProvider
{
	public class GetSchemaOptions
	{
		public bool     GetTables             = true;
		public bool     GetProcedures         = true;
		public bool     GenerateChar1AsString = false;
		public string[] IncludedSchemas;
		public string[] ExcludedSchemas;

		public Func<ProcedureSchema,bool>    LoadProcedure            = _ => true;
		public Func<ForeignKeySchema,string> GetAssociationMemberName = null;
		public Action<int,int>               ProcedureLoadingProgress = (outOf,current) => {};
	}
}
