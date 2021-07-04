using System.Collections.Generic;

namespace LinqToDB.CodeGen.ContextModel
{
	public class StoredProcedureModel : TableFunctionModelBase
	{
		public string? ResultSetClassName { get; set; }
		public List<(ResultTableModel? customTable, EntityModel? entity)> Results { get; set; } = new();
		public string FullName { get; set; } = null!;

		public ReturnParameter? Return { get; set; }
	}
}
