using System;

namespace LinqToDB.Mapping
{
	// TODO: V2 - why it allows Class and Interface as target?
	/// <summary>
	/// This attribute allows to override default types, defined in mapping schema, for current column.
	/// Also see <seealso cref="ColumnAttribute.DataType"/> and <seealso cref="ColumnAttribute.DbType"/>.
	/// Applying this attribute to class or interface will have no effect.
	/// </summary>
	[AttributeUsage(
		AttributeTargets.Field | AttributeTargets.Property| AttributeTargets.Class | AttributeTargets.Interface,
		AllowMultiple = true, Inherited = true)]
	public class DataTypeAttribute : Attribute
	{
		/// <summary>
		/// Creates attribute instance.
		/// </summary>
		/// <param name="dataType">linq2db column type name.</param>
		public DataTypeAttribute(DataType dataType)
		{
			DataType = dataType;
		}

		/// <summary>
		/// Creates attribute instance.
		/// </summary>
		/// <param name="dbType">SQL column type name.</param>
		public DataTypeAttribute(string dbType)
		{
			DbType   = dbType;
		}

		/// <summary>
		/// Creates attribute instance.
		/// </summary>
		/// <param name="dataType">linq2db column type name.</param>
		/// <param name="dbType">SQL column type name.</param>
		public DataTypeAttribute(DataType dataType, string dbType)
		{
			DataType = dataType;
			DbType   = dbType;
		}

		/// <summary>
		/// Gets or sets mapping schema configuration name, for which this attribute should be taken into account.
		/// <see cref="ProviderName"/> for standard names.
		/// Attributes with <c>null</c> or empty string <see cref="Configuration"/> value applied to all configurations (if no attribute found for current configuration).
		/// </summary>
		public string Configuration { get; set; }

		/// <summary>
		/// Gets or sets linq2db type of the database column.
		/// </summary>
		public DataType? DataType { get; set; }

		/// <summary>
		/// Gets or sets the name of the database column type.
		/// </summary>
		public string DbType { get; set; }
	}
}
