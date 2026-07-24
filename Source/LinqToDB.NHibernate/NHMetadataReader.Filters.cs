using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

using LinqToDB.Internal.Reflection;
using LinqToDB.Mapping;

using NHibernate;
using NHibernate.Engine;
using NHibernate.Persister.Entity;
using NHibernate.SqlCommand;

namespace LinqToDB.NHibernate
{
	// Bridges NHibernate's session-enabled filters (raw SQL-fragment conditions) to linq2db query filters.
	// Each filtered entity gets a QueryFilterAttribute whose function, at query-build time, reads the session's
	// enabled filters and appends every applicable condition. Column references in a condition are alias-qualified
	// by NHibernate's own tokenizer (Template) and re-expressed as member accesses, so linq2db qualifies them with
	// its own alias — keeping filters correct inside joins.
	partial class NHMetadataReader
	{
		// A collision-proof alias handed to NHibernate's Template so it prefixes every column reference in a
		// condition; the prefix is stripped again while building the predicate, so it never reaches the SQL.
		const string FilterColumnMarker = "l2dbqfilterorigin";

		static readonly MethodInfo _applyFiltersMethod =
			typeof(NHMetadataReader).GetMethod(nameof(ApplyFilters), BindingFlags.Instance | BindingFlags.NonPublic)!;

		static readonly MethodInfo _sqlExprMethod = typeof(Sql).GetMethods()
			.Single(m => string.Equals(m.Name, nameof(Sql.Expr), StringComparison.Ordinal)
				&& m.IsGenericMethodDefinition
				&& m.GetParameters() is { Length: 2 } ps
				&& ps[0].ParameterType == typeof(RawSqlString)
				&& ps[1].ParameterType == typeof(object[]));

		static readonly MethodInfo _rawSqlStringOp =
			typeof(RawSqlString).GetMethod("op_Implicit", new[] { typeof(string) })!;

		static readonly MethodInfo _sqlParameterMethod = typeof(Sql).GetMethods()
			.Single(m => string.Equals(m.Name, nameof(Sql.Parameter), StringComparison.Ordinal)
				&& m.IsGenericMethodDefinition
				&& m.GetParameters().Length == 1);

		/// <summary>
		/// Emits a <see cref="QueryFilterAttribute"/> whose function applies the entity's enabled NHibernate
		/// filters at query time, or <see langword="null"/> when the model declares no filters or the type is
		/// not a mapped entity.
		/// </summary>
		QueryFilterAttribute? BuildQueryFilterAttribute(Type type)
		{
			if (_sessionFactory is not ISessionFactoryImplementor)
				return null;
			if (_sessionFactory.DefinedFilterNames.Count == 0)
				return null;
			if (_sessionFactory.GetClassMetadata(type) is not AbstractEntityPersister)
				return null;

			var apply     = _applyFiltersMethod.MakeGenericMethod(type);
			var queryable = typeof(IQueryable<>).MakeGenericType(type);
			var funcType  = typeof(Func<,,>).MakeGenericType(queryable, typeof(IDataContext), queryable);

			return new QueryFilterAttribute { FilterFunc = Delegate.CreateDelegate(funcType, this, apply) };
		}

		IQueryable<T> ApplyFilters<T>(IQueryable<T> query, IDataContext dataContext)
		{
			if (_sessionFactory is not ISessionFactoryImplementor sfi)
				return query;
			if ((dataContext as LinqToDBForNHibernateToolsDataConnection)?.Session is not { IsOpen: true } session)
				return query;
			if (_sessionFactory.GetClassMetadata(typeof(T)) is not AbstractEntityPersister persister)
				return query;

			var impl = session.GetSessionImplementation();
			if (impl.EnabledFilters.Count == 0)
				return query;

			if (!GetPropertyMap(typeof(T), out var map) || map == null)
				return query;

			foreach (var entry in impl.EnabledFilters)
			{
				var filter = entry.Value;

				// NHibernate produces a non-empty fragment only when this entity actually declares the filter.
				var single = new Dictionary<string, IFilter> { [entry.Key] = filter };
				if (string.IsNullOrWhiteSpace(persister.FilterFragment(FilterColumnMarker, single)))
					continue;

				var condition = filter.FilterDefinition.DefaultFilterCondition;
				if (string.IsNullOrWhiteSpace(condition))
					continue;

				var predicate = BuildFilterPredicate<T>(condition, entry.Key, filter.FilterDefinition, map, impl, sfi);
				if (predicate != null)
					query = query.Where(predicate);
			}

			return query;
		}

		static Expression<Func<T, bool>>? BuildFilterPredicate<T>(
			string               condition,
			string               filterName,
			FilterDefinition     definition,
			PropertyMap          map,
			ISessionImplementor  impl,
			ISessionFactoryImplementor sfi)
		{
			// NHibernate's tokenizer prefixes each column with our marker (functions/keywords/string literals and
			// :parameters are left untouched); we then lift each marked column out as a member access.
			var qualified = Template.RenderWhereStringTemplate(condition, FilterColumnMarker, sfi.Dialect, sfi.SQLFunctionRegistry);

			var entity = Expression.Parameter(typeof(T), "e");
			var args   = new List<Expression>();
			var sql    = new StringBuilder();

			var i = 0;
			while (i < qualified.Length)
			{
				if (TryReadMarkerColumn(qualified, i, out var column, out var columnLength))
				{
					var arg = BuildColumnArgument(entity, map, column);
					if (arg == null)
						return null; // a column we cannot resolve: skip the whole filter rather than emit wrong SQL

					AppendPlaceholder(sql, args.Count);
					args.Add(arg);
					i += columnLength;
				}
				else if (qualified[i] == ':' && TryReadParameter(qualified, i, out var parameter, out var parameterLength))
				{
					AppendPlaceholder(sql, args.Count);
					args.Add(BuildParameterArgument(impl, filterName, parameter));
					i += parameterLength;
				}
				else
				{
					sql.Append(qualified[i]);
					i++;
				}
			}

			var rawSql    = Expression.Convert(Expression.Constant(sql.ToString()), typeof(RawSqlString), _rawSqlStringOp);
			var argsArray = Expression.NewArrayInit(typeof(object), args);
			var body      = Expression.Call(_sqlExprMethod.MakeGenericMethod(typeof(bool)), rawSql, argsArray);

			return Expression.Lambda<Func<T, bool>>(body, entity);
		}

		static void AppendPlaceholder(StringBuilder sql, int index)
		{
			sql.Append('{').Append(index.ToString(CultureInfo.InvariantCulture)).Append('}');
		}

		static Expression? BuildColumnArgument(ParameterExpression entity, PropertyMap map, string column)
		{
			var member = map.FindPropByColumnName(column)?.MemberInfo;

			Expression access = member != null
				? Expression.MakeMemberAccess(entity, member)
				: Expression.Call(Methods.LinqToDB.SqlExt.Property.MakeGenericMethod(typeof(object)), entity, Expression.Constant(column));

			return Expression.Convert(access, typeof(object));
		}

		static Expression BuildParameterArgument(ISessionImplementor impl, string filterName, string parameter)
		{
			var value = impl.GetFilterParameterValue($"{filterName}.{parameter}");
			if (value == null)
				return Expression.Constant(null, typeof(object));

			// Wrap the enabled-filter value in Sql.Parameter so linq2db emits it as a query parameter of the
			// correct SQL type (cache-friendly), rather than inlining it as a literal.
			var type     = value.GetType();
			var paramExpr = Expression.Call(_sqlParameterMethod.MakeGenericMethod(type), Expression.Constant(value, type));

			return Expression.Convert(paramExpr, typeof(object));
		}

		static bool TryReadMarkerColumn(string s, int pos, out string column, out int totalLength)
		{
			column      = string.Empty;
			totalLength = 0;

			if (string.CompareOrdinal(s, pos, FilterColumnMarker, 0, FilterColumnMarker.Length) != 0)
				return false;

			var afterMarker = pos + FilterColumnMarker.Length;
			if (afterMarker >= s.Length || s[afterMarker] != '.')
				return false;

			var start = afterMarker + 1;
			var end   = ReadIdentifierEnd(s, start);
			if (end == start)
				return false;

			column      = Unquote(s.Substring(start, end - start));
			totalLength = end - pos;
			return true;
		}

		static bool TryReadParameter(string s, int pos, out string name, out int totalLength)
		{
			name        = string.Empty;
			totalLength = 0;

			var start = pos + 1; // skip ':'
			var end   = start;
			while (end < s.Length && (char.IsLetterOrDigit(s[end]) || s[end] == '_'))
				end++;

			if (end == start)
				return false;

			name        = s.Substring(start, end - start);
			totalLength = end - pos;
			return true;
		}

		static int ReadIdentifierEnd(string s, int start)
		{
			if (start >= s.Length)
				return start;

			var close = s[start] switch { '"' => '"', '[' => ']', '`' => '`', _ => '\0' };
			if (close != '\0')
			{
				var j = start + 1;
				while (j < s.Length && s[j] != close)
					j++;
				return j < s.Length ? j + 1 : start; // include the closing quote
			}

			var end = start;
			while (end < s.Length && (char.IsLetterOrDigit(s[end]) || s[end] == '_'))
				end++;
			return end;
		}

		static string Unquote(string name)
		{
			if (name.Length >= 2)
			{
				var first = name[0];
				var last  = name[name.Length - 1];
				if ((first == '"' && last == '"') || (first == '[' && last == ']') || (first == '`' && last == '`'))
					return name.Substring(1, name.Length - 2);
			}

			return name;
		}
	}
}
