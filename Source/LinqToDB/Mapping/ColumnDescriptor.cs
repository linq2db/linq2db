using System;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.Mapping;
using LinqToDB.Reflection;
using LinqToDB.SqlQuery;

namespace LinqToDB.Mapping
{
	/// <summary>
	/// Stores mapping entity column descriptor.
	/// </summary>
	[DebuggerDisplay("Member:{MemberName}, Column:'{ColumnName}'")]
	public class ColumnDescriptor : IColumnChangeDescriptor
	{
		/// <summary>
		/// Creates descriptor instance.
		/// </summary>
		/// <param name="mappingSchema">Mapping schema, associated with descriptor.</param>
		/// <param name="entityDescriptor">Entity descriptor.</param>
		/// <param name="columnAttribute">Column attribute, from which descriptor data should be extracted.</param>
		/// <param name="memberAccessor">Column mapping member accessor.</param>
		/// <param name="hasInheritanceMapping">Owning entity included in inheritance mapping.</param>
		public ColumnDescriptor(MappingSchema mappingSchema, EntityDescriptor entityDescriptor, ColumnAttribute? columnAttribute, MemberAccessor memberAccessor, bool hasInheritanceMapping)
		{
			MappingSchema         = mappingSchema;
			EntityDescriptor      = entityDescriptor;
			MemberAccessor        = memberAccessor;
			MemberInfo            = memberAccessor.MemberInfo;
			HasInheritanceMapping = hasInheritanceMapping;

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
			else
				throw new LinqToDBException($"Column should be mapped to property of field. Was: {MemberInfo.GetType()}.");

			var dataType = mappingSchema.GetDataType(MemberType);
			if (dataType.Type.DataType == DataType.Undefined)
				dataType = mappingSchema.GetUnderlyingDataType(MemberType, out var _);

			MemberName        = columnAttribute?.MemberName ?? MemberInfo.Name;
			ColumnName        = columnAttribute?.Name       ?? MemberInfo.Name;
			Storage           = columnAttribute?.Storage;
			PrimaryKeyOrder   = columnAttribute?.PrimaryKeyOrder   ?? 0;
			IsDiscriminator   = columnAttribute?.IsDiscriminator   ?? false;
			SkipOnEntityFetch = columnAttribute?.SkipOnEntityFetch ?? false;
			DataType          = columnAttribute != null ? columnAttribute.DataType : dataType.Type.DataType;
			DbType            = columnAttribute != null ? columnAttribute.DbType   : dataType.Type.DbType;
			CreateFormat      = columnAttribute?.CreateFormat;

			if (columnAttribute == null || columnAttribute.DataType == DataType.Undefined || columnAttribute.DataType == dataType.Type.DataType)
			{
				Length    = columnAttribute?.HasLength()    != true ? dataType.Type.Length    : columnAttribute.Length;
				Precision = columnAttribute?.HasPrecision() != true ? dataType.Type.Precision : columnAttribute.Precision;
				Scale     = columnAttribute?.HasScale()     != true ? dataType.Type.Scale     : columnAttribute.Scale;
			}
			else
			{
				if (columnAttribute.HasLength())    Length    = columnAttribute.Length;
				if (columnAttribute.HasPrecision()) Precision = columnAttribute.Precision;
				if (columnAttribute.HasScale())     Scale     = columnAttribute.Scale;
			}

			if (columnAttribute?.HasOrder() == true)
				Order = columnAttribute.Order;

			if (Storage == null)
			{
				StorageType = MemberType;
				StorageInfo = MemberInfo;
			}
			else
			{
				var expr = ExpressionHelper.PropertyOrField(Expression.Constant(null, MemberInfo.DeclaringType!), Storage);
				StorageType = expr.Type;
				StorageInfo = expr.Member;
			}

			if (columnAttribute?.HasIsIdentity() == true)
			{
				IsIdentity = columnAttribute.IsIdentity;
			}
			else if (MemberName.IndexOf(".") < 0)
			{
				var hasIdentity = mappingSchema.HasAttribute<IdentityAttribute>(MemberAccessor.TypeAccessor.Type, MemberInfo);
				if (hasIdentity)
					IsIdentity = true;
			}

			SequenceName = mappingSchema.GetAttribute<SequenceNameAttribute>(memberAccessor.TypeAccessor.Type, MemberInfo);

			if (SequenceName != null)
				IsIdentity = true;

			SkipOnInsert = columnAttribute?.HasSkipOnInsert() == true ? columnAttribute.SkipOnInsert : IsIdentity;
			SkipOnUpdate = columnAttribute?.HasSkipOnUpdate() == true ? columnAttribute.SkipOnUpdate : IsIdentity;

			CanBeNull = AnalyzeCanBeNull(columnAttribute);

			if (columnAttribute?.HasIsPrimaryKey() == true)
				IsPrimaryKey = columnAttribute.IsPrimaryKey;
			else if (MemberName.IndexOf(".") < 0)
			{
				var a = mappingSchema.GetAttribute<PrimaryKeyAttribute>(MemberAccessor.TypeAccessor.Type, MemberInfo);

				if (a != null)
				{
					IsPrimaryKey    = true;
					PrimaryKeyOrder = a.Order;
				}
			}

			if (DbType == null || DataType == DataType.Undefined)
			{
				var a = mappingSchema.GetAttribute<DataTypeAttribute>(MemberAccessor.TypeAccessor.Type, MemberInfo);

				if (a != null)
				{
					DbType ??= a.DbType;

					if (DataType == DataType.Undefined && a.DataType.HasValue)
						DataType = a.DataType.Value;
				}
			}

			var vc = mappingSchema.GetAttribute<ValueConverterAttribute>(memberAccessor.TypeAccessor.Type, MemberInfo);
			if (vc != null)
			{
				ValueConverter = vc.GetValueConverter(this);
			}

			var skipValueAttributes = mappingSchema.GetAttributes<SkipBaseAttribute>(MemberAccessor.TypeAccessor.Type, MemberInfo);
			if (skipValueAttributes.Length > 0)
			{
				SkipBaseAttributes    = skipValueAttributes;
				SkipModificationFlags = SkipBaseAttributes.Aggregate(SkipModification.None, (s, c) => s | c.Affects);
			}
		}

		private bool AnalyzeCanBeNull(ColumnAttribute? columnAttribute)
		{
			if (columnAttribute?.HasCanBeNull() == true)
				return columnAttribute.CanBeNull;

			var na = MappingSchema.GetAttribute<NullableAttribute>(MemberAccessor.TypeAccessor.Type, MemberInfo);
			if (na != null)
				return na.CanBeNull;

			if (Common.Configuration.UseNullableTypesMetadata && Nullability.TryAnalyzeMember(MemberInfo, out var isNullable))
				return isNullable;

			if (IsIdentity)
				return false;

			return MappingSchema.GetCanBeNull(MemberType);
		}

		/// <summary>
		/// Gets MappingSchema for current ColumnDescriptor.
		/// </summary>
		public MappingSchema  MappingSchema   { get; }

		/// <summary>
		/// Gets Entity descriptor.
		/// </summary>
		public EntityDescriptor EntityDescriptor { get; }

		/// <summary>
		/// Gets column mapping member accessor.
		/// </summary>
		public MemberAccessor MemberAccessor  { get; }

		/// <summary>
		/// Indicates that owning entity included in inheritance mapping.
		/// </summary>
		public bool HasInheritanceMapping { get; }

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
		public Type           MemberType      { get; }

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
		/// <see cref="DataExtensions.Insert{T}(IDataContext, T, string?, string?, string?, string?, TableOptions)"/>
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
		/// <see cref="DataExtensions.Update{T}(IDataContext, T, string?, string?, string?, string?, TableOptions)"/>
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
		/// <see cref="DataExtensions.CreateTable{T}(IDataContext, string?, string?, string?, string?, string?, DefaultNullable, string?, TableOptions)"/> methods.
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
		LambdaExpression?    _getOriginalValueLambda;

		LambdaExpression?    _getDbValueLambda;
		Expression?          _getDefaultDbValueExpression;
		LambdaExpression?    _getDbParamLambda;
		Expression?          _getDefaultDbParamExpression;

		Func<object,object>? _getter;

		/// <summary>
		/// Returns DbDataType for current column.
		/// </summary>
		/// <returns></returns>
		public DbDataType GetDbDataType(bool completeDataType)
		{
			var systemType         = MemberType;
			var dataType           = DataType;
			DbDataType? dbDataType = null;

			if (completeDataType && dataType == DataType.Undefined)
				dbDataType = CalculateDbDataType(MappingSchema, systemType);

			return new DbDataType(systemType, dbDataType?.DataType ?? dataType, DbType ?? dbDataType?.DbType, Length ?? dbDataType?.Length, Precision ?? dbDataType?.Precision, Scale ?? dbDataType?.Scale);
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
				{
					var enumType = systemType.UnwrapNullableType();
					if (enumType.IsEnum)
					{
						var type = Converter.GetDefaultMappingFromEnumType(MappingSchema, dbDataType.SystemType);
						if (type != null)
						{
							systemType = type;
						}
					}
				}
			}

			if (dbDataType.SystemType != systemType)
				dbDataType = dbDataType.WithSystemType(systemType);

			return dbDataType;
		}

		public static DbDataType CalculateDbDataType(MappingSchema mappingSchema, Type systemType)
		{
			DbDataType dbDataType = default;

			if (systemType.UnwrapNullableType().IsEnum)
			{
				var enumType = mappingSchema.GetDefaultFromEnumType(systemType);

				if (enumType != null)
					dbDataType = mappingSchema.GetDataType(enumType).Type;

				if (dbDataType.DataType == DataType.Undefined && systemType.IsNullableType)
				{
					enumType = mappingSchema.GetDefaultFromEnumType(systemType.UnwrapNullableType());

					if (enumType != null)
						dbDataType = mappingSchema.GetDataType(enumType).Type;
				}

				if (dbDataType.DataType == DataType.Undefined)
				{
					enumType = mappingSchema.GetDefaultFromEnumType(typeof(Enum));

					if (enumType != null)
						dbDataType = mappingSchema.GetDataType(enumType).Type;
				}

				if (dbDataType.DataType == DataType.Undefined)
					dbDataType = mappingSchema.GetUnderlyingDataType(systemType, out var canBeNull).Type;
			}

			if (dbDataType.DataType == DataType.Undefined)
				dbDataType = mappingSchema.GetDataType(systemType).Type;
			if (dbDataType.DataType == DataType.Undefined)
				dbDataType = mappingSchema.GetUnderlyingDataType(systemType, out var _).Type;

			return dbDataType.WithSystemType(systemType);
		}

		/// <summary>
		/// Returns Lambda for extracting original column value from entity object.
		/// </summary>
		/// <returns>Returns Lambda which extracts member value.</returns>
		public LambdaExpression GetOriginalValueLambda()
		{
			if (_getOriginalValueLambda != null)
				return _getOriginalValueLambda;

			var objParam   = Expression.Parameter(MemberAccessor.TypeAccessor.Type, "obj");
			var getterExpr = MemberAccessor.GetGetterExpression(objParam);

			_getOriginalValueLambda = Expression.Lambda(getterExpr, objParam);
			return _getOriginalValueLambda;
		}

		/// <summary>
		/// Returns Lambda for extracting column value, converted to database type, from entity object.
		/// </summary>
		/// <returns>Returns Lambda which extracts member value to database type.</returns>
		public LambdaExpression GetDbValueLambda()
		{
			if (_getDbValueLambda != null)
				return _getDbValueLambda;

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

			_getDbValueLambda = paramGetter;
			return _getDbValueLambda;
		}

		/// <summary>
		/// Returns Lambda for extracting column value, converted to database type, from entity object.
		/// </summary>
		/// <returns>Returns Lambda which extracts member value to database type.</returns>
		public Expression GetDefaultDbValueExpression()
		{
			if (_getDefaultDbValueExpression != null)
				return _getDefaultDbValueExpression;

			var paramGetter = GetDefaultDbParamExpression();

			if (typeof(DataParameter).IsSameOrParentOf(paramGetter.Type))
			{
				var param             = Expression.Parameter(typeof(DataParameter), "p");
				var convertExpression = Expression.Lambda(ExpressionHelper.PropertyOrField(param, "Value"), param);

				convertExpression = MappingSchema.AddNullCheck(convertExpression);
				paramGetter       = InternalExtensions.ApplyLambdaToExpression(convertExpression, paramGetter);
			}

			_getDefaultDbValueExpression = paramGetter;
			return _getDefaultDbValueExpression;
		}

		/// <summary>
		/// Returns Lambda for extracting column value, converted to database type or <see cref="DataParameter"/>, from entity object.
		/// </summary>
		/// <returns>Returns Lambda which extracts member value to database type or <see cref="DataParameter"/>.</returns>
		public LambdaExpression GetDbParamLambda()
		{
			if (_getDbParamLambda != null)
				return _getDbParamLambda;

			var objParam   = Expression.Parameter(MemberAccessor.TypeAccessor.Type, "obj");
			var getterExpr = MemberAccessor.GetGetterExpression(objParam);
			var dbDataType = GetDbDataType(true);

			if (IsDiscriminator && MemberAccessor.HasSetter)
			{
				var param = Expression.Parameter(getterExpr.Type, "v");

				var current = EntityDescriptor.InheritanceRoot ?? EntityDescriptor;

				if (current.InheritanceMapping.Count > 0)
				{
					// suggest default discriminator value

					var defaultValue = current.InheritanceMapping.FirstOrDefault(m => m.IsDefault);
					var valueExpr = MappingSchema.GenerateConvertedValueExpression(defaultValue?.Code, param.Type);

					for (var index = current.InheritanceMapping.Count - 1; index >= 0; index--)
					{
						var mapping = current.InheritanceMapping[index];
						valueExpr = Expression.Condition(Expression.TypeIs(objParam, mapping.Type),
							MappingSchema.GenerateConvertedValueExpression(mapping.Code, param.Type), valueExpr);
					}

					Expression defaultCheckExpression = param.Type == typeof(string)
						? Expression.Call(typeof(string), nameof(string.IsNullOrEmpty), Type.EmptyTypes, param)
						: Expression.Equal(param, new DefaultValueExpression(MappingSchema, param.Type));

					var suggestLambda = Expression.Lambda(
						Expression.Condition(
							defaultCheckExpression,
							valueExpr,
							param
						),
						param
					);

					getterExpr = InternalExtensions.ApplyLambdaToExpression(suggestLambda, getterExpr);
				}

			}

			getterExpr = ApplyConversions(getterExpr, dbDataType, true);

			_getDbParamLambda = Expression.Lambda(getterExpr, objParam);
			return _getDbParamLambda;
		}

		/// <summary>
		/// Returns default column value, converted to database type or <see cref="DataParameter"/>.
		/// </summary>
		public Expression GetDefaultDbParamExpression()
		{
			if (_getDefaultDbParamExpression != null)
				return _getDefaultDbParamExpression;

			Expression defaultExpression = new DefaultValueExpression(MappingSchema, MemberAccessor.Type);

			var dbDataType = GetDbDataType(true);

			defaultExpression = ApplyConversions(defaultExpression, dbDataType, true);

			_getDefaultDbParamExpression = defaultExpression;
			return _getDefaultDbParamExpression;
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
			var prepareConverter = mappingSchema.GetConvertExpression(getterExpr.Type, getterExpr.Type, false, false, ConversionType.ToDatabase);

			if (prepareConverter != null)
				getterExpr = InternalExtensions.ApplyLambdaToExpression(prepareConverter, getterExpr);

			if (valueConverter != null)
			{
				var toProvider = valueConverter.ToProviderExpression;
				var fromType   = toProvider.Parameters[0].Type;
				var assignable = fromType.IsAssignableFrom(getterExpr.Type);

				if (assignable || fromType.IsAssignableFrom(getterExpr.Type.UnwrapNullableType()))
				{
					if (!valueConverter.HandlesNulls)
					{
						toProvider = mappingSchema.AddNullCheck(toProvider);
					}

					if (!assignable)
					{
						var variable = Expression.Variable(getterExpr.Type);
						var assign   = Expression.Assign(variable, getterExpr);

						var resultType = toProvider.ReturnType.MakeNullable();

						var convert = InternalExtensions.ApplyLambdaToExpression(toProvider, ExpressionHelper.Property(variable, nameof(Nullable<>.Value)));
						if (resultType != toProvider.ReturnType)
							convert = Expression.Convert(convert, resultType);

						getterExpr = Expression.Block(
							new[] { variable },
							assign,
							Expression.Condition(
								ExpressionHelper.Property(variable, nameof(Nullable<>.HasValue)),
								convert,
								new DefaultValueExpression(mappingSchema, resultType)));
					}
					else
					{
						getterExpr = InternalExtensions.ApplyLambdaToExpression(toProvider, getterExpr);
					}
				}
			}

			if (!getterExpr.Type.IsSameOrParentOf(typeof(DataParameter)) || getterExpr.Type == typeof(object))
			{
				var convertLambda = mappingSchema.GetConvertExpression(
					dbDataType.WithSystemType(getterExpr.Type),
					dbDataType.WithSystemType(typeof(DataParameter)),
					createDefault  : false,
					conversionType : ConversionType.ToDatabase);

				if (convertLambda != null)
				{
					getterExpr = InternalExtensions.ApplyLambdaToExpression(convertLambda, getterExpr);
				}
				else
				{
					// For DataType.Enum we do not provide any additional conversion
					if (valueConverter == null && includingEnum && dbDataType.DataType != DataType.Enum && getterExpr.Type.UnwrapNullableType().IsEnum)
					{
						var type = dbDataType.SystemType != typeof(object) && !dbDataType.SystemType.UnwrapNullableType().IsEnum
							? dbDataType.SystemType
							: Converter.GetDefaultMappingFromEnumType(mappingSchema, getterExpr.Type);

						if (type != null)
						{
							var enumConverter = mappingSchema.GetConvertExpression(getterExpr.Type, type, conversionType: ConversionType.ToDatabase)!;
							getterExpr = InternalExtensions.ApplyLambdaToExpression(enumConverter, getterExpr);

							convertLambda = mappingSchema.GetConvertExpression(
								dbDataType.WithSystemType(getterExpr.Type),
								dbDataType.WithSystemType(typeof(DataParameter)),
								createDefault  : false,
								conversionType : ConversionType.ToDatabase);

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
		public virtual object? GetProviderValue(object obj)
		{
			if (_getter == null)
			{
				var objParam      = Expression.Parameter(typeof(object), "obj");

				var objExpression = Expression.Convert(objParam, MemberAccessor.TypeAccessor.Type);
				var getterExpr    = InternalExtensions.ApplyLambdaToExpression(GetDbValueLambda(), objExpression);

				if (HasInheritanceMapping)
				{
					// Additional check that column member belong to proper entity
					//
					getterExpr = Expression.Condition(Expression.TypeIs(objParam, MemberAccessor.TypeAccessor.Type),
						getterExpr, GetDefaultDbValueExpression());
				}

				var getterLambda = Expression.Lambda<Func<object, object>>(Expression.Convert(getterExpr, typeof(object)), objParam);

				_getter = getterLambda.CompileExpression();
			}

			return _getter(obj);
		}
	}
}
