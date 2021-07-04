using System.Collections.Generic;

namespace LinqToDB.CodeGen.ContextModel
{
	public class ResultTableModel
	{
		public string ClassName { get; set; } = null!;

		public string? ResultSetPropertyName { get; set; }

		public List<ResultTableColumnModel> Columns { get; } = new ();
	}
}
