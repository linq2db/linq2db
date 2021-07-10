using System.Collections.Generic;

namespace LinqToDB.CodeGen.ContextModel
{
	public class SchemaContextModel
	{
		public SchemaContextModel(string baseTypeName)
		{
			BaseTypeName = baseTypeName;
		}

		public string BaseTypeName { get; }
		public List<EntityModel> Entities { get; } = new List<EntityModel>();

		public List<StoredProcedureModel> StoredProcedures { get; } = new List<StoredProcedureModel>();
		public List<ScalarFunctionModel> ScalarFunctions { get; } = new List<ScalarFunctionModel>();
		public List<TableFunctionModel> TableFunctions { get; } = new List<TableFunctionModel>();
		public List<AggregateModel> Aggregates { get; } = new List<AggregateModel>();
	}
}
