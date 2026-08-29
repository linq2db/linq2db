using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.Reflection;

using NHibernate.Persister.Collection;

namespace LinqToDB.NHibernate
{
	// Many-to-many where the junction table is not mapped as an entity — the ordinary HasManyToMany shape.
	//
	// The junction is known to NHibernate only by table name, so there is no entity for linq2db to join through.
	// It is queried as an untyped table instead: the table name is set on the query and its columns are addressed
	// by name, which lets the association keep the same join it would use for a mapped junction.
	partial class NHMetadataReader
	{
		/// <summary>
		/// Builds <c>(t, db) =&gt; db.GetTable&lt;object&gt;().TableName(junction).Where(j =&gt; j links t)
		/// .SelectMany(j =&gt; db.GetTable&lt;other&gt;().Where(o =&gt; o is linked by j))</c>, or <see langword="null"/>
		/// when the keys and junction columns do not line up.
		/// </summary>
		static LambdaExpression? BuildUnmappedJunctionQueryExpression(
			Type thisType,  PropertyMap thisEntityMap,
			Type otherType, PropertyMap otherEntityMap,
			AbstractCollectionPersister m2m)
		{
			var thisPk  = thisEntityMap .Properties.Where(p => p.IsPrimaryKey).OrderBy(p => p.PkOrder).ToList();
			var otherPk = otherEntityMap.Properties.Where(p => p.IsPrimaryKey).OrderBy(p => p.PkOrder).ToList();

			var keyCols     = m2m.KeyColumnNames;     // junction -> this entity
			var elementCols = m2m.ElementColumnNames; // junction -> other entity

			if (thisPk.Count == 0 || otherPk.Count == 0 || thisPk.Count != keyCols.Length || otherPk.Count != elementCols.Length)
				return null;

			var thisParam     = Expression.Parameter(thisType,             "t");
			var dcParam       = Expression.Parameter(typeof(IDataContext), "db");
			var junctionParam = Expression.Parameter(typeof(object),       "j");
			var otherParam    = Expression.Parameter(otherType,            "o");

			// db.GetTable<object>().TableName("<junction table>")
			var junctionTable = Expression.Call(
				Methods.LinqToDB.Table.TableName.MakeGenericMethod(typeof(object)),
				Expression.Call(Methods.LinqToDB.GetTable.MakeGenericMethod(typeof(object)), dcParam),
				Expression.Constant(m2m.TableName));

			// j => junction row links this record: junction.<key column> == this.<primary key>
			Expression? joinPredicate = null;
			for (var i = 0; i < keyCols.Length; i++)
			{
				var member = thisPk[i].MemberInfo;
				var left   = JunctionColumn(junctionParam, keyCols[i], member.GetMemberType());
				var right  = Expression.MakeMemberAccess(thisParam, member);

				joinPredicate = AndAlso(joinPredicate, EqualWithConvert(left, right));
			}

			var filteredJunction = Expression.Call(
				Methods.Queryable.Where.MakeGenericMethod(typeof(object)),
				junctionTable,
				Expression.Quote(Expression.Lambda(joinPredicate!, junctionParam)));

			// o => target record linked by the junction row: other.<primary key> == junction.<element column>
			Expression? otherPredicate = null;
			for (var i = 0; i < elementCols.Length; i++)
			{
				var member = otherPk[i].MemberInfo;
				var left   = Expression.MakeMemberAccess(otherParam, member);
				var right  = JunctionColumn(junctionParam, elementCols[i], member.GetMemberType());

				otherPredicate = AndAlso(otherPredicate, EqualWithConvert(left, right));
			}

			var linkedOther = Expression.Call(
				Methods.Queryable.Where.MakeGenericMethod(otherType),
				Expression.Call(Methods.LinqToDB.GetTable.MakeGenericMethod(otherType), dcParam),
				Expression.Quote(Expression.Lambda(otherPredicate!, otherParam)));

			var collectionSelectorType = typeof(Func<,>).MakeGenericType(typeof(object), typeof(IEnumerable<>).MakeGenericType(otherType));
			var collectionSelector     = Expression.Lambda(collectionSelectorType, linkedOther, junctionParam);

			var selectMany = Expression.Call(
				Methods.Queryable.SelectManySimple.MakeGenericMethod(typeof(object), otherType),
				filteredJunction,
				Expression.Quote(collectionSelector));

			return Expression.Lambda(selectMany, thisParam, dcParam);
		}

		// Sql.Property addresses a column of the untyped junction row, typed as the key it is compared with.
		static Expression JunctionColumn(ParameterExpression junction, string column, Type columnType)
		{
			return Expression.Call(
				Methods.LinqToDB.SqlExt.Property.MakeGenericMethod(columnType),
				junction,
				Expression.Constant(column));
		}
	}
}
