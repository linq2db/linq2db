using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using LinqToDB.Extensions;

namespace LinqToDB.Mapping
{
	using System.Linq;

	using Expressions;

	/// <summary>
	/// Column or association fluent mapping builder.
	/// </summary>
	/// <typeparam name="T">Column or asociation member type.</typeparam>
	public class PropertyMappingBuilder<T>
	{
		#region Init

		/// <summary>
		/// Creates column or association fluent mapping builder.
		/// </summary>
		/// <param name="entity">Entity fluent mapping builder.</param>
		/// <param name="memberGetter">Column or association member getter expression.</param>
		public PropertyMappingBuilder(
			[JetBrains.Annotations.NotNull] EntityMappingBuilder<T>    entity,
			[JetBrains.Annotations.NotNull] Expression<Func<T,object>> memberGetter)
		{
			if (entity       == null) throw new ArgumentNullException("entity");
			if (memberGetter == null) throw new ArgumentNullException("memberGetter");

			_entity       = entity;
			_memberGetter = memberGetter;
			_memberInfo   = MemberHelper.MemberOf(memberGetter);

			if (_memberInfo.ReflectedTypeEx() != typeof(T))
				_memberInfo = typeof(T).GetMemberEx(_memberInfo) ?? _memberInfo;
		}

		readonly Expression<Func<T,object>> _memberGetter;
		readonly MemberInfo                 _memberInfo;
		readonly EntityMappingBuilder<T>    _entity;

		#endregion
		/// <summary>
		/// Adds attribute to current mapping member.
		/// </summary>
		/// <param name="attribute">Mapping attribute to add to specified member.</param>
		/// <returns>Returns current column or association mapping builder.</returns>
		public PropertyMappingBuilder<T> HasAttribute(Attribute attribute)
		{
			_entity.HasAttribute(_memberInfo, attribute);
			return this;
		}

		/// <summary>
		/// Creates entity builder for specified mapping type.
		/// </summary>
		/// <typeparam name="TE">Mapping type.</typeparam>
		/// <param name="configuration">Optional mapping schema configuration name, for which this entity builder should be taken into account.
		/// <see cref="ProviderName"/> for standard configuration names.</param>
		/// <returns>Returns entity mapping builder.</returns>
		public EntityMappingBuilder<TE> Entity<TE>(string configuration = null)
		{
			return _entity.Entity<TE>(configuration);
		}

		/// <summary>
		/// Adds new column mapping to current column's entity.
		/// </summary>
		/// <param name="func">Column mapping property or field getter expression.</param>
		/// <returns>Returns property mapping builder.</returns>
		public PropertyMappingBuilder<T> Property(Expression<Func<T,object>> func)
		{
			return _entity.Property(func);
		}

		/// <summary>
		/// Adds association mapping to current column's entity.
		/// </summary>
		/// <typeparam name="S">Association member type.</typeparam>
		/// <typeparam name="ID1">This association side key type.</typeparam>
		/// <typeparam name="ID2">Other association side key type.</typeparam>
		/// <param name="prop">Association member getter expression.</param>
		/// <param name="thisKey">This association key getter expression.</param>
		/// <param name="otherKey">Other association key getter expression.</param>
		/// <returns>Returns association mapping builder.</returns>
		public PropertyMappingBuilder<T> Association<S, ID1, ID2>(
			Expression<Func<T, S>> prop,
			Expression<Func<T, ID1>> thisKey,
			Expression<Func<S, ID2>> otherKey )
		{
			return _entity.Association( prop, thisKey, otherKey );
		}

		/// <summary>
		/// Marks current column as primary key member.
		/// </summary>
		/// <param name="order">Order of property in primary key.</param>
		/// <returns>Returns current column mapping builder.</returns>
		public PropertyMappingBuilder<T> IsPrimaryKey(int order = -1)
		{
			_entity.HasPrimaryKey(_memberGetter, order);
			return this;
		}

		/// <summary>
		/// Marks current column as identity column.
		/// </summary>
		/// <returns>Returns current column mapping builder.</returns>
		public PropertyMappingBuilder<T> IsIdentity()
		{
			_entity.HasIdentity(_memberGetter);
			return this;
		}

		PropertyMappingBuilder<T> SetColumn(Action<ColumnAttribute> setColumn)
		{
			var getter     = _memberGetter;
			var memberName = null as string;
			var me         = _memberGetter.Body.Unwrap() as MemberExpression;

			if (me != null && me.Expression is MemberExpression)
			{
				for (var m = me; m != null; m = m.Expression as MemberExpression)
				{
					if (me != m)
						memberName = me.Member.Name + (string.IsNullOrEmpty(memberName) ? "" : "." + memberName);

					me = m;
				}

				var p  = Expression.Parameter(typeof(T));
				getter = Expression.Lambda<Func<T, object>>(Expression.PropertyOrField(p, me.Member.Name), p);
			}

			_entity.SetAttribute(
					getter,
					false,
					 _ =>
					 {
						var a = new ColumnAttribute { Configuration = _entity.Configuration, MemberName = memberName};
						setColumn(a);
						return a;
					 },
					(_,a) => setColumn(a),
					a     => a.Configuration,
					a     => new ColumnAttribute(a),
					attrs => attrs.FirstOrDefault(_ => memberName == null || memberName.Equals(_.MemberName)));

			return this;
		}

		/// <summary>
		/// Sets name for current column.
		/// </summary>
		/// <param name="columnName">Column name.</param>
		/// <returns>Returns current column mapping builder.</returns>
		public PropertyMappingBuilder<T> HasColumnName(string columnName)
		{
			return SetColumn(a => a.Name = columnName);
		}

		/// <summary>
		/// Sets LINQ to DB type for current column.
		/// </summary>
		/// <param name="dataType">Data type.</param>
		/// <returns>Returns current column mapping builder.</returns>
		public PropertyMappingBuilder<T> HasDataType(DataType dataType)
		{
			return SetColumn(a => a.DataType = dataType);
		}

		/// <summary>
		/// Sets database type for current column.
		/// </summary>
		/// <param name="dbType">Column type.</param>
		/// <returns>Returns current column mapping builder.</returns>
		public PropertyMappingBuilder<T> HasDbType(string dbType)
		{
			return SetColumn(a => a.DbType = dbType);
		}

		/// <summary>
		/// Sets custom column create SQL template.
		/// </summary>
		/// <param name="format">
		/// Custom template for column definition in create table SQL expression, generated using
		/// <see cref="DataExtensions.CreateTable{T}(IDataContext, string, string, string, string, string, SqlQuery.DefaulNullable)"/> methods.
		/// Template accepts following string parameters:
		/// - {0} - column name;
		/// - {1} - column type;
		/// - {2} - NULL specifier;
		/// - {3} - identity specification.
		/// </param>
		/// <returns>Returns current column mapping builder.</returns>
		public PropertyMappingBuilder<T> HasCreateFormat(string format)
		{
			return SetColumn(a => a.CreateFormat = format);
		}

		/// <summary>
		/// Adds data storage property or field for current column.
		/// </summary>
		/// <param name="storage">Name of storage property or field for current column.</param>
		/// <returns>Returns current column mapping builder.</returns>
		public PropertyMappingBuilder<T> HasStorage(string storage)
		{
			return SetColumn(a => a.Storage = storage);
		}

		/// <summary>
		/// Marks current column as discriminator column for inheritance mapping.
		/// </summary>
		/// <param name="isDiscriminator">If <c>true</c> - column is used as inheritance mapping discriminator.</param>
		/// <returns>Returns current column mapping builder.</returns>
		public PropertyMappingBuilder<T> IsDiscriminator(bool isDiscriminator = true)
		{
			return SetColumn(a => a.IsDiscriminator = isDiscriminator);
		}

		/// <summary>
		/// Sets whether a column is insertable.
		/// This flag will affect only insert operations with implicit columns specification like
		/// <see cref="DataExtensions.Insert{T}(IDataContext, T, string, string, string)"/>
		/// method and will be ignored when user explicitly specifies value for this column.
		/// </summary>
		/// <param name="skipOnInsert">If <c>true</c> - column will be ignored for implicit insert operations.</param>
		/// <returns>Returns current column mapping builder.</returns>
		public PropertyMappingBuilder<T> HasSkipOnInsert(bool skipOnInsert = true)
		{
			return SetColumn(a => a.SkipOnInsert = skipOnInsert);
		}

		/// <summary>
		/// Sets whether a column is updatable.
		/// This flag will affect only update operations with implicit columns specification like
		/// <see cref="DataExtensions.Update{T}(IDataContext, T)"/>
		/// method and will be ignored when user explicitly specifies value for this column.
		/// </summary>
		/// <param name="skipOnUpdate">If <c>true</c> - column will be ignored for implicit update operations.</param>
		/// <returns>Returns current column mapping builder.</returns>
		public PropertyMappingBuilder<T> HasSkipOnUpdate(bool skipOnUpdate = true)
		{
			return SetColumn(a => a.SkipOnUpdate = skipOnUpdate);
		}

		/// <summary>
		/// Sets whether a column can contain <c>NULL</c> values.
		/// </summary>
		/// <param name="isNullable">If <c>true</c> - column could contain <c>NULL</c> values.</param>
		/// <returns>Returns current column mapping builder.</returns>
		public PropertyMappingBuilder<T> IsNullable(bool isNullable = true)
		{
			return SetColumn(a => a.CanBeNull = isNullable);
		}

		/// <summary>
		/// Sets current member to be excluded from mapping.
		/// </summary>
		/// <returns>Returns current mapping builder.</returns>
		public PropertyMappingBuilder<T> IsNotColumn()
		{
			return SetColumn(a => a.IsColumn = false);
		}

		/// <summary>
		/// Sets current member to be included into mapping as column.
		/// </summary>
		/// <returns>Returns current column mapping builder.</returns>
		public PropertyMappingBuilder<T> IsColumn()
		{
			return SetColumn(a => a.IsColumn = true);
		}

		/// <summary>
		/// Sets the length of the database column.
		/// </summary>
		/// <param name="length">Column length.</param>
		/// <returns>Returns current column mapping builder.</returns>
		public PropertyMappingBuilder<T> HasLength(int length)
		{
			return SetColumn(a => a.Length = length);
		}

		/// <summary>
		/// Sets the precision of the database column.
		/// </summary>
		/// <param name="precision">Column precision.</param>
		/// <returns>Returns current column mapping builder.</returns>
		public PropertyMappingBuilder<T> HasPrecision(int precision)
		{
			return SetColumn(a => a.Precision = precision);
		}

		/// <summary>
		/// Sets the Scale of the database column.
		/// </summary>
		/// <param name="scale">Column scale.</param>
		/// <returns>Returns current column mapping builder.</returns>
		public PropertyMappingBuilder<T> HasScale(int scale)
		{
			return SetColumn(a => a.Scale = scale);
		}
	}
}
