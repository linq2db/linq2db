using System;
using System.Reflection;

namespace LinqToDB.Mapping
{
	class ColumnDescriptor
	{
		public ColumnDescriptor(MappingSchema mappingSchema, ColumnAttribute columnAttribute, MemberInfo memberInfo)
		{
			MemberInfo = memberInfo;

			if (memberInfo.MemberType == MemberTypes.Field)
			{
				var fieldInfo = (FieldInfo)memberInfo;
				MemberType = fieldInfo.FieldType;
			}
			else if (memberInfo.MemberType == MemberTypes.Property)
			{
				var propertyInfo = (PropertyInfo)memberInfo;
				MemberType = propertyInfo.PropertyType;
			}

			MemberName      = MemberInfo.Name;
			ColumnName      = columnAttribute.Name ?? MemberInfo.Name;
			Storage         = columnAttribute.Storage;
			IsDiscriminator = columnAttribute.IsDiscriminator;
			DataType        = columnAttribute.DataType;
			DbType          = columnAttribute.DbType;
			IsIdentity      = columnAttribute.IsIdentity;
			SkipOnInsert    = columnAttribute.GetSkipOnInsert() ?? IsIdentity;
			SkipOnUpdate    = columnAttribute.GetSkipOnUpdate() ?? IsIdentity;
			PrimaryKeyOrder = columnAttribute.PrimaryKeyOrder;
			IsPrimaryKey    = columnAttribute.IsPrimaryKey || PrimaryKeyOrder >= 0;
			CanBeNull       = columnAttribute.GetCanBeNull() ?? mappingSchema.GetCanBeNull(MemberType);
		}

		public MemberInfo MemberInfo      { get; private set; }
		public Type       MemberType      { get; private set; }
		public string     MemberName      { get; private set; }
		public string     ColumnName      { get; private set; }
		public string     Storage         { get; private set; }
		public bool       IsDiscriminator { get; private set; }
		public DataType   DataType        { get; private set; }
		public string     DbType          { get; private set; }
		public bool       IsIdentity      { get; private set; }
		public bool       SkipOnInsert    { get; private set; }
		public bool       SkipOnUpdate    { get; private set; }
		public bool       IsPrimaryKey    { get; private set; }
		public int        PrimaryKeyOrder { get; private set; }
		public bool       CanBeNull       { get; private set; }
	}
}
