using System;

namespace LinqToDB.Mapping
{
	/// <summary>
	/// This attribute allows to override default types, defined in mapping schema, for current column.
	/// Also see <seealso cref="ColumnAttribute.DataType"/> and <seealso cref="ColumnAttribute.DbType"/>.
	/// Applying this attribute to class or interface will have no effect.
	/// </summary>
	[AttributeUsage(
		AttributeTargets.Field | AttributeTargets.Property,
		AllowMultiple = true, Inherited = true)]
	public class DataTypeAttribute : MappingAttribute
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
		/// Gets or sets linq2db type of the database column.
		/// </summary>
		public DataType? DataType { get; set; }

		/// <summary>
		/// Gets or sets the name of the database column type.
		/// </summary>
		public string? DbType { get; set; }

		public override string GetObjectID()
		{
			return $".{Configuration}.{DataType}.{DbType}.";
		}
	}
}
