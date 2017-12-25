using System;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Mapping
{
	using Common;
	using Data;
	using Expressions;

	using Extensions;

	using Reflection;

	/// <summary>
	/// Stores mapping entity column descriptor.
	/// </summary>
	public class ColumnDescriptor
	{
		/// <summary>
		/// Creates descriptor instance.
		/// </summary>
		/// <param name="mappingSchema">Mapping schema, associated with descriptor.</param>
		/// <param name="columnAttribute">Column attribute, from which descriptor data should be extracted.</param>
		/// <param name="memberAccessor">Column mapping member accessor.</param>
		public ColumnDescriptor(MappingSchema mappingSchema, ColumnAttribute columnAttribute, MemberAccessor memberAccessor)
		{
			MemberAccessor = memberAccessor;
			MemberInfo     = memberAccessor.MemberInfo;

			if (MemberInfo.IsFieldEx())
			{
				var fieldInfo = (FieldInfo)MemberInfo;
				MemberType = fieldInfo.FieldType;
			}
			else if (MemberInfo.IsPropertyEx())
			{
				var propertyInfo = (PropertyInfo)MemberInfo;
				MemberType = propertyInfo.PropertyType;
			}
#if !NETSTANDARD1_6
			else if (MemberInfo is DynamicColumnInfo dynamicColumnInfo)
			{
				MemberType = dynamicColumnInfo.ColumnType;
			}
#endif

			MemberName      = columnAttribute.MemberName ?? MemberInfo.Name;
			ColumnName      = columnAttribute.Name       ?? MemberInfo.Name;
			Storage         = columnAttribute.Storage;
			PrimaryKeyOrder = columnAttribute.PrimaryKeyOrder;
			IsDiscriminator = columnAttribute.IsDiscriminator;
			DataType        = columnAttribute.DataType;
			DbType          = columnAttribute.DbType;
			CreateFormat    = columnAttribute.CreateFormat;

			if (columnAttribute.HasLength   ()) Length    = columnAttribute.Length;
			if (columnAttribute.HasPrecision()) Precision = columnAttribute.Precision;
			if (columnAttribute.HasScale    ()) Scale     = columnAttribute.Scale;

			if (Storage == null)
			{
				StorageType = MemberType;
				StorageInfo = MemberInfo;
			}
			else
			{
				var expr = Expression.PropertyOrField(Expression.Constant(null, MemberInfo.DeclaringType), Storage);
				StorageType = expr.Type;
				StorageInfo = expr.Member;
			}

			var defaultCanBeNull = false;

			if (columnAttribute.HasCanBeNull())
				CanBeNull = columnAttribute.CanBeNull;
			else
			{
				var na = mappingSchema.GetAttribute<NullableAttribute>(MemberAccessor.TypeAccessor.Type, MemberInfo, attr => attr.Configuration);

				if (na != null)
				{
					CanBeNull = na.CanBeNull;
				}
				else
				{
					CanBeNull        = mappingSchema.GetCanBeNull(MemberType);
					defaultCanBeNull = true;
				}
			}

			if (columnAttribute.HasIsIdentity())
			{
				IsIdentity = columnAttribute.IsIdentity;
			}
			else if (MemberName.IndexOf(".") < 0)
			{
				var a = mappingSchema.GetAttribute<IdentityAttribute>(MemberAccessor.TypeAccessor.Type, MemberInfo, attr => attr.Configuration);
				if (a != null)
					IsIdentity = true;
			}

			SequenceName = mappingSchema.GetAttribute<SequenceNameAttribute>(memberAccessor.TypeAccessor.Type, MemberInfo, attr => attr.Configuration);

			if (SequenceName != null)
				IsIdentity = true;

			SkipOnInsert = columnAttribute.HasSkipOnInsert() ? columnAttribute.SkipOnInsert : IsIdentity;
			SkipOnUpdate = columnAttribute.HasSkipOnUpdate() ? columnAttribute.SkipOnUpdate : IsIdentity;

			if (defaultCanBeNull && IsIdentity)
				CanBeNull = false;

			if (columnAttribute.HasIsPrimaryKey())
				IsPrimaryKey = columnAttribute.IsPrimaryKey;
			else if (MemberName.IndexOf(".") < 0)
			{
				var a = mappingSchema.GetAttribute<PrimaryKeyAttribute>(MemberAccessor.TypeAccessor.Type, MemberInfo, attr => attr.Configuration);

				if (a != null)
				{
					IsPrimaryKey    = true;
					PrimaryKeyOrder = a.Order;
				}
			}

			if (DbType == null || DataType == DataType.Undefined)
			{
				var a = mappingSchema.GetAttribute<DataTypeAttribute>(MemberAccessor.TypeAccessor.Type, MemberInfo, attr => attr.Configuration);

				if (a != null)
				{
					if (DbType == null)
						DbType = a.DbType;

					if (DataType == DataType.Undefined && a.DataType.HasValue)
						DataType = a.DataType.Value;
				}
			}
		}

		/// <summary>
		/// Gets column mapping member accessor.
		/// </summary>
		public MemberAccessor MemberAccessor  { get; private set; }

		/// <summary>
		/// Gets column mapping member (field or property).
		/// </summary>
		public MemberInfo     MemberInfo      { get; private set; }

		/// <summary>
		/// Gets value storage member (field or property).
		/// </summary>
		public MemberInfo     StorageInfo     { get; private set; }

		/// <summary>
		/// Gets type of column mapping member (field or property).
		/// </summary>
		public Type           MemberType      { get; private set; }

		/// <summary>
		/// Gets type of column value storage member (field or property).
		/// </summary>
		public Type           StorageType     { get; private set; }

		/// <summary>
		/// Gets the name of mapped member.
		/// When applied to class or interface, should contain name of property of field.
		///
		/// If column is mapped to a property or field of composite object, <see cref="MemberName"/> should contain a path to that
		/// member using dot as separator.
		/// <example>
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
		/// </example>
		/// </summary>
		public string         MemberName      { get; private set; }

		/// <summary>
		/// Gets the name of a column in database.
		/// If not specified, <see cref="MemberName"/> value will be used.
		/// </summary>
		public string         ColumnName      { get; private set; }

		/// <summary>
		/// Gets storage property or field to hold the value from a column.
		/// Could be usefull e.g. in combination of private storage field and getter-only mapping property.
		/// </summary>
		public string         Storage         { get; private set; }

		/// <summary>
		/// Gets whether a column contains a discriminator value for a LINQ to DB inheritance hierarchy.
		/// <see cref="InheritanceMappingAttribute"/> for more details.
		/// Default value: <c>false</c>.
		/// </summary>
		public bool           IsDiscriminator { get; private set; }

		/// <summary>
		/// Gets LINQ to DB type for column.
		/// </summary>
		public DataType       DataType        { get; private set; }

		/// <summary>
		/// Gets the name of the database column type.
		/// </summary>
		public string         DbType          { get; private set; }

		/// <summary>
		/// Gets whether a column contains values that the database auto-generates.
		/// </summary>
		public bool           IsIdentity      { get; private set; }

		/// <summary>
		/// Gets whether a column is insertable.
		/// This flag will affect only insert operations with implicit columns specification like
		/// <see cref="DataExtensions.Insert{T}(IDataContext, T, string, string, string)"/>
		/// method and will be ignored when user explicitly specifies value for this column.
		/// </summary>
		public bool           SkipOnInsert    { get; private set; }

		/// <summary>
		/// Gets whether a column is updatable.
		/// This flag will affect only update operations with implicit columns specification like
		/// <see cref="DataExtensions.Update{T}(IDataContext, T)"/>
		/// method and will be ignored when user explicitly specifies value for this column.
		/// </summary>
		public bool           SkipOnUpdate    { get; private set; }

		/// <summary>
		/// Gets whether this member represents a column that is part or all of the primary key of the table.
		/// Also see <see cref="PrimaryKeyAttribute"/>.
		/// </summary>
		public bool           IsPrimaryKey    { get; private set; }

		/// <summary>
		/// Gets order of current column in composite primary key.
		/// Order is used for query generation to define in which order primary key columns must be mentioned in query
		/// from columns with smallest order value to greatest.
		/// </summary>
		public int            PrimaryKeyOrder { get; private set; }

		/// <summary>
		/// Gets whether a column can contain null values.
		/// </summary>
		public bool           CanBeNull       { get; private set; }

		/// <summary>
		/// Gets the length of the database column.
		/// </summary>
		public int?           Length          { get; private set; }

		/// <summary>
		/// Gets the precision of the database column.
		/// </summary>
		public int?           Precision       { get; private set; }

		/// <summary>
		/// Gets the Scale of the database column.
		/// </summary>
		public int?           Scale           { get; private set; }

		/// <summary>
		/// Custom template for column definition in create table SQL expression, generated using
		/// <see cref="DataExtensions.CreateTable{T}(IDataContext, string, string, string, string, string, SqlQuery.DefaulNullable)"/> methods.
		/// Template accepts following string parameters:
		/// - {0} - column name;
		/// - {1} - column type;
		/// - {2} - NULL specifier;
		/// - {3} - identity specification.
		/// </summary>
		public string         CreateFormat    { get; private set; }

		/// <summary>
		/// Gets sequence name for specified column.
		/// </summary>
		public SequenceNameAttribute SequenceName { get; private set; }

		Func<object,object> _getter;

		// TODO: passing mapping schema to generate converter in combination with converter caching looks wrong
		/// <summary>
		/// Extracts column value, converted to database type, from entity object.
		/// </summary>
		/// <param name="mappingSchema">Mapping schema with conversion information.</param>
		/// <param name="obj">Enity object to extract column value from.</param>
		/// <returns>Returns column value, converted to database type.</returns>
		public virtual object GetValue(MappingSchema mappingSchema, object obj)
		{
			if (_getter == null)
			{
				var objParam   = Expression.Parameter(typeof(object), "obj");
				var getterExpr = MemberAccessor.GetterExpression.GetBody(Expression.Convert(objParam, MemberAccessor.TypeAccessor.Type));

				var expr = mappingSchema.GetConvertExpression(MemberType, typeof(DataParameter), createDefault : false);

				if (expr != null)
				{
					getterExpr = Expression.PropertyOrField(expr.GetBody(getterExpr), "Value");
				}
				else
				{
					var type = Converter.GetDefaultMappingFromEnumType(mappingSchema, MemberType);

					if (type != null)
					{
						expr = mappingSchema.GetConvertExpression(MemberType, type);
						getterExpr = expr.GetBody(getterExpr);
					}
				}

				var getter = Expression.Lambda<Func<object,object>>(Expression.Convert(getterExpr, typeof(object)), objParam);

				_getter = getter.Compile();
			}

			return _getter(obj);
		}
	}
}
