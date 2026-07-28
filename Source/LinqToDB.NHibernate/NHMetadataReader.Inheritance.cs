using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

using LinqToDB.Expressions;
using LinqToDB.Internal.Reflection;
using LinqToDB.Mapping;

using NHibernate.Persister.Entity;

namespace LinqToDB.NHibernate
{
	// Restricts a table-per-hierarchy subclass to its own rows.
	//
	// With table-per-hierarchy every class in the hierarchy shares one table and is told apart by a discriminator
	// column, which NHibernate applies for you. linq2db knows only the table, so without this a query for a subclass
	// would read the whole table and hand back its siblings' rows as instances of the queried type.
	//
	// The discriminator is usually not mapped as a property, so it cannot be expressed through linq2db's
	// ColumnAttribute.IsDiscriminator. Instead the restriction is emitted as a query filter that names the column
	// directly, the same way session filters reference unmapped columns.
	partial class NHMetadataReader
	{
		// Key for the discriminator restriction, keeping it in its own filter slot (see BuildDiscriminatorFilterAttribute).
		const string DiscriminatorFilterKey = "LinqToDB.NHibernate.Discriminator";

		static readonly MethodInfo _applyDiscriminatorMethod =
			MemberHelper.MethodOfGeneric<NHMetadataReader>(r => r.ApplyDiscriminator<object>(default!, default!));

		readonly ConcurrentDictionary<Type, LambdaExpression?> _discriminatorCache = new();

		/// <summary>
		/// Refuses the inheritance shapes that cannot be read from a single table, so they fail with an
		/// explanation rather than with whatever the database says about the SQL that would be built.
		/// </summary>
		void EnsureInheritanceIsQueryable(Type type)
		{
			if (_sessionFactory?.GetClassMetadata(type) is not AbstractEntityPersister persister)
				return;

			// Table-per-subclass keeps a subclass's own columns in its own table, joined to the base table by key.
			// linq2db reads the entity from one table, so it would look for those columns in the base table.
			if (persister is JoinedSubclassEntityPersister && persister.IsInherited)
			{
				throw new LinqToDBForNHibernateToolsException(
					$"'{type.Name}' is mapped table-per-subclass (<joined-subclass>), which spreads its columns over several tables and cannot be read as one. Query its base class, or map the hierarchy table-per-hierarchy (a discriminator) or table-per-concrete-class (<union-subclass>).");
			}

			// The root of a table-per-concrete-class hierarchy (<union-subclass>) has no table of its own either —
			// NHibernate reads it as a union over the subclass tables. It is not refused here: a subclass's own
			// metadata is built by walking up to its base, so refusing the root would refuse the subclasses too,
			// and those read from their own tables perfectly well.
		}

		/// <summary>
		/// Emits a <see cref="QueryFilterAttribute"/> restricting a table-per-hierarchy subclass to its own
		/// discriminator values, or <see langword="null"/> when the type needs no restriction.
		/// </summary>
		QueryFilterAttribute? BuildDiscriminatorFilterAttribute(Type type)
		{
			if (GetDiscriminatorPredicate(type) == null)
				return null;

			var apply     = _applyDiscriminatorMethod.MakeGenericMethod(type);
			var queryable = typeof(IQueryable<>).MakeGenericType(type);
			var funcType  = typeof(Func<,,>).MakeGenericType(queryable, typeof(IDataContext), queryable);

			// Filters are stored per key, so this must not share the (unkeyed) slot the session filters use —
			// otherwise whichever is applied last silently replaces the other.
			return new QueryFilterAttribute
			{
				FilterKey  = DiscriminatorFilterKey,
				FilterFunc = Delegate.CreateDelegate(funcType, this, apply),
			};
		}

		LambdaExpression? GetDiscriminatorPredicate(Type type)
		{
			return _discriminatorCache.GetOrAdd(type, CreateDiscriminatorPredicate);
		}

		LambdaExpression? CreateDiscriminatorPredicate(Type type)
		{
			if (_sessionFactory?.GetClassMetadata(type) is not AbstractEntityPersister persister)
				return null;

			// Only table-per-hierarchy shares one table across the hierarchy. Table-per-subclass and
			// table-per-concrete-class give each class its own table, which linq2db already reads on its own.
			if (persister is not SingleTableEntityPersister)
				return null;

			// The root of a hierarchy selects the whole table, exactly as NHibernate does for it.
			if (!persister.IsInherited)
				return null;

			var column = persister.DiscriminatorColumnName;
			if (string.IsNullOrEmpty(column))
				return null;

			// A subclass may itself have subclasses, whose rows belong to it as well.
			var values = new List<object>();
			foreach (var entityName in persister.SubclassClosure)
			{
				if (_sessionFactory.GetClassMetadata(entityName) is AbstractEntityPersister subclass && subclass.DiscriminatorValue != null)
					values.Add(subclass.DiscriminatorValue);
			}

			if (values.Count == 0)
				return null;

			return BuildDiscriminatorPredicate(type, column, values);
		}

		// e => Sql.Expr&lt;bool&gt;("{0} = {1}", Sql.Property(e, column), Sql.Parameter(value)) -- or IN (...) when the
		// subclass has subclasses of its own.
		static LambdaExpression BuildDiscriminatorPredicate(Type type, string column, IReadOnlyList<object> values)
		{
			var entity = Expression.Parameter(type, "e");
			var sql    = new StringBuilder("{0}");
			var args   = new List<Expression>
			{
				Expression.Convert(
					Expression.Call(_sqlPropertyObjectMethod, entity, Expression.Constant(column)),
					typeof(object)),
			};

			if (values.Count == 1)
			{
				sql.Append(" = {1}");
				args.Add(BuildDiscriminatorValue(values[0]));
			}
			else
			{
				sql.Append(" IN (");

				for (var i = 0; i < values.Count; i++)
				{
					if (i > 0)
						sql.Append(", ");

					sql.Append('{').Append((i + 1).ToString(CultureInfo.InvariantCulture)).Append('}');
					args.Add(BuildDiscriminatorValue(values[i]));
				}

				sql.Append(')');
			}

			var rawSql = Expression.Convert(Expression.Constant(sql.ToString()), typeof(RawSqlString), _rawSqlStringOp);
			var body   = Expression.Call(_sqlExprBoolMethod, rawSql, Expression.NewArrayInit(typeof(object), args));

			return Expression.Lambda(body, entity);
		}

		// Discriminator values are emitted as parameters, so the query shape stays cache-friendly.
		static Expression BuildDiscriminatorValue(object value)
		{
			var type = value.GetType();

			return Expression.Convert(
				Expression.Call(Methods.LinqToDB.SqlParameter.MakeGenericMethod(type), Expression.Constant(value, type)),
				typeof(object));
		}

		IQueryable<T> ApplyDiscriminator<T>(IQueryable<T> query, IDataContext dataContext)
		{
			return GetDiscriminatorPredicate(typeof(T)) is Expression<Func<T, bool>> predicate
				? query.Where(predicate)
				: query;
		}
	}
}
