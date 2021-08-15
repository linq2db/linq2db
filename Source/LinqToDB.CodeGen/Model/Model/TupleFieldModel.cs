using LinqToDB.CodeGen.Schema;

namespace LinqToDB.CodeGen.Model
{
	public class TupleFieldModel
	{
		public TupleFieldModel(PropertyModel property, DatabaseType type)
		{
			Property = property;
			Type = type;
		}

		public PropertyModel Property { get; set; }

		public DatabaseType Type { get; set; }
		public DataType? DataType { get; set; }
	}

}
