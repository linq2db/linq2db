using System.Collections.Generic;

namespace LinqToDB.CodeGen.DataModel
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
