using System.Collections.Generic;

namespace LinqToDB.CodeGen.DataModel
{
	public abstract class SchemaModelBase
	{
		public List<EntityModel> Entities { get; } = new();

		public List<StoredProcedureModel> StoredProcedures { get; } = new();
		public List<ScalarFunctionModel> ScalarFunctions { get; } = new();
		public List<TableFunctionModel> TableFunctions { get; } = new();
		public List<AggregateFunctionModel> AggregateFunctions { get; } = new();
	}
}
