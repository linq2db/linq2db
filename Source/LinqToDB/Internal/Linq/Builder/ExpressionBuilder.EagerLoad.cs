using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;

using LinqToDB.Expressions;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.SqlQuery;

namespace LinqToDB.Internal.Linq.Builder
{
	partial class ExpressionBuilder
	{
		public static readonly ParameterExpression PreambleParam =
			Expression.Parameter(typeof(object[]), "preamble");

		void CollectDependencies(IBuildContext context, Expression expression, HashSet<Expression> dependencies)
		{
			var toIgnore     = new HashSet<Expression>(ExpressionEqualityComparer.Instance);
			expression.Visit((dependencies, context, builder: this, toIgnore), static (ctx, e) =>
			{
				if (ctx.toIgnore.Contains(e))
					return false;

				if (e.NodeType == ExpressionType.MemberAccess)
				{
					var current = e;
					do
					{
						if (current is not MemberExpression me)
							break;

						current = me.Expression;
						if (current is ContextRefExpression)
						{
							break;
						}
					} while (true);

					if (current is ContextRefExpression)
					{
						var testExpr = ctx.builder.BuildSqlExpression(ctx.context, e, BuildPurpose.Sql, BuildFlags.ForKeys);
						if (testExpr is SqlPlaceholderExpression or SqlGenericConstructorExpression)
							ctx.dependencies.Add(e);

						return false;
					}
				}
				else if (e is BinaryExpression binary)
				{
					if (binary.Left is ContextRefExpression)
					{
						ctx.dependencies.Add(binary.Left);
						return false;
					}

					if (binary.Right is ContextRefExpression)
					{
						ctx.dependencies.Add(binary.Right);
						return false;
					}
				}
				else if (e is SqlPlaceholderExpression placeholder)
				{
					ctx.dependencies.Add(placeholder);
					return false;
				}

				return true;
			});
		}

		private static readonly Type[] ValueTupleTypes =
		[
			typeof(ValueTuple<>),
			typeof(ValueTuple<,>),
			typeof(ValueTuple<,,>),
			typeof(ValueTuple<,,,>),
			typeof(ValueTuple<,,,,>),
			typeof(ValueTuple<,,,,,>),
			typeof(ValueTuple<,,,,,,>),
			typeof(ValueTuple<,,,,,,,>),
		];

		static Expression GenerateKeyExpression(Expression[] members, int startIndex)
		{
			var count = members.Length - startIndex;
			if (count == 0)
				throw new ArgumentOutOfRangeException(nameof(startIndex));

			Expression[] arguments;

			if (count > ValueTupleTypes.Length)
			{
				count     = ValueTupleTypes.Length;
				arguments = new Expression[count];
				Array.Copy(members, startIndex, arguments, 0, count - 1);
				arguments[count - 1] = GenerateKeyExpression(members, startIndex + count);
			}
			else
			{
				arguments = new Expression[count];
				Array.Copy(members, startIndex, arguments, 0, count);
			}

			var type         = ValueTupleTypes[count - 1];
			var argTypes     = arguments.Select(a => a.Type).ToArray();
			var concreteType = type.MakeGenericType(argTypes);
			var constructor  = concreteType.GetConstructor(argTypes) ??
				throw new LinqToDBException($"Cannot retrieve default constructor for '{type.Name}'");

			return Expression.New(
				constructor,
				arguments
			);
		}

		[StructLayout(LayoutKind.Auto)]
		readonly struct KeyDetailEnvelope<TKey, TDetail>
			where TKey: notnull
		{
			public KeyDetailEnvelope(TKey key, TDetail detail)
			{
				Key    = key;
				Detail = detail;
			}

			public readonly TKey    Key;
			public readonly TDetail Detail;
		}

		/// <summary>
		/// CTE envelope for CteUnion eager loading strategy.
		/// Carries RN (row number for deterministic ordering),
		/// Key (parent correlation key), and Data (source entity).
		/// RN = ROW_NUMBER() OVER (ORDER BY Key) is computed in the CTE.
		/// </summary>
		[StructLayout(LayoutKind.Auto)]
		readonly struct CteUnionEnvelope<TKey, TData>
			where TKey : notnull
		{
			public CteUnionEnvelope(long rn, TKey key, TData data)
			{
				RN   = rn;
				Key  = key;
				Data = data;
			}

			public readonly long  RN;
			public readonly TKey  Key;
			public readonly TData Data;
		}

		Expression ExpandContexts(IBuildContext context, Expression expression)
		{
			//var before = new ExpressionPrinter().PrintExpression(expression);

			var projected = BuildExpandExpression(context, expression);

			//var after = new ExpressionPrinter().PrintExpression(projected);

			return projected;
		}

		List<(LambdaExpression, bool)>? CollectOrderBy(Expression sequenceExpression)
		{
			var current = sequenceExpression.UnwrapConvert();

			List<(LambdaExpression, bool)>? result = null;
			LambdaExpression? selectProjection = null;

			while (current is MethodCallExpression { IsQueryable: true } mc)
			{
				if (mc.Method.Name is nameof(Enumerable.ThenBy))
				{
					result ??= new ();
					result.Add((mc.Arguments[1].UnwrapLambda(), false));
				}
				else if (mc.Method.Name is nameof(Enumerable.ThenByDescending))
				{
					result ??= new ();
					result.Add((mc.Arguments[1].UnwrapLambda(), true));
				}
				else if (mc.Method.Name is nameof(Enumerable.OrderBy))
				{
					result ??= new ();
					result.Add((mc.Arguments[1].UnwrapLambda(), false));
					break;
				}
				else if (mc.Method.Name is nameof(Enumerable.OrderByDescending))
				{
					result ??= new ();
					result.Add((mc.Arguments[1].UnwrapLambda(), true));
					break;
				}
				else if (mc.Method.Name is "Select" && mc.Arguments.Count == 2)
				{
					// Record the Select so we can remap OrderBy through it later
					selectProjection = mc.Arguments[1].UnwrapLambda();
				}

				current = mc.Arguments[0];
			}

			result?.Reverse();

			// Compose OrderBy lambdas through the Select projection so they reference
			// the projected type (which matches detailParameter / branchSourceType).
			if (result != null && selectProjection != null)
				result = RemapOrderByThroughSelect(result, selectProjection);

			return result;
		}

		/// <summary>
		/// Remaps OrderBy lambdas (in entity-type terms) through a <c>Select</c> projection
		/// so they reference members of the projected type.
		/// E.g., <c>(Department d =&gt; d.Id)</c> through <c>Select(d =&gt; new { d.Id, d.Name })</c>
		/// becomes <c>(AnonymousType p =&gt; p.Id)</c>.
		/// </summary>
		static List<(LambdaExpression, bool)> RemapOrderByThroughSelect(
			List<(LambdaExpression lambda, bool descending)> orderByList,
			LambdaExpression selectProjection)
		{
			var selectBody = selectProjection.Body;
			var selectParam = selectProjection.Parameters[0];

			if (selectBody is not NewExpression { Members: not null } ne)
				return orderByList;

			var projectedParam = Expression.Parameter(selectBody.Type, "p");
			var remapped = new List<(LambdaExpression, bool)>(orderByList.Count);

			foreach (var (lambda, descending) in orderByList)
			{
				var body  = lambda.GetBody(selectParam);
				var found = false;

				for (int i = 0; i < ne.Arguments.Count; i++)
				{
					// Direct match: Select(d => new { d.Id, ... }) + OrderBy(d => d.Id) → p.Id
					if (ExpressionEqualityComparer.Instance.Equals(ne.Arguments[i], body))
					{
						var memberAccess = Expression.MakeMemberAccess(projectedParam, ne.Members[i]);
						remapped.Add((Expression.Lambda(memberAccess, projectedParam), descending));
						found = true;
						break;
					}

					// Nested match: Select(d => new { Dept = d, ... }) + OrderBy(d => d.Id) → p.Dept.Id
					if (body is MemberExpression me
						&& me.Expression != null
						&& ExpressionEqualityComparer.Instance.Equals(ne.Arguments[i], me.Expression))
					{
						var wrapperAccess = Expression.MakeMemberAccess(projectedParam, ne.Members[i]);
						var nestedAccess  = Expression.MakeMemberAccess(wrapperAccess, me.Member);
						remapped.Add((Expression.Lambda(nestedAccess, projectedParam), descending));
						found = true;
						break;
					}
				}

				if (!found)
					remapped.Add((lambda, descending));
			}

			return remapped;
		}

		static Expression UnwrapDefaultIfEmpty(Expression expression)
		{
			do
			{
				if (expression is MethodCallExpression { IsQueryable: true } mc)
				{
					if (mc.Method.Name is nameof(Enumerable.DefaultIfEmpty))
					{
						expression = mc.Arguments[0];
					}
					else if (mc.Method.Name is nameof(Enumerable.Where) or nameof(Enumerable.Select))
					{
						return mc.Update(mc.Object, mc.Arguments.Select(UnwrapDefaultIfEmpty));
					}
					else if (mc.Method.Name is nameof(Enumerable.SelectMany))
					{
						return mc.Update(mc.Object, mc.Arguments.Select(UnwrapDefaultIfEmpty));
					}
					else
						break;
				}
				else if (expression is SqlAdjustTypeExpression adjust)
				{
					return adjust.Update(UnwrapDefaultIfEmpty(adjust.Expression));
				}
				else
					break;
			} while (true);

			return expression;
		}

		static Expression ApplyEnumerableOrderBy(Expression queryExpr, List<(LambdaExpression Expression, bool Descending)> orderBy)
		{
			var isFirst = true;
			foreach (var order in orderBy)
			{
				var methodName = (isFirst, order.Descending) switch
				{
					(true, true)   => nameof(Queryable.OrderByDescending),
					(true, false)  => nameof(Queryable.OrderBy),
					(false, true)  => nameof(Queryable.ThenByDescending),
					(false, false) => nameof(Queryable.ThenBy),
				};

				var lambda = order.Expression;
				queryExpr = Expression.Call(typeof(Enumerable), methodName, new[] { lambda.Parameters[0].Type, lambda.Body.Type }, queryExpr, lambda);
				isFirst = false;
			}

			return queryExpr;
		}

		static MethodInfo _buildSelectManyDetailSelectorInfo =
			typeof(ExpressionBuilder).GetMethod(nameof(BuildSelectManyDetailSelector), BindingFlags.Static | BindingFlags.NonPublic) ?? throw new InvalidOperationException();

		static LambdaExpression BuildSelectManyDetailSelector<TMain, TDetail>(Expression body, ParameterExpression mainParam)
		{
			return Expression.Lambda<Func<TMain, IEnumerable<TDetail>>>(body, mainParam);
		}

		static MethodInfo _buildPreambleQueryAttachedMethodInfo =
			typeof(ExpressionBuilder).GetMethod(nameof(BuildPreambleQueryAttached), BindingFlags.Instance | BindingFlags.NonPublic) ?? throw new InvalidOperationException();

		Expression BuildPreambleQueryAttached<TKey, T>(
			IBuildContext                   sequence,
			Expression                      keyExpression,
			ParameterExpression             queryParameter,
			List<Preamble>                  preambles,
			List<(LambdaExpression, bool)>? additionalOrderBy,
			Expression[]                    previousKeys)
			where TKey : notnull
		{
			var query = new Query<KeyDetailEnvelope<TKey, T>>(DataContext);

			query.Init(sequence);
			query.SetParametersAccessors(_parametersContext.CurrentSqlParameters.ToList());

			if (!BuildQuery(query, sequence, queryParameter, ref preambles!, previousKeys))
				return query.ErrorExpression!;

			var idx      = preambles.Count;
			var preamble = new Preamble<TKey, T>(query);
			preambles.Add(preamble);

			var getListMethod = MemberHelper.MethodOf((PreambleResult<TKey, T> c) => c.GetList(default!));

			Expression resultExpression =
				Expression.Call(
					Expression.Convert(Expression.ArrayIndex(PreambleParam, ExpressionInstances.Int32(idx)),
						typeof(PreambleResult<TKey, T>)), getListMethod, keyExpression);

			if (additionalOrderBy != null)
			{
				resultExpression = ApplyEnumerableOrderBy(resultExpression, additionalOrderBy);
			}

			return resultExpression;
		}

		static MethodInfo _buildPreambleQueryDetachedMethodInfo =
			typeof(ExpressionBuilder).GetMethod(nameof(BuildPreambleQueryDetached), BindingFlags.Instance | BindingFlags.NonPublic) ?? throw new InvalidOperationException();

		Expression BuildPreambleQueryDetached<T>(
			IBuildContext       sequence,
			ParameterExpression queryParameter,
			List<Preamble>      preambles)
		{
			var query = new Query<T>(DataContext);

			query.Init(sequence);
			query.SetParametersAccessors(_parametersContext.CurrentSqlParameters.ToList());

			BuildQuery(query, sequence, queryParameter, ref preambles!, []);

			var idx      = preambles.Count;
			var preamble = new DatachedPreamble<T>(query);
			preambles.Add(preamble);

			var resultExpression = Expression.Convert(Expression.ArrayIndex(PreambleParam, ExpressionInstances.Int32(idx)), typeof(List<T>));

			return resultExpression;
		}

		static Type BuildValueTupleType(Type[] types)
		{
			if (types.Length is 0 or > 56)
				throw new ArgumentException($"Cannot build ValueTuple for {types.Length} fields.", nameof(types));

			if (types.Length <= 7)
				return ValueTupleTypes[types.Length - 1].MakeGenericType(types);

			var restType = BuildValueTupleType(types.Skip(7).ToArray());
			var topTypes = new Type[8];
			Array.Copy(types, 0, topTypes, 0, 7);
			topTypes[7] = restType;
			return typeof(ValueTuple<,,,,,,,>).MakeGenericType(topTypes);
		}

		static Expression BuildValueTupleNew(Type tupleType, Expression[] args)
		{
			if (args.Length <= 7)
			{
				var argTypes = args.Select(a => a.Type).ToArray();
				return Expression.New(tupleType.GetConstructor(argTypes)!, args);
			}

			var restArgs = args.Skip(7).ToArray();
			var restType = tupleType.GetGenericArguments()[7];
			var restNew  = BuildValueTupleNew(restType, restArgs);

			var topArgs = new Expression[8];
			Array.Copy(args, 0, topArgs, 0, 7);
			topArgs[7] = restNew;

			var topArgTypes = topArgs.Select(a => a.Type).ToArray();
			return Expression.New(tupleType.GetConstructor(topArgTypes)!, topArgs);
		}

		/// <summary>
		/// Accesses Item{position+1} field of a ValueTuple expression. Handles nesting via Rest for position >= 7.
		/// </summary>
		static Expression AccessValueTupleField(Expression tuple, int position)
		{
			if (position < 7)
				return Expression.Field(tuple, "Item" + (position + 1).ToString(System.Globalization.CultureInfo.InvariantCulture));
			return AccessValueTupleField(Expression.Field(tuple, "Rest"), position - 7);
		}

		/// <summary>
		/// Resolves the effective <see cref="EagerLoadingStrategy"/> from the build context and global options.
		/// <see cref="EagerLoadingStrategy.CteUnion"/> is transparently remapped to
		/// <see cref="EagerLoadingStrategy.KeyedQuery"/> when the current provider does not support CTEs.
		/// </summary>
		EagerLoadingStrategy ResolveStrategy(IBuildContext buildContext)
		{
			var strategy = buildContext.TranslationModifier.EagerLoadingStrategy
			            ?? DataContext.Options.LinqOptions.DefaultEagerLoadingStrategy;

			if (strategy == EagerLoadingStrategy.CteUnion && !DataContext.SqlProviderFlags.IsCommonTableExpressionsSupported)
				strategy = EagerLoadingStrategy.KeyedQuery;

			return strategy;
		}

		/// <summary>
		/// Processes all <see cref="SqlEagerLoadExpression"/> nodes in <paramref name="expression"/>,
		/// applying the resolved strategy (<c>CteUnion → KeyedQuery → Default</c>).
		/// If any expression cannot be handled by the current strategy the entire set retries
		/// with the next strategy in the chain — ensuring all eager loads use a consistent strategy.
		/// </summary>
		Expression CompleteEagerLoadingExpressions(
			Expression                       expression,
			IBuildContext                    buildContext,
			ParameterExpression              queryParameter,
			ref List<Preamble>?              preambles,
			Expression[]                     previousKeys,
			out Func<Expression, Expression> finalizer)
		{
			if (!ValidateSubqueries)
			{
				finalizer = e => ToColumns(buildContext.GetResultQuery(), e);
				return expression.Transform(static e =>
					e.NodeType == ExpressionType.Extension && e is SqlEagerLoadExpression eagerLoad
						? SqlErrorExpression.EnsureError(eagerLoad.SequenceExpression, e.Type)
						: e);
			}

			var strategy       = ResolveStrategy(buildContext);
			var localPreambles = preambles ?? [];

			while (true)
			{
				// Snapshot mutable state so we can roll back if any expression forces fallback
				var preambleSnapshot   = localPreambles.Count;
				var hasKeyedQuerySaved = _hasKeyedQueryPreambles;

				// Phase 1: CteUnion — try to batch all eager loads into a single UNION ALL query
				Dictionary<Expression, Expression>? cteUnionCache = null;

				if (strategy == EagerLoadingStrategy.CteUnion)
					cteUnionCache = ProcessCteUnionBatch(expression, buildContext, queryParameter, localPreambles, previousKeys);

				// Phase 2: Process each remaining eager load with the current strategy
				var failed             = false;
				Dictionary<Expression, Expression>? eagerLoadingCache = null;

				var updated = expression.Transform(e =>
				{
					if (failed)
						return e;

					if (e.NodeType != ExpressionType.Extension || e is not SqlEagerLoadExpression eagerLoad)
						return e;

					if (cteUnionCache != null && cteUnionCache.TryGetValue(eagerLoad.SequenceExpression, out var cachedExpression))
						return cachedExpression;

					eagerLoadingCache ??= new Dictionary<Expression, Expression>(ExpressionEqualityComparer.Instance);
					if (!eagerLoadingCache.TryGetValue(eagerLoad.SequenceExpression, out var preambleExpression))
					{
						preambleExpression = strategy switch
						{
							// Not caught by the CteUnion batch — force whole-strategy fallback
							EagerLoadingStrategy.CteUnion   => null,
							EagerLoadingStrategy.KeyedQuery => ProcessEagerLoadingKeyedQuery(buildContext, eagerLoad, queryParameter, localPreambles, previousKeys),
							_                               => ProcessEagerLoadingExpression(buildContext, eagerLoad, queryParameter, localPreambles, previousKeys),
						};

						if (preambleExpression == null)
						{
							failed = true;
							return e;
						}

						eagerLoadingCache.Add(eagerLoad.SequenceExpression, preambleExpression);
					}

					return preambleExpression;
				});

				if (!failed)
				{
					// Single-query CteUnion (parent branch inlined into carrier) reconstructs from
					// path-based carrier slots — ToColumns must be skipped. All other modes (preamble-only
					// CteUnion, KeyedQuery, Default) use column-index-based projection via ToColumns.
					finalizer = _hasCteUnionQuery
						? static e => e
						: e => ToColumns(buildContext.GetResultQuery(), e);

					preambles = localPreambles;
					return updated;
				}

				// Roll back preambles and KeyedQuery flags added during this attempt
				localPreambles.RemoveRange(preambleSnapshot, localPreambles.Count - preambleSnapshot);
				_hasKeyedQueryPreambles = hasKeyedQuerySaved;

				strategy = strategy switch
				{
					EagerLoadingStrategy.CteUnion   => EagerLoadingStrategy.KeyedQuery,
					EagerLoadingStrategy.KeyedQuery => EagerLoadingStrategy.Default,
					_                               => throw new InvalidOperationException("EagerLoadingStrategy.Default must never fail."),
				};
			}
		}

		sealed class PreambleResult<TKey, T>
			where TKey : notnull
		{
			Dictionary<TKey, List<T>>? _items;
			TKey                       _prevKey = default!;
			List<T>?                   _prevList;
			readonly IEqualityComparer<TKey>? _comparer;

			public PreambleResult()
			{
			}

			public PreambleResult(IEqualityComparer<TKey> comparer)
			{
				_comparer = comparer;
			}

			public void Add(TKey key, T item)
			{
				List<T>? list;

				if (_prevList != null && _prevKey!.Equals(key))
				{
					list = _prevList;
				}
				else
				{
					if (_items == null)
					{
						_items = new Dictionary<TKey, List<T>>(
							_comparer ?? (IEqualityComparer<TKey>)ValueComparer.GetDefaultValueComparer<TKey>(favorStructuralComparisons: true));
						list   = new List<T>();
						_items.Add(key, list);
					}
					else if (!_items.TryGetValue(key, out list))
					{
						list = new List<T>();
						_items.Add(key, list);
					}

					_prevKey  = key;
					_prevList = list;
				}

				list.Add(item);
			}

			public List<T> GetList(TKey key)
			{
				if (_items == null || !_items.TryGetValue(key, out var list))
					return new List<T>();
				return list;
			}
		}

	}
}
