using System;

namespace LinqToDB.Mapping
{
	using SqlQuery;

	// TODO: V2 - make Has* methods internal
	/// <summary>
	/// Configures mapping of mapping class member to database column.
	/// Could be applied directly to a property or field or to mapping class/interface.
	/// In latter case you should specify member name using <see cref="MemberName"/> property.
	/// </summary>
	[AttributeUsage(
		AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Interface,
		AllowMultiple = true, Inherited = true)]
	public class ColumnAttribute : Attribute
	{
		/// <summary>
		/// Creates attribute instance.
		/// </summary>
		public ColumnAttribute()
		{
			IsColumn = true;
		}

		/// <summary>
		/// Creates attribute instance.
		/// </summary>
		/// <param name="columnName">Database column name.</param>
		public ColumnAttribute(string columnName) : this()
		{
			Name = columnName;
		}

		/// <summary>
		/// Creates attribute instance.
		/// </summary>
		/// <param name="columnName">Database column name.</param>
		/// <param name="memberName">Name of mapped member. See <see cref="MemberName"/> for more details.</param>
		public ColumnAttribute(string columnName, string memberName) : this()
		{
			Name       = columnName;
			MemberName = memberName;
		}

		/// <summary>
		/// Copy constructor.
		/// </summary>
		/// <param name="memberName">Name of mapped member. See <see cref="MemberName"/> for more details.</param>
		/// <param name="ca">Attribute to clone.</param>
		internal ColumnAttribute(string memberName, ColumnAttribute ca)
			: this(ca)
		{
			MemberName = memberName + "." + ca.MemberName.TrimStart('.');
		}

		/// <summary>
		/// Copy constructor.
		/// </summary>
		/// <param name="ca">Attribute to clone.</param>
		internal ColumnAttribute(ColumnAttribute ca)
		{
			MemberName      = ca.MemberName;
			Configuration   = ca.Configuration;
			Name            = ca.Name;
			DataType        = ca.DataType;
			DbType          = ca.DbType;
			Storage         = ca.Storage;
			IsDiscriminator = ca.IsDiscriminator;
			PrimaryKeyOrder = ca.PrimaryKeyOrder;
			IsColumn        = ca.IsColumn;
			CreateFormat    = ca.CreateFormat;

			if (ca.HasSkipOnInsert()) SkipOnInsert = ca.SkipOnInsert;
			if (ca.HasSkipOnUpdate()) SkipOnUpdate = ca.SkipOnUpdate;
			if (ca.HasCanBeNull())    CanBeNull    = ca.CanBeNull;
			if (ca.HasIsIdentity())   IsIdentity   = ca.IsIdentity;
			if (ca.HasIsPrimaryKey()) IsPrimaryKey = ca.IsPrimaryKey;
			if (ca.HasLength())       Length       = ca.Length;
			if (ca.HasPrecision())    Precision    = ca.Precision;
			if (ca.HasScale())        Scale        = ca.Scale;
			if (ca.HasOrder())        Order        = ca.Order;
		}

		/// <summary>
		/// Gets or sets mapping schema configuration name, for which this attribute should be taken into account.
		/// <see cref="ProviderName"/> for standard names.
		/// Attributes with <c>null</c> or empty string <see cref="Configuration"/> value applied to all configurations (if no attribute found for current configuration).
		/// </summary>
		public string Configuration { get; set; }

		/// <summary>
		/// Gets or sets the name of a column in database.
		/// If not specified, member name will be used.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Gets or sets the name of mapped member.
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
		public string MemberName { get; set; }

		/// <summary>
		/// Gets or sets linq2db type for column.
		/// Default value: default type, defined for member type in mapping schema.
		/// </summary>
		public DataType DataType { get; set; }

		/// <summary>
		/// Gets or sets the name of the database column type.
		/// Default value: default type, defined for member type in mapping schema.
		/// </summary>
		public string DbType { get; set; }

		/// <summary>
		/// Gets or sets flag that tells that current member should be included into mapping.
		/// Use NonColumnAttribute instead as a shorthand.
		/// Default value: <c>true</c>.
		/// </summary>
		public bool IsColumn { get; set; }

		/// <summary>
		/// Gets or sets a storage property or field to hold the value from a column.
		/// Could be usefull e.g. in combination of private storage field and getter-only mapping property.
		/// </summary>
		public string Storage { get; set; }

		/// <summary>
		/// Gets or sets whether a column contains a discriminator value for a LINQ to DB inheritance hierarchy.
		/// <see cref="InheritanceMappingAttribute"/> for more details.
		/// Default value: <c>false</c>.
		/// </summary>
		public bool IsDiscriminator { get; set; }

		private bool? _skipOnInsert;
		/// <summary>
		/// Gets or sets whether a column is insertable.
		/// This flag will affect only insert operations with implicit columns specification like
		/// <see cref="DataExtensions.Insert{T}(IDataContext, T, string, string, string, string)"/>
		/// method and will be ignored when user explicitly specifies value for this column.
		/// </summary>
		public bool   SkipOnInsert
		{
			get => _skipOnInsert ?? false;
			set => _skipOnInsert = value;
		}

		/// <summary>
		/// Returns <c>true</c>, if <see cref="SkipOnInsert"/> was configured for current attribute.
		/// </summary>
		/// <returns><c>true</c> if <see cref="SkipOnInsert"/> property was set in attribute.</returns>
		public bool HasSkipOnInsert() { return _skipOnInsert.HasValue; }

		private bool? _skipOnUpdate;
		/// <summary>
		/// Gets or sets whether a column is updatable.
		/// This flag will affect only update operations with implicit columns specification like
		/// <see cref="DataExtensions.Update{T}(IDataContext, T, string, string, string, string)"/>
		/// method and will be ignored when user explicitly specifies value for this column.
		/// </summary>
		public bool   SkipOnUpdate
		{
			get => _skipOnUpdate ?? false;
			set => _skipOnUpdate = value;
		}

		/// <summary>
		/// Returns <c>true</c>, if <see cref="SkipOnUpdate"/> was configured for current attribute.
		/// </summary>
		/// <returns><c>true</c> if <see cref="SkipOnUpdate"/> property was set in attribute.</returns>
		public bool HasSkipOnUpdate() { return _skipOnUpdate.HasValue; }

		private bool? _isIdentity;
		/// <summary>
		/// Gets or sets whether a column contains values that the database auto-generates.
		/// Also see <see cref="IdentityAttribute"/>.
		/// </summary>
		public  bool   IsIdentity
		{
			get => _isIdentity ?? false;
			set => _isIdentity = value;
		}

		/// <summary>
		/// Returns <c>true</c>, if <see cref="IsIdentity"/> was configured for current attribute.
		/// </summary>
		/// <returns><c>true</c> if <see cref="IsIdentity"/> property was set in attribute.</returns>
		public bool HasIsIdentity() { return _isIdentity.HasValue; }

		private bool? _isPrimaryKey;
		/// <summary>
		/// Gets or sets whether this class member represents a column that is part or all of the primary key of the table.
		/// Also see <see cref="PrimaryKeyAttribute"/>.
		/// </summary>
		public bool   IsPrimaryKey
		{
			get => _isPrimaryKey ?? false;
			set => _isPrimaryKey = value;
		}

		/// <summary>
		/// Returns <c>true</c>, if <see cref="IsPrimaryKey"/> was configured for current attribute.
		/// </summary>
		/// <returns><c>true</c> if <see cref="IsPrimaryKey"/> property was set in attribute.</returns>
		public bool HasIsPrimaryKey() { return _isPrimaryKey.HasValue; }

		/// <summary>
		/// Gets or sets the Primary Key order.
		/// See <see cref="PrimaryKeyAttribute.Order"/> for more details.
		/// </summary>
		public int PrimaryKeyOrder { get; set; }

		private bool? _canBeNull;
		/// <summary>
		/// Gets or sets whether a column can contain <c>NULL</c> values.
		/// </summary>
		public  bool   CanBeNull
		{
			get => _canBeNull ?? true;
			set => _canBeNull = value;
		}

		/// <summary>
		/// Returns <c>true</c>, if <see cref="CanBeNull"/> was configured for current attribute.
		/// </summary>
		/// <returns><c>true</c> if <see cref="CanBeNull"/> property was set in attribute.</returns>
		public bool HasCanBeNull() { return _canBeNull.HasValue; }

		private int? _length;
		/// <summary>
		/// Gets or sets the length of the database column.
		/// Default value: value, defined for member type in mapping schema.
		/// </summary>
		public  int   Length
		{
			get => _length ?? 0;
			set => _length = value;
		}

		/// <summary>
		/// Returns <c>true</c>, if <see cref="Length"/> was configured for current attribute.
		/// </summary>
		/// <returns><c>true</c> if <see cref="Length"/> property was set in attribute.</returns>
		public bool HasLength() { return _length.HasValue; }

		private int? _precision;
		/// <summary>
		/// Gets or sets the precision of the database column.
		/// Default value: value, defined for member type in mapping schema.
		/// </summary>
		public int   Precision
		{
			get => _precision ?? 0;
			set => _precision = value;
		}

		/// <summary>
		/// Returns <c>true</c>, if <see cref="Precision"/> was configured for current attribute.
		/// </summary>
		/// <returns><c>true</c> if <see cref="Precision"/> property was set in attribute.</returns>
		public bool HasPrecision() { return _precision.HasValue; }

		private int? _scale;
		/// <summary>
		/// Gets or sets the Scale of the database column.
		/// Default value: value, defined for member type in mapping schema.
		/// </summary>
		public int   Scale
		{
			get => _scale ?? 0;
			set => _scale = value;
		}

		/// <summary>
		/// Returns <c>true</c>, if <see cref="Scale"/> was configured for current attribute.
		/// </summary>
		/// <returns><c>true</c> if <see cref="Scale"/> property was set in attribute.</returns>
		public bool HasScale() { return _scale.HasValue; }

		/// <summary>
		/// Custom template for column definition in create table SQL expression, generated using
		/// <see cref="DataExtensions.CreateTable{T}(IDataContext, string, string, string, string, string, DefaultNullable, string)"/> methods.
		/// Template accepts following string parameters:
		/// - {0} - column name;
		/// - {1} - column type;
		/// - {2} - NULL specifier;
		/// - {3} - identity specification.
		/// </summary>
		public string CreateFormat { get; set; }

		private int? _order;
		/// <summary>
		/// Specifies the order of the field in table creation.
		/// Positive values first (ascending), then unspecified (arbitrary), then negative values (ascending).
		/// </summary>
		/// <remarks>
		/// Ordering performed in <see cref="SqlTable.SqlTable(MappingSchema, Type, string)"/> constructor.
		/// </remarks>
		public int Order
		{
			get => _order ?? int.MaxValue;
			set => _order = value;
		}

		/// <summary>
		/// Returns <c>true</c>, if <see cref="Order"/> was configured for current attribute.
		/// </summary>
		/// <returns><c>true</c> if <see cref="Order"/> property was set in attribute.</returns>
		public bool HasOrder() { return _order.HasValue; }
	}
}
