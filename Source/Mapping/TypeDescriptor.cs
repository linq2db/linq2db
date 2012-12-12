using System;
using System.Collections.Generic;
using System.Reflection;

namespace LinqToDB.Mapping
{
	public class TypeDescriptor
	{
		public TypeDescriptor(MappingSchema mappingSchema, Type type)
		{
			_mappingSchema = mappingSchema;
			_type          = type;

			Init();
		}

		void Init()
		{
			foreach (var memberInfo in _type.GetMembers(BindingFlags.Instance | BindingFlags.Public))
			{
				Type memberType;

				if (memberInfo.MemberType == MemberTypes.Field)
				{
					var fieldInfo = (FieldInfo)memberInfo;
					memberType = fieldInfo.FieldType;
				}
				else if (memberInfo.MemberType == MemberTypes.Property)
				{
					var propertyInfo = (PropertyInfo)memberInfo;
					memberType = propertyInfo.PropertyType;
				}
				else
					continue;

				if (!_mappingSchema.IsScalarType(memberType))
					continue;

				_members.Add(new MemberDescriptior(memberInfo));
			}
		}

		readonly MappingSchema _mappingSchema;
		readonly Type          _type;

		readonly List<MemberDescriptior> _members = new List<MemberDescriptior>();
		public   List<MemberDescriptior>  Members
		{
			get { return _members; }
		}
	}
}
