using System;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Mapping
{
	using Common;
	using Data;
	using Expressions;
	using Reflection;

	class ColumnDescriptor
	{
		public ColumnDescriptor(MappingSchema mappingSchema, ColumnAttribute columnAttribute, MemberAccessor memberAccessor)
		{
			MappingSchema  = mappingSchema;
			MemberAccessor = memberAccessor;
			MemberInfo     = memberAccessor.MemberInfo;

			if (MemberInfo.MemberType == MemberTypes.Field)
			{
				var fieldInfo = (FieldInfo)MemberInfo;
				MemberType = fieldInfo.FieldType;
			}
			else if (MemberInfo.MemberType == MemberTypes.Property)
			{
				var propertyInfo = (PropertyInfo)MemberInfo;
				MemberType = propertyInfo.PropertyType;
			}

			MemberName      = columnAttribute.MemberName ?? MemberInfo.Name;
			ColumnName      = columnAttribute.Name       ?? MemberInfo.Name;
			Storage         = columnAttribute.Storage;
			PrimaryKeyOrder = columnAttribute.PrimaryKeyOrder;
			IsDiscriminator = columnAttribute.IsDiscriminator;
			DataType        = columnAttribute.DataType;
			DbType          = columnAttribute.DbType;

			var defaultCanBeNull = false;

			if (columnAttribute.HasCanBeNull())
				CanBeNull = columnAttribute.CanBeNull;
			else
			{
				var na = mappingSchema.GetAttribute<NullableAttribute>(MemberInfo, attr => attr.Configuration);

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
				IsIdentity = columnAttribute.IsIdentity;
			else
			{
				var a = mappingSchema.GetAttribute<IdentityAttribute>(MemberInfo, attr => attr.Configuration);
				if (a != null)
					IsIdentity = true;
			}

			SkipOnInsert = columnAttribute.HasSkipOnInsert() ? columnAttribute.SkipOnInsert : IsIdentity;
			SkipOnUpdate = columnAttribute.HasSkipOnUpdate() ? columnAttribute.SkipOnUpdate : IsIdentity;

			if (defaultCanBeNull && IsIdentity)
				CanBeNull = false;

			if (columnAttribute.HasIsPrimaryKey())
				IsPrimaryKey = columnAttribute.IsPrimaryKey;
			else
			{
				var a = mappingSchema.GetAttribute<PrimaryKeyAttribute>(MemberInfo, attr => attr.Configuration);

				if (a != null)
				{
					IsPrimaryKey    = true;
					PrimaryKeyOrder = a.Order;
				}
			}
		}

		public MappingSchema  MappingSchema   { get; private set; }
		public MemberAccessor MemberAccessor  { get; private set; }
		public MemberInfo     MemberInfo      { get; private set; }
		public Type           MemberType      { get; private set; }
		public string         MemberName      { get; private set; }
		public string         ColumnName      { get; private set; }
		public string         Storage         { get; private set; }
		public bool           IsDiscriminator { get; private set; }
		public DataType       DataType        { get; private set; }
		public string         DbType          { get; private set; }
		public bool           IsIdentity      { get; private set; }
		public bool           SkipOnInsert    { get; private set; }
		public bool           SkipOnUpdate    { get; private set; }
		public bool           IsPrimaryKey    { get; private set; }
		public int            PrimaryKeyOrder { get; private set; }
		public bool           CanBeNull       { get; private set; }

		Func<object,object> _getter;

		public virtual object GetValue(object obj)
		{
			if (_getter == null)
			{
				var objParam   = Expression.Parameter(typeof(object), "obj");
				var getterExpr = MemberAccessor.Getter.GetBody(Expression.Convert(objParam, MemberAccessor.TypeAccessor.Type));

				var expr = MappingSchema.GetConvertExpression(MemberType, typeof(DataParameter), createDefault : false);

				if (expr != null)
				{
					getterExpr = expr.GetBody(getterExpr);
				}
				else
				{
					var type = Converter.GetDefaultMappingFromEnumType(MappingSchema, MemberType);

					if (type != null)
					{
						expr = MappingSchema.GetConvertExpression(MemberType, type);
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
