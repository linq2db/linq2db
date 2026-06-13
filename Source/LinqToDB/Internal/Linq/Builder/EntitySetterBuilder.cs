using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB.Expressions;
using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.Mapping;
using LinqToDB.Mapping;

namespace LinqToDB.Internal.Linq.Builder
{
	/// <summary>
	/// Builds <c>s =&gt; new T { ... }</c> / <c>(t, s) =&gt; new T { ... }</c> setter lambdas from an
	/// <see cref="EntityDescriptor"/>'s columns plus user <c>Set</c> / <c>Ignore</c> overlays.
	/// Lookups (<see cref="IsIgnored"/>, <see cref="FindOverride"/>) and the user-lambda parameter
	/// substitution helper (<see cref="InstantiateSetter"/>) are exposed so callers building their
	/// own setter shapes (e.g. <c>UpsertBuilder</c>'s native ON-CONFLICT envelope path) can reuse
	/// them.
	/// </summary>
	static class EntitySetterBuilder
	{
		/// <summary>
		/// Build <c>s =&gt; new T { Col1 = v1, Col2 = v2, ... }</c>. Whole-object defaults from the
		/// source row, with user <c>Set</c> overrides overlaid and <c>Ignore</c>d /
		/// <see cref="ColumnDescriptor.SkipOnInsert"/> columns omitted.
		/// </summary>
		/// <remarks>
		/// The body is emitted as <see cref="SqlGenericConstructorExpression"/> rather than
		/// <c>MemberInit(New(T), …)</c> so the entity type does not need a public parameterless
		/// constructor. <c>Expression.New(Type)</c> validates that requirement at expression-tree
		/// construction time, which would break positional records / DTOs with non-default ctors.
		/// Downstream <c>ParseGenericConstructor</c> / <c>ParseSetter</c> already accept
		/// <see cref="SqlGenericConstructorExpression"/> directly.
		/// </remarks>
		public static LambdaExpression BuildInsertSetter(
			Type                                                                  entityType,
			EntityDescriptor                                                      entityDescriptor,
			ParameterExpression                                                   entityParameter,
			IReadOnlyList<(Expression Field, LambdaExpression Value)>             setOverrides,
			IReadOnlyList<Expression>                                             ignoreList)
		{
			var sParm = Expression.Parameter(entityType, "s");
			var items = new List<(string[] Path, ColumnDescriptor Cd, Expression Value)>();

			foreach (var cd in entityDescriptor.Columns)
			{
				if (cd.SkipOnInsert) continue;

				// Match key for .Set/.Ignore overrides — must equal EntityBuilderParser.Canonicalise
				// (fieldLambda.GetBody(entityParameter)). GetMemberAccessExpression produces the same
				// null-check-free member chain for nested columns and the same Sql.Property node for
				// dynamic columns; MemberAccessor.GetGetterExpression would instead emit a null-check
				// block / the store getter, which never matches the user selector.
				var canonicalField = cd.GetMemberAccessExpression(entityParameter);
				if (IsIgnored(canonicalField, ignoreList))
					continue;

				var @override = FindOverride(canonicalField, setOverrides);

				// Auto-derived value reads the column off the source row. GetMemberAccessExpression gives a
				// null-check-free member chain (flat: s.Col; nested: s.Sub.Field) that converts to SQL —
				// MemberAccessor.GetGetterExpression would wrap nested access in a null-check block that
				// can't. Matches the native Upsert path (UpsertBuilder).
				var value = @override != null
					? InstantiateSetter(@override, sParm, sParm) // no target context for INSERT
					: cd.GetMemberAccessExpression(sParm);

				items.Add((SplitMemberPath(cd.MemberName), cd, value));
			}

			Expression body = new SqlGenericConstructorExpression(entityType, BuildBindings(entityType, items, 0).AsReadOnly());
			return Expression.Lambda(body, sParm);
		}

		/// <summary>
		/// Build <c>(t, s) =&gt; new T { Col1 = v1, Col2 = v2, ... }</c>. Skips PK,
		/// <see cref="ColumnDescriptor.SkipOnUpdate"/>, ignored, and (when supplied) match-key
		/// columns. Match-key columns are excluded so MERGE's UPDATE SET doesn't try to overwrite
		/// the ON-clause columns (Oracle ORA-38104, pointless self-assign elsewhere) — pass
		/// <see langword="null"/> for standalone Update where match is the table's primary key.
		/// </summary>
		/// <remarks>
		/// See <see cref="BuildInsertSetter"/> for why the body is emitted as
		/// <see cref="SqlGenericConstructorExpression"/> instead of <c>MemberInit(New(T), …)</c>.
		/// </remarks>
		public static LambdaExpression BuildUpdateSetter(
			Type                                                                  entityType,
			EntityDescriptor                                                      entityDescriptor,
			ParameterExpression                                                   entityParameter,
			IReadOnlyList<(Expression Field, LambdaExpression Value)>             setOverrides,
			IReadOnlyList<Expression>                                             ignoreList,
			HashSet<Expression>?                                                  matchColumns = null)
		{
			var tParm = Expression.Parameter(entityType, "t");
			var sParm = Expression.Parameter(entityType, "s");
			var items = new List<(string[] Path, ColumnDescriptor Cd, Expression Value)>();

			foreach (var cd in entityDescriptor.Columns)
			{
				if (cd.IsPrimaryKey) continue;
				if (cd.SkipOnUpdate) continue;

				// See BuildInsertSetter: match key must equal the user selector's Canonicalise form.
				var canonicalField = cd.GetMemberAccessExpression(entityParameter);

				// Match columns appear in the ON clause — including them in UPDATE SET is
				// forbidden by Oracle (ORA-38104) and pointless elsewhere. Skip unless the
				// user explicitly opted in via .Set(x => x.MatchCol, ...).
				if (matchColumns != null && matchColumns.Contains(canonicalField) && FindOverride(canonicalField, setOverrides) == null)
					continue;

				if (IsIgnored(canonicalField, ignoreList))
					continue;

				var @override = FindOverride(canonicalField, setOverrides);

				// See BuildInsertSetter: null-check-free member chain so nested auto-derived values convert to SQL.
				var value = @override != null
					? InstantiateSetter(@override, tParm, sParm)
					: cd.GetMemberAccessExpression(sParm);

				items.Add((SplitMemberPath(cd.MemberName), cd, value));
			}

			Expression body = new SqlGenericConstructorExpression(entityType, BuildBindings(entityType, items, 0).AsReadOnly());
			return Expression.Lambda(body, tParm, sParm);
		}

		/// <summary>
		/// Member-name path split on '.', dropping empty segments so a storage-style leading-dot
		/// <c>MemberName</c> (e.g. <c>".Building"</c>) collapses to a single root-level segment — the
		/// flat behaviour it had before nested grouping.
		/// </summary>
		static string[] SplitMemberPath(string memberName)
			=> memberName.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);

		/// <summary>
		/// Build the member-binding list for a <see cref="SqlGenericConstructorExpression"/> from the
		/// collected (column-path, value) items, grouping nested complex columns
		/// (<c>[Column("Db", "Sub.Field")]</c>) into <see cref="MemberMemberBinding"/>s so the leaf is
		/// bound on its own sub-object type rather than (invalidly) on the entity root. A flat column
		/// becomes a <see cref="MemberAssignment"/>; columns sharing a prefix segment recurse one level
		/// deeper. <see cref="SqlGenericConstructorExpression"/> turns a <see cref="MemberMemberBinding"/>
		/// into a nested constructor (no <c>Expression.New</c> on the sub-object), and
		/// <c>UpdateBuilder.ParseSet</c> recurses through the nested constructor to map each leaf to its column.
		/// </summary>
		static List<MemberBinding> BuildBindings(Type type, List<(string[] Path, ColumnDescriptor Cd, Expression Value)> items, int depth)
		{
			var bindings = new List<MemberBinding>();

			foreach (var grp in items.GroupBy(i => i.Path[depth]))
			{
				foreach (var item in grp.Where(i => i.Path.Length == depth + 1))
					bindings.Add(Expression.Bind(item.Cd.MemberInfo, item.Value));

				var deeper = grp.Where(i => i.Path.Length > depth + 1).ToList();
				if (deeper.Count > 0)
				{
					var complexMember = ((MemberExpression)ExpressionHelper.PropertyOrField(Expression.Parameter(type), grp.Key)).Member;
					bindings.Add(Expression.MemberBind(complexMember, BuildBindings(complexMember.GetMemberType(), deeper, depth + 1)));
				}
			}

			return bindings;
		}

		/// <summary>True if <paramref name="canonicalField"/> appears in <paramref name="list"/> by structural equality.</summary>
		public static bool IsIgnored(Expression canonicalField, IReadOnlyList<Expression> list)
		{
			foreach (var e in list)
			{
				if (ExpressionEqualityComparer.Instance.Equals(e, canonicalField))
					return true;
			}

			return false;
		}

		/// <summary>
		/// Find the user-provided <c>Set(field, value)</c> override for <paramref name="canonicalField"/>.
		/// Later entries win — append branch overrides after root overrides so the branch wins.
		/// </summary>
		public static LambdaExpression? FindOverride(Expression canonicalField, IReadOnlyList<(Expression Field, LambdaExpression Value)> list)
		{
			LambdaExpression? winner = null;
			foreach (var (f, v) in list)
			{
				if (ExpressionEqualityComparer.Instance.Equals(f, canonicalField))
					winner = v;
			}

			return winner;
		}

		/// <summary>
		/// Bind a user-provided setter lambda's parameters to in-scope expressions and return its body.
		/// Supported arities:
		/// <list type="bullet">
		///   <item>0 params — context-free (<c>() =&gt; …</c>); returns body unchanged.</item>
		///   <item>1 param — source row (<c>s =&gt; …</c>); binds to <paramref name="sourceItemConstant"/>.</item>
		///   <item>2 params — <c>(t, s) =&gt; …</c>; binds first to <paramref name="targetContextRef"/>, second to <paramref name="sourceItemConstant"/>.</item>
		/// </list>
		/// </summary>
		public static Expression InstantiateSetter(LambdaExpression lambda, Expression targetContextRef, Expression sourceItemConstant)
			=> lambda.Parameters.Count switch
			{
				0 => lambda.Body,
				1 => lambda.GetBody(sourceItemConstant),
				2 => lambda.GetBody(targetContextRef, sourceItemConstant),
				_ => throw new LinqToDBException($"Unexpected entity-setter lambda arity: {lambda.Parameters.Count}"),
			};
	}
}
