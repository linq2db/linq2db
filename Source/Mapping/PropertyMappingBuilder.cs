﻿using System;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Mapping
{
	using Expressions;

	using SqlQuery;

	public class PropertyMappingBuilder<T>
	{
		#region Init

		public PropertyMappingBuilder(
			[JetBrains.Annotations.NotNull] EntityMappingBuilder<T>    entity,
			[JetBrains.Annotations.NotNull] Expression<Func<T,object>> memberGetter)
		{
			if (entity       == null) throw new ArgumentNullException("entity");
			if (memberGetter == null) throw new ArgumentNullException("memberGetter");

			_entity       = entity;
			_memberGetter = memberGetter;
			_memberInfo   = MemberHelper.MemberOf(memberGetter);
		}

		readonly Expression<Func<T,object>> _memberGetter;
		readonly MemberInfo                 _memberInfo;
		readonly EntityMappingBuilder<T>    _entity;

		#endregion

		public PropertyMappingBuilder<T> HasAttribute(Attribute attribute)
		{
			_entity.HasAttribute(_memberInfo, attribute);
			return this;
		}

		public EntityMappingBuilder<TE> Entity<TE>(string configuration = null)
		{
			return _entity.Entity<TE>(configuration);
		}

		public PropertyMappingBuilder<T> Property(Expression<Func<T,object>> func)
		{
			return _entity.Property(func);
		}

		public PropertyMappingBuilder<T> IsPrimaryKey(int order = -1)
		{
			_entity.HasPrimaryKey(_memberGetter, order);
			return this;
		}

		public PropertyMappingBuilder<T> IsIdentity()
		{
			_entity.HasIdentity(_memberGetter);
			return this;
		}

		PropertyMappingBuilder<T> SetColumn(Action<ColumnAttribute> setColumn)
		{
			_entity.SetAttribute(
				_memberGetter,
				false,
				 _ =>
				 {
					var a = new ColumnAttribute { Configuration = _entity.Configuration };
					setColumn(a);
					return a;
				 },
				(_,a) => setColumn(a),
				a => a.Configuration,
				a => new ColumnAttribute(a));

			return this;
		}

		public PropertyMappingBuilder<T> HasColumnName(string columnName)
		{
			return SetColumn(a => a.Name = columnName);
		}

		public PropertyMappingBuilder<T> HasDataType(DataType dataType)
		{
			return SetColumn(a => a.DataType = dataType);
		}

		public PropertyMappingBuilder<T> HasDbType(string dbType)
		{
			return SetColumn(a => a.DbType = dbType);
		}

		public PropertyMappingBuilder<T> HasStorage(string storage)
		{
			return SetColumn(a => a.Storage = storage);
		}

		public PropertyMappingBuilder<T> IsDiscriminator(bool isDiscriminator = true)
		{
			return SetColumn(a => a.IsDiscriminator = isDiscriminator);
		}

		public PropertyMappingBuilder<T> HasSkipOnInsert(bool skipOnInsert = true)
		{
			return SetColumn(a => a.SkipOnInsert = skipOnInsert);
		}

		public PropertyMappingBuilder<T> HasSkipOnUpdate(bool skipOnUpdate = true)
		{
			return SetColumn(a => a.SkipOnUpdate = skipOnUpdate);
		}

		public PropertyMappingBuilder<T> IsNullable(bool isNullable = true)
		{
			return SetColumn(a => a.CanBeNull = isNullable);
		}

		public PropertyMappingBuilder<T> IsNotColumn()
		{
			return SetColumn(a => a.IsColumn = false);
		}

		public PropertyMappingBuilder<T> HasLength(int length)
		{
			return SetColumn(a => a.Length = length);
		}

		public PropertyMappingBuilder<T> HasPrecision(int precision)
		{
			return SetColumn(a => a.Precision = precision);
		}

		public PropertyMappingBuilder<T> HasScale(int scale)
		{
			return SetColumn(a => a.Scale = scale);
		}
	}
}
