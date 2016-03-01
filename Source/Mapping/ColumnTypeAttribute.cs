using System;

namespace LinqToDB.Mapping
{
	/// <summary>
	/// Associates a member with a column type in a database table.
	/// </summary>
	[AttributeUsage(
		AttributeTargets.Field | AttributeTargets.Property| AttributeTargets.Class | AttributeTargets.Interface,
		AllowMultiple = true, Inherited = true)]
	public class ColumnTypeAttribute : Attribute
	{
		public ColumnTypeAttribute(DataType dataType)
		{
			DataType = dataType;
		}

		public ColumnTypeAttribute(string dbType)
		{
			DbType   = dbType;
		}

		public ColumnTypeAttribute(DataType dataType, string dbType)
		{
			DataType = dataType;
			DbType   = dbType;
		}

		public string Configuration { get; set; }

		/// <summary>
		/// Gets or sets the type of the database column.
		/// </summary>
		public DataType? DataType { get; set; }

		/// <summary>
		/// Gets or sets the name of the database column type.
		/// </summary>
		public string DbType { get; set; }
	}
}
