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
	public class TableAttribute : MappingAttribute
	{
		/// <summary>
		/// Creates new table mapping attribute.
		/// </summary>
		public TableAttribute()
		{
			IsColumnAttributeRequired = true;
		}

		/// <summary>
		/// Creates new table mapping attribute.
		/// </summary>
		/// <param name="tableName">Name of mapped table or view in database.</param>
		public TableAttribute(string tableName) : this()
		{
			Name = tableName;
		}

		/// <summary>
		/// Gets or sets name of table or view in database.
		/// When not specified, name of class or interface will be used.
		/// </summary>
		public string? Name                     { get; set; }

		/// <summary>
		/// Gets or sets optional schema/owner name, to override default name. See <see cref="LinqExtensions.SchemaName{T}(ITable{T}, string)"/> method for support information per provider.
		/// </summary>
		public string? Schema                   { get; set; }

		/// <summary>
		/// Gets or sets optional database name, to override default database name. See <see cref="LinqExtensions.DatabaseName{T}(ITable{T}, string)"/> method for support information per provider.
		/// </summary>
		public string? Database                 { get; set; }

		/// <summary>
		/// Gets or sets optional linked server name. See <see cref="LinqExtensions.ServerName{T}(ITable{T}, string)"/> method for support information per provider.
		/// </summary>
		public string? Server                   { get; set; }

		/// <summary>
		/// Gets or sets IsTemporary flag. See <see cref="TableExtensions.IsTemporary{T}(ITable{T}, bool)"/> method for support information per provider.
		/// </summary>
		public bool IsTemporary
		{
			get => TableOptions.HasIsTemporary();
			set
			{
				if (value)
					TableOptions |= TableOptions.IsTemporary;
				else
					TableOptions &= ~TableOptions.IsTemporary;
			}
		}

		/// <summary>
		/// Gets or sets Table options. See <see cref="TableOptions"/> enum for support information per provider.
		/// </summary>
		public TableOptions TableOptions        { get; set; }

		/// <summary>
		/// Gets or sets column mapping rules for current class or interface.
		/// If <c>true</c>, properties and fields should be marked with one of those attributes to be used for mapping:
		/// - <see cref="ColumnAttribute"/>;
		/// - <see cref="PrimaryKeyAttribute"/>;
		/// - <see cref="IdentityAttribute"/>;
		/// - <see cref="ColumnAliasAttribute"/>.
		/// Otherwise all supported members of scalar type will be used:
		/// - public instance fields and properties;
		/// - explicit interface implementation properties.
		/// Also see <seealso cref="Common.Configuration.IsStructIsScalarType"/> and <seealso cref="ScalarTypeAttribute"/>.
		/// Default value: <c>true</c>.
		/// </summary>
		public bool   IsColumnAttributeRequired { get; set; }

		/// <summary>
		/// This property is not used by linq2db and could be used for informational purposes.
		/// </summary>
		public bool   IsView                    { get; set; }

		public override string GetObjectID()
		{
			return FormattableString.Invariant($".{Configuration}.{Name}.{Schema}.{Database}.{Server}.{(IsTemporary?'1':'0')}.{(int)TableOptions}.{(IsColumnAttributeRequired?'1':'0')}.{(IsView?'1':'0')}.");
		}
	}
}
