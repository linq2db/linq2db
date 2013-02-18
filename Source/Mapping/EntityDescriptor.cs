using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqToDB.Mapping
{
	using Reflection;
	using Linq;

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

		public Type ObjectType { get { return TypeAccessor.Type; } }

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

		private List<InheritanceMapping> _inheritanceMapping;
		public  List<InheritanceMapping>  InheritanceMapping
		{
			get
			{
				if (_inheritanceMapping == null)
				{
					var mappingAttrs = _mappingSchema.GetAttributes<InheritanceMappingAttribute>(ObjectType, a => a.Configuration);

					_inheritanceMapping = new List<InheritanceMapping>(mappingAttrs.Length);

					if (mappingAttrs.Length > 0)
					{
						foreach (var m in mappingAttrs)
						{
							var mapping = new InheritanceMapping
							{
								Code      = m.Code,
								IsDefault = m.IsDefault,
								Type      = m.Type,
							};

							if (mapping.Type != ObjectType)
							{
								var ed = _mappingSchema.GetEntityDescriptor(mapping.Type);

								foreach (var column in ed.Columns)
								{
									if (Columns.All(f => f.MemberName != column.MemberName))
										Columns.Add(column);

									if (column.IsDiscriminator)
										mapping.Discriminator = column;
								}
							}

							_inheritanceMapping.Add(mapping);
						}

						var discriminator = _inheritanceMapping.Select(m => m.Discriminator).FirstOrDefault(d => d != null);

						if (discriminator == null)
							throw new LinqException("Inheritance Discriminator is not defined for the '{0}' hierarchy.", ObjectType);

						foreach (var mapping in _inheritanceMapping)
							if (mapping.Discriminator == null)
								mapping.Discriminator = discriminator;
					}
				}

				return _inheritanceMapping;
			}
		}
	}
}
