using System;
using System.Collections.Generic;

namespace LinqToDB.Mapping
{
	using Reflection;

	class EntityDescriptor
	{
		public EntityDescriptor(MappingSchema mappingSchema, Type type)
		{
			_mappingSchema = mappingSchema;

			TypeAccessor = TypeAccessor.GetAccessor(type);
			Associations = new List<AssociationDescriptor>();
			Columns      = new List<ColumnDescriptor>();

			Init();
		}

		readonly MappingSchema _mappingSchema;

		public TypeAccessor                TypeAccessor              { get; private set; }
		public string                      TableName                 { get; private set; }
		public string                      SchemaName                { get; private set; }
		public string                      DatabaseName              { get; private set; }
		public bool                        IsColumnAttributeRequired { get; private set; }
		public List<ColumnDescriptor>      Columns                   { get; private set; }
		public List<AssociationDescriptor> Associations              { get; private set; }

		void Init()
		{
			var ta = _mappingSchema.GetAttribute<TableAttribute>(TypeAccessor.Type, a => a.Configuration);

			if (ta == null)
			{
				TableName = TypeAccessor.Type.Name;

				if (TypeAccessor.Type.IsInterface && TableName.Length > 1 && TableName[0] == 'I')
					TableName = TableName.Substring(1);
			}
			else
			{
				TableName                 = ta.Name;
				SchemaName                = ta.Schema;
				DatabaseName              = ta.Database;
				IsColumnAttributeRequired = ta.IsColumnAttributeRequired;
			}

			foreach (var member in TypeAccessor.Members)
			{
				var aa = _mappingSchema.GetAttribute<AssociationAttribute>(member.MemberInfo, attr => attr.Configuration);

				if (aa != null)
				{
					Associations.Add(new AssociationDescriptor(
						TypeAccessor.Type, member.MemberInfo, aa.GetThisKeys(), aa.GetOtherKeys(), aa.Storage, aa.CanBeNull));
					continue;
				}

				var ca = _mappingSchema.GetAttribute<ColumnAttribute>(member.MemberInfo, attr => attr.Configuration);

				if (ca != null)
				{
					if (ca.IsColumn)
						Columns.Add(new ColumnDescriptor(_mappingSchema, ca, member.MemberInfo));
				}
				else if (!IsColumnAttributeRequired && _mappingSchema.IsScalarType(member.Type))
				{
					Columns.Add(new ColumnDescriptor(_mappingSchema, new ColumnAttribute(), member.MemberInfo));
				}
			}
		}
	}
}
