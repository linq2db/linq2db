using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

using LinqToDB.Expressions;
using LinqToDB.Internal.Reflection;
using LinqToDB.Mapping;

using NHibernate;
using NHibernate.Engine;
using NHibernate.Persister.Entity;

namespace LinqToDB.NHibernate
{
	// Bridges NHibernate's session-enabled filters (raw SQL-fragment conditions) to linq2db query filters.
	// Each filtered entity gets a QueryFilterAttribute whose function, at query-build time, reads the session's
	// enabled filters. For each applicable filter it takes NHibernate's own per-entity filter fragment (the applied
	// condition, honouring per-entity overrides, already alias-qualified by NHibernate's tokenizer with a
	// collision-proof marker alias), then re-expresses each marked column as a member access — so linq2db qualifies
	// it with its own alias, keeping filters correct inside joins — and each :parameter as a Sql.Parameter.
	partial class NHMetadataReader
	{
		// A collision-proof alias handed to NHibernate's FilterFragment so it prefixes every column reference in a
		// condition; the prefix is stripped again while building the predicate, so it never reaches the SQL.
		const string FilterColumnMarker = "l2dbqfilterorigin";

		static readonly MethodInfo _applyFiltersMethod =
			MemberHelper.MethodOfGeneric<NHMetadataReader>(r => r.ApplyFilters<object>(default!, default!));

		static readonly MethodInfo _sqlExprBoolMethod       = MemberHelper.MethodOf(() => Sql.Expr<bool>(default(RawSqlString), Array.Empty<object>()));
		static readonly MethodInfo _sqlPropertyObjectMethod = Methods.LinqToDB.SqlExt.Property.MakeGenericMethod(typeof(object));

		static readonly MethodInfo _rawSqlStringOp =
			typeof(RawSqlString).GetMethod("op_Implicit", new[] { typeof(string) })!;

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
			if ((dataContext as LinqToDBForNHibernateToolsDataConnection)?.Session is not { IsOpen: true } session)
				return query;
			if (_sessionFactory?.GetClassMetadata(typeof(T)) is not AbstractEntityPersister persister)
				return query;

			var impl = session.GetSessionImplementation();
			if (impl.EnabledFilters.Count == 0)
				return query;

			if (!GetPropertyMap(typeof(T), out var map) || map == null)
				return query;

			foreach (var entry in impl.EnabledFilters)
			{
				// NHibernate's own per-entity fragment: the applied condition (honouring a per-entity <filter>
				// override), alias-qualified with our marker; empty when this entity doesn't declare the filter.
				var fragment = persister.FilterFragment(FilterColumnMarker, new Dictionary<string, IFilter> { [entry.Key] = entry.Value });
				if (string.IsNullOrWhiteSpace(fragment))
					continue;

				var predicate = BuildFilterPredicate<T>(StripFragmentPrefix(fragment), map, impl);
				if (predicate != null)
					query = query.Where(predicate);
			}

			return query;
		}

		// NHibernate prefixes each rendered filter fragment with " and "; strip it to get the bare condition.
		static string StripFragmentPrefix(string fragment)
		{
			var s = fragment.Trim();
			if (s.StartsWith("and ", StringComparison.OrdinalIgnoreCase))
				s = s.Substring(4);
			return s;
		}

		static Expression<Func<T, bool>> BuildFilterPredicate<T>(string qualified, PropertyMap map, ISessionImplementor impl)
		{
			// `qualified` is NHibernate's rendered filter condition: columns prefixed with FilterColumnMarker,
			// parameters as :filterName.paramName, with string literals / functions / operators as raw SQL. We lift
			// each marked column out as a member access and each parameter as a Sql.Parameter, copying everything
			// else verbatim into a Sql.Expr format string (with '{'/'}' escaped, and string literals left intact).
			var entity = Expression.Parameter(typeof(T), "e");
			var args   = new List<Expression>();
			var sql    = new StringBuilder();

			var i = 0;
			while (i < qualified.Length)
			{
				var c = qualified[i];

				if (c == '\'')
				{
					// SQL string literal: copy verbatim (brace-escaped) without scanning for tokens inside it.
					var end = ScanStringLiteral(qualified, i);
					AppendEscaped(sql, qualified, i, end);
					i = end;
				}
				else if (TryReadMarkerColumn(qualified, i, out var column, out var columnLength))
				{
					AppendPlaceholder(sql, args.Count);
					args.Add(BuildColumnArgument(entity, map, column));
					i += columnLength;
				}
				else if (c == ':' && i + 1 < qualified.Length && qualified[i + 1] == ':')
				{
					// '::' cast operator (e.g. PostgreSQL) — not a parameter.
					sql.Append("::");
					i += 2;
				}
				else if (c == ':' && TryReadParameter(qualified, i, out var parameter, out var parameterLength))
				{
					AppendPlaceholder(sql, args.Count);
					args.Add(BuildParameterArgument(impl, parameter));
					i += parameterLength;
				}
				else
				{
					AppendEscapedChar(sql, c);
					i++;
				}
			}

			var rawSql    = Expression.Convert(Expression.Constant(sql.ToString()), typeof(RawSqlString), _rawSqlStringOp);
			var argsArray = Expression.NewArrayInit(typeof(object), args);
			var body      = Expression.Call(_sqlExprBoolMethod, rawSql, argsArray);

			return Expression.Lambda<Func<T, bool>>(body, entity);
		}

		// Returns the index just past the closing quote of the SQL string literal starting at <paramref name="pos"/>
		// ('...'), treating a doubled '' as an escaped quote.
		static int ScanStringLiteral(string s, int pos)
		{
			var i = pos + 1;
			while (i < s.Length)
			{
				if (s[i] == '\'')
				{
					if (i + 1 < s.Length && s[i + 1] == '\'')
					{
						i += 2;
						continue;
					}

					return i + 1;
				}

				i++;
			}

			return i; // unterminated literal: copy the rest as-is
		}

		static void AppendPlaceholder(StringBuilder sql, int index)
		{
			sql.Append('{').Append(index.ToString(CultureInfo.InvariantCulture)).Append('}');
		}

		// Copies s[start, end) into the Sql.Expr format string, escaping '{' and '}' so they are treated as literal
		// text rather than argument placeholders.
		static void AppendEscaped(StringBuilder sql, string s, int start, int end)
		{
			for (var i = start; i < end; i++)
				AppendEscapedChar(sql, s[i]);
		}

		static void AppendEscapedChar(StringBuilder sql, char c)
		{
			if (c == '{')
				sql.Append("{{");
			else if (c == '}')
				sql.Append("}}");
			else
				sql.Append(c);
		}

		static Expression BuildColumnArgument(ParameterExpression entity, PropertyMap map, string column)
		{
			var member = map.FindPropByColumnName(column)?.MemberInfo;

			// A mapped scalar member resolves to a real column; an unmapped column (formula/shadow) falls back to
			// Sql.Property, which linq2db still alias-qualifies via the entity.
			Expression access = member != null
				? Expression.MakeMemberAccess(entity, member)
				: Expression.Call(_sqlPropertyObjectMethod, entity, Expression.Constant(column));

			return Expression.Convert(access, typeof(object));
		}

		static Expression BuildParameterArgument(ISessionImplementor impl, string qualifiedParameterName)
		{
			// The fragment already gives the fully-qualified "filterName.parameterName" NHibernate expects.
			var value = impl.GetFilterParameterValue(qualifiedParameterName);
			if (value == null)
				return Expression.Constant(null, typeof(object));

			// Wrap the value in Sql.Parameter so linq2db emits it as a query parameter of the correct SQL type
			// (cache-friendly) rather than inlining it as a literal.
			var type      = value.GetType();
			var paramExpr = Expression.Call(Methods.LinqToDB.SqlParameter.MakeGenericMethod(type), Expression.Constant(value, type));

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

			// NHibernate renders a filter parameter as :filterName.parameterName, so '.' is part of the name.
			while (end < s.Length && (char.IsLetterOrDigit(s[end]) || s[end] == '_' || s[end] == '.'))
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
