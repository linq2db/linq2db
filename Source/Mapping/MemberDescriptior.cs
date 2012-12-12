using System;
using System.Reflection;

namespace LinqToDB.Mapping
{
	public class MemberDescriptior
	{
		public MemberDescriptior(MemberInfo memberInfo)
		{
			_memberInfo = memberInfo;

			if (memberInfo.MemberType == MemberTypes.Field)
			{
				var fieldInfo = (FieldInfo)memberInfo;
				_memberType = fieldInfo.FieldType;
			}
			else if (memberInfo.MemberType == MemberTypes.Property)
			{
				var propertyInfo = (PropertyInfo)memberInfo;
				_memberType = propertyInfo.PropertyType;
			}
		}

		readonly MemberInfo _memberInfo;
		public   MemberInfo  MemberInfo
		{
			get { return _memberInfo; }
		}

		public string MemberName
		{
			get { return _memberInfo.Name; }
		}

		public string ColumnName
		{
			get { return _memberInfo.Name; }
		}

		readonly Type _memberType;
		public   Type  MemberType
		{
			get { return _memberType; }
		}
	}
}
