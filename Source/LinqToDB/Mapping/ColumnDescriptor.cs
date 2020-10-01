using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Mapping
{
	using Common;
	using Data;
	using Expressions;
	using Extensions;
	using Reflection;
	using SqlQuery;

	/// <summary>
	/// Stores mapping entity column descriptor.
	/// </summary>
	public class ColumnDescriptor : IColumnChangeDescriptor
	{
		/// <summary>
		/// Creates descriptor instance.
		/// </summary>
		/// <param name="mappingSchema">Mapping schema, associated with descriptor.</param>
		/// <param name="columnAttribute">Column attribute, from which descriptor data should be extracted.</param>
		/// <param name="memberAccessor">Column mapping member accessor.</param>
		public ColumnDescriptor(MappingSchema mappingSchema, ColumnAttribute? columnAttribute, MemberAccessor memberAccessor)
		{
			MappingSchema  = mappingSchema;
			MemberAccessor = memberAccessor;
			MemberInfo     = memberAccessor.MemberInfo;

			if (MemberInfo.IsFieldEx())
			{
				var fieldInfo = (FieldInfo)MemberInfo;
				MemberType    = fieldInfo.FieldType;
			}
			else if (MemberInfo.IsPropertyEx())
			{
				var propertyInfo = (PropertyInfo)MemberInfo;
				MemberType       = propertyInfo.PropertyType;
			}

			var dataType = mappingSchema.GetDataType(MemberType);
			if (dataType.Type.DataType == DataType.Undefined)
				dataType = mappingSchema.GetUnderlyingDataType(MemberType, out var _);

			if (columnAttribute == null)
			{
				columnAttribute = new ColumnAttribute();

				columnAttribute.DataType  = dataType.Type.DataType;
				columnAttribute.DbType    = dataType.Type.DbType;

				if (dataType.Type.Length    != null) columnAttribute.Length    = dataType.Type.Length.Value;
				if (dataType.Type.Precision != null) columnAttribute.Precision = dataType.Type.Precision.Value;
				if (dataType.Type.Scale     != null) columnAttribute.Scale     = dataType.Type.Scale.Value;
			}
			else if (columnAttribute.DataType == DataType.Undefined || columnAttribute.DataType == dataType.Type.DataType)
			{
				if (dataType.Type.Length    != null && !columnAttribute.HasLength())    columnAttribute.Length    = dataType.Type.Length.Value;
				if (dataType.Type.Precision != null && !columnAttribute.HasPrecision()) columnAttribute.Precision = dataType.Type.Precision.Value;
				if (dataType.Type.Scale     != null && !columnAttribute.HasScale())     columnAttribute.Scale     = dataType.Type.Scale.Value;
			}

			MemberName        = columnAttribute.MemberName ?? MemberInfo.Name;
			ColumnName        = columnAttribute.Name       ?? MemberInfo.Name;
			Storage           = columnAttribute.Storage;
			PrimaryKeyOrder   = columnAttribute.PrimaryKeyOrder;
			IsDiscriminator   = columnAttribute.IsDiscriminator;
			SkipOnEntityFetch = columnAttribute.SkipOnEntityFetch;
			DataType          = columnAttribute.DataType;
			DbType            = columnAttribute.DbType;
			CreateFormat      = columnAttribute.CreateFormat;

			if (columnAttribute.HasLength   ()) Length    = columnAttribute.Length;
			if (columnAttribute.HasPrecision()) Precision = columnAttribute.Precision;
			if (columnAttribute.HasScale    ()) Scale     = columnAttribute.Scale;
			if (columnAttribute.HasOrder    ()) Order     = columnAttribute.Order;

			if (Storage == null)
			{
				StorageType = MemberType;
				StorageInfo = MemberInfo;
			}
			else
			{
				var expr = ExpressionHelper.PropertyOrField(Expression.Constant(null, MemberInfo.DeclaringType), Storage);
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

			var vc = mappingSchema.GetAttribute<ValueConverterAttribute>(memberAccessor.TypeAccessor.Type, MemberInfo, attr => attr.Configuration);
			if (vc != null)
			{
				ValueConverter = vc.GetValueConverter(this);
			}

			var skipValueAttributes = mappingSchema.GetAttributes<SkipBaseAttribute>(MemberAccessor.TypeAccessor.Type, MemberInfo, attr => attr.Configuration);
			if (skipValueAttributes.Length > 0)
			{
				SkipBaseAttributes    = skipValueAttributes;
				SkipModificationFlags = SkipBaseAttributes.Aggregate(SkipModification.None, (s, c) => s | c.Affects);
			}
		}

		/// <summary>
		/// Gets MappingSchema for current ColumnDescriptor
		/// </summary>
		public MappingSchema  MappingSchema   { get; }

		/// <summary>
		/// Gets column mapping member accessor.
		/// </summary>
		public MemberAccessor MemberAccessor  { get; }

		/// <summary>
		/// Gets column mapping member (field or property).
		/// </summary>
		public MemberInfo     MemberInfo      { get; }

		/// <summary>
		/// Gets value storage member (field or property).
		/// </summary>
		public MemberInfo     StorageInfo     { get; }

		/// <summary>
		/// Gets type of column mapping member (field or property).
		/// </summary>
		public Type           MemberType      { get; } = null!;

		/// <summary>
		/// Gets type of column value storage member (field or property).
		/// </summary>
		public Type           StorageType     { get; }

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
		public string         MemberName      { get; }

		string IColumnChangeDescriptor.MemberName => MemberName;

		/// <summary>
		/// Gets the name of a column in database.
		/// If not specified, <see cref="MemberName"/> value will be used.
		/// </summary>
		public string        ColumnName      { get; private set; }

		string IColumnChangeDescriptor.ColumnName
		{
			get => ColumnName;
			set => ColumnName = value;
		}

		/// <summary>
		/// Gets storage property or field to hold the value from a column.
		/// Could be usefull e.g. in combination of private storage field and getter-only mapping property.
		/// </summary>
		public string?        Storage         { get; }

		/// <summary>
		/// Gets whether a column contains a discriminator value for a LINQ to DB inheritance hierarchy.
		/// <see cref="InheritanceMappingAttribute"/> for more details.
		/// Default value: <c>false</c>.
		/// </summary>
		public bool           IsDiscriminator { get; }

		/// <summary>
		/// Gets LINQ to DB type for column.
		/// </summary>
		public DataType       DataType        { get; }

		/// <summary>
		/// Gets the name of the database column type.
		/// </summary>
		public string?        DbType          { get; }

		/// <summary>
		/// Gets whether a column contains values that the database auto-generates.
		/// </summary>
		public bool           IsIdentity      { get; }

		/// <summary>
		/// Gets whether a column is insertable.
		/// This flag will affect only insert operations with implicit columns specification like
		/// <see cref="DataExtensions.Insert{T}(IDataContext, T, string, string, string, string)"/>
		/// method and will be ignored when user explicitly specifies value for this column.
		/// </summary>
		public bool           SkipOnInsert    { get; }

		/// <summary>
		/// Gets whether a column must be explicitly defined in a Select statement to be fetched. If <c>true</c>, a "SELECT *"-ish statement won't retrieve this column.
		/// Default value: <c>false</c>.
		/// </summary>
		public bool           SkipOnEntityFetch { get; }

		/// <summary>
		/// Gets whether the column has specific values that should be skipped on insert.
		/// </summary>
		public bool           HasValuesToSkipOnInsert => SkipBaseAttributes?.Any(s => (s.Affects & SkipModification.Insert) != 0) ?? false;

		/// <summary>
		/// Gets whether the column has specific values that should be skipped on update.
		/// </summary>
		public bool           HasValuesToSkipOnUpdate => SkipBaseAttributes?.Any(s => (s.Affects & SkipModification.Update) != 0) ?? false;

		/// <summary>
		/// Gets whether the column has specific values that should be skipped on insert.
		/// </summary>
		private SkipBaseAttribute[]? SkipBaseAttributes { get; }

		/// <summary>
		/// Gets flags for which operation values are skipped.
		/// </summary>
		public SkipModification SkipModificationFlags { get; }

		/// <summary> 
		/// Checks if the passed object has values that should bes skipped based on the given flags. 
		/// </summary>
		/// <param name="obj">The object containing the values for the operation.</param>
		/// <param name="descriptor"><see cref="EntityDescriptor"/> of the current instance.</param>
		/// <param name="flags">The flags that specify which operation should be checked.</param>
		/// <returns><c>true</c> if object contains values that should be skipped. </returns>
		public virtual bool ShouldSkip(object obj, EntityDescriptor descriptor, SkipModification flags)
		{
			if (SkipBaseAttributes == null)
				return false;
	
			foreach (var skipBaseAttribute in SkipBaseAttributes)
				if ((skipBaseAttribute.Affects & flags) != 0 && skipBaseAttribute.ShouldSkip(obj, descriptor, this))
					return true;

			return false;
		}

		/// <summary>
		/// Gets whether a column is updatable.
		/// This flag will affect only update operations with implicit columns specification like
		/// <see cref="DataExtensions.Update{T}(IDataContext, T, string, string, string, string)"/>
		/// method and will be ignored when user explicitly specifies value for this column.
		/// </summary>
		public bool           SkipOnUpdate    { get; }

		/// <summary>
		/// Gets whether this member represents a column that is part or all of the primary key of the table.
		/// Also see <see cref="PrimaryKeyAttribute"/>.
		/// </summary>
		public bool           IsPrimaryKey    { get; }

		/// <summary>
		/// Gets order of current column in composite primary key.
		/// Order is used for query generation to define in which order primary key columns must be mentioned in query
		/// from columns with smallest order value to greatest.
		/// </summary>
		public int            PrimaryKeyOrder { get; }

		/// <summary>
		/// Gets whether a column can contain null values.
		/// </summary>
		public bool           CanBeNull       { get; }

		/// <summary>
		/// Gets the length of the database column.
		/// </summary>
		public int?           Length          { get; }

		/// <summary>
		/// Gets the precision of the database column.
		/// </summary>
		public int?           Precision       { get; }

		/// <summary>
		/// Gets the Scale of the database column.
		/// </summary>
		public int?           Scale           { get; }

		/// <summary>
		/// Custom template for column definition in create table SQL expression, generated using
		/// <see cref="DataExtensions.CreateTable{T}(IDataContext, string, string, string, string, string, DefaultNullable, string)"/> methods.
		/// Template accepts following string parameters:
		/// - {0} - column name;
		/// - {1} - column type;
		/// - {2} - NULL specifier;
		/// - {3} - identity specification.
		/// </summary>
		public string?        CreateFormat    { get; }

		/// <summary>
		/// Sort order for column list.
		/// Positive values first, then unspecified (null), then negative values.
		/// </summary>
		public int?           Order           { get; }

		/// <summary>
		/// Gets sequence name for specified column.
		/// </summary>
		public SequenceNameAttribute? SequenceName { get; }

		/// <summary>
		/// Gets value converter for specific column.
		/// </summary>
		public IValueConverter? ValueConverter  { get; }

		LambdaExpression?    _getterValueLambda;
		LambdaExpression?    _getterParameterLambda;
		Func<object,object>? _getter;

		/// <summary>
		/// Returns DbDataType for current column.
		/// </summary>
		/// <returns></returns>
		public DbDataType GetDbDataType(bool completeDataType)
		{
			var systemType = MemberType;
			var dataType   = DataType;

			if (completeDataType && dataType == DataType.Undefined)
			{
				dataType = CalculateDataType(MappingSchema, systemType);
			}

			return new DbDataType(systemType, dataType, DbType, Length, Precision, Scale);
		}


		/// <summary>
		/// Returns DbDataType for current column after conversions.
		/// </summary>
		/// <returns></returns>
		public DbDataType GetConvertedDbDataType()
		{
			var dbDataType = GetDbDataType(true);

			var systemType = dbDataType.SystemType;

			if (ValueConverter != null)
				systemType = ValueConverter.ToProviderExpression.Body.Type;
			else
			{
				var convertLambda = MappingSchema.GetConvertExpression(
					dbDataType.WithSystemType(dbDataType.SystemType),
					dbDataType.WithSystemType(typeof(DataParameter)), createDefault: false);

				// it is conversion via DataParameter, so we don't know destination type
				if (convertLambda != null)
				{
					systemType = typeof(object);
				}
				else
					if (systemType.IsEnum)
					{
						var type = Converter.GetDefaultMappingFromEnumType(MappingSchema, dbDataType.SystemType);
						if (type != null)
						{
							systemType = type;
						}
					}
			}

			if (dbDataType.SystemType != systemType)
				dbDataType = dbDataType.WithSystemType(systemType);

			return dbDataType;
		}

		public static DataType CalculateDataType(MappingSchema mappingSchema, Type systemType)
		{
			var dataType = DataType.Undefined;
			if (systemType.ToNullableUnderlying().IsEnum)
			{
				var enumType = mappingSchema.GetDefaultFromEnumType(systemType);

				if (enumType != null)
					dataType = mappingSchema.GetDataType(enumType).Type.DataType;

				if (dataType == DataType.Undefined && systemType.IsNullable())
				{
					enumType = mappingSchema.GetDefaultFromEnumType(systemType.ToNullableUnderlying());

					if (enumType != null)
						dataType = mappingSchema.GetDataType(enumType).Type.DataType;
				}

				if (dataType == DataType.Undefined)
				{
					enumType = mappingSchema.GetDefaultFromEnumType(typeof(Enum));

					if (enumType != null)
						dataType = mappingSchema.GetDataType(enumType).Type.DataType;
				}

				if (dataType == DataType.Undefined)
				{
					dataType = mappingSchema.GetUnderlyingDataType(systemType, out var canBeNull).Type.DataType;
				}
			}

			if (dataType == DataType.Undefined)
				dataType = mappingSchema.GetDataType(systemType).Type.DataType;

			return dataType;
		}

		/// <summary>
		/// Returns Lambda for extracting column value, converted to database type, from entity object.
		/// </summary>
		/// <returns>Returns Lambda which extracts member value to database type.</returns>
		public LambdaExpression GetDbValueLambda()
		{
			if (_getterValueLambda != null)
				return _getterValueLambda;

			var paramGetter = GetDbParamLambda();

			if (typeof(DataParameter).IsSameOrParentOf(paramGetter.Body.Type))
			{
				var param = Expression.Parameter(typeof(DataParameter), "p");
				var convertExpression = Expression.Lambda(ExpressionHelper.PropertyOrField(param, "Value"), param);
				convertExpression = MappingSchema.AddNullCheck(convertExpression);
				paramGetter =
					Expression.Lambda(InternalExtensions.ApplyLambdaToExpression(convertExpression, paramGetter.Body),
						paramGetter.Parameters);
			}

			_getterValueLambda = paramGetter;
			return _getterValueLambda;
		}

		/// <summary>
		/// Returns Lambda for extracting column value, converted to database type or <see cref="DataParameter"/>, from entity object.
		/// </summary>
		/// <returns>Returns Lambda which extracts member value to database type or <see cref="DataParameter"/>.</returns>
		public LambdaExpression GetDbParamLambda()
		{
			if (_getterParameterLambda != null)
				return _getterParameterLambda;

			var objParam   = Expression.Parameter(MemberAccessor.TypeAccessor.Type, "obj");
			var getterExpr = MemberAccessor.GetterExpression.GetBody(objParam);
			var dbDataType = GetDbDataType(true);

			getterExpr = ApplyConversions(getterExpr, dbDataType, true);

			_getterParameterLambda = Expression.Lambda(getterExpr, objParam);
			return _getterParameterLambda;
		}

		/// <summary>
		/// Helper function for applying all needed conversions for converting value to database type.
		/// </summary>
		/// <param name="mappingSchema">Mapping schema.</param>
		/// <param name="getterExpr">Expression which returns value which has to be converted.</param>
		/// <param name="dbDataType">Database type.</param>
		/// <param name="valueConverter">Optional <see cref="IValueConverter"/></param>
		/// <param name="includingEnum">Provides default enum conversion.</param>
		/// <returns>Expression with applied conversions.</returns>
		public static Expression ApplyConversions(MappingSchema mappingSchema, Expression getterExpr, DbDataType dbDataType, IValueConverter? valueConverter, bool includingEnum)
		{
			// search for type preparation converter
			var prepareConverter = mappingSchema.GetConvertExpression(getterExpr.Type, getterExpr.Type, false, false);

			if (prepareConverter != null)
				getterExpr = InternalExtensions.ApplyLambdaToExpression(prepareConverter, getterExpr);

			if (valueConverter != null)
			{
				var toProvider = valueConverter.ToProviderExpression;
				if (!valueConverter.HandlesNulls)
					toProvider = mappingSchema.AddNullCheck(toProvider);
				getterExpr = InternalExtensions.ApplyLambdaToExpression(toProvider, getterExpr);
			}

			if (!getterExpr.Type.IsSameOrParentOf(typeof(DataParameter)))
			{
				var convertLambda = mappingSchema.GetConvertExpression(
					dbDataType.WithSystemType(getterExpr.Type),
					dbDataType.WithSystemType(typeof(DataParameter)), createDefault: false);

				if (convertLambda != null)
				{
					getterExpr = InternalExtensions.ApplyLambdaToExpression(convertLambda, getterExpr);
				}
				else
				{
					if (valueConverter == null && includingEnum)
					{
						var type = Converter.GetDefaultMappingFromEnumType(mappingSchema, getterExpr.Type);
						if (type != null)
						{
							var enumConverter = mappingSchema.GetConvertExpression(getterExpr.Type, type)!;
							getterExpr = InternalExtensions.ApplyLambdaToExpression(enumConverter, getterExpr);

							convertLambda = mappingSchema.GetConvertExpression(
								dbDataType.WithSystemType(getterExpr.Type),
								dbDataType.WithSystemType(typeof(DataParameter)), createDefault: false);

							if (convertLambda != null)
							{
								getterExpr = InternalExtensions.ApplyLambdaToExpression(convertLambda, getterExpr);
							}
						}
					}
				}
			}

			return getterExpr;
		}

		/// <summary>
		/// Helper function for applying all needed conversions for converting value to database type.
		/// </summary>
		/// <param name="getterExpr">Expression which returns value which has to be converted.</param>
		/// <param name="dbDataType">Database type.</param>
		/// <param name="includingEnum">Provides default enum conversion.</param>
		/// <returns>Expression with applied conversions.</returns>
		public Expression ApplyConversions(Expression getterExpr, DbDataType dbDataType, bool includingEnum)
		{
			return ApplyConversions(MappingSchema, getterExpr, dbDataType, ValueConverter, includingEnum);
		}

		/// <summary>
		/// Extracts column value, converted to database type, from entity object.
		/// </summary>
		/// <param name="obj">Entity object to extract column value from.</param>
		/// <returns>Returns column value, converted to database type.</returns>
		public virtual object? GetValue(object obj)
		{
			if (_getter == null)
			{
				var objParam      = Expression.Parameter(typeof(object), "obj");
				var objExpression = Expression.Convert(objParam, MemberAccessor.TypeAccessor.Type);

				var getterExpr    = InternalExtensions.ApplyLambdaToExpression(GetDbValueLambda(), objExpression);

				var getterLambda = Expression.Lambda<Func<object,object>>(Expression.Convert(getterExpr, typeof(object)), objParam);
				_getter = getterLambda.Compile();
			}

			return _getter(obj);
		}
	}
}
