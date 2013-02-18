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

			MemberName = MemberInfo.Name;
			ColumnName = columnAttribute.Name ?? MemberInfo.Name;
			Storage    = columnAttribute.Storage;

		}

		public MemberInfo MemberInfo { get; private set; }
		public Type       MemberType { get; private set; }
		public string     MemberName { get; private set; }
		public string     ColumnName { get; private set; }
		public string     Storage    { get; private set; }
	}
}
