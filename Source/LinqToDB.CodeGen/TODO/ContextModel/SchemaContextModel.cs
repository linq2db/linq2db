using System.Collections.Generic;
using LinqToDB.CodeGen.Model;

namespace LinqToDB.CodeGen.ContextModel
{
	public class SchemaContextModel
	{
		public SchemaContextModel(string baseTypeName)
		{
			BaseTypeName = baseTypeName;
		}

		public string BaseTypeName { get; }
		public List<EntityModel> Entities { get; } = new ();

		public List<StoredProcedureModel> StoredProcedures { get; } = new ();
		public List<ScalarFunctionModel> ScalarFunctions { get; } = new ();
		public List<TableFunctionModel> TableFunctions { get; } = new ();
		public List<AggregateFunctionModel> Aggregates { get; } = new ();
	}
}
