using System.Collections.Generic;

namespace LinqToDB.CodeGen.Model
{
	public class TupleModel
	{
		public TupleModel(ClassModel @class)
		{
			Class = @class;
		}

		public ClassModel Class { get; set; }

		public bool CanBeNull { get; set; }

		public List<TupleFieldModel> Fields { get; } = new ();
	}

}
