﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using LinqToDB.Extensions;

namespace LinqToDB.Mapping
{
	public class EntityMappingBuilder<T>
	{
		#region Init

		public EntityMappingBuilder([JetBrains.Annotations.NotNull] FluentMappingBuilder builder, string configuration)
		{
			if (builder == null) throw new ArgumentNullException("builder");

			_builder      = builder;
			Configuration = configuration;

			// We'll reset cache here, because there is no need to create builder if you don't want to change something
			_builder.MappingSchema.ResetEntityDescriptor(typeof(T));

		}

		readonly FluentMappingBuilder _builder;

		public string Configuration { get; private set; }

		#endregion

		#region GetAttributes

		public TA[] GetAttributes<TA>()
			where TA : Attribute
		{
			return _builder.GetAttributes<TA>(typeof(T));
		}

		public TA[] GetAttributes<TA>(Type type)
			where TA : Attribute
		{
			return _builder.GetAttributes<TA>(type);
		}

		public TA[] GetAttributes<TA>(MemberInfo memberInfo)
			where TA : Attribute
		{
			return _builder.GetAttributes<TA>(typeof(T), memberInfo);
		}

		public TA[] GetAttributes<TA>(Func<TA,string> configGetter)
			where TA : Attribute
		{
			var attrs = GetAttributes<TA>();

			return string.IsNullOrEmpty(Configuration) ?
				attrs.Where(a => string.IsNullOrEmpty(configGetter(a))).ToArray() :
				attrs.Where(a => Configuration ==    configGetter(a)). ToArray();
		}

		public TA[] GetAttributes<TA>(Type type, Func<TA,string> configGetter)
			where TA : Attribute
		{
			var attrs = GetAttributes<TA>(type);

			return string.IsNullOrEmpty(Configuration) ?
				attrs.Where(a => string.IsNullOrEmpty(configGetter(a))).ToArray() :
				attrs.Where(a => Configuration ==    configGetter(a)). ToArray();
		}

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

		public EntityMappingBuilder<T> HasAttribute(Attribute attribute)
		{
			_builder.HasAttribute(typeof(T), attribute);
			return this;
		}

		public EntityMappingBuilder<T> HasAttribute(MemberInfo memberInfo, Attribute attribute)
		{
			_builder.HasAttribute(memberInfo, attribute);
			return this;
		}

		public EntityMappingBuilder<T> HasAttribute(LambdaExpression func, Attribute attribute)
		{
			_builder.HasAttribute(func, attribute);
			return this;
		}

		public EntityMappingBuilder<T> HasAttribute(Expression<Func<T,object>> func, Attribute attribute)
		{
			_builder.HasAttribute(func, attribute);
			return this;
		}

		#endregion

		public EntityMappingBuilder<TE> Entity<TE>(string configuration = null)
		{
			return _builder.Entity<TE>(configuration);
		}

		public PropertyMappingBuilder<T> Property(Expression<Func<T,object>> func)
		{
			return (new PropertyMappingBuilder<T>(this, func)).IsColumn();
		}

		public PropertyMappingBuilder<T> Association<S, ID1, ID2>(
			Expression<Func<T, S>> prop,
			Expression<Func<T, ID1>> thisKey,
			Expression<Func<S, ID2>> otherKey )
		{
			var thisKeyName = ((MemberExpression)thisKey.Body).Member.Name;
			var otherKeyName = ((MemberExpression)otherKey.Body).Member.Name;

			var objProp = Expression.Lambda<Func<T, object>>(Expression.Convert(prop.Body, typeof(object)), prop.Parameters );

			return Property( objProp ).HasAttribute( new AssociationAttribute { ThisKey = thisKeyName, OtherKey = otherKeyName } );
		}

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

		public EntityMappingBuilder<T> HasIdentity(Expression<Func<T,object>> func)
		{
			return SetAttribute(
				func,
				false,
				 _    => new IdentityAttribute(Configuration),
				(_,a) => {},
				a => a.Configuration);
		}

		public EntityMappingBuilder<T> HasColumn(Expression<Func<T,object>> func, int order = -1)
		{
			return SetAttribute(
				func,
				true,
				 _    => new ColumnAttribute(Configuration),
				(_,a) => a.IsColumn = true,
				a => a.Configuration);
		}

		public EntityMappingBuilder<T> Ignore(Expression<Func<T,object>> func, int order = -1)
		{
			return SetAttribute(
				func,
				true,
				 _    => new NotColumnAttribute { Configuration = Configuration },
				(_,a) => a.IsColumn = false,
				a => a.Configuration);
		}

		public EntityMappingBuilder<T> HasTableName(string tableName)
		{
			return SetTable(a => a.Name = tableName);
		}

		public EntityMappingBuilder<T> HasSchemaName(string schemaName)
		{
			return SetTable(a => a.Schema = schemaName);
		}

		public EntityMappingBuilder<T> HasDatabaseName(string databaseName)
		{
			return SetTable(a => a.Database = databaseName);
		}

		public EntityMappingBuilder<T> Inheritance<S>(Expression<Func<T, S>> key, S value, Type type, bool isDefault = false)
		{
			HasAttribute(new InheritanceMappingAttribute() {Code = value, Type = type, IsDefault = isDefault});
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

			Action<Expression,bool> setAttr = (e,m) =>
			{
				var memberInfo =
					e is MemberExpression     ? ((MemberExpression)    e).Member :
					e is MethodCallExpression ? ((MethodCallExpression)e).Method : null;

				if (e is MemberExpression && memberInfo.ReflectedTypeEx() != typeof(T))
					memberInfo = typeof(T).GetMemberEx(memberInfo);

				if (memberInfo == null)
					throw new ArgumentException(string.Format("'{0}' cant be converted to a class member.", e));

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
			};

			if (processNewExpression && ex.NodeType == ExpressionType.New)
			{
				var nex = (NewExpression)ex;

				if (nex.Arguments.Count > 0)
				{
					foreach (var arg in nex.Arguments)
						setAttr(arg, true);
					return this;
				}
			}

			setAttr(ex, false);

			return this;
		}

		private TA GetExisting<TA>(IEnumerable<TA> attrs)
			where TA : Attribute
		{
			return attrs.FirstOrDefault();
		}
	}
}
