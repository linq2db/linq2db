using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

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
		public List<InheritanceMapping>    InheritanceMapping        { get; private set; }

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

			var attrs = new List<ColumnAttribute>();

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
					{
						if (ca.MemberName != null)
						{
							attrs.Add(new ColumnAttribute(member.Name, ca));
						}
						else
						{
							var cd = new ColumnDescriptor(_mappingSchema, ca, member);
							Columns.Add(cd);
							_columnNames.Add(member.Name, cd);
						}
					}
				}
				else if (!IsColumnAttributeRequired && _mappingSchema.IsScalarType(member.Type))
				{
					var cd = new ColumnDescriptor(_mappingSchema, new ColumnAttribute(), member);
					Columns.Add(cd);
					_columnNames.Add(member.Name, cd);
				}
			}

			var typeColumnAttrs = _mappingSchema.GetAttributes<ColumnAttribute>(TypeAccessor.Type, a => a.Configuration);

			foreach (var attr in typeColumnAttrs.Concat(attrs))
				if (attr.IsColumn)
					SetColumn(attr);
		}

		void SetColumn(ColumnAttribute attr)
		{
			if (attr.MemberName == null)
				throw new LinqToDBException(string.Format("The Column attribute of the '{0}' type must have MemberName.", TypeAccessor.Type));

			if (attr.MemberName.IndexOf('.') < 0)
			{
				var ex = TypeAccessor[attr.MemberName];

				if (ex == null)
					throw new LinqToDBException(string.Format("The '{0}' type does not have '{1}' member.", TypeAccessor.Type, attr.MemberName));

				var cd = new ColumnDescriptor(_mappingSchema, attr, ex);

				Columns.Add(cd);
				_columnNames.Add(attr.MemberName, cd);
			}
			else
			{
				var cd = new ColumnDescriptor(_mappingSchema, attr, new MemberAccessor(TypeAccessor, _mappingSchema, attr.MemberName));

				Columns.Add(cd);
				_columnNames.Add(attr.MemberName, cd);
			}
		}

		readonly Dictionary<string,ColumnDescriptor> _columnNames = new Dictionary<string, ColumnDescriptor>();

		public ColumnDescriptor this[string memberName]
		{
			get
			{
				ColumnDescriptor cd;
				_columnNames.TryGetValue(memberName, out cd);
				return cd;
			}
		}

		internal void InitInheritanceMapping()
		{
			var mappingAttrs = _mappingSchema.GetAttributes<InheritanceMappingAttribute>(ObjectType, a => a.Configuration, false);

			InheritanceMapping = new List<InheritanceMapping>(mappingAttrs.Length);

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

					var ed = _mappingSchema.GetEntityDescriptor(mapping.Type);

					foreach (var column in ed.Columns)
					{
						if (Columns.All(f => f.MemberName != column.MemberName))
							Columns.Add(column);

						if (column.IsDiscriminator)
							mapping.Discriminator = column;
					}

					InheritanceMapping.Add(mapping);
				}

				var discriminator = InheritanceMapping.Select(m => m.Discriminator).FirstOrDefault(d => d != null);

				if (discriminator == null)
					throw new LinqException("Inheritance Discriminator is not defined for the '{0}' hierarchy.", ObjectType);

				foreach (var mapping in InheritanceMapping)
					if (mapping.Discriminator == null)
						mapping.Discriminator = discriminator;
			}
		}
	}
}
