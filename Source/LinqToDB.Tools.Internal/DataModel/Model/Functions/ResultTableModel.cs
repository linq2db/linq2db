using System.Collections.Generic;

namespace LinqToDB.DataModel
{
	/// <summary>
	/// Custom mapping class descriptor for procedure or table function return record.
	/// </summary>
	public sealed class ResultTableModel
	{
		public ResultTableModel(ClassModel @class)
		{
			Class = @class;
		}

		/// <summary>
		/// Gets or sets class descriptor.
		/// </summary>
		public ClassModel        Class   { get; set; }
		/// <summary>
		/// Record column descriptors. Must be ordered by ordinal (position in data set, returned by database) as we
		/// can use by-ordinal columns mapping in cases where is it not possible to use by-name mapping (e.g. when
		/// columns doesn't have names or have multiple columns with same name).
		/// </summary>
		public List<ColumnModel> Columns { get;      } = new ();
	}
}
