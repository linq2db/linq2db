using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using LinqToDB.Expressions;
using LinqToDB.Extensions;

namespace LinqToDB.Mapping
{
	/// <summary>
	/// Fluent mapping entity builder.
	/// </summary>
	/// <typeparam name="T">Entity mapping type.</typeparam>
	public class EntityMappingBuilder<T>
	{
		#region Init

		/// <summary>
		/// Creates enity mapping builder.
		/// </summary>
		/// <param name="builder">Fluent mapping builder.</param>
		/// <param name="configuration">Optional mapping schema configuration name, for which this entity builder should be taken into account.
		/// <see cref="ProviderName"/> for standard configuration names.</param>
		public EntityMappingBuilder([JetBrains.Annotations.NotNull] FluentMappingBuilder builder, string configuration)
		{
			_builder = builder ?? throw new ArgumentNullException(nameof(builder));

			Configuration = configuration;

			// We'll reset cache here, because there is no need to create builder if you don't want to change something
			_builder.MappingSchema.ResetEntityDescriptor(typeof(T));
		}

		readonly FluentMappingBuilder _builder;

		/// <summary>
		/// Gets mapping schema configuration name, for which this entity builder should be taken into account.
		/// <see cref="ProviderName"/> for standard configuration names.
		/// </summary>
		public string Configuration { get; }

		#endregion

		#region GetAttributes

		/// <summary>
		/// Returns attributes of specified type, applied to current entity type.
		/// </summary>
		/// <typeparam name="TA">Attribute type.</typeparam>
		/// <returns>Returns list of attributes, applied to current entity type.</returns>
		public TA[] GetAttributes<TA>()
			where TA : Attribute
		{
			return _builder.GetAttributes<TA>(typeof(T));
		}

		/// <summary>
		/// Returns attributes of specified type, applied to specified entity type.
		/// </summary>
		/// <typeparam name="TA">Attribute type.</typeparam>
		/// <param name="type">Entity type.</param>
		/// <returns>Returns list of attributes, applied to specified entity type.</returns>
		public TA[] GetAttributes<TA>(Type type)
			where TA : Attribute
		{
			return _builder.GetAttributes<TA>(type);
		}

		/// <summary>
		/// Returns attributes of specified type, applied to specified entity member.
		/// Member could be inherited from parent classes.
		/// </summary>
		/// <typeparam name="TA">Attribute type.</typeparam>
		/// <param name="memberInfo">Member info object.</param>
		/// <returns>Returns list of attributes, applied to specified entity member.</returns>
		public TA[] GetAttributes<TA>(MemberInfo memberInfo)
			where TA : Attribute
		{
			return _builder.GetAttributes<TA>(typeof(T), memberInfo);
		}

		/// <summary>
		/// Returns attributes of specified type, applied to current entity type and active for current configuration.
		/// </summary>
		/// <typeparam name="TA">Attribute type.</typeparam>
		/// <param name="configGetter">Function to extract configuration name from attribute instance.</param>
		/// <returns>Returns list of attributes.</returns>
		public TA[] GetAttributes<TA>(Func<TA,string> configGetter)
			where TA : Attribute
		{
			var attrs = GetAttributes<TA>();

			return string.IsNullOrEmpty(Configuration) ?
				attrs.Where(a => string.IsNullOrEmpty(configGetter(a))).ToArray() :
				attrs.Where(a => Configuration ==    configGetter(a)). ToArray();
		}

		/// <summary>
		/// Returns attributes of specified type, applied to specified entity type and active for current configuration.
		/// </summary>
		/// <typeparam name="TA">Attribute type.</typeparam>
		/// <param name="type">Entity type.</param>
		/// <param name="configGetter">Function to extract configuration name from attribute instance.</param>
		/// <returns>Returns list of attributes.</returns>
		public TA[] GetAttributes<TA>(Type type, Func<TA,string> configGetter)
			where TA : Attribute
		{
			var attrs = GetAttributes<TA>(type);

			return string.IsNullOrEmpty(Configuration) ?
				attrs.Where(a => string.IsNullOrEmpty(configGetter(a))).ToArray() :
				attrs.Where(a => Configuration ==    configGetter(a)). ToArray();
		}

		/// <summary>
		/// Returns attributes of specified type, applied to specified entity member and active for current configuration.
		/// </summary>
		/// <typeparam name="TA">Attribute type.</typeparam>
		/// <param name="memberInfo">Member info object.</param>
		/// <param name="configGetter">Function to extract configuration name from attribute instance.</param>
		/// <returns>Returns list of attributes.</returns>
		public TA[] GetAttributes<TA>(MemberInfo memberInfo, Func<TA,string> configGetter)
			where TA : Attribute
		{
			var attrs = GetAttributes<TA>(memberInfo);

			return string.IsNullOrEmpty(Configuration) ?
				attrs.Where(a => string.IsNullOrEmpty(configGetter(a))).ToArray() :
				attrs.Where(a => Configuration ==    configGetter(a)). ToArray();
		}

		#endregion

		#region HasAttribute

		/// <summary>
		/// Adds mapping attribute to current entity.
		/// </summary>
		/// <param name="attribute">Mapping attribute to add.</param>
		/// <returns>Returns current fluent entity mapping builder.</returns>
		public EntityMappingBuilder<T> HasAttribute(Attribute attribute)
		{
			_builder.HasAttribute(typeof(T), attribute);
			return this;
		}

		/// <summary>
		/// Adds mapping attribute to specified member.
		/// </summary>
		/// <param name="memberInfo">Target member.</param>
		/// <param name="attribute">Mapping attribute to add to specified member.</param>
		/// <returns>Returns current fluent entity mapping builder.</returns>
		public EntityMappingBuilder<T> HasAttribute(MemberInfo memberInfo, Attribute attribute)
		{
			_builder.HasAttribute(memberInfo, attribute);
			return this;
		}

		/// <summary>
		/// Adds mapping attribute to a member, specified using lambda expression.
		/// </summary>
		/// <param name="func">Target member, specified using lambda expression.</param>
		/// <param name="attribute">Mapping attribute to add to specified member.</param>
		/// <returns>Returns current fluent entity mapping builder.</returns>
		public EntityMappingBuilder<T> HasAttribute(LambdaExpression func, Attribute attribute)
		{
			_builder.HasAttribute(func, attribute);
			return this;
		}

		/// <summary>
		/// Adds mapping attribute to a member, specified using lambda expression.
		/// </summary>
		/// <param name="func">Target member, specified using lambda expression.</param>
		/// <param name="attribute">Mapping attribute to add to specified member.</param>
		/// <returns>Returns current fluent entity mapping builder.</returns>
		public EntityMappingBuilder<T> HasAttribute(Expression<Func<T,object>> func, Attribute attribute)
		{
			_builder.HasAttribute(func, attribute);
			return this;
		}

		#endregion

		/// <summary>
		/// Creates entity builder for specified mapping type.
		/// </summary>
		/// <typeparam name="TE">Mapping type.</typeparam>
		/// <param name="configuration">Optional mapping schema configuration name, for which this entity builder should be taken into account.
		/// <see cref="ProviderName"/> for standard configuration names.</param>
		/// <returns>Returns new fluent entity mapping builder.</returns>
		public EntityMappingBuilder<TE> Entity<TE>(string configuration = null)
		{
			return _builder.Entity<TE>(configuration);
		}

		/// <summary>
		/// Adds column mapping to current entity.
		/// </summary>
		/// <param name="func">Column mapping property or field getter expression.</param>
		/// <returns>Returns fluent property mapping builder.</returns>
		public PropertyMappingBuilder<T> Property(Expression<Func<T,object>> func)
		{
			return (new PropertyMappingBuilder<T>(this, func)).IsColumn();
		}

		/// <summary>
		/// Adds association mapping to current entity.
		/// </summary>
		/// <typeparam name="S">Association member type.</typeparam>
		/// <typeparam name="ID1">This association side key type.</typeparam>
		/// <typeparam name="ID2">Other association side key type.</typeparam>
		/// <param name="prop">Association member getter expression.</param>
		/// <param name="thisKey">This association key getter expression.</param>
		/// <param name="otherKey">Other association key getter expression.</param>
		/// <param name="canBeNull">Defines type of join. True - left join, False - inner join.</param>
		/// <returns>Returns fluent property mapping builder.</returns>
		public PropertyMappingBuilder<T> Association<S, ID1, ID2>(
			[JetBrains.Annotations.NotNull] Expression<Func<T, S>>   prop,
			[JetBrains.Annotations.NotNull] Expression<Func<T, ID1>> thisKey,
			[JetBrains.Annotations.NotNull] Expression<Func<S, ID2>> otherKey,
			                                bool                     canBeNull = true)
		{
			if (prop     == null) throw new ArgumentNullException(nameof(prop));
			if (thisKey  == null) throw new ArgumentNullException(nameof(thisKey));
			if (otherKey == null) throw new ArgumentNullException(nameof(otherKey));

			var thisKeyName  = MemberHelper.GetMemberInfo(thisKey).Name;
			var otherKeyName = MemberHelper.GetMemberInfo(otherKey).Name;

			var objProp = Expression.Lambda<Func<T, object>>(Expression.Convert(prop.Body, typeof(object)), prop.Parameters );

			return Property( objProp ).HasAttribute( new AssociationAttribute { ThisKey = thisKeyName, OtherKey = otherKeyName, CanBeNull = canBeNull } );
		}

		/// <summary>
		/// Adds one-to-many association mapping to current entity.
		/// </summary>
		/// <typeparam name="TOther">Other association side type</typeparam>
		/// <param name="prop">Association member getter expression.</param>
		/// <param name="predicate">Predicate expresssion.</param>
		/// <param name="canBeNull">Defines type of join. True - left join, False - inner join.</param>
		/// <returns>Returns fluent property mapping builder.</returns>
		public PropertyMappingBuilder<T> Association<TOther>(
			[JetBrains.Annotations.NotNull] Expression<Func<T, IEnumerable<TOther>>> prop,
			[JetBrains.Annotations.NotNull] Expression<Func<T, TOther, bool>>        predicate,
			                                bool                                     canBeNull = true)
		{
			if (prop      == null) throw new ArgumentNullException(nameof(prop));
			if (predicate == null) throw new ArgumentNullException(nameof(predicate));

			var objProp = Expression.Lambda<Func<T, object>>(Expression.Convert(prop.Body, typeof(object)), prop.Parameters );

			return Property( objProp ).HasAttribute( new AssociationAttribute { Predicate = predicate, CanBeNull = canBeNull, Relationship = Relationship.OneToMany } );
		}

		/// <summary>
		/// Adds one-to-one association mapping to current entity.
		/// </summary>
		/// <typeparam name="TOther">Other association side type</typeparam>
		/// <param name="prop">Association member getter expression.</param>
		/// <param name="predicate">Predicate expresssion</param>
		/// <param name="canBeNull">Defines type of join. True - left join, False - inner join.</param>
		/// <returns>Returns fluent property mapping builder.</returns>
		public PropertyMappingBuilder<T> Association<TOther>(
			[JetBrains.Annotations.NotNull] Expression<Func<T, TOther>>       prop,
			[JetBrains.Annotations.NotNull] Expression<Func<T, TOther, bool>> predicate,
			                                bool                              canBeNull = true)
		{
			if (prop      == null) throw new ArgumentNullException(nameof(prop));
			if (predicate == null) throw new ArgumentNullException(nameof(predicate));

			var objProp = Expression.Lambda<Func<T, object>>(Expression.Convert(prop.Body, typeof(object)), prop.Parameters );

			return Property( objProp ).HasAttribute( new AssociationAttribute { Predicate = predicate, CanBeNull = canBeNull } );
		}

		/// <summary>
		/// Adds primary key mapping to current entity.
		/// </summary>
		/// <param name="func">Primary key getter expression.</param>
		/// <param name="order">Primary key field order.
		/// When multiple fields specified by getter expression, fields will be ordered from first menthioned
		/// field to last one starting from provided order with step <c>1</c>.</param>
		/// <returns>Returns current fluent entity mapping builder.</returns>
		public EntityMappingBuilder<T> HasPrimaryKey(Expression<Func<T,object>> func, int order = -1)
		{
			var n = 0;

			return SetAttribute(
				func,
				true,
				m => new PrimaryKeyAttribute(Configuration, order + n++ + (m && order == -1 ? 1 : 0)),
				(m,a) => a.Order = order + n++ + (m && order == -1 ? 1 : 0),
				a => a.Configuration);
		}

		/// <summary>
		/// Adds identity column mappping to current entity.
		/// </summary>
		/// <param name="func">Identity field getter expression.</param>
		/// <returns>Returns current fluent entity mapping builder.</returns>
		public EntityMappingBuilder<T> HasIdentity(Expression<Func<T,object>> func)
		{
			return SetAttribute(
				func,
				false,
				 _    => new IdentityAttribute(Configuration),
				(_,a) => {},
				a => a.Configuration);
		}

		// TODO: V2 - remove unused parameters
		/// <summary>
		/// Adds column mapping to current entity.
		/// </summary>
		/// <param name="func">Column member getter expression.</param>
		/// <param name="order">Unused.</param>
		/// <returns>Returns current fluent entity mapping builder.</returns>
		public EntityMappingBuilder<T> HasColumn(Expression<Func<T,object>> func, int order = -1)
		{
			return SetAttribute(
				func,
				true,
				 _    => new ColumnAttribute(Configuration),
				(_,a) => a.IsColumn = true,
				a => a.Configuration);
		}

		// TODO: V2 - remove unused parameters
		/// <summary>
		/// Instruct LINQ to DB to not incude specified member into mapping.
		/// </summary>
		/// <param name="func">Member getter expression.</param>
		/// <param name="order">Unused.</param>
		/// <returns>Returns current fluent entity mapping builder.</returns>
		public EntityMappingBuilder<T> Ignore(Expression<Func<T,object>> func, int order = -1)
		{
			return SetAttribute(
				func,
				true,
				 _    => new NotColumnAttribute { Configuration = Configuration },
				(_,a) => a.IsColumn = false,
				a => a.Configuration);
		}

		/// <summary>
		/// Sets database table name for current entity.
		/// </summary>
		/// <param name="tableName">Table name.</param>
		/// <returns>Returns current fluent entity mapping builder.</returns>
		public EntityMappingBuilder<T> HasTableName(string tableName)
		{
			return SetTable(a => a.Name = tableName);
		}

		/// <summary>
		/// Sets database schema/owner name for current entity, to override default name.
		/// See <see cref="LinqExtensions.SchemaName{T}(ITable{T}, string)"/> method for support information per provider.
		/// </summary>
		/// <param name="schemaName">Schema/owner name.</param>
		/// <returns>Returns current fluent entity mapping builder.</returns>
		public EntityMappingBuilder<T> HasSchemaName(string schemaName)
		{
			return SetTable(a => a.Schema = schemaName);
		}

		/// <summary>
		/// Sets database name, to override default database name.
		/// See <see cref="LinqExtensions.DatabaseName{T}(ITable{T}, string)"/> method for support information per provider.
		/// </summary>
		/// <param name="databaseName">Database name.</param>
		/// <returns>Returns current fluent entity mapping builder.</returns>
		public EntityMappingBuilder<T> HasDatabaseName(string databaseName)
		{
			return SetTable(a => a.Database = databaseName);
		}

		/// <summary>
		/// Adds inheritance mapping for specified discriminator value.
		/// </summary>
		/// <typeparam name="S">Discriminator value type.</typeparam>
		/// <param name="key">Discriminator member getter expression.</param>
		/// <param name="value">Discriminator value.</param>
		/// <param name="type">Mapping type, used with specified discriminator value.</param>
		/// <param name="isDefault">If <c>true</c>, current mapping type used by default.</param>
		/// <returns>Returns current fluent entity mapping builder.</returns>
		public EntityMappingBuilder<T> Inheritance<S>(Expression<Func<T, S>> key, S value, Type type, bool isDefault = false)
		{
			HasAttribute(new InheritanceMappingAttribute {Code = value, Type = type, IsDefault = isDefault});
			var objProp = Expression.Lambda<Func<T, object>>(Expression.Convert(key.Body, typeof(object)), key.Parameters);
			Property(objProp).IsDiscriminator();

			return this;
		}

		EntityMappingBuilder<T> SetTable(Action<TableAttribute> setColumn)
		{
			return SetAttribute(
				() =>
				{
					var a = new TableAttribute { Configuration = Configuration, IsColumnAttributeRequired = false };
					setColumn(a);
					return a;
				},
				setColumn,
				a => a.Configuration,
				a => new TableAttribute
				{
					Configuration             = a.Configuration,
					Name                      = a.Name,
					Schema                    = a.Schema,
					Database                  = a.Database,
					IsColumnAttributeRequired = a.IsColumnAttributeRequired,
				});
		}

		EntityMappingBuilder<T> SetAttribute<TA>(
			Func<TA>        getNew,
			Action<TA>      modifyExisting,
			Func<TA,string> configGetter,
			Func<TA,TA>     overrideAttribute)
			where TA : Attribute
		{
			var attrs = GetAttributes(typeof(T), configGetter);

			if (attrs.Length == 0)
			{
				var attr = _builder.MappingSchema.GetAttribute(typeof(T), configGetter);

				if (attr != null)
				{
					var na = overrideAttribute(attr);

					modifyExisting(na);
					_builder.HasAttribute(typeof(T), na);

					return this;
				}

				_builder.HasAttribute(typeof(T), getNew());
			}
			else
				modifyExisting(attrs[0]);

			return this;
		}

		internal EntityMappingBuilder<T> SetAttribute<TA>(
			Func<TA> getNew,
			Action<TA> modifyExisting,
			Func<TA, string> configGetter,
			Func<IEnumerable<TA>, TA> existingGetter)
			where TA : Attribute
		{
			var attr = existingGetter(GetAttributes(typeof(T), configGetter));

			if (attr == null)
			{
				_builder.HasAttribute(typeof(T), getNew());
			}
			else
			{
				modifyExisting(attr);
			}

			return this;
		}

		internal EntityMappingBuilder<T> SetAttribute<TA>(
			Expression<Func<T,object>> func,
			bool                       processNewExpression,
			Func<bool,TA>              getNew,
			Action<bool,TA>            modifyExisting,
			Func<TA,string>            configGetter,
			Func<TA,TA>                overrideAttribute = null,
			Func<IEnumerable<TA>, TA>  existingGetter    = null
			)
			where TA : Attribute
		{
			var ex = func.Body;

			if (ex is UnaryExpression)
				ex = ((UnaryExpression)ex).Operand;

			if (existingGetter == null)
				existingGetter = GetExisting;

			void SetAttr(Expression e, bool m)
			{
				var memberInfo = MemberHelper.GetMemberInfo(e);

				if (e is MemberExpression && memberInfo.ReflectedTypeEx() != typeof(T)) memberInfo = typeof(T).GetMemberEx(memberInfo);

				if (memberInfo == null) throw new ArgumentException($"'{e}' cant be converted to a class member.");

				var attr = existingGetter(GetAttributes(memberInfo, configGetter));

				if (attr == null)
				{
					if (overrideAttribute != null)
					{
						attr = existingGetter(_builder.MappingSchema.GetAttributes(typeof(T), memberInfo, configGetter));

						if (attr != null)
						{
							var na = overrideAttribute(attr);

							modifyExisting(m, na);
							_builder.HasAttribute(memberInfo, na);

							return;
						}
					}

					_builder.HasAttribute(memberInfo, getNew(m));
				}
				else
					modifyExisting(m, attr);
			}

			if (processNewExpression && ex.NodeType == ExpressionType.New)
			{
				var nex = (NewExpression)ex;

				if (nex.Arguments.Count > 0)
				{
					foreach (var arg in nex.Arguments)
						SetAttr(arg, true);
					return this;
				}
			}

			SetAttr(ex, false);

			return this;
		}

		private TA GetExisting<TA>(IEnumerable<TA> attrs)
			where TA : Attribute
		{
			return attrs.FirstOrDefault();
		}
	}
}
