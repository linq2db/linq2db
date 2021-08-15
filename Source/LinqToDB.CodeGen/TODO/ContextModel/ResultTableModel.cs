using System.Collections.Generic;
using LinqToDB.CodeGen.Model;

namespace LinqToDB.CodeGen.ContextModel
{
	public class ResultTableModel
	{
		public ResultTableModel(ClassModel @class)
		{
			Class = @class;
		}
		public ClassModel Class { get; set; }

		public List<ColumnModel> Columns { get; } = new ();
	}
}
