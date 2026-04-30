using System;
using System.Collections.Generic;
using System.Linq.Expressions;

using LinqToDB.Expressions;
using LinqToDB.Internal.Expressions;
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
		public static LambdaExpression BuildInsertSetter(
			Type                                                                  entityType,
			EntityDescriptor                                                      entityDescriptor,
			ParameterExpression                                                   entityParm,
			IReadOnlyList<(Expression Field, LambdaExpression Value)>             setOverrides,
			IReadOnlyList<Expression>                                             ignoreList)
		{
			var sParm    = Expression.Parameter(entityType, "s");
			var bindings = new List<MemberBinding>();

			foreach (var cd in entityDescriptor.Columns)
			{
				if (cd.SkipOnInsert) continue;

				var canonicalField = cd.MemberAccessor.GetGetterExpression(entityParm);
				if (IsIgnored(canonicalField, ignoreList))
					continue;

				var @override = FindOverride(canonicalField, setOverrides);

				var value = @override != null
					? InstantiateSetter(@override, sParm, sParm) // no target context for INSERT
					: cd.MemberAccessor.GetGetterExpression(sParm);

				bindings.Add(Expression.Bind(cd.MemberInfo, value));
			}

			var body = Expression.MemberInit(Expression.New(entityType), bindings);
			return Expression.Lambda(body, sParm);
		}

		/// <summary>
		/// Build <c>(t, s) =&gt; new T { Col1 = v1, Col2 = v2, ... }</c>. Skips PK,
		/// <see cref="ColumnDescriptor.SkipOnUpdate"/>, ignored, and (when supplied) match-key
		/// columns. Match-key columns are excluded so MERGE's UPDATE SET doesn't try to overwrite
		/// the ON-clause columns (Oracle ORA-38104, pointless self-assign elsewhere) — pass
		/// <see langword="null"/> for standalone Update where match is the table's primary key.
		/// </summary>
		public static LambdaExpression BuildUpdateSetter(
			Type                                                                  entityType,
			EntityDescriptor                                                      entityDescriptor,
			ParameterExpression                                                   entityParm,
			IReadOnlyList<(Expression Field, LambdaExpression Value)>             setOverrides,
			IReadOnlyList<Expression>                                             ignoreList,
			HashSet<Expression>?                                                  matchColumns = null)
		{
			var tParm    = Expression.Parameter(entityType, "t");
			var sParm    = Expression.Parameter(entityType, "s");
			var bindings = new List<MemberBinding>();

			foreach (var cd in entityDescriptor.Columns)
			{
				if (cd.IsPrimaryKey) continue;
				if (cd.SkipOnUpdate) continue;

				var canonicalField = cd.MemberAccessor.GetGetterExpression(entityParm);

				// Match columns appear in the ON clause — including them in UPDATE SET is
				// forbidden by Oracle (ORA-38104) and pointless elsewhere. Skip unless the
				// user explicitly opted in via .Set(x => x.MatchCol, ...).
				if (matchColumns != null && matchColumns.Contains(canonicalField) && FindOverride(canonicalField, setOverrides) == null)
					continue;

				if (IsIgnored(canonicalField, ignoreList))
					continue;

				var @override = FindOverride(canonicalField, setOverrides);

				var value = @override != null
					? InstantiateSetter(@override, tParm, sParm)
					: cd.MemberAccessor.GetGetterExpression(sParm);

				bindings.Add(Expression.Bind(cd.MemberInfo, value));
			}

			var body = Expression.MemberInit(Expression.New(entityType), bindings);
			return Expression.Lambda(body, tParm, sParm);
		}

		/// <summary>True if <paramref name="canonicalField"/> appears in <paramref name="list"/> by structural equality.</summary>
		public static bool IsIgnored(Expression canonicalField, IReadOnlyList<Expression> list)
		{
			foreach (var e in list)
				if (ExpressionEqualityComparer.Instance.Equals(e, canonicalField)) return true;
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
				if (ExpressionEqualityComparer.Instance.Equals(f, canonicalField))
					winner = v;
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
