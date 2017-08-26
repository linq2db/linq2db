using System;

namespace LinqToDB.Mapping
{
	/// <summary>
	/// Maps databse table or view to a class or interface.
	/// You can apply it to any class including non-public, nester or abstract classes.
	/// Applying it to interfaces will allow you to perform queries against target table, but you need to specify
	/// projection in your query explicitly, if you want to select data from such mapping.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true, Inherited = true)]
	public class TableAttribute : Attribute
	{
		/// <summary>
		/// Creates new table mapping atteribute.
		/// </summary>
		public TableAttribute()
		{
			IsColumnAttributeRequired = true;
		}

		/// <summary>
		/// Creates new table mapping atteribute.
		/// </summary>
		/// <param name="tableName">Name of mapped table or view in database.</param>
		public TableAttribute(string tableName) : this()
		{
			Name = tableName;
		}

		/// <summary>
		/// Gets or sets mapping schema configuration name, for which this attribute should be taken into account.
		/// <see cref="ProviderName"/> for standard names.
		/// Attributes with <c>null</c> or empty string <see cref="Configuration"/> value applied to all configurations (if no attribute found for current configuration).
		/// </summary>
		public string Configuration             { get; set; }

		/// <summary>
		/// Gets or sets name of table or view in database.
		/// When not specified, name of class on interface will be used.
		/// </summary>
		public string Name                      { get; set; }

		/// <summary>
		/// Gets or sets optional schema/owner name, to override default name. See <see cref="LinqExtensions.SchemaName{T}(ITable{T}, string)(ITable{T}, string)"/> method for support information per provider.
		/// </summary>
		public string Schema                    { get; set; }

		/// <summary>
		/// Gets or sets optional database name, to override default database name. See <see cref="LinqExtensions.DatabaseName{T}(ITable{T}, string)"/> method for support information per provider.
		/// </summary>
		public string Database                  { get; set; }

		/// <summary>
		/// Gets or sets column mapping rules for current class or interface.
		/// If <c>true</c>, properties and fields should be marked with one of those attributes to be used for mapping:
		/// - <see cref="ColumnAttribute"/>;
		/// - <see cref="PrimaryKeyAttribute"/>;
		/// - <see cref="IdentityAttribute"/>;
		/// - <see cref="ColumnAliasAttribute"/>.
		/// Otherwise all supported members of scalar type will be used:
		/// - public instance fields and properties;
		/// - explicit interface implmentation properties.
		/// Also see <seealso cref="LinqToDB.Common.Configuration.IsStructIsScalarType"/> and <seealso cref="ScalarTypeAttribute"/>.
		/// Default value: <c>true</c>.
		/// </summary>
		public bool   IsColumnAttributeRequired { get; set; }

		// TODO: V2 - remove?
		/// <summary>
		/// This property currently not implemented and setting it will have no any effect.
		/// </summary>
		public bool   IsView                    { get; set; }
	}
}
