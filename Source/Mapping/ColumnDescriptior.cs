using System;
using System.Reflection;

namespace LinqToDB.Mapping
{
	class ColumnDescriptior
	{
		public ColumnDescriptior(MemberInfo memberInfo)
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
			ColumnName = MemberInfo.Name;
		}

		public MemberInfo MemberInfo { get; private set; }
		public Type       MemberType { get; private set; }
		public string     MemberName { get; set; }
		public string     ColumnName { get; set; }
		public string     Storage    { get; set; }
	}
}
