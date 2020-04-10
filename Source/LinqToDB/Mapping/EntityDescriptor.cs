using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqToDB.Mapping
{
	using Common;
	using Extensions;
	using Linq;
	using Reflection;
	using System.Linq.Expressions;
	using System.Reflection;

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
			MappingSchema = mappingSchema;
			TypeAccessor  = TypeAccessor.GetAccessor(type);
			Associations  = new List<AssociationDescriptor>();
			Columns       = new List<ColumnDescriptor>();

			Init();
			InitInheritanceMapping();
		}

		internal MappingSchema MappingSchema { get; }

		/// <summary>
		/// Gets or sets mapping type accessor.
		/// </summary>
		public TypeAccessor TypeAccessor { get; set; }

		/// <summary>
		/// Gets name of table or view in database.
		/// </summary>
		public string TableName { get; private set; } = null!;

		string IEntityChangeDescriptor.TableName
		{
			get => TableName;
			set => TableName = value;
		}

		/// <summary>
		/// Gets optional schema/owner name, to override default name. See <see cref="LinqExtensions.SchemaName{T}(ITable{T}, string)"/> method for support information per provider.
		/// </summary>
		public string? SchemaName { get; private set; }

		string? IEntityChangeDescriptor.SchemaName
		{
			get => SchemaName;
			set => SchemaName = value;
		}

		/// <summary>
		/// Gets optional database name, to override default database name. See <see cref="LinqExtensions.DatabaseName{T}(ITable{T}, string)"/> method for support information per provider.
		/// </summary>
		public string? DatabaseName { get; private set; }

		string? IEntityChangeDescriptor.DatabaseName
		{
			get => DatabaseName;
			set => DatabaseName = value;
		}

		/// <summary>
		/// Gets or sets optional linked server name. See <see cref="LinqExtensions.ServerName{T}(ITable{T}, string)"/> method for support information per provider.
		/// </summary>
		public string? ServerName { get; private set; }

		string? IEntityChangeDescriptor.ServerName
		{
			get => ServerName;
			set => ServerName = value;
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
		public Dictionary<string, string?>? Aliases { get; private set; }

		/// <summary>
		/// Gets list of calculated column members (properties with <see cref="ExpressionMethodAttribute.IsColumn"/> set to <c>true</c>).
		/// </summary>
		public List<MemberAccessor>? CalculatedMembers { get; private set; }

		/// <summary>
		/// Returns <c>true</c>, if entity has calculated columns.
		/// Also see <seealso cref="CalculatedMembers"/>.
		/// </summary>
		public bool HasCalculatedMembers => CalculatedMembers != null && CalculatedMembers.Count > 0;

		private List<InheritanceMapping> _inheritanceMappings = null!;
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

		public Delegate? QueryFilterFunc { get; private set; }

		void Init()
		{
			var ta = MappingSchema.GetAttribute<TableAttribute>(TypeAccessor.Type, a => a.Configuration);

			if (ta != null)
			{
				TableName                 = ta.Name!;
				SchemaName                = ta.Schema;
				DatabaseName              = ta.Database;
				ServerName                = ta.Server;
				IsColumnAttributeRequired = ta.IsColumnAttributeRequired;
			}

			if (TableName == null)
			{
				TableName = TypeAccessor.Type.Name;

				if (TypeAccessor.Type.IsInterface && TableName.Length > 1 && TableName[0] == 'I')
					TableName = TableName.Substring(1);
			}

			var qf = MappingSchema.GetAttribute<QueryFilterAttribute>(TypeAccessor.Type);
			if (qf != null)
			{
				QueryFilterFunc = qf.FilterFunc;
			}

			InitializeDynamicColumnsAccessors();

			var attrs = new List<ColumnAttribute>();
			var members = TypeAccessor.Members.Concat(
				MappingSchema.GetDynamicColumns(ObjectType).Select(dc => new MemberAccessor(TypeAccessor, dc, this)));

			foreach (var member in members)
			{
				var aa = MappingSchema.GetAttribute<AssociationAttribute>(TypeAccessor.Type, member.MemberInfo, attr => attr.Configuration);

				if (aa != null)
				{
					Associations.Add(new AssociationDescriptor(
						TypeAccessor.Type, member.MemberInfo, aa.GetThisKeys(), aa.GetOtherKeys(),
						aa.ExpressionPredicate, aa.Predicate, aa.QueryExpressionMethod, aa.QueryExpression, aa.Storage, aa.CanBeNull,
						aa.AliasName));
					continue;
				}

				var ca = MappingSchema.GetAttribute<ColumnAttribute>(TypeAccessor.Type, member.MemberInfo, attr => attr.Configuration);

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
							var cd = new ColumnDescriptor(MappingSchema, ca, member);
							AddColumn(cd);
							_columnNames.Add(member.Name, cd);
						}
					}
				}
				else if (
					!IsColumnAttributeRequired && MappingSchema.IsScalarType(member.Type) ||
					MappingSchema.GetAttribute<IdentityAttribute>(TypeAccessor.Type, member.MemberInfo, attr => attr.Configuration) != null ||
					MappingSchema.GetAttribute<PrimaryKeyAttribute>(TypeAccessor.Type, member.MemberInfo, attr => attr.Configuration) != null)
				{
					var cd = new ColumnDescriptor(MappingSchema, null, member);
					AddColumn(cd);
					_columnNames.Add(member.Name, cd);
				}

				var caa = MappingSchema.GetAttribute<ColumnAliasAttribute>(TypeAccessor.Type, member.MemberInfo, attr => attr.Configuration);

				if (caa != null)
				{
					if (Aliases == null)
						Aliases = new Dictionary<string, string?>();

					Aliases.Add(member.Name, caa.MemberName);
				}

				var ma = MappingSchema.GetAttribute<ExpressionMethodAttribute>(TypeAccessor.Type, member.MemberInfo, attr => attr.Configuration);
				if (ma != null && ma.IsColumn)
				{
					if (CalculatedMembers == null)
						CalculatedMembers = new List<MemberAccessor>();
					CalculatedMembers.Add(member);
				}
			}

			var typeColumnAttrs = MappingSchema.GetAttributes<ColumnAttribute>(TypeAccessor.Type, a => a.Configuration);

			foreach (var attr in typeColumnAttrs.Concat(attrs))
				if (attr.IsColumn)
					SetColumn(attr);

			SkipModificationFlags = Columns.Aggregate(SkipModification.None, (s, c) => s | c.SkipModificationFlags);
		}

		void SetColumn(ColumnAttribute attr)
		{
			if (attr.MemberName == null)
				throw new LinqToDBException($"The Column attribute of the '{TypeAccessor.Type}' type must have MemberName.");

			if (attr.MemberName.IndexOf('.') < 0)
			{
				var ex = TypeAccessor[attr.MemberName];
				var cd = new ColumnDescriptor(MappingSchema, attr, ex);

				if (_columnNames.Remove(attr.MemberName))
					Columns.RemoveAll(c => c.MemberName == attr.MemberName);

				AddColumn(cd);
				_columnNames.Add(attr.MemberName, cd);
			}
			else
			{
				var cd = new ColumnDescriptor(MappingSchema, attr, new MemberAccessor(TypeAccessor, attr.MemberName, this));

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
						return this[alias!];

				return cd;
			}
		}

		internal void InitInheritanceMapping()
		{
			var mappingAttrs = MappingSchema.GetAttributes<InheritanceMappingAttribute>(ObjectType, a => a.Configuration, false);
			var result = new List<InheritanceMapping>(mappingAttrs.Length);

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
						: MappingSchema.GetEntityDescriptor(mapping.Type);

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

		#region Dynamic Columns
		/// <summary>
		/// Gets the dynamic columns store descriptor.
		/// </summary>
		public ColumnDescriptor?   DynamicColumnsStore { get; private set; }

		/// <summary>
		/// Gets or sets optional dynamic column value getter expression with following signature:
		/// <code>
		/// object Getter(TEntity entity, string propertyName, object defaultValue);
		/// </code>
		/// </summary>
		internal LambdaExpression? DynamicColumnGetter { get; private set; }

		/// <summary>
		/// Gets or sets optional dynamic column value setter expression with following signature:
		/// <code>
		/// void Setter(TEntity entity, string propertyName, object value);
		/// </code>
		/// </summary>
		internal LambdaExpression? DynamicColumnSetter { get; private set; }

		private void InitializeDynamicColumnsAccessors()
		{
			// initialize dynamic columns store accessors
			var dynamicStoreAttributes = new List<IConfigurationProvider>();
			var accessors = MappingSchema.GetAttribute<DynamicColumnAccessorAttribute>(TypeAccessor.Type, attr => attr.Configuration);
			if (accessors != null)
			{
				dynamicStoreAttributes.Add(accessors);
			}
			var storeMembers = new Dictionary<DynamicColumnsStoreAttribute, MemberAccessor>();

			foreach (var member in TypeAccessor.Members)
			{
				// dynamic columns store property
				var dcsProp = MappingSchema.GetAttribute<DynamicColumnsStoreAttribute>(TypeAccessor.Type, member.MemberInfo, attr => attr.Configuration);

				if (dcsProp != null)
				{
					dynamicStoreAttributes.Add(dcsProp);
					storeMembers.Add(dcsProp, member);
				}
			}

			if (dynamicStoreAttributes.Count > 0)
			{
				IConfigurationProvider dynamicStoreAttribute;
				if (dynamicStoreAttributes.Count > 1)
				{
					var orderedAttributes = MappingSchema.SortByConfiguration(attr => attr.Configuration, dynamicStoreAttributes).ToArray();

					if (orderedAttributes[1].Configuration == orderedAttributes[0].Configuration)
						throw new LinqToDBException($"Multiple dynamic store configuration attributes with same configuration found for {TypeAccessor.Type}");

					dynamicStoreAttribute = orderedAttributes[0];
				}
				else
					dynamicStoreAttribute = dynamicStoreAttributes[0];

				var objParam          = Expression.Parameter(TypeAccessor.Type, "obj");
				var propNameParam     = Expression.Parameter(typeof(string), "propertyName");
				var defaultValueParam = Expression.Parameter(typeof(object), "defaultValue");
				var valueParam        = Expression.Parameter(typeof(object), "value");

				if (dynamicStoreAttribute is DynamicColumnsStoreAttribute storeAttribute)
				{
					var member          = storeMembers[storeAttribute];
					DynamicColumnsStore = new ColumnDescriptor(MappingSchema, new ColumnAttribute(member.Name), member);

					// getter expression
					var storageType = member.MemberInfo.GetMemberType();
					var storedType  = storageType.GetGenericArguments()[1];
					var outVar      = Expression.Variable(storedType);

					var tryGetValueMethodInfo = storageType.GetMethod("TryGetValue");

					if (tryGetValueMethodInfo == null)
						throw new LinqToDBException("Storage property do not have method 'TryGetValue'");

					// get value via "Item" accessor; we're not null-checking
					DynamicColumnGetter =
						Expression.Lambda(
							Expression.Block(
								new[] { outVar },
								Expression.IfThen(
									Expression.Not(
										Expression.Call(
											Expression.MakeMemberAccess(
												objParam,
												member.MemberInfo),
											tryGetValueMethodInfo,
											propNameParam,
											outVar)),
									Expression.Assign(outVar, defaultValueParam)),
								outVar),
							objParam,
							propNameParam,
							defaultValueParam);

					// if null, create new dictionary; then assign value
					DynamicColumnSetter =
						Expression.Lambda(
							Expression.Block(
								Expression.IfThen(
									Expression.ReferenceEqual(
										Expression.MakeMemberAccess(objParam, member.MemberInfo),
										Expression.Constant(null)),
									Expression.Assign(
										Expression.MakeMemberAccess(objParam, member.MemberInfo),
										Expression.New(typeof(Dictionary<string, object>)))),
								Expression.Assign(
									Expression.Property(
										Expression.MakeMemberAccess(objParam, member.MemberInfo),
										"Item",
										propNameParam),
									Expression.Convert(valueParam, typeof(object)))),
							objParam,
							propNameParam,
							valueParam);
				}
				else
				{
					var accessorsAttribute = (DynamicColumnAccessorAttribute)dynamicStoreAttribute;

					if (accessorsAttribute.GetterMethod != null)
					{
						var mi = TypeAccessor.Type.GetMethod(accessorsAttribute.GetterMethod, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

						if (mi.IsStatic)
							DynamicColumnGetter =
								Expression.Lambda(
									Expression.Call(
										mi,
										objParam,
										propNameParam,
										defaultValueParam),
									objParam,
									propNameParam,
									defaultValueParam);
						else
							DynamicColumnGetter =
								Expression.Lambda(
									Expression.Call(
										objParam,
										mi,
										propNameParam,
										defaultValueParam),
									objParam,
									propNameParam,
									defaultValueParam);
					}
					else if (accessorsAttribute.GetterExpressionMethod != null)
						DynamicColumnGetter = TypeAccessor.Type.GetExpressionFromExpressionMember<LambdaExpression>(accessorsAttribute.GetterExpressionMethod);
					else
						DynamicColumnGetter = accessorsAttribute.GetterExpression;

					if (accessorsAttribute.SetterMethod != null)
					{
						var mi = TypeAccessor.Type.GetMethod(accessorsAttribute.SetterMethod, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

						if (mi.IsStatic)
							DynamicColumnSetter =
								Expression.Lambda(
									Expression.Call(
										mi,
										objParam,
										propNameParam,
										valueParam),
									objParam,
									propNameParam,
									valueParam);
						else
							DynamicColumnSetter =
								Expression.Lambda(
									Expression.Call(
										objParam,
										mi,
										propNameParam,
										valueParam),
									objParam,
									propNameParam,
									valueParam);
					}
					else if (accessorsAttribute.SetterExpressionMethod != null)
						DynamicColumnSetter = TypeAccessor.Type.GetExpressionFromExpressionMember<LambdaExpression>(accessorsAttribute.SetterExpressionMethod);
					else
						DynamicColumnSetter = accessorsAttribute.SetterExpression;
				}
			}
		}
		#endregion
	}
}
