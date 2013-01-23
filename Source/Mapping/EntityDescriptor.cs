using System;
using System.Collections.Generic;
using System.Reflection;

namespace LinqToDB.Mapping
{
	class EntityDescriptor
	{
		public EntityDescriptor(MappingSchema mappingSchema, Type type)
		{
			_mappingSchema = mappingSchema;
			_type          = type;

			Associations = new List<AssociationDescriptor>();
			Columns      = new List<ColumnDescriptior>();

			Init();
		}

		void Init()
		{
			var ta = _mappingSchema.GetAttribute<TableAttribute>(_type, a => a.Configuration);

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

				var aa = _mappingSchema.GetAttribute<AssociationAttribute>(memberInfo, attr => attr.Configuration);

				if (aa != null)
				{
					Associations.Add(new AssociationDescriptor(_type, memberInfo, aa.GetThisKeys(), aa.GetOtherKeys(), aa.Storage, aa.CanBeNull));
					continue;
				}

				var ca = _mappingSchema.GetAttribute<ColumnAttribute>(memberInfo, attr => attr.Configuration);

				if (ca != null)
				{
					if (ca.IsColumn)
						Columns.Add(new ColumnDescriptior(memberInfo)
						{
							ColumnName = ca.Name ?? memberInfo.Name,
							Storage    = ca.Storage,
						});
				}
				else if (!ta.IsColumnAttributeRequired && _mappingSchema.IsScalarType(memberType))
					Columns.Add(new ColumnDescriptior(memberInfo));
			}
		}

		readonly MappingSchema _mappingSchema;
		readonly Type          _type;

		public List<ColumnDescriptior>     Columns      { get; private set; }
		public List<AssociationDescriptor> Associations { get; private set; }
	}
}
