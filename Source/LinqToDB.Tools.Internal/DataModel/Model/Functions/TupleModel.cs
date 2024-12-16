using System.Collections.Generic;

namespace LinqToDB.DataModel
{
	/// <summary>
	/// Tuple class descriptor for specific scalar function return value, when function returns tuple.
	/// </summary>
	public sealed class TupleModel
	{
		public TupleModel(ClassModel @class)
		{
			Class = @class;
		}

		/// <summary>
		/// Gets or sets class descriptor.
		/// </summary>
		public ClassModel            Class     { get; set; }
		/// <summary>
		/// Gets or sets flag, indicating that function could return <c>null</c> instead as tuple value.
		/// </summary>
		public bool                  CanBeNull { get; set; }
		/// <summary>
		/// Ordered list of tuple field models. Order must correspond to order of field in tuple definition in database.
		/// </summary>
		public List<TupleFieldModel> Fields    { get;      } = new ();
	}

}
