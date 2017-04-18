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

	public class ColumnDescriptor
	{
		public ColumnDescriptor(MappingSchema mappingSchema, ColumnAttribute columnAttribute, MemberAccessor memberAccessor)
		{
			MappingSchema  = mappingSchema;
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
				IsIdentity = columnAttribute.IsIdentity;
			else
			{
				var a = mappingSchema.GetAttribute<IdentityAttribute>(MemberAccessor.TypeAccessor.Type, MemberInfo, attr => attr.Configuration);
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

		public MappingSchema  MappingSchema   { get; private set; }
		public MemberAccessor MemberAccessor  { get; private set; }
		public MemberInfo     MemberInfo      { get; private set; }
		public MemberInfo     StorageInfo     { get; private set; }
		public Type           MemberType      { get; private set; }
		public Type           StorageType     { get; private set; }
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
		public int?           Length          { get; private set; }
		public int?           Precision       { get; private set; }
		public int?           Scale           { get; private set; }
		public string         CreateFormat    { get; private set; }

		Func<object,object> _getter;

		public virtual object GetValue(object obj)
		{
			if (_getter == null)
			{
				var objParam   = Expression.Parameter(typeof(object), "obj");
				var getterExpr = MemberAccessor.GetterExpression.GetBody(Expression.Convert(objParam, MemberAccessor.TypeAccessor.Type));

				var expr = MappingSchema.GetConvertExpression(MemberType, typeof(DataParameter), createDefault : false);

				if (expr != null)
				{
					getterExpr = Expression.PropertyOrField(expr.GetBody(getterExpr), "Value");
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
