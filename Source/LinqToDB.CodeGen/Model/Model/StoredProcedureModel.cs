using System.Collections.Generic;
using LinqToDB.CodeGen.ContextModel;
using LinqToDB.CodeGen.Schema;

namespace LinqToDB.CodeGen.Model
{
	public class StoredProcedureModel : TableFunctionModelBase
	{
		public StoredProcedureModel(ObjectName name, MethodModel method)
			: base(name, method)
		{
		}

		public List<(ResultTableModel? customTable, EntityModel? entity)> Results { get; set; } = new();

		public ReturnParameter? Return { get; set; }
	}
}
