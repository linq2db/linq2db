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
			sequenceExpression = sequenceExpression.UnwrapConvert();
			var current = sequenceExpression;

			List<(LambdaExpression, bool)>? result = null;

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

				current = mc.Arguments[0];
				if (!mc.Type.IsSameOrParentOf(current.Type))
					break;
			}

			result?.Reverse();

			return result;
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
		/// Resolves the effective <see cref="EagerLoadingStrategy"/> for a given eager-load node,
		/// taking into account the per-association override and the global default from <see cref="LinqOptions"/>.
		/// </summary>
		EagerLoadingStrategy ResolveStrategy(SqlEagerLoadExpression eagerLoad, IBuildContext? buildContext = null)
		{
			var strategy = eagerLoad.Strategy != EagerLoadingStrategy.Default
				? eagerLoad.Strategy
				: buildContext?.TranslationModifier.EagerLoadingStrategy
				  ?? DataContext.Options.LinqOptions.DefaultEagerLoadingStrategy;

			return strategy;
		}

		/// <summary>
		/// Executes the given eager loading strategy with automatic fallback chain:
		/// <c>CteUnion → PostQuery → Default</c>.
		/// Each strategy returns the preamble access expression, or falls through to the next
		/// strategy if it can't handle the query shape. Recursion is detected to prevent
		/// infinite loops if a misconfigured fallback order revisits a strategy.
		/// </summary>
		Expression ExecuteWithFallback(
			EagerLoadingStrategy strategy,
			IBuildContext        buildContext,
			SqlEagerLoadExpression eagerLoad,
			ParameterExpression  queryParameter,
			List<Preamble>       preambles,
			Expression[]         previousKeys)
		{
			var tried = EagerLoadingStrategy.Default; // bitmask: Default=0 never blocks

			while (true)
			{
				// Recursion guard: detect if we've already tried this strategy
				var bit = (EagerLoadingStrategy)(1 << (int)strategy);
				if ((tried & bit) != 0)
				{
					// Already tried — fall through to Default (terminal)
					return ProcessEagerLoadingExpression(
						buildContext, eagerLoad, queryParameter, preambles, previousKeys);
				}

				tried |= bit;

				Expression? result = strategy switch
				{
					EagerLoadingStrategy.CteUnion  => ProcessEagerLoadingCteUnion(
						buildContext, eagerLoad, queryParameter, preambles, previousKeys),
					// PostQuery always returns non-null (handles its own fallback to Default internally)
					EagerLoadingStrategy.PostQuery => ProcessEagerLoadingPostQuery(
						buildContext, eagerLoad, queryParameter, preambles, previousKeys),
					_                              => ProcessEagerLoadingExpression(
						buildContext, eagerLoad, queryParameter, preambles, previousKeys),
				};

				if (result != null)
					return result;

				// Strategy returned null — fall back to Default (terminal).
				// CteUnion → Default (skip PostQuery to avoid side effects from ExpandContexts).
				// PostQuery handles its own fallback to Default internally.
				strategy = EagerLoadingStrategy.Default;
			}
		}

		Expression CompleteEagerLoadingExpressions(
			Expression          expression,
			IBuildContext       buildContext,
			ParameterExpression queryParameter,
			ref List<Preamble>? preambles,
			Expression[]        previousKeys)
		{
			if (!ValidateSubqueries)
			{
				return expression.Transform(static e =>
					e.NodeType == ExpressionType.Extension && e is SqlEagerLoadExpression eagerLoad
						? SqlErrorExpression.EnsureError(eagerLoad.SequenceExpression, e.Type)
						: e);
			}

			// Phase 1: Try batch-processing CteUnion eager loads into a single UNION ALL query
			var preamblesLocal = preambles;
			preamblesLocal ??= [];

			var cteUnionCache = ProcessCteUnionBatch(expression, buildContext, queryParameter, preamblesLocal, previousKeys);

			// Phase 2: Process remaining eager loads (Default, PostQuery, or CteUnion fallbacks)
			Dictionary<Expression, Expression>? eagerLoadingCache = null;

			var updatedEagerLoading = expression.Transform(e =>
			{
				if (e.NodeType == ExpressionType.Extension && e is SqlEagerLoadExpression eagerLoad)
				{
					// Check if already handled by CteUnion batch
					if (cteUnionCache != null && cteUnionCache.TryGetValue(eagerLoad.SequenceExpression, out var cachedExpression))
						return cachedExpression;

					eagerLoadingCache ??= new Dictionary<Expression, Expression>(ExpressionEqualityComparer.Instance);
					if (!eagerLoadingCache.TryGetValue(eagerLoad.SequenceExpression, out var preambleExpression))
					{
						var strategy = ResolveStrategy(eagerLoad, buildContext);

						preambleExpression = ExecuteWithFallback(
							strategy, buildContext, eagerLoad, queryParameter, preamblesLocal, previousKeys);

						eagerLoadingCache.Add(eagerLoad.SequenceExpression, preambleExpression);
					}

					return preambleExpression;
				}

				return e;
			});

			preambles = preamblesLocal;

			return updatedEagerLoading;
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
