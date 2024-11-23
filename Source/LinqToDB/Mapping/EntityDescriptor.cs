using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Mapping
{
	using Common;
	using Expressions;
	using Extensions;
	using Linq;
	using LinqToDB.SqlQuery;
	using Reflection;

	/// <summary>
	/// Stores mapping entity descriptor.
	/// </summary>
	[DebuggerDisplay("{TypeAccessor.Type.Name} (\"{Name}\")")]
	public class EntityDescriptor : IEntityChangeDescriptor
	{
		/// <summary>
		/// Creates descriptor instance.
		/// </summary>
		/// <param name="mappingSchema">Mapping schema, associated with descriptor.</param>
		/// <param name="type">Mapping class type.</param>
		public EntityDescriptor(MappingSchema mappingSchema, Type type, Action<MappingSchema, IEntityChangeDescriptor>? onEntityDescriptorCreated)
		{
			MappingSchema = mappingSchema;
			TypeAccessor  = TypeAccessor.GetAccessor(type);

			Init(onEntityDescriptorCreated);
		}

		internal MappingSchema MappingSchema { get; }

		/// <summary>
		/// Gets mapping type accessor.
		/// </summary>
		public TypeAccessor TypeAccessor { get; }

		/// <summary>
		/// Gets name of table or view in database.
		/// </summary>
		public SqlObjectName Name { get; private set; }

		string IEntityChangeDescriptor.TableName
		{
			get => Name.Name;
			set => Name = Name with { Name = value };
		}

		string? IEntityChangeDescriptor.SchemaName
		{
			get => Name.Schema;
			set => Name = Name with { Schema = value };
		}

		string? IEntityChangeDescriptor.DatabaseName
		{
			get => Name.Database;
			set => Name = Name with { Database = value };
		}

		string? IEntityChangeDescriptor.ServerName
		{
			get => Name.Server;
			set => Name = Name with { Server = value };
		}

		/// <summary>
		/// Gets or sets table options. See <see cref="TableOptions"/> enum for support information per provider.
		/// </summary>
		public TableOptions TableOptions { get; private set; }

		TableOptions IEntityChangeDescriptor.TableOptions
		{
			get => TableOptions;
			set => TableOptions = value;
		}

		/// <summary>
		/// Gets or sets column mapping rules for current mapping class or interface.
		/// If <c>true</c>, properties and fields should be marked with one of those attributes to be used for mapping:
		/// - <see cref="ColumnAttribute"/>;
		/// - <see cref="PrimaryKeyAttribute"/>;
		/// - <see cref="IdentityAttribute"/>;
		/// - <see cref="ColumnAliasAttribute"/>.
		/// Otherwise all supported members of scalar type will be used:
		/// - public instance fields and properties;
		/// - explicit interface implementation properties.
		/// Also see <seealso cref="Configuration.IsStructIsScalarType"/> and <seealso cref="ScalarTypeAttribute"/>.
		/// </summary>
		public bool IsColumnAttributeRequired { get; private set; }

		/// <summary>
		/// Gets flags for which operation values are skipped.
		/// </summary>
		public SkipModification SkipModificationFlags { get; private set; }

		readonly List<ColumnDescriptor> _columns = new ();
		/// <summary>
		/// Gets list of column descriptors for current entity.
		/// </summary>
		public IReadOnlyList<ColumnDescriptor> Columns => _columns;

		IEnumerable<IColumnChangeDescriptor> IEntityChangeDescriptor.Columns => _columns;

		readonly List<AssociationDescriptor> _associations = new ();
		/// <summary>
		/// Gets list of association descriptors for current entity.
		/// </summary>
		public IReadOnlyList<AssociationDescriptor> Associations => _associations;

		Dictionary<string, string>? _aliases;
		/// <summary>
		/// Gets mapping dictionary to map column aliases to target columns or aliases.
		/// </summary>
		public IReadOnlyDictionary<string, string>? Aliases => _aliases;

		List<MemberAccessor>? _calculatedMembers;
		/// <summary>
		/// Gets list of calculated column members (properties with <see cref="ExpressionMethodAttribute.IsColumn"/> set to <c>true</c>).
		/// </summary>
		public IReadOnlyList<MemberAccessor>? CalculatedMembers => _calculatedMembers;

		/// <summary>
		/// Returns <c>true</c>, if entity has calculated columns.
		/// Also see <seealso cref="CalculatedMembers"/>.
		/// </summary>
		public bool HasCalculatedMembers => CalculatedMembers != null && CalculatedMembers.Count > 0;

		private InheritanceMapping[] _inheritanceMappings = [];
		/// <summary>
		/// Gets list of inheritance mapping descriptors for current entity.
		/// </summary>
		public IReadOnlyList<InheritanceMapping> InheritanceMapping => _inheritanceMappings;

		/// <summary>
		/// For entity descriptor with inheritance mapping gets descriptor of root (base) entity.
		/// </summary>
		public EntityDescriptor? InheritanceRoot { get; private set; }

		/// <summary>
		/// Gets mapping class type.
		/// </summary>
		public Type ObjectType => TypeAccessor.Type;

		/// <summary>
		/// Returns <c>true</c>, if entity has complex columns (with <see cref="MemberAccessor.IsComplex"/> flag set).
		/// </summary>
		internal bool HasComplexColumns { get; private set; }

		public LambdaExpression? QueryFilterLambda { get; private set; }
		public Delegate?         QueryFilterFunc   { get; private set; }

		bool HasInheritanceMapping()
		{
			return TypeAccessor.Type.BaseType != null && MappingSchema.HasAttribute<InheritanceMappingAttribute>(TypeAccessor.Type.BaseType);
		}

		void Init(Action<MappingSchema, IEntityChangeDescriptor>? onEntityDescriptorCreated)
		{
			var hasInheritanceMapping = HasInheritanceMapping();
			var ta = MappingSchema.GetAttribute<TableAttribute>(TypeAccessor.Type);

			string? tableName = null;
			string? schema    = null;
			string? database  = null;
			string? server    = null;

			if (ta != null)
			{
				tableName                 = ta.Name;
				schema                    = ta.Schema;
				database                  = ta.Database;
				server                    = ta.Server;
				TableOptions              = ta.TableOptions;
				IsColumnAttributeRequired = ta.IsColumnAttributeRequired;
			}

			if (tableName == null)
			{
				tableName = TypeAccessor.Type.Name;

				if (TypeAccessor.Type.IsInterface && tableName.Length > 1 && tableName[0] == 'I')
					tableName = tableName.Substring(1);
			}

			Name = new SqlObjectName(tableName, Server: server, Database: database, Schema: schema);

			var qf = MappingSchema.GetAttribute<QueryFilterAttribute>(TypeAccessor.Type);

			if (qf != null)
			{
				QueryFilterLambda = qf.FilterLambda;
				QueryFilterFunc   = qf.FilterFunc;
			}

			InitializeDynamicColumnsAccessors(hasInheritanceMapping);

			List<ColumnAttribute>? attrs = null;
			var members = TypeAccessor.Members.Concat(
				MappingSchema.GetDynamicColumns(ObjectType).Select(dc => new MemberAccessor(TypeAccessor, dc, this)));

			foreach (var member in members)
			{
				var aa = MappingSchema.GetAttribute<AssociationAttribute>(TypeAccessor.Type, member.MemberInfo);

				if (aa != null)
				{
					_associations.Add(new AssociationDescriptor(
						MappingSchema,
						TypeAccessor.Type,
						member.MemberInfo,
						aa.GetThisKeys(),
						aa.GetOtherKeys(),
						aa.ExpressionPredicate,
						aa.Predicate,
						aa.QueryExpressionMethod,
						aa.QueryExpression,
						aa.Storage,
						aa.AssociationSetterExpressionMethod,
						aa.AssociationSetterExpression,
						aa.ConfiguredCanBeNull,
						aa.AliasName));
					continue;
				}

				var columnAttributes = MappingSchema.GetAttributes<ColumnAttribute>(TypeAccessor.Type, member.MemberInfo);

				if (columnAttributes.Length > 0)
				{
					var mappedMembers = new HashSet<string>();
					foreach (var ca in columnAttributes)
					{
						if (mappedMembers.Add(ca.MemberName ?? string.Empty) && ca.IsColumn)
						{
							if (ca.MemberName != null)
							{
								(attrs ??= new()).Add(new ColumnAttribute(member.Name, ca));
							}
							else
							{
								var cd = new ColumnDescriptor(MappingSchema, this, ca, member, hasInheritanceMapping);
								AddColumn(cd);
								_columnNames.Add(member.Name, cd);
							}
						}
					}
				}
				else if (
					!IsColumnAttributeRequired && MappingSchema.IsScalarType(member.Type) ||
					MappingSchema.HasAttribute<IdentityAttribute  >(TypeAccessor.Type, member.MemberInfo) ||
					MappingSchema.HasAttribute<PrimaryKeyAttribute>(TypeAccessor.Type, member.MemberInfo))
				{
					var cd = new ColumnDescriptor(MappingSchema, this, null, member, hasInheritanceMapping);
					AddColumn(cd);
					_columnNames.Add(member.Name, cd);
				}

				var caa = MappingSchema.GetAttribute<ColumnAliasAttribute>(TypeAccessor.Type, member.MemberInfo);

				if (caa != null)
				{
					_aliases ??= new Dictionary<string, string>();

					_aliases.Add(
						member.Name,
						caa.MemberName ?? throw new LinqToDBException($"The {nameof(ColumnAliasAttribute)} attribute of the '{TypeAccessor.Type}.{member.MemberInfo.Name}' must have MemberName."));
				}

				var ma = MappingSchema.GetAttribute<ExpressionMethodAttribute>(TypeAccessor.Type, member.MemberInfo);
				if (ma != null && ma.IsColumn)
				{
					_calculatedMembers ??= new List<MemberAccessor>();
					_calculatedMembers.Add(member);
				}
			}

			var typeColumnAttrs = MappingSchema.GetAttributes<ColumnAttribute>(TypeAccessor.Type);

			foreach (var attr in typeColumnAttrs)
				if (attr.IsColumn)
					SetColumn(attr, hasInheritanceMapping);
			if (attrs != null)
				foreach (var attr in attrs)
					if (attr.IsColumn)
						SetColumn(attr, hasInheritanceMapping);

			SkipModificationFlags = Columns.Aggregate(SkipModification.None, (s, c) => s | c.SkipModificationFlags);

			if (!hasInheritanceMapping)
				InitInheritanceMapping(onEntityDescriptorCreated);
		}

		void SetColumn(ColumnAttribute attr, bool hasInheritanceMapping)
		{
			if (attr.MemberName == null)
				throw new LinqToDBException($"The Column attribute of the '{TypeAccessor.Type}' type must have MemberName.");

			if (attr.MemberName.IndexOf('.') < 0)
			{
				var ex = TypeAccessor[attr.MemberName];
				var cd = new ColumnDescriptor(MappingSchema, this, attr, ex, hasInheritanceMapping);

				if (_columnNames.Remove(attr.MemberName))
					_columns.RemoveAll(c => c.MemberName == attr.MemberName);

				AddColumn(cd);
				_columnNames.Add(attr.MemberName, cd);
			}
			else
			{
				var cd = new ColumnDescriptor(MappingSchema, this, attr, new MemberAccessor(TypeAccessor, attr.MemberName, this), hasInheritanceMapping);

				if (!string.IsNullOrWhiteSpace(attr.MemberName))
				{
					if (_columnNames.Remove(attr.MemberName))
						_columns.RemoveAll(c => c.MemberName == attr.MemberName);

					AddColumn(cd);
					_columnNames.Add(attr.MemberName, cd);
				}
			}
		}

		readonly Dictionary<string, ColumnDescriptor> _columnNames = new ();

		/// <summary>
		/// Gets column descriptor by member name.
		/// </summary>
		/// <param name="memberName">Member name.</param>
		/// <returns>Returns column descriptor or <c>null</c>, if descriptor not found.</returns>
		public ColumnDescriptor? this[string memberName]
		{
			get
			{
				if (!_columnNames.TryGetValue(memberName, out var cd))
					if (Aliases != null && Aliases.TryGetValue(memberName, out var alias) && memberName != alias)
						return this[alias];

				return cd;
			}
		}

		void InitInheritanceMapping(Action<MappingSchema, IEntityChangeDescriptor>? onEntityDescriptorCreated)
		{
			var mappingAttrs = MappingSchema.GetAttributes<InheritanceMappingAttribute>(ObjectType);

			if (mappingAttrs.Length == 0)
				return;

			_inheritanceMappings = new InheritanceMapping[mappingAttrs.Length];
			InheritanceRoot      = this;

			for (var i = 0; i < mappingAttrs.Length; i++)
			{
				var m = mappingAttrs[i];

				var mapping = new InheritanceMapping()
				{
					Code      = m.Code,
					IsDefault = m.IsDefault,
					Type      = m.Type,
				};

				_inheritanceMappings[i] = mapping;
			}

			var allColumnMemberNames = new HashSet<string>();

			foreach (var cd in Columns)
				allColumnMemberNames.Add(cd.MemberName);

			foreach (var m in _inheritanceMappings)
			{
				if (m.Type == ObjectType)
					continue;

				var ed = MappingSchema.GetEntityDescriptor(m.Type, onEntityDescriptorCreated);
				ed.InheritanceRoot = this;

				foreach (var cd in ed.Columns)
					if (allColumnMemberNames.Add(cd.MemberName))
						AddColumn(cd);
			}

			var discriminator = Columns.FirstOrDefault(x => x.IsDiscriminator)
				?? throw new LinqToDBException($"Inheritance Discriminator is not defined for the '{ObjectType}' hierarchy.");

			foreach (var m in _inheritanceMappings)
				m.Discriminator = discriminator;
		}

		void AddColumn(ColumnDescriptor columnDescriptor)
		{
			_columns.Add(columnDescriptor);

			if (columnDescriptor.MemberAccessor.IsComplex)
				HasComplexColumns = true;
		}

		/// <summary>
		/// Returns column descriptor based on its MemberInfo
		/// </summary>
		/// <param name="memberInfo"></param>
		/// <returns></returns>
		public ColumnDescriptor? FindColumnDescriptor(MemberInfo memberInfo)
		{
			return Columns.FirstOrDefault(c => c.MemberInfo == memberInfo);
		}

		/// <summary>
		/// Returns association descriptor based on its MemberInfo
		/// </summary>
		/// <param name="memberInfo"></param>
		/// <returns></returns>
		public AssociationDescriptor? FindAssociationDescriptor(MemberInfo memberInfo)
		{
			return Associations.FirstOrDefault(a => a.MemberInfo.EqualsTo(memberInfo));
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

		private void InitializeDynamicColumnsAccessors(bool hasInheritanceMapping)
		{
			// initialize dynamic columns store accessors
			List<MappingAttribute>?                                   dynamicStoreAttributes = null;
			Dictionary<DynamicColumnsStoreAttribute, MemberAccessor>? storeMembers           = null;

			var accessors = MappingSchema.GetAttribute<DynamicColumnAccessorAttribute>(TypeAccessor.Type);
			if (accessors != null)
			{
#pragma warning disable CA1508 // Avoid dead conditional code : analyzer bug
				(dynamicStoreAttributes ??= new()).Add(accessors);
#pragma warning restore CA1508 // Avoid dead conditional code
			}

			foreach (var member in TypeAccessor.Members)
			{
				// dynamic columns store property
				var dcsProp = MappingSchema.GetAttribute<DynamicColumnsStoreAttribute>(TypeAccessor.Type, member.MemberInfo);

				if (dcsProp != null)
				{
					(dynamicStoreAttributes ??= new()).Add(dcsProp);
					(storeMembers ??= new()).Add(dcsProp, member);
				}
			}

			if (dynamicStoreAttributes != null)
			{
				MappingAttribute dynamicStoreAttribute;
				if (dynamicStoreAttributes.Count > 1)
				{
					var orderedAttributes = MappingSchema.SortByConfiguration(dynamicStoreAttributes).Take(2).ToArray();

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
					var member          = storeMembers![storeAttribute];
					DynamicColumnsStore = new ColumnDescriptor(MappingSchema, this, new ColumnAttribute(member.Name), member, hasInheritanceMapping);

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
										ExpressionInstances.UntypedNull),
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
						var mi = TypeAccessor.Type.GetMethod(accessorsAttribute.GetterMethod, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!;

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
						var mi = TypeAccessor.Type.GetMethod(accessorsAttribute.SetterMethod, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!;

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
