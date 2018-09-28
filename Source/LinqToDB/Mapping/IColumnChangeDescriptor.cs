namespace LinqToDB.Mapping
{
	/// <summary>
	/// Mapping entity column descriptor change interface.
	/// </summary>
	public interface IColumnChangeDescriptor
	{
		/// <summary>
		/// Gets the name of mapped member.
		/// When applied to class or interface, should contain name of property of field.
		///
		/// If column is mapped to a property or field of composite object, <see cref="MemberName"/> should contain a path to that
		/// member using dot as separator.
		/// <example>
		/// <code>
		/// public class Address
		/// {
		///     public string City     { get; set; }
		///     public string Street   { get; set; }
		///     public int    Building { get; set; }
		/// }
		///
		/// [Column("city", "Residence.Street")]
		/// [Column("user_name", "Name")]
		/// public class User
		/// {
		///     public string Name;
		///
		///     [Column("street", ".Street")]
		///     [Column("building_number", MemberName = ".Building")]
		///     public Address Residence { get; set; }
		/// }
		/// </code>
		/// </example>
		/// </summary>
		string MemberName { get; }

		/// <summary>
		/// Gets or sets the name of a column in database.
		/// If not specified, <see cref="MemberName"/> value will be returned.
		/// </summary>
		string ColumnName { get; set; }
	}
}
