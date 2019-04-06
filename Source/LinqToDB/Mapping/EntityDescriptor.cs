﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqToDB.Mapping
{
	using Common;
	using Extensions;
	using Linq;
	using Reflection;

	/// <summary>
	/// Stores mapping entity descriptor.
	/// </summary>
	public class EntityDescriptor : IEntityChangeDescriptor
	{
		/// <summary>
		/// Creates descriptor instance.
		/// </summary>
		/// <param name="mappingSchema">Mapping schema, associated with descriptor.</param>
		/// <param name="type">Mapping class type.</param>
		public EntityDescriptor(MappingSchema mappingSchema, Type type)
		{
			TypeAccessor = TypeAccessor.GetAccessor(type);
			Associations = new List<AssociationDescriptor>();
			Columns = new List<ColumnDescriptor>();

			Init(mappingSchema);
			InitInheritanceMapping(mappingSchema);
		}

		/// <summary>
		/// Gets or sets mapping type accessor.
		/// </summary>
		public TypeAccessor TypeAccessor { get; set; }

		/// <summary>
		/// Gets name of table or view in database.
		/// </summary>
		public string TableName { get; private set; }

		string IEntityChangeDescriptor.TableName
		{
			get => TableName;
			set => TableName = value;
		}

		/// <summary>
		/// Gets optional schema/owner name, to override default name. See <see cref="LinqExtensions.SchemaName{T}(ITable{T}, string)"/> method for support information per provider.
		/// </summary>
		public string SchemaName { get; private set; }

		string IEntityChangeDescriptor.SchemaName
		{
			get => SchemaName;
			set => SchemaName = value;
		}

		/// <summary>
		/// Gets optional database name, to override default database name. See <see cref="LinqExtensions.DatabaseName{T}(ITable{T}, string)"/> method for support information per provider.
		/// </summary>
		public string DatabaseName { get; private set; }

		string IEntityChangeDescriptor.DatabaseName
		{
			get => DatabaseName;
			set => DatabaseName = value;
		}

		// TODO: V2: remove?
		/// <summary>
		/// Gets or sets column mapping rules for current mapping class or interface.
		/// If <c>true</c>, properties and fields should be marked with one of those attributes to be used for mapping:
		/// - <see cref="ColumnAttribute"/>;
		/// - <see cref="PrimaryKeyAttribute"/>;
		/// - <see cref="IdentityAttribute"/>;
		/// - <see cref="ColumnAliasAttribute"/>.
		/// Otherwise all supported members of scalar type will be used:
		/// - public instance fields and properties;
		/// - explicit interface implmentation properties.
		/// Also see <seealso cref="Configuration.IsStructIsScalarType"/> and <seealso cref="ScalarTypeAttribute"/>.
		/// </summary>
		public bool IsColumnAttributeRequired { get; private set; }

		/// <summary>
		/// Gets flags for which operation values are skipped.
		/// </summary>
		public SkipModification SkipModificationFlags { get; private set; }

		/// <summary>
		/// Gets the dynamic columns store descriptor.
		/// </summary>
		public ColumnDescriptor DynamicColumnsStore { get; private set; }

		/// <summary>
		/// Gets list of column descriptors for current entity.
		/// </summary>
		public List<ColumnDescriptor> Columns { get; private set; }

		IEnumerable<IColumnChangeDescriptor> IEntityChangeDescriptor.Columns => Columns.Cast<IColumnChangeDescriptor>();

		/// <summary>
		/// Gets list of association descriptors for current entity.
		/// </summary>
		public List<AssociationDescriptor> Associations { get; private set; }

		/// <summary>
		/// Gets mapping dictionary to map column aliases to target columns or aliases.
		/// </summary>
		public Dictionary<string, string> Aliases { get; private set; }

		/// <summary>
		/// Gets list of calculated column members (properties with <see cref="ExpressionMethodAttribute.IsColumn"/> set to <c>true</c>).
		/// </summary>
		public List<MemberAccessor> CalculatedMembers { get; private set; }

		/// <summary>
		/// Returns <c>true</c>, if entity has calculated columns.
		/// Also see <seealso cref="CalculatedMembers"/>.
		/// </summary>
		public bool HasCalculatedMembers => CalculatedMembers != null && CalculatedMembers.Count > 0;

		private List<InheritanceMapping> _inheritanceMappings;
		/// <summary>
		/// Gets list of inheritace mapping descriptors for current entity.
		/// </summary>
		public List<InheritanceMapping> InheritanceMapping => _inheritanceMappings;

		/// <summary>
		/// Gets mapping class type.
		/// </summary>
		public Type ObjectType { get { return TypeAccessor.Type; } }

		/// <summary>
		/// Returns <c>true</c>, if entity has complex columns (with <see cref="MemberAccessor.IsComplex"/> flag set).
		/// </summary>
		internal bool HasComplexColumns { get; private set; }

		void Init(MappingSchema mappingSchema)
		{
			var ta = mappingSchema.GetAttribute<TableAttribute>(TypeAccessor.Type, a => a.Configuration);

			if (ta != null)
			{
				TableName = ta.Name;
				SchemaName = ta.Schema;
				DatabaseName = ta.Database;
				IsColumnAttributeRequired = ta.IsColumnAttributeRequired;
			}

			if (TableName == null)
			{
				TableName = TypeAccessor.Type.Name;

				if (TypeAccessor.Type.IsInterfaceEx() && TableName.Length > 1 && TableName[0] == 'I')
					TableName = TableName.Substring(1);
			}

			var attrs = new List<ColumnAttribute>();
			var members = TypeAccessor.Members.Concat(
				mappingSchema.GetDynamicColumns(ObjectType).Select(dc => new MemberAccessor(TypeAccessor, dc)));

			foreach (var member in members)
			{
				var aa = mappingSchema.GetAttribute<AssociationAttribute>(TypeAccessor.Type, member.MemberInfo, attr => attr.Configuration);

				if (aa != null)
				{
					Associations.Add(new AssociationDescriptor(
						TypeAccessor.Type, member.MemberInfo, aa.GetThisKeys(), aa.GetOtherKeys(),
						aa.ExpressionPredicate, aa.Predicate, aa.QueryExpressionMethod, aa.QueryExpression, aa.Storage, aa.CanBeNull,
						aa.AliasName));
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
							AddColumn(cd);
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
					AddColumn(cd);
					_columnNames.Add(member.Name, cd);
				}

				var caa = mappingSchema.GetAttribute<ColumnAliasAttribute>(TypeAccessor.Type, member.MemberInfo, attr => attr.Configuration);

				if (caa != null)
				{
					if (Aliases == null)
						Aliases = new Dictionary<string, string>();

					Aliases.Add(member.Name, caa.MemberName);
				}

				var ma = mappingSchema.GetAttribute<ExpressionMethodAttribute>(TypeAccessor.Type, member.MemberInfo, attr => attr.Configuration);
				if (ma != null && ma.IsColumn)
				{
					if (CalculatedMembers == null)
						CalculatedMembers = new List<MemberAccessor>();
					CalculatedMembers.Add(member);
				}

				// dynamic columns store property
				var dcsProp = mappingSchema.GetAttribute<DynamicColumnsStoreAttribute>(TypeAccessor.Type, member.MemberInfo, attr => attr.Configuration);

				if (dcsProp != null)
					DynamicColumnsStore = new ColumnDescriptor(mappingSchema, new ColumnAttribute(member.Name), member);
			}

			var typeColumnAttrs = mappingSchema.GetAttributes<ColumnAttribute>(TypeAccessor.Type, a => a.Configuration);

			foreach (var attr in typeColumnAttrs.Concat(attrs))
				if (attr.IsColumn)
					SetColumn(attr, mappingSchema);

			SkipModificationFlags = Columns.Aggregate(SkipModification.None, (s, c) => s | c.SkipModificationFlags);
		}

		void SetColumn(ColumnAttribute attr, MappingSchema mappingSchema)
		{
			if (attr.MemberName == null)
				throw new LinqToDBException($"The Column attribute of the '{TypeAccessor.Type}' type must have MemberName.");

			if (attr.MemberName.IndexOf('.') < 0)
			{
				var ex = TypeAccessor[attr.MemberName];
				var cd = new ColumnDescriptor(mappingSchema, attr, ex);

				if (_columnNames.Remove(attr.MemberName))
					Columns.RemoveAll(c => c.MemberName == attr.MemberName);

				AddColumn(cd);
				_columnNames.Add(attr.MemberName, cd);
			}
			else
			{
				var cd = new ColumnDescriptor(mappingSchema, attr, new MemberAccessor(TypeAccessor, attr.MemberName));

				if (!string.IsNullOrWhiteSpace(attr.MemberName))
				{
					if (_columnNames.Remove(attr.MemberName))
						Columns.RemoveAll(c => c.MemberName == attr.MemberName);

					AddColumn(cd);
					_columnNames.Add(attr.MemberName, cd);
				}
			}
		}

		readonly Dictionary<string, ColumnDescriptor> _columnNames = new Dictionary<string, ColumnDescriptor>();

		/// <summary>
		/// Gets column descriptor by member name.
		/// </summary>
		/// <param name="memberName">Member name.</param>
		/// <returns>Returns column descriptor or <c>null</c>, if descriptor not found.</returns>
		public ColumnDescriptor this[string memberName]
		{
			get
			{
				if (!_columnNames.TryGetValue(memberName, out var cd))
					if (Aliases != null && Aliases.TryGetValue(memberName, out var alias) && memberName != alias)
						return this[alias];

				return cd;
			}
		}

		internal void InitInheritanceMapping(MappingSchema mappingSchema)
		{
			var mappingAttrs = mappingSchema.GetAttributes<InheritanceMappingAttribute>(ObjectType, a => a.Configuration, false);
			var result = new List<InheritanceMapping>(mappingAttrs.Length);

			if (mappingAttrs.Length > 0)
			{
				foreach (var m in mappingAttrs)
				{
					var mapping = new InheritanceMapping
					{
						Code = m.Code,
						IsDefault = m.IsDefault,
						Type = m.Type,
					};

					var ed = mapping.Type.Equals(ObjectType)
						? this
						: mappingSchema.GetEntityDescriptor(mapping.Type);

					//foreach (var column in this.Columns)
					//{
					//	if (ed.Columns.All(f => f.MemberName != column.MemberName))
					//		ed.AddColumn(column);
					//}

					foreach (var column in ed.Columns)
					{
						if (Columns.All(f => f.MemberName != column.MemberName))
							AddColumn(column);

						if (column.IsDiscriminator)
							mapping.Discriminator = column;
					}

					mapping.Discriminator = mapping.Discriminator ?? Columns.FirstOrDefault(x => x.IsDiscriminator);

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

		internal void AddColumn(ColumnDescriptor columnDescriptor)
		{
			Columns.Add(columnDescriptor);

			if (columnDescriptor.MemberAccessor.IsComplex)
				HasComplexColumns = true;
		}
	}
}
