using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using LinqToDB.Expressions;
using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Extensions;
using LinqToDB.SqlQuery;

namespace LinqToDB.Mapping
{
	/// <summary>
	/// Column or association fluent mapping builder.
	/// </summary>
	/// <typeparam name="TEntity">Entity type.</typeparam>
	/// <typeparam name="TProperty">Column or association member type.</typeparam>
	public class PropertyMappingBuilder<TEntity, TProperty>
	{
		#region Init

		/// <summary>
		/// Creates column or association fluent mapping builder.
		/// </summary>
		/// <param name="entity">Entity fluent mapping builder.</param>
		/// <param name="memberGetter">Column or association member getter expression.</param>
		public PropertyMappingBuilder(
			EntityMappingBuilder<TEntity>       entity,
			Expression<Func<TEntity,TProperty>> memberGetter)
		{
			_entity       = entity       ?? throw new ArgumentNullException(nameof(entity));
			_memberGetter = memberGetter ?? throw new ArgumentNullException(nameof(memberGetter));
			_memberInfo   = MemberHelper.MemberOf(memberGetter);

			if (_memberInfo.ReflectedType != typeof(TEntity))
				_memberInfo = typeof(TEntity).GetMemberEx(_memberInfo) ?? _memberInfo;
		}

		readonly Expression<Func<TEntity,TProperty>> _memberGetter;
		readonly MemberInfo                          _memberInfo;
		readonly EntityMappingBuilder<TEntity>       _entity;

		#endregion
		/// <summary>
		/// Adds attribute to current mapping member.
		/// </summary>
		/// <param name="attribute">Mapping attribute to add to specified member.</param>
		/// <returns>Returns current column or association mapping builder.</returns>
		public PropertyMappingBuilder<TEntity, TProperty> HasAttribute(MappingAttribute attribute)
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
		public EntityMappingBuilder<TE> Entity<TE>(string? configuration = null)
		{
			return _entity.Entity<TE>(configuration);
		}

		/// <summary>
		/// Adds new column mapping to current column's entity.
		/// </summary>
		/// <param name="func">Column mapping property or field getter expression.</param>
		/// <returns>Returns property mapping builder.</returns>
		public PropertyMappingBuilder<TEntity, TMember> Property<TMember>(Expression<Func<TEntity, TMember>> func)
		{
			return _entity.Property(func);
		}

		/// <summary>
		/// Adds member mapping to current entity.
		/// </summary>
		/// <param name="func">Column mapping property or field getter expression.</param>
		/// <returns>Returns fluent property mapping builder.</returns>
		public PropertyMappingBuilder<TEntity, TMember> Member<TMember>(Expression<Func<TEntity,TMember>> func)
		{
			return _entity.Member(func);
		}

		/// <summary>
		/// Adds association mapping to current column's entity.
		/// </summary>
		/// <typeparam name="TOther">Association member type.</typeparam>
		/// <typeparam name="TThisKey">This association side key type.</typeparam>
		/// <typeparam name="TOtherKey">Other association side key type.</typeparam>
		/// <param name="prop">Association member getter expression.</param>
		/// <param name="thisKey">This association key getter expression.</param>
		/// <param name="otherKey">Other association key getter expression.</param>
		/// <param name="canBeNull">Defines type of join. True - left join, False - inner join.</param>
		/// <returns>Returns association mapping builder.</returns>
		public PropertyMappingBuilder<TEntity, TOther> Association<TOther, TThisKey, TOtherKey>(
			Expression<Func<TEntity, TOther>>   prop,
			Expression<Func<TEntity, TThisKey>> thisKey,
			Expression<Func<TOther, TOtherKey>> otherKey,
			bool?                               canBeNull = null)
		{
			return _entity.Association(prop, thisKey, otherKey, canBeNull);
		}

		/// <summary>
		/// Adds association mapping to current column's entity.
		/// </summary>
		/// <typeparam name="TPropElement">Association member type.</typeparam>
		/// <typeparam name="TThisKey">This association side key type.</typeparam>
		/// <typeparam name="TOtherKey">Other association side key type.</typeparam>
		/// <param name="prop">Association member getter expression.</param>
		/// <param name="thisKey">This association key getter expression.</param>
		/// <param name="otherKey">Other association key getter expression.</param>
		/// <param name="canBeNull">Defines type of join. True - left join, False - inner join.</param>
		/// <returns>Returns fluent property mapping builder.</returns>
		public PropertyMappingBuilder<TEntity, IEnumerable<TPropElement>> Association<TPropElement, TThisKey, TOtherKey>(
			Expression<Func<TEntity, IEnumerable<TPropElement>>> prop,
			Expression<Func<TEntity, TThisKey>>                  thisKey,
			Expression<Func<TPropElement, TOtherKey>>            otherKey,
			bool?                                                canBeNull = null)
		{
			return _entity.Association(prop, thisKey, otherKey, canBeNull);
		}

		/// <summary>
		/// Adds association mapping to current column's entity.
		/// </summary>
		/// <typeparam name="TOther">Other association side type</typeparam>
		/// <param name="prop">Association member getter expression.</param>
		/// <param name="predicate">Predicate expression.</param>
		/// <param name="canBeNull">Defines type of join. True - left join, False - inner join.</param>
		/// <returns>Returns fluent property mapping builder.</returns>
		public PropertyMappingBuilder<TEntity, IEnumerable<TOther>> Association<TOther>(
			Expression<Func<TEntity, IEnumerable<TOther>>> prop,
			Expression<Func<TEntity, TOther, bool>>        predicate,
			bool?                                          canBeNull = null)
		{
			return _entity.Association(prop, predicate, canBeNull);
		}

		/// <summary>
		/// Adds association mapping to current column's entity.
		/// </summary>
		/// <typeparam name="TOther">Other association side type</typeparam>
		/// <param name="prop">Association member getter expression.</param>
		/// <param name="predicate">Predicate expression</param>
		/// <param name="canBeNull">Defines type of join. True - left join, False - inner join.</param>
		/// <returns>Returns fluent property mapping builder.</returns>
		public PropertyMappingBuilder<TEntity, TOther> Association<TOther>(
			Expression<Func<TEntity, TOther>>       prop,
			Expression<Func<TEntity, TOther, bool>> predicate,
			bool?                                   canBeNull = null)
		{
			return _entity.Association(prop, predicate, canBeNull);
		}

		/// <summary>
		/// Adds association mapping to current column's entity.
		/// </summary>
		/// <typeparam name="TOther">Other association side type</typeparam>
		/// <param name="prop">Association member getter expression.</param>
		/// <param name="queryExpression">Query expression.</param>
		/// <param name="canBeNull">Defines type of join. True - left join, False - inner join.</param>
		/// <returns>Returns fluent property mapping builder.</returns>
		public PropertyMappingBuilder<TEntity, IEnumerable<TOther>> Association<TOther>(
			Expression<Func<TEntity, IEnumerable<TOther>>>              prop,
			Expression<Func<TEntity, IDataContext, IQueryable<TOther>>> queryExpression,
			bool?                                                       canBeNull = null)
		{
			return _entity.Association(prop, queryExpression, canBeNull);
		}

		/// <summary>
		/// Adds association mapping to current column's entity.
		/// </summary>
		/// <typeparam name="TOther">Other association side type</typeparam>
		/// <param name="prop">Association member getter expression.</param>
		/// <param name="queryExpression">Query expression.</param>
		/// <param name="canBeNull">Defines type of join. True - left join, False - inner join.</param>
		/// <returns>Returns fluent property mapping builder.</returns>
		public PropertyMappingBuilder<TEntity, TOther> Association<TOther>(
			Expression<Func<TEntity, TOther>>                           prop,
			Expression<Func<TEntity, IDataContext, IQueryable<TOther>>> queryExpression,
			bool?                                                       canBeNull = null)
		{
			return _entity.Association(prop, queryExpression, canBeNull);
		}

		/// <summary>
		/// Marks current column as primary key member.
		/// </summary>
		/// <param name="order">Order of property in primary key.</param>
		/// <returns>Returns current column mapping builder.</returns>
		public PropertyMappingBuilder<TEntity, TProperty> IsPrimaryKey(int order = -1)
		{
			_entity.HasPrimaryKey(_memberGetter, order);
			return this;
		}

		/// <summary>
		/// Marks current column as identity column.
		/// </summary>
		/// <returns>Returns current column mapping builder.</returns>
		public PropertyMappingBuilder<TEntity, TProperty> IsIdentity()
		{
			_entity.HasIdentity(_memberGetter);
			return this;
		}

		PropertyMappingBuilder<TEntity, TProperty> SetColumn(Action<ColumnAttribute> setColumn)
		{
			var getter     = _memberGetter;
			var memberName = null as string;

			if (_memberGetter.Body.Unwrap() is MemberExpression { Expression: MemberExpression } me)
			{
				for (var m = me; m != null; m = m.Expression as MemberExpression)
				{
					memberName = m.Member.Name + (memberName != null ? "." + memberName : "");
				}

				_entity.SetAttribute(
					() =>
					{
						var a = new ColumnAttribute { Configuration = _entity.Configuration, MemberName = memberName };
						setColumn(a);
						return a;
					},
					setColumn,
					attrs => attrs.FirstOrDefault(_ => memberName == null || memberName.Equals(_.MemberName, StringComparison.Ordinal)));

				return this;
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
					a     => new ColumnAttribute(a),
					attrs => attrs.FirstOrDefault(_ => memberName == null || memberName.Equals(_.MemberName, StringComparison.Ordinal)));

			return this;
		}

		/// <summary>
		/// Sets name for current column.
		/// </summary>
		/// <param name="columnName">Column name.</param>
		/// <returns>Returns current column mapping builder.</returns>
		public PropertyMappingBuilder<TEntity, TProperty> HasColumnName(string columnName)
		{
			return SetColumn(a => a.Name = columnName);
		}

		/// <summary>
		/// Sets LINQ to DB type for current column.
		/// </summary>
		/// <param name="dataType">Data type.</param>
		/// <returns>Returns current column mapping builder.</returns>
		public PropertyMappingBuilder<TEntity, TProperty> HasDataType(DataType dataType)
		{
			return SetColumn(a => a.DataType = dataType);
		}

		/// <summary>
		/// Sets database type for current column.
		/// </summary>
		/// <param name="dbType">Column type.</param>
		/// <returns>Returns current column mapping builder.</returns>
		public PropertyMappingBuilder<TEntity, TProperty> HasDbType(string dbType)
		{
			return SetColumn(a => a.DbType = dbType);
		}

		/// <summary>
		/// Sets custom column create SQL template.
		/// </summary>
		/// <param name="format">
		/// Custom template for column definition in create table SQL expression, generated using
		/// <see cref="DataExtensions.CreateTable{T}(IDataContext, string?, string?, string?, string?, string?, DefaultNullable, string?, TableOptions)"/> methods.
		/// Template accepts following string parameters:
		/// - {0} - column name;
		/// - {1} - column type;
		/// - {2} - NULL specifier;
		/// - {3} - identity specification.
		/// </param>
		/// <returns>Returns current column mapping builder.</returns>
		public PropertyMappingBuilder<TEntity, TProperty> HasCreateFormat(string format)
		{
			return SetColumn(a => a.CreateFormat = format);
		}

		/// <summary>
		/// Adds data storage property or field for current column.
		/// </summary>
		/// <param name="storage">Name of storage property or field for current column.</param>
		/// <returns>Returns current column mapping builder.</returns>
		public PropertyMappingBuilder<TEntity, TProperty> HasStorage(string storage)
		{
			return SetColumn(a => a.Storage = storage);
		}

		/// <summary>
		/// Marks current column as discriminator column for inheritance mapping.
		/// </summary>
		/// <param name="isDiscriminator">If <see langword="true"/> - column is used as inheritance mapping discriminator.</param>
		/// <returns>Returns current column mapping builder.</returns>
		public PropertyMappingBuilder<TEntity, TProperty> IsDiscriminator(bool isDiscriminator = true)
		{
			return SetColumn(a => a.IsDiscriminator = isDiscriminator);
		}

		/// <summary>
		/// Marks current column to be skipped by default during a full entity fetch
		/// </summary>
		/// <param name="skipOnEntityFetch">If <see langword="true"/>, column won't be fetched unless explicity selected in a Linq query.</param>
		/// <returns>Returns current column mapping builder.</returns>
		public PropertyMappingBuilder<TEntity, TProperty> SkipOnEntityFetch(bool skipOnEntityFetch = true)
		{
			return SetColumn(a => a.SkipOnEntityFetch = skipOnEntityFetch);
		}

		/// <summary>
		/// Sets whether a column is insertable.
		/// This flag will affect only insert operations with implicit columns specification like
		/// <see cref="DataExtensions.Insert{T}(IDataContext, T, string?, string?, string?, string?, TableOptions)"/>
		/// method and will be ignored when user explicitly specifies value for this column.
		/// </summary>
		/// <param name="skipOnInsert">If <see langword="true"/> - column will be ignored for implicit insert operations.</param>
		/// <returns>Returns current column mapping builder.</returns>
		public PropertyMappingBuilder<TEntity, TProperty> HasSkipOnInsert(bool skipOnInsert = true)
		{
			return SetColumn(a => a.SkipOnInsert = skipOnInsert);
		}

		/// <summary>
		/// Sets whether a column is updatable.
		/// This flag will affect only update operations with implicit columns specification like
		/// <see cref="DataExtensions.Update{T}(IDataContext, T, string?, string?, string?, string?, TableOptions)"/>
		/// method and will be ignored when user explicitly specifies value for this column.
		/// </summary>
		/// <param name="skipOnUpdate">If <see langword="true"/> - column will be ignored for implicit update operations.</param>
		/// <returns>Returns current column mapping builder.</returns>
		public PropertyMappingBuilder<TEntity, TProperty> HasSkipOnUpdate(bool skipOnUpdate = true)
		{
			return SetColumn(a => a.SkipOnUpdate = skipOnUpdate);
		}

		/// <summary>
		/// Sets whether a column can contain <c>NULL</c> values.
		/// </summary>
		/// <param name="isNullable">If <see langword="true"/> - column could contain <c>NULL</c> values.</param>
		/// <returns>Returns current column mapping builder.</returns>
		public PropertyMappingBuilder<TEntity, TProperty> IsNullable(bool isNullable = true)
		{
			return SetColumn(a => a.CanBeNull = isNullable);
		}

		/// <summary>
		/// Sets the column as <c>NOT NULL</c>, disallowing any <c>NULL</c> values.
		/// </summary>
		/// <returns>Returns current column mapping builder.</returns>
		public PropertyMappingBuilder<TEntity, TProperty> IsNotNull()
		{
			return SetColumn(a => a.CanBeNull = false);
		}

		/// <summary>
		/// Sets current member to be excluded from mapping.
		/// </summary>
		/// <returns>Returns current mapping builder.</returns>
		public PropertyMappingBuilder<TEntity, TProperty> IsNotColumn()
		{
			return SetColumn(a => a.IsColumn = false);
		}

		/// <summary>
		/// Sets current member to be included into mapping as column.
		/// </summary>
		/// <returns>Returns current column mapping builder.</returns>
		public PropertyMappingBuilder<TEntity, TProperty> IsColumn()
		{
			return SetColumn(a => a.IsColumn = true);
		}

		/// <summary>
		/// Sets the length of the database column.
		/// </summary>
		/// <param name="length">Column length.</param>
		/// <returns>Returns current column mapping builder.</returns>
		public PropertyMappingBuilder<TEntity, TProperty> HasLength(int length)
		{
			return SetColumn(a => a.Length = length);
		}

		/// <summary>
		/// Sets the precision of the database column.
		/// </summary>
		/// <param name="precision">Column precision.</param>
		/// <returns>Returns current column mapping builder.</returns>
		public PropertyMappingBuilder<TEntity, TProperty> HasPrecision(int precision)
		{
			return SetColumn(a => a.Precision = precision);
		}

		/// <summary>
		/// Sets the Scale of the database column.
		/// </summary>
		/// <param name="scale">Column scale.</param>
		/// <returns>Returns current column mapping builder.</returns>
		public PropertyMappingBuilder<TEntity, TProperty> HasScale(int scale)
		{
			return SetColumn(a => a.Scale = scale);
		}

		/// <summary>
		/// Sets the Order of the database column.
		/// </summary>
		/// <param name="order">Column order.</param>
		/// <returns>Returns current column mapping builder.</returns>
		public PropertyMappingBuilder<TEntity, TProperty> HasOrder(int order)
		{
			return SetColumn(a => a.Order = order);
		}

		/// <summary>
		/// Sets that property is alias to another member.
		/// </summary>
		/// <param name="aliasMember">Alias member getter expression.</param>
		/// <returns>Returns current column mapping builder.</returns>
		public PropertyMappingBuilder<TEntity, TProperty> IsAlias(Expression<Func<TEntity, object>> aliasMember)
		{
			ArgumentNullException.ThrowIfNull(aliasMember);

			var memberInfo = MemberHelper.GetMemberInfo(aliasMember);

			if (memberInfo == null)
				throw new ArgumentException($"Cannot deduce MemberInfo from Lambda: '{aliasMember}'");

			return HasAttribute(new ColumnAliasAttribute(memberInfo.Name));
		}

		/// <summary>
		/// Sets that property is alias to another member.
		/// </summary>
		/// <param name="aliasMember">Alias member name.</param>
		/// <returns>Returns current column mapping builder.</returns>
		public PropertyMappingBuilder<TEntity, TProperty> IsAlias(string aliasMember)
		{
			if (string.IsNullOrEmpty(aliasMember))
				throw new ArgumentException("Value cannot be null or empty.", nameof(aliasMember));

			var memberInfo = typeof(TEntity).GetMember(aliasMember);
			if (memberInfo == null)
				throw new ArgumentException($"Member '{aliasMember}' not found in type '{typeof(TEntity)}'");

			return HasAttribute(new ColumnAliasAttribute(aliasMember));
		}

		/// <summary>
		/// Configure property as alias to another member.
		/// </summary>
		/// <param name="expression">Expression for mapping member during read.</param>
		/// <param name="isColumn">Indicates whether a property value should be filled during entity materialization (calculated property).</param>
		/// <param name="alias">Optional alias for specific member expression. By default Member Name is used.</param>
		/// <returns>Returns current column mapping builder.</returns>
		public PropertyMappingBuilder<TEntity, TProperty> IsExpression<TR>(Expression<Func<TEntity, TR>> expression, bool isColumn = false, string? alias = null)
		{
			ArgumentNullException.ThrowIfNull(expression);

			return HasAttribute(new ExpressionMethodAttribute(expression) { IsColumn = isColumn, Alias = alias }).IsNotColumn();
		}

		/// <summary>
		///     Configures the property so that the property value is converted to the given type before
		///     writing to the database and converted back when reading from the database.
		/// </summary>
		/// <typeparam name="TProvider"> The type to convert to and from. </typeparam>
		/// <returns>Returns current column mapping builder.</returns>
		public PropertyMappingBuilder<TEntity, TProperty> HasConversionFunc<TProvider>(Func<TProperty, TProvider> toProvider, Func<TProvider, TProperty> toModel, bool handlesNulls = false)
		{
			return HasAttribute(new ValueConverterAttribute { ValueConverter = new ValueConverterFunc<TProperty, TProvider>(toProvider, toModel, handlesNulls) });
		}

		/// <summary>
		///     Configures the property so that the property value is converted to the given type before
		///     writing to the database and converted back when reading from the database.
		/// </summary>
		/// <typeparam name="TProvider"> The type to convert to and from. </typeparam>
		/// <returns>Returns current column mapping builder.</returns>
		public PropertyMappingBuilder<TEntity, TProperty> HasConversion<TProvider>(Expression<Func<TProperty, TProvider>> toProvider, Expression<Func<TProvider, TProperty>> toModel, bool handlesNulls = false)
		{
			return HasAttribute(new ValueConverterAttribute { ValueConverter = new ValueConverter<TProperty, TProvider>(toProvider, toModel, handlesNulls) });
		}

		/// <summary>
		/// Specifies value generation sequence for current column.
		/// See <see cref="SequenceNameAttribute"/> notes for list of supported databases.
		/// </summary>
		/// <param name="sequenceName">Name of sequence.</param>
		/// <param name="schema">Optional sequence schema name.</param>
		/// <param name="configuration">Optional mapping configuration name. If not specified, entity configuration used.</param>
		/// <returns>Returns current column mapping builder.</returns>
		public PropertyMappingBuilder<TEntity, TProperty> UseSequence(string sequenceName, string? schema = null, string? configuration = null)
		{
			return HasAttribute(new SequenceNameAttribute(configuration ?? _entity.Configuration, sequenceName) { Schema = schema });
		}

		/// <summary>
		/// Adds configured mappings to builder's mapping schema.
		/// </summary>
		public FluentMappingBuilder Build()
		{
			return _entity.Build();
		}
	}
}
