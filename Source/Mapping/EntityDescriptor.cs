﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqToDB.Mapping
{
	using Common;
	using Extensions;
	using Linq;
	using Reflection;

	public class EntityDescriptor
	{
		public EntityDescriptor(MappingSchema mappingSchema, Type type)
		{
			TypeAccessor = TypeAccessor.GetAccessor(type);
			Associations = new List<AssociationDescriptor>();
			Columns      = new List<ColumnDescriptor>();

			Init(mappingSchema);
			InitInheritanceMapping(mappingSchema);
		}

		public TypeAccessor                TypeAccessor              { get; private set; }
		public string                      TableName                 { get; private set; }
		public string                      SchemaName                { get; private set; }
		public string                      DatabaseName              { get; private set; }
		public bool                        IsColumnAttributeRequired { get; private set; }
		public List<ColumnDescriptor>      Columns                   { get; private set; }
		public List<AssociationDescriptor> Associations              { get; private set; }
		public Dictionary<string,string>   Aliases                   { get; private set; }

		private List<InheritanceMapping> _inheritanceMappings;
		public  List<InheritanceMapping>  InheritanceMapping
		{
			get
			{
				return _inheritanceMappings;
			}
		}

		public Type ObjectType { get { return TypeAccessor.Type; } }

		void Init(MappingSchema mappingSchema)
		{
			var ta = mappingSchema.GetAttribute<TableAttribute>(TypeAccessor.Type, a => a.Configuration);

			if (ta != null)
			{
				TableName                 = ta.Name;
				SchemaName                = ta.Schema;
				DatabaseName              = ta.Database;
				IsColumnAttributeRequired = ta.IsColumnAttributeRequired;
			}

			if (TableName == null)
			{
				TableName = TypeAccessor.Type.Name;

				if (TypeAccessor.Type.IsInterfaceEx() && TableName.Length > 1 && TableName[0] == 'I')
					TableName = TableName.Substring(1);
			}

			var attrs = new List<ColumnAttribute>();

			foreach (var member in TypeAccessor.Members)
			{
				var aa = mappingSchema.GetAttribute<AssociationAttribute>(TypeAccessor.Type, member.MemberInfo, attr => attr.Configuration);

				if (aa != null)
				{
					Associations.Add(new AssociationDescriptor(
						TypeAccessor.Type, member.MemberInfo, aa.GetThisKeys(), aa.GetOtherKeys(), aa.Storage, aa.CanBeNull));
					continue;
				}

				var ca = mappingSchema.GetAttribute<ColumnAttribute>(TypeAccessor.Type, member.MemberInfo, attr => attr.Configuration);

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
							var cd = new ColumnDescriptor(mappingSchema, ca, member);
							Columns.Add(cd);
							_columnNames.Add(member.Name, cd);
						}
					}
				}
				else if (
					!IsColumnAttributeRequired && mappingSchema.IsScalarType(member.Type) ||
					mappingSchema.GetAttribute<IdentityAttribute>(TypeAccessor.Type, member.MemberInfo, attr => attr.Configuration) != null ||
					mappingSchema.GetAttribute<PrimaryKeyAttribute>(TypeAccessor.Type, member.MemberInfo, attr => attr.Configuration) != null)
				{
					var cd = new ColumnDescriptor(mappingSchema, new ColumnAttribute(), member);
					Columns.Add(cd);
					_columnNames.Add(member.Name, cd);
				}
				else
				{
					var caa = mappingSchema.GetAttribute<ColumnAliasAttribute>(TypeAccessor.Type, member.MemberInfo, attr => attr.Configuration);

					if (caa != null)
					{
						if (Aliases == null)
							Aliases = new Dictionary<string,string>();

						Aliases.Add(member.Name, caa.MemberName);
					}
				}
			}

			var typeColumnAttrs = mappingSchema.GetAttributes<ColumnAttribute>(TypeAccessor.Type, a => a.Configuration);

			foreach (var attr in typeColumnAttrs.Concat(attrs))
				if (attr.IsColumn)
					SetColumn(attr, mappingSchema);
		}

		void SetColumn(ColumnAttribute attr, MappingSchema mappingSchema)
		{
			if (attr.MemberName == null)
				throw new LinqToDBException("The Column attribute of the '{0}' type must have MemberName.".Args(TypeAccessor.Type));

			if (attr.MemberName.IndexOf('.') < 0)
			{
				var ex = TypeAccessor[attr.MemberName];
				var cd = new ColumnDescriptor(mappingSchema, attr, ex);

				Columns.Add(cd);
				_columnNames.Add(attr.MemberName, cd);
			}
			else
			{
				var cd = new ColumnDescriptor(mappingSchema, attr, new MemberAccessor(TypeAccessor, attr.MemberName));

				if (!string.IsNullOrWhiteSpace(attr.MemberName))
				{
					Columns.Add(cd);
					_columnNames.Add(attr.MemberName, cd);
				}
			}
		}

		readonly Dictionary<string,ColumnDescriptor> _columnNames = new Dictionary<string, ColumnDescriptor>();

		public ColumnDescriptor this[string memberName]
		{
			get
			{
				ColumnDescriptor cd;

				if (!_columnNames.TryGetValue(memberName, out cd))
				{
					string alias;

					if (Aliases != null && Aliases.TryGetValue(memberName, out alias) && memberName != alias)
						return this[alias];
				}

				return cd;
			}
		}

		internal void InitInheritanceMapping(MappingSchema mappingSchema)
		{
			var mappingAttrs = mappingSchema.GetAttributes<InheritanceMappingAttribute>(ObjectType, a => a.Configuration, false);
			var result       = new List<InheritanceMapping>(mappingAttrs.Length);

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

					var ed = mapping.Type.Equals(ObjectType)
						? this
						: mappingSchema.GetEntityDescriptor(mapping.Type);

					//foreach (var column in this.Columns)
					//{
					//	if (ed.Columns.All(f => f.MemberName != column.MemberName))
					//		ed.Columns.Add(column);
					//}

					foreach (var column in ed.Columns)
					{
						if (Columns.All(f => f.MemberName != column.MemberName))
							Columns.Add(column);

						if (column.IsDiscriminator)
							mapping.Discriminator = column;
					}

					mapping.Discriminator = mapping.Discriminator ?? this.Columns.FirstOrDefault(x => x.IsDiscriminator);

					result.Add(mapping);
				}

				var discriminator = result.Select(m => m.Discriminator).FirstOrDefault(d => d != null);

				if (discriminator == null)
					throw new LinqException("Inheritance Discriminator is not defined for the '{0}' hierarchy.", ObjectType);

				foreach (var mapping in result)
					if (mapping.Discriminator == null)
						mapping.Discriminator = discriminator;
			}

			_inheritanceMappings = result;
		}
	}
}
