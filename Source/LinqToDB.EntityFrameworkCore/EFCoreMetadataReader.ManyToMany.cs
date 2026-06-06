#if !EF31
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;

using LinqToDB.Mapping;
using LinqToDB.Internal.Mapping;
using LinqToDB.Internal.Reflection;
using LinqToDB.EntityFrameworkCore.Internal;

namespace LinqToDB.EntityFrameworkCore
{
	// Many-to-many (skip navigation) support: EF Core models many-to-many relationships through a
	// hidden join entity rather than a regular navigation. This partial builds an association query
	// expression that joins the target entities through that join table, addressed via the
	// EfJoinTable<,> marker type (see EfJoinTable.cs).
	partial class EFCoreMetadataReader
	{
		sealed class ManyToManyJoinInfo
		{
			public IEntityType JoinEntityType  { get; init; } = null!;
			public IForeignKey ThisForeignKey  { get; init; } = null!; // join -> this
			public IForeignKey OtherForeignKey { get; init; } = null!; // join -> other
		}

		/// <summary>
		/// Resolves EF Core many-to-many join metadata for an <see cref="EfJoinTable{TThis,TOther}"/> marker type.
		/// Returns <see langword="null"/> when <paramref name="type"/> is not such a marker or no matching skip navigation exists.
		/// </summary>
		private ManyToManyJoinInfo? ResolveManyToManyJoin(Type type)
		{
			if (_model == null)
				return null;

			if (!type.IsGenericType || type.GetGenericTypeDefinition() != typeof(EfJoinTable<,>))
				return null;

			return _manyToManyJoins.GetOrAdd(type, static (markerType, model) =>
			{
				var args      = markerType.GetGenericArguments();
				var thisType  = args[0];
				var otherType = args[1];

				var entityType = model.FindEntityType(thisType);
				if (entityType == null)
					return null;

				ISkipNavigation? skipNavigation = null;
				foreach (var nav in entityType.GetSkipNavigations())
				{
					if (nav.TargetEntityType.ClrType == otherType)
					{
						if (skipNavigation != null)
							throw new LinqToDBException($"Multiple many-to-many relationships between '{thisType.Name}' and '{otherType.Name}' are not supported. Use an explicit join entity.");

						skipNavigation = nav;
					}
				}

				if (skipNavigation == null)
					return null;

				return new ManyToManyJoinInfo()
				{
					JoinEntityType  = skipNavigation.JoinEntityType,
					ThisForeignKey  = skipNavigation.ForeignKey,
					OtherForeignKey = skipNavigation.Inverse.ForeignKey,
				};
			}, _model);
		}

		/// <summary>
		/// Builds a <see cref="ColumnAttribute"/> for a foreign key column of a many-to-many join table,
		/// exposed on the marker type as a dynamic column.
		/// </summary>
		private ColumnAttribute? BuildJoinColumnAttribute(ManyToManyJoinInfo joinInfo, string memberName)
		{
			IProperty? prop = null;

			foreach (var p in joinInfo.ThisForeignKey.Properties)
			{
				if (string.Equals(p.Name, memberName, StringComparison.Ordinal))
				{
					prop = p;
					break;
				}
			}

			if (prop == null)
			{
				foreach (var p in joinInfo.OtherForeignKey.Properties)
				{
					if (string.Equals(p.Name, memberName, StringComparison.Ordinal))
					{
						prop = p;
						break;
					}
				}
			}

			if (prop == null)
				return null;

			var storeId  = GetStoreObjectIdentifier(joinInfo.JoinEntityType);
			var dataType = DataType.Undefined;

			if (prop.GetTypeMapping() is RelationalTypeMapping typeMapping)
			{
				if (typeMapping.DbType != null)
				{
					dataType = DbTypeToDataType(typeMapping.DbType.Value);
				}
				else
				{
					var ms = _model != null ? LinqToDBForEFTools.GetMappingSchema(_model, _mappingSource, _valueConverterSelector, null) : MappingSchema.Default;
					dataType = ms.GetDataType(typeMapping.ClrType).Type.DataType;
				}
			}

			return new ColumnAttribute()
			{
				Name      = storeId != null ? prop.GetColumnName(storeId.Value) : prop.Name,
				DataType  = dataType,
				DbType    = prop.GetColumnType(),
				CanBeNull = prop.IsNullable,
			};
		}

		/// <summary>
		/// Builds the association query expression for a many-to-many (skip) navigation. The resulting lambda
		/// <c>(thisRecord, db) =&gt; db.GetTable&lt;join&gt;().Where(j =&gt; j links thisRecord).SelectMany(j =&gt; db.GetTable&lt;TOther&gt;().Where(o =&gt; o linked by j))</c>
		/// navigates from the source record through the hidden EF join table to the target entities.
		/// </summary>
		private LambdaExpression BuildManyToManyQueryExpression(IEntityType entityType, ISkipNavigation skipNavigation)
		{
			var thisType   = entityType.ClrType;
			var otherType  = skipNavigation.TargetEntityType.ClrType;
			var markerType = typeof(EfJoinTable<,>).MakeGenericType(thisType, otherType);

			var thisForeignKey  = skipNavigation.ForeignKey;         // join -> this
			var otherForeignKey = skipNavigation.Inverse.ForeignKey; // join -> other

			var thisParam  = Expression.Parameter(thisType,             "t");
			var dcParam    = Expression.Parameter(typeof(IDataContext), "db");
			var otherParam = Expression.Parameter(otherType,            "o");
			var joinParam  = Expression.Parameter(markerType,           "j");

			var joinTable  = Expression.Call(Methods.LinqToDB.GetTable.MakeGenericMethod(markerType), dcParam);
			var otherTable = Expression.Call(Methods.LinqToDB.GetTable.MakeGenericMethod(otherType),  dcParam);

			// db.GetTable<join>().Where(j => <join row links this record>)
			var filteredJoin = Expression.Call(
				Methods.Queryable.Where.MakeGenericMethod(markerType),
				joinTable,
				Expression.Quote(Expression.Lambda(BuildJoinPredicate(thisParam, joinParam, thisForeignKey), joinParam)));

			// db.GetTable<TOther>().Where(o => <target record linked by the join row>)
			var linkedOther = Expression.Call(
				Methods.Queryable.Where.MakeGenericMethod(otherType),
				otherTable,
				Expression.Quote(Expression.Lambda(BuildJoinPredicate(otherParam, joinParam, otherForeignKey), otherParam)));

			// .SelectMany(j => <linked target records>) — flattens the junction join to IQueryable<TOther>
			var collectionSelectorType = typeof(Func<,>).MakeGenericType(markerType, typeof(IEnumerable<>).MakeGenericType(otherType));
			var collectionSelector     = Expression.Lambda(collectionSelectorType, linkedOther, joinParam);

			var selectMany = Expression.Call(
				Methods.Queryable.SelectManySimple.MakeGenericMethod(markerType, otherType),
				filteredJoin,
				Expression.Quote(collectionSelector));

			return Expression.Lambda(selectMany, thisParam, dcParam);
		}

		/// <summary>
		/// Builds an equality predicate matching each principal key member of <paramref name="entityParam"/> against the
		/// corresponding foreign key column on the join row (accessed via <see cref="Sql.Property{T}"/>). Handles composite keys.
		/// </summary>
		private static Expression BuildJoinPredicate(ParameterExpression entityParam, ParameterExpression joinParam, IForeignKey foreignKey)
		{
			Expression? predicate = null;

			var principalProperties = foreignKey.PrincipalKey.Properties;
			var foreignProperties   = foreignKey.Properties;

			for (var i = 0; i < principalProperties.Count; i++)
			{
				var principalProperty = principalProperties[i];
				var foreignProperty   = foreignProperties[i];

				var left = principalProperty.PropertyInfo != null
					? (Expression)Expression.MakeMemberAccess(entityParam, principalProperty.PropertyInfo)
					: BuildSqlProperty(entityParam, principalProperty.ClrType, principalProperty.Name);

				var right = BuildSqlProperty(joinParam, foreignProperty.ClrType, foreignProperty.Name);

				if (left.Type != right.Type)
					right = Expression.Convert(right, left.Type);

				var equal = Expression.Equal(left, right);
				predicate = predicate == null ? equal : Expression.AndAlso(predicate, equal);
			}

			return predicate!;
		}

		private static Expression BuildSqlProperty(Expression entity, Type columnType, string memberName)
		{
			return Expression.Call(
				Methods.LinqToDB.SqlExt.Property.MakeGenericMethod(columnType),
				entity,
				Expression.Constant(memberName));
		}
	}
}
#endif
