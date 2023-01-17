using LinqToDB.Schema;

namespace LinqToDB.DataModel
{
	/// <summary>
	/// Scalar function return tuple field descriptor.
	/// </summary>
	public sealed class TupleFieldModel
	{
		public TupleFieldModel(PropertyModel property, DatabaseType type)
		{
			Property = property;
			Type     = type;
		}

		/// <summary>
		/// Gets or sets field property descriptor.
		/// </summary>
		public PropertyModel Property { get; set; }
		/// <summary>
		/// Gets or sets field's database type.
		/// </summary>
		public DatabaseType  Type     { get; set; }
		/// <summary>
		/// Gets or sets field's <see cref="LinqToDB.DataType"/> enum value for field.
		/// </summary>
		public DataType?     DataType { get; set; }
	}
}
