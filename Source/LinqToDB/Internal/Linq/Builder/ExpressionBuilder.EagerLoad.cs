using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Expressions;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.Reflection;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Metrics;

namespace LinqToDB.Internal.Linq.Builder
{
	partial class ExpressionBuilder
	{
		public static readonly ParameterExpression PreambleParam =
			Expression.Parameter(typeof(object[]), "preamble");

		/// <summary>Set by ProcessEagerLoadingPostQuery to signal BuildQuery that buffer materialization is needed.</summary>
		bool _hasPostQueryPreambles;

		/// <summary>Marker interface for PostQueryKeysPreamble, used during buffer setup.</summary>
		interface IPostQueryKeysPreamble
		{
			/// <summary>Extract distinct keys from the buffer and populate the holder.</summary>
			void SetKeysFromBuffer(IList buffer);
		}

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
			var concreteType = type.MakeGenericType(arguments.Select(a => a.Type).ToArray());
			var constructor  = concreteType.GetConstructor(arguments.Select(a => a.Type).ToArray()) ??
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

		Expression ProcessEagerLoadingExpression(
			IBuildContext          buildContext,
			SqlEagerLoadExpression eagerLoad,
			ParameterExpression    queryParameter,
			List<Preamble>         preambles,
			Expression[]           previousKeys)
		{
			var cloningContext = new CloningContext();

			var itemType = eagerLoad.Type.GetItemType();

			if (itemType == null)
				throw new InvalidOperationException("Could not retrieve itemType for EagerLoading.");

			var dependencies = new HashSet<Expression>(ExpressionEqualityComparer.Instance);

			var sequenceExpression = eagerLoad.SequenceExpression;
			//var sequenceExpression = UnwrapDefaultIfEmpty(eagerLoad.SequenceExpression);

			sequenceExpression = ExpandContexts(buildContext, sequenceExpression);
			//sequenceExpression = UnwrapDefaultIfEmpty(sequenceExpression);

			CollectDependencies(buildContext, sequenceExpression, dependencies);

			var clonedParentContext = cloningContext.CloneContext(buildContext);
			clonedParentContext = new EagerContext(new SubQueryContext(clonedParentContext), buildContext.ElementType);

			var correctedSequence  = cloningContext.CloneExpression(sequenceExpression);
			var correctedPredicate = cloningContext.CloneExpression(eagerLoad.Predicate);

			dependencies.AddRange(previousKeys);

			var mainKeys   = new Expression[dependencies.Count];
			var detailKeys = new Expression[dependencies.Count];

			int i = 0;
			foreach (var dependency in dependencies)
			{
				mainKeys[i]   = dependency;
				detailKeys[i] = cloningContext.CloneExpression(dependency);
				++i;
			}

			Expression resultExpression;

			var mainType   = clonedParentContext.ElementType;
			var detailType = TypeHelper.GetEnumerableElementType(eagerLoad.Type);

			if (dependencies.Count == 0)
			{
				var detailSequence = BuildSequence(new BuildInfo((IBuildContext?)null, correctedSequence, new SelectQuery()));

				var parameters = new object[] { detailSequence, queryParameter, preambles };

				resultExpression = _buildPreambleQueryDetachedMethodInfo
					.MakeGenericMethod(detailType)
					.InvokeExt<Expression>(this, parameters);
			}
			else
			{
				if (correctedPredicate != null)
				{
					var predicateExpr = BuildSqlExpression(clonedParentContext, correctedPredicate);

					if (predicateExpr is not SqlPlaceholderExpression { Sql: ISqlPredicate predicateSql })
					{
						throw SqlErrorExpression.EnsureError(predicateExpr, correctedPredicate.Type).CreateException();
					}

					clonedParentContext.SelectQuery.Where.EnsureConjunction().Add(predicateSql);
				}

				var orderByToApply = CollectOrderBy(correctedSequence);

				var mainKeyExpression   = GenerateKeyExpression(mainKeys, 0);
				var detailKeyExpression = GenerateKeyExpression(detailKeys, 0);

				var keyDetailType   = typeof(KeyDetailEnvelope<,>).MakeGenericType(mainKeyExpression.Type, detailType);
				var mainParameter   = Expression.Parameter(mainType, "m");
				var detailParameter = Expression.Parameter(detailType, "d");

				var keyDetailExpression = Expression.New(keyDetailType.GetConstructor([mainKeyExpression.Type, detailType])!, detailKeyExpression, detailParameter);

				var clonedParentContextRef = new ContextRefExpression(typeof(IQueryable<>).MakeGenericType(clonedParentContext.ElementType), clonedParentContext);

				Expression sourceQuery = clonedParentContextRef;

				if (!typeof(IQueryable<>).IsSameOrParentOf(sourceQuery.Type))
				{
					sourceQuery = Expression.Call(Methods.Queryable.AsQueryable.MakeGenericMethod(mainType), sourceQuery);
				}

				sourceQuery = Expression.Call(Methods.LinqToDB.SelectDistinct.MakeGenericMethod(mainType), sourceQuery);

				var selector = Expression.Lambda(keyDetailExpression, mainParameter, detailParameter);

				var detailSelectorBody = correctedSequence;

				var detailSelector = _buildSelectManyDetailSelectorInfo
					.MakeGenericMethod(mainType, detailType)
					.InvokeExt<LambdaExpression>(null, new object[] { detailSelectorBody, mainParameter });

				var selectManyCall =
					Expression.Call(
						Methods.Queryable.SelectManyProjection.MakeGenericMethod(mainType, detailType, keyDetailType),
						sourceQuery, Expression.Quote(detailSelector), Expression.Quote(selector));

				var saveVisitor = _buildVisitor;
				_buildVisitor = _buildVisitor.Clone(cloningContext);

				cloningContext.UpdateContextParents();

				var detailSequence = BuildSequence(new BuildInfo((IBuildContext?)null, selectManyCall,
					clonedParentContextRef.BuildContext.SelectQuery));

				var parameters = new object?[] { detailSequence, mainKeyExpression, queryParameter, preambles, orderByToApply, detailKeys };

				resultExpression = _buildPreambleQueryAttachedMethodInfo
					.MakeGenericMethod(mainKeyExpression.Type, detailType)
					.InvokeExt<Expression>(this, parameters);

				_buildVisitor = saveVisitor;
			}

			if (resultExpression is SqlErrorExpression errorExpression)
			{
				return errorExpression.WithType(eagerLoad.Type);
			}

			resultExpression = SqlAdjustTypeExpression.AdjustType(resultExpression, eagerLoad.Type, MappingSchema);

			return resultExpression;
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

		/// <summary>
		/// PostQuery strategy: joins child records to a local key collection (VALUES table)
		/// instead of re-querying the parent table. Keys are provided at runtime through
		/// a <see cref="PostQueryKeysHolder{TKey}"/> populated by a key-extraction preamble.
		/// Inner eager loads within the child query fall back to Default strategy.
		/// </summary>
		Expression ProcessEagerLoadingPostQuery(
			IBuildContext          buildContext,
			SqlEagerLoadExpression eagerLoad,
			ParameterExpression    queryParameter,
			List<Preamble>         preambles,
			Expression[]           previousKeys)
		{
			var cloningContext = new CloningContext();

			var itemType = eagerLoad.Type.GetItemType();

			if (itemType == null)
				throw new InvalidOperationException("Could not retrieve itemType for EagerLoading.");

			var dependencies = new HashSet<Expression>(ExpressionEqualityComparer.Instance);

			var sequenceExpression = eagerLoad.SequenceExpression;
			sequenceExpression     = ExpandContexts(buildContext, sequenceExpression);

			CollectDependencies(buildContext, sequenceExpression, dependencies);

			var clonedParentContext = cloningContext.CloneContext(buildContext);
			clonedParentContext     = new EagerContext(new SubQueryContext(clonedParentContext), buildContext.ElementType);

			var correctedSequence  = cloningContext.CloneExpression(sequenceExpression);
			var correctedPredicate = cloningContext.CloneExpression(eagerLoad.Predicate);

			dependencies.AddRange(previousKeys);

			var mainKeys   = new Expression[dependencies.Count];
			var detailKeys = new Expression[dependencies.Count];

			int i = 0;
			foreach (var dependency in dependencies)
			{
				mainKeys[i]   = dependency;
				detailKeys[i] = cloningContext.CloneExpression(dependency);
				++i;
			}

			Expression resultExpression;

			var mainType   = clonedParentContext.ElementType;
			var detailType = TypeHelper.GetEnumerableElementType(eagerLoad.Type);

			if (dependencies.Count == 0)
			{
				// No dependencies — identical to Default for the detached case
				var detailSequence = BuildSequence(new BuildInfo((IBuildContext?)null, correctedSequence, new SelectQuery()));

				var parameters = new object[] { detailSequence, queryParameter, preambles };

				resultExpression = _buildPreambleQueryDetachedMethodInfo
					.MakeGenericMethod(detailType)
					.InvokeExt<Expression>(this, parameters);
			}
			else
			{
				if (correctedPredicate != null)
				{
					var predicateExpr = BuildSqlExpression(clonedParentContext, correctedPredicate);

					if (predicateExpr is not SqlPlaceholderExpression { Sql: ISqlPredicate predicateSql })
					{
						throw SqlErrorExpression.EnsureError(predicateExpr, correctedPredicate.Type).CreateException();
					}

					clonedParentContext.SelectQuery.Where.EnsureConjunction().Add(predicateSql);
				}

				var orderByToApply = CollectOrderBy(correctedSequence);

				var mainKeyExpression   = GenerateKeyExpression(mainKeys, 0);
				var detailKeyExpression = GenerateKeyExpression(detailKeys, 0);

				var keyType          = mainKeyExpression.Type;
				var keyDetailType    = typeof(KeyDetailEnvelope<,>).MakeGenericType(keyType, detailType);
				var keyParameter     = Expression.Parameter(keyType, "k");
				var detailParameter  = Expression.Parameter(detailType, "d");

				// --- PostQuery: local key collection join ---
				// Replace parent-key references in correctedSequence with keyParameter,
				// then build SelectMany with local keys from PostQueryKeysHolder.
				var correctedSequenceWithLocalKey = ReplaceDetailKeysWithParameter(
					correctedSequence, detailKeys, keyParameter);

				var keyDetailExpression = Expression.New(
					keyDetailType.GetConstructor([keyType, detailType])!,
					keyParameter,
					detailParameter);
				var selector = Expression.Lambda(keyDetailExpression, keyParameter, detailParameter);

				var detailSelector = _buildSelectManyDetailSelectorInfo
					.MakeGenericMethod(keyType, detailType)
					.InvokeExt<LambdaExpression>(null, new object[] { correctedSequenceWithLocalKey, keyParameter });

				// Source: local key collection from PostQueryKeysHolder
				var holderAndSourceExpr = _buildPostQueryKeysSourceMethodInfo
					.MakeGenericMethod(keyType)
					.InvokeExt<(object holder, Expression sourceExpr)>(null, Array.Empty<object>());

				Expression sourceQuery = Expression.Call(
					Methods.Queryable.AsQueryable.MakeGenericMethod(keyType),
					holderAndSourceExpr.sourceExpr);

				var selectManyCall =
					Expression.Call(
						Methods.Queryable.SelectManyProjection.MakeGenericMethod(keyType, detailType, keyDetailType),
						sourceQuery, Expression.Quote(detailSelector), Expression.Quote(selector));

				// --- Build key extraction query using a SEPARATE clone (Default-style) ---
				// Key extraction uses standard SQL tables (not VALUES) so it works at any nesting depth.
				// We create a fresh CloningContext for key extraction to avoid the outer VALUES table.
				var keyCloningContext      = new CloningContext();
				var keyClonedParent        = keyCloningContext.CloneContext(buildContext);
				keyClonedParent            = new EagerContext(new SubQueryContext(keyClonedParent), buildContext.ElementType);

				if (eagerLoad.Predicate != null)
				{
					// Apply the same predicate to the key extraction context
					var keyPredicateExpr = BuildSqlExpression(keyClonedParent, keyCloningContext.CloneExpression(eagerLoad.Predicate)!);

					if (keyPredicateExpr is SqlPlaceholderExpression { Sql: ISqlPredicate keyPredicateSql })
						keyClonedParent.SelectQuery.Where.EnsureConjunction().Add(keyPredicateSql);
				}

				var keyClonedParentRef = new ContextRefExpression(
					typeof(IQueryable<>).MakeGenericType(keyClonedParent.ElementType), keyClonedParent);

				var keyDetailKeys = new Expression[dependencies.Count];
				{
					int ki = 0;
					foreach (var dep in dependencies)
					{
						keyDetailKeys[ki] = keyCloningContext.CloneExpression(dep);
						++ki;
					}
				}
				var keyDetailKeyExpression = GenerateKeyExpression(keyDetailKeys, 0);

				Expression keyExtractionQuery = keyClonedParentRef;
				if (!typeof(IQueryable<>).IsSameOrParentOf(keyExtractionQuery.Type))
					keyExtractionQuery = Expression.Call(Methods.Queryable.AsQueryable.MakeGenericMethod(mainType), keyExtractionQuery);

				var mainParameter = Expression.Parameter(mainType, "m");
				var keySelector   = Expression.Lambda(keyDetailKeyExpression, mainParameter);

				keyExtractionQuery = Expression.Call(
					Methods.Queryable.Select.MakeGenericMethod(mainType, keyType),
					keyExtractionQuery,
					Expression.Quote(keySelector));

				keyExtractionQuery = Expression.Call(Methods.LinqToDB.SelectDistinct.MakeGenericMethod(keyType), keyExtractionQuery);

				var saveVisitor = _buildVisitor;

				// Build key extraction sequence using its own cloning context
				var keyVisitor = _buildVisitor.Clone(keyCloningContext);
				_buildVisitor = keyVisitor;
				keyCloningContext.UpdateContextParents();

				var keyExtractionSequence = BuildSequence(new BuildInfo((IBuildContext?)null, keyExtractionQuery,
					keyClonedParentRef.BuildContext.SelectQuery));

				// Restore visitor before building child query — the child query uses
				// its own local keys (VALUES) and doesn't reference the outer context.
				_buildVisitor = saveVisitor;

				var detailSequence = BuildSequence(new BuildInfo((IBuildContext?)null, selectManyCall,
					new SelectQuery()));

				var parameters = new object?[] { detailSequence, mainKeyExpression, queryParameter, preambles, orderByToApply, detailKeys, holderAndSourceExpr.holder, keyExtractionSequence };

				resultExpression = _buildPostQueryPreambleAttachedMethodInfo
					.MakeGenericMethod(keyType, detailType)
					.InvokeExt<Expression>(this, parameters);
			}

			if (resultExpression is SqlErrorExpression errorExpression)
				return errorExpression.WithType(eagerLoad.Type);

			resultExpression = SqlAdjustTypeExpression.AdjustType(resultExpression, eagerLoad.Type, MappingSchema);
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
				var ctor = tupleType.GetConstructor(args.Select(a => a.Type).ToArray())!;
				return Expression.New(ctor, args);
			}

			var restArgs = args.Skip(7).ToArray();
			var restType = tupleType.GetGenericArguments()[7];
			var restNew  = BuildValueTupleNew(restType, restArgs);

			var topArgs = new Expression[8];
			Array.Copy(args, 0, topArgs, 0, 7);
			topArgs[7] = restNew;

			var ctor8 = tupleType.GetConstructor(topArgs.Select(a => a.Type).ToArray())!;
			return Expression.New(ctor8, topArgs);
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

		static Expression ReplaceDetailKeysWithParameter(
			Expression           expression,
			Expression[]         detailKeys,
			ParameterExpression  keyParameter)
		{
			var replacements = new Dictionary<Expression, Expression>(ExpressionEqualityComparer.Instance);
			for (var idx = 0; idx < detailKeys.Length; idx++)
			{
				// Always use field access since GenerateKeyExpression wraps even single keys in ValueTuple<T>
				Expression keyAccess = AccessValueTupleField(keyParameter, idx);

				if (keyAccess.Type != detailKeys[idx].Type)
					keyAccess = Expression.Convert(keyAccess, detailKeys[idx].Type);

				replacements[detailKeys[idx]] = keyAccess;
			}

			return expression.Transform(replacements, static (ctx, e) =>
			{
				if (ctx.TryGetValue(e, out var replacement))
					return replacement;
				return e;
			})!;
		}

		/// <summary>
		/// Thread-safe holder for PostQuery local key collections.
		/// </summary>
		sealed class PostQueryKeysHolder<TKey>
		{
			readonly AsyncLocal<TKey[]?> _keys = new();

			public TKey[]? Keys
			{
				get => _keys.Value;
				set => _keys.Value = value;
			}
		}

		static readonly MethodInfo _buildPostQueryKeysSourceMethodInfo =
			typeof(ExpressionBuilder).GetMethod(nameof(BuildPostQueryKeysSource), BindingFlags.Static | BindingFlags.NonPublic)
			?? throw new InvalidOperationException();

		static (object holder, Expression sourceExpr) BuildPostQueryKeysSource<TKey>()
		{
			var holder     = new PostQueryKeysHolder<TKey>();
			var holderExpr = Expression.Constant(holder);
			var keysExpr   = Expression.Property(holderExpr, nameof(PostQueryKeysHolder<TKey>.Keys));

			return (holder, keysExpr);
		}

		static readonly MethodInfo _buildPostQueryPreambleAttachedMethodInfo =
			typeof(ExpressionBuilder).GetMethod(nameof(BuildPostQueryPreambleAttached), BindingFlags.Instance | BindingFlags.NonPublic)
			?? throw new InvalidOperationException();

		Expression BuildPostQueryPreambleAttached<TKey, T>(
			IBuildContext                   childSequence,
			Expression                      keyExpression,
			ParameterExpression             queryParameter,
			List<Preamble>                  preambles,
			List<(LambdaExpression, bool)>? additionalOrderBy,
			Expression[]                    previousKeys,
			object                          keysHolder,
			IBuildContext                   keyExtractionSequence)
			where TKey : notnull
		{
			var holder = (PostQueryKeysHolder<TKey>)keysHolder;

			// --- Step 1: Build key extraction preamble ---
			var keyQuery = new Query<TKey>(DataContext);
			keyQuery.Init(keyExtractionSequence);
			keyQuery.SetParametersAccessors(_parametersContext.CurrentSqlParameters.ToList());

			if (!BuildQuery(keyQuery, keyExtractionSequence, queryParameter, ref preambles!, previousKeys))
				return keyQuery.ErrorExpression!;

			var keyPreamble = new PostQueryKeysPreamble<TKey>(keyQuery, holder) { MainKeyExpression = keyExpression };
			preambles.Add(keyPreamble);

			// Signal BuildQuery to set up buffer materialization
			_hasPostQueryPreambles = true;

			// --- Step 2: Build child query preamble ---
			// Pass empty previousKeys: the child query joins VALUES (local keys) to the
			// child table directly. Ancestor-level filtering is already handled by the
			// key extraction query. Inner eager loads don't need outer key references.
			// Save/restore _hasPostQueryPreambles to prevent buffer setup leaking into inner queries.
			var savedHasPostQuery = _hasPostQueryPreambles;
			_hasPostQueryPreambles = false;

			var childQuery = new Query<KeyDetailEnvelope<TKey, T>>(DataContext);
			childQuery.Init(childSequence);
			childQuery.SetParametersAccessors(_parametersContext.CurrentSqlParameters.ToList());

			if (!BuildQuery(childQuery, childSequence, queryParameter, ref preambles!, Array.Empty<Expression>()))
			{
				_hasPostQueryPreambles = savedHasPostQuery;
				return childQuery.ErrorExpression!;
			}

			_hasPostQueryPreambles = savedHasPostQuery;

			var idx          = preambles.Count;
			var childPreamble = new PostQueryChildPreamble<TKey, T>(childQuery, holder);
			preambles.Add(childPreamble);

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

		sealed class PostQueryKeysPreamble<TKey> : Preamble, IPostQueryKeysPreamble
			where TKey : notnull
		{
			readonly Query<TKey>               _query;
			readonly PostQueryKeysHolder<TKey>  _holder;

			/// <summary>
			/// Set during buffer setup: extracts TKey from a buffer row (ValueTuple cast to object).
			/// Null when buffer is not used (fallback to SQL key extraction).
			/// </summary>
			public Func<object, TKey>? BufferKeyExtractor { get; set; }

			/// <summary>
			/// The main key expression (composed of SqlPlaceholderExpressions) used to build BufferKeyExtractor.
			/// </summary>
			public Expression? MainKeyExpression { get; set; }

			public PostQueryKeysPreamble(Query<TKey> query, PostQueryKeysHolder<TKey> holder)
			{
				_query  = query;
				_holder = holder;
			}

			public override object Execute(IDataContext dataContext, IQueryExpressions expressions, object?[]? parameters, object?[]? preambles)
			{
				var keys = _query.GetResultEnumerable(dataContext, expressions, preambles, preambles).ToArray();
				_holder.Keys = keys;
				return keys;
			}

			public override async Task<object> ExecuteAsync(IDataContext dataContext, IQueryExpressions expressions, object?[]? parameters, object[]? preambles, CancellationToken cancellationToken)
			{
				var keys = await _query.GetResultEnumerable(dataContext, expressions, preambles, preambles)
					.ToArrayAsync(cancellationToken).ConfigureAwait(false);
				_holder.Keys = keys;
				return keys;
			}

			public void SetKeysFromBuffer(IList buffer)
			{
				if (BufferKeyExtractor == null)
				{
					// Extractor not set — buffer optimization not available for this preamble.
					// Fall back to SQL-based key extraction (Execute will be called normally).
					return;
				}

				var keySet = new HashSet<TKey>(ValueComparer.GetDefaultValueComparer<TKey>(favorStructuralComparisons: true));
				foreach (var row in buffer)
					keySet.Add(BufferKeyExtractor(row));

				_holder.Keys = keySet.ToArray();
			}

			public override void GetUsedParametersAndValues(ICollection<SqlParameter> parameters, ICollection<SqlValue> values)
			{
				foreach (var query in _query.Queries)
					QueryHelper.CollectParametersAndValues(query.Statement, parameters, values);
			}
		}

		sealed class PostQueryChildPreamble<TKey, T> : Preamble
			where TKey : notnull
		{
			readonly Query<KeyDetailEnvelope<TKey, T>> _query;
			readonly PostQueryKeysHolder<TKey>         _holder;

			public PostQueryChildPreamble(
				Query<KeyDetailEnvelope<TKey, T>> query,
				PostQueryKeysHolder<TKey>         holder)
			{
				_query  = query;
				_holder = holder;
			}

			public override object Execute(IDataContext dataContext, IQueryExpressions expressions, object?[]? parameters, object?[]? preambles)
			{
				try
				{
					var result = new PreambleResult<TKey, T>();
					foreach (var e in _query.GetResultEnumerable(dataContext, expressions, preambles, preambles))
					{
						result.Add(e.Key, e.Detail);
					}
					return result;
				}
				finally
				{
					_holder.Keys = null;
				}
			}

			public override async Task<object> ExecuteAsync(IDataContext dataContext, IQueryExpressions expressions, object?[]? parameters, object[]? preambles, CancellationToken cancellationToken)
			{
				try
				{
					var result = new PreambleResult<TKey, T>();
					var enumerator = _query.GetResultEnumerable(dataContext, expressions, preambles, preambles)
						.GetAsyncEnumerator(cancellationToken);

					while (await enumerator.MoveNextAsync().ConfigureAwait(false))
					{
						var e = enumerator.Current;
						result.Add(e.Key, e.Detail);
					}
					return result;
				}
				finally
				{
					_holder.Keys = null;
				}
			}

			public override void GetUsedParametersAndValues(ICollection<SqlParameter> parameters, ICollection<SqlValue> values)
			{
				foreach (var query in _query.Queries)
					QueryHelper.CollectParametersAndValues(query.Statement, parameters, values);
			}
		}

		#region PostQuery Buffer Materialization

		/// <summary>
		/// Sets up buffer materialization: the main SQL runs once as a preamble producing ValueTuple rows,
		/// keys are extracted client-side, and the main query iterates the buffer to reconstruct T.
		/// Called from BuildQuery when _hasPostQueryPreambles is true.
		/// </summary>
		void SetRunQueryWithPostQueryBuffer<T>(Query<T> query, IBuildContext sequence, Expression finalized, List<Preamble> preambles)
		{
			var selectQuery = sequence.SelectQuery;

			// 1. Collect unique resolved SqlPlaceholderExpressions
			var placeholders = new List<SqlPlaceholderExpression>();
			finalized.Visit(placeholders, static (ctx, e) =>
			{
				if (e is SqlPlaceholderExpression p && p.Index != null)
				{
					if (!ctx.Exists(x => x.Index == p.Index))
						ctx.Add(p);
				}
				return true;
			});

			if (placeholders.Count == 0)
			{
				// No columns to buffer — fall back to normal SetRunQuery
				sequence.SetRunQuery(query, finalized);
				return;
			}

			// Sort by index to have stable ordering
			placeholders.Sort((a, b) => a.Index!.Value.CompareTo(b.Index!.Value));

			// 2. Build TBuffer = ValueTuple<col1Type, col2Type, ...>
			var colTypes   = placeholders.Select(p => p.ConvertType).ToArray();
			var bufferType = BuildValueTupleType(colTypes);

			// 3-7: Dispatch to generic method (needs TBuffer type parameter)
			_setupPostQueryBufferMethodInfo
				.MakeGenericMethod(typeof(T), bufferType)
				.InvokeExt(this, new object[] { query, sequence, finalized, preambles, selectQuery, placeholders.ToArray(), colTypes });
		}

		static readonly MethodInfo _setupPostQueryBufferMethodInfo =
			typeof(ExpressionBuilder).GetMethod(nameof(SetupPostQueryBuffer), BindingFlags.Instance | BindingFlags.NonPublic)
			?? throw new InvalidOperationException();

		void SetupPostQueryBuffer<T, TBuffer>(
			Query<T>                    query,
			IBuildContext               sequence,
			Expression                  finalized,
			List<Preamble>              preambles,
			SelectQuery                 selectQuery,
			SqlPlaceholderExpression[]  placeholders,
			Type[]                      colTypes)
		{
			// 3. Build buffer mapper: new TBuffer(placeholder0, placeholder1, ...)
			var bufferBody  = BuildValueTupleNew(typeof(TBuffer), placeholders.Cast<Expression>().ToArray());
			var bufferMapper = BuildMapper<TBuffer>(selectQuery, bufferBody);

			// 4. Create Query<TBuffer> sharing the main SQL statement
			var bufferQuery = new Query<TBuffer>(DataContext);
			bufferQuery.Queries.Add(new QueryInfo { Statement = query.Queries[0].Statement });
			QueryRunner.SetRunQuery(bufferQuery, bufferMapper);

			// 5. Build reconstruction using a visitor that handles all custom expression types.
			var placeholderMap = new Dictionary<int, int>();
			for (var i = 0; i < placeholders.Length; i++)
				placeholderMap[placeholders[i].Index!.Value] = i;

			var bufferRowParam = Expression.Parameter(typeof(TBuffer), "bufRow");
			var preambleParam  = Expression.Parameter(typeof(object?[]), "pr");

			var visitor = new BufferReconstructionVisitor(placeholderMap, bufferRowParam, preambleParam);
			var reconstructed = visitor.Visit(finalized)!;

			if (reconstructed.Type != typeof(T))
				reconstructed = Expression.Convert(reconstructed, typeof(T));

			var reconstructionLambda = Expression.Lambda<Func<TBuffer, object?[], T>>(reconstructed, bufferRowParam, preambleParam);
			var reconstructionFunc   = reconstructionLambda.Compile();

			// 6. Build key extractors for each PostQueryKeysPreamble and replace with buffer preamble
			var keysPreambles = new List<IPostQueryKeysPreamble>();
			var firstKeyIdx   = -1;

			for (var i = 0; i < preambles.Count; i++)
			{
				if (preambles[i] is IPostQueryKeysPreamble kp)
				{
					keysPreambles.Add(kp);
					if (firstKeyIdx == -1) firstKeyIdx = i;
				}
			}

			// Build key extractors: extract key expressions from the finalized expression
			// by finding PreambleResult.GetList(keyExpr) calls for each child preamble index.
			var keyExpressions = new Dictionary<int, Expression>();
			finalized.Visit(keyExpressions, static (ctx, e) =>
			{
				if (e is MethodCallExpression { Method.Name: "GetList" } call
					&& call.Arguments.Count == 1
					&& call.Object is UnaryExpression { NodeType: ExpressionType.Convert, Operand: { } operand })
				{
					var current = operand;
					if (current is BinaryExpression { NodeType: ExpressionType.ArrayIndex, Right: ConstantExpression { Value: int idx } })
					{
						ctx[idx] = call.Arguments[0];
					}
				}
				return true;
			});

			// Verify ALL key preambles can get extractors. If not, skip buffer optimization.
			var allExtractorsFound = true;
			for (var pi = 0; pi < preambles.Count && allExtractorsFound; pi++)
			{
				if (preambles[pi] is IPostQueryKeysPreamble)
				{
					if (!keyExpressions.ContainsKey(pi + 1))
						allExtractorsFound = false;
				}
			}

			if (!allExtractorsFound)
			{
				// Can't build all key extractors — fall back to normal SetRunQuery
				sequence.SetRunQuery(query, finalized);
				return;
			}

			for (var pi = 0; pi < preambles.Count; pi++)
			{
				if (preambles[pi] is IPostQueryKeysPreamble kp)
				{
					var kpType = kp.GetType();
					if (kpType.IsGenericType && kpType.GetGenericTypeDefinition() == typeof(PostQueryKeysPreamble<>))
					{
						var tKey = kpType.GetGenericArguments()[0];

						if (keyExpressions.TryGetValue(pi + 1, out var keyExpr))
						{
							_setKeyExtractorMethodInfo
								.MakeGenericMethod(typeof(TBuffer), tKey)
								.Invoke(null, new object[] { kp, keyExpr, placeholderMap });
						}
					}
				}
			}

			// Replace first key preamble with BufferMaterializePreamble, rest become no-ops
			if (firstKeyIdx >= 0)
			{
				preambles[firstKeyIdx] = new BufferMaterializePreamble<TBuffer>(bufferQuery, keysPreambles.ToArray());
				for (var i = firstKeyIdx + 1; i < preambles.Count; i++)
				{
					if (preambles[i] is IPostQueryKeysPreamble)
						preambles[i] = NoOpPreamble.Instance;
				}
			}

			// 7. Override GetResultEnumerable to iterate buffer
			var bufferPreambleIdx = firstKeyIdx;

			query.GetResultEnumerable = (db, expr, ps, preambleResults) =>
			{
				using var _ = ActivityService.Start(ActivityID.GetIEnumerable);
				var buffer = (List<TBuffer>)preambleResults![bufferPreambleIdx]!;
				return new BufferResultEnumerable<TBuffer, T>(buffer, reconstructionFunc, preambleResults);
			};
		}

		static readonly MethodInfo _setKeyExtractorMethodInfo =
			typeof(ExpressionBuilder).GetMethod(nameof(SetKeyExtractorFromBuffer), BindingFlags.Static | BindingFlags.NonPublic)
			?? throw new InvalidOperationException();

		/// <summary>
		/// Builds and sets the BufferKeyExtractor on a PostQueryKeysPreamble.
		/// The extractor takes a buffer row (TBuffer as object) and returns TKey.
		/// </summary>
		static void SetKeyExtractorFromBuffer<TBuffer, TKey>(PostQueryKeysPreamble<TKey> keysPreamble, Expression mainKeyExpression, Dictionary<int, int> placeholderMap)
			where TKey : notnull
		{
			var bufferRowParam = Expression.Parameter(typeof(object), "row");
			// Use a Convert expression as the "buffer row" so the visitor reads tuple fields from it
			var typedRow       = Expression.Convert(bufferRowParam, typeof(TBuffer));
			var dummyPreamble  = Expression.Parameter(typeof(object?[]), "unused");

			var visitor = new BufferReconstructionVisitor(placeholderMap, typedRow, dummyPreamble);
			var keyFromBuffer = visitor.Visit(mainKeyExpression)!;

			if (keyFromBuffer.Type != typeof(TKey))
				keyFromBuffer = Expression.Convert(keyFromBuffer, typeof(TKey));

			var lambda = Expression.Lambda<Func<object, TKey>>(keyFromBuffer, bufferRowParam);
			keysPreamble.BufferKeyExtractor = lambda.Compile();
		}

		/// <summary>
		/// Visitor that transforms a finalized mapper expression into a reconstruction expression
		/// by replacing SqlPlaceholderExpressions with buffer field access and handling all
		/// other custom expression types (SqlAdjustType, SqlReaderIsNull, SqlGenericConstructor, etc.).
		/// </summary>
		sealed class BufferReconstructionVisitor : ExpressionVisitorBase
		{
			readonly Dictionary<int, int>  _placeholderMap;
			readonly Expression            _bufferRowExpr;
			readonly Expression            _preambleExpr;

			public BufferReconstructionVisitor(
				Dictionary<int, int> placeholderMap,
				Expression           bufferRowExpr,
				Expression           preambleExpr)
			{
				_placeholderMap = placeholderMap;
				_bufferRowExpr  = bufferRowExpr;
				_preambleExpr   = preambleExpr;
			}

			protected override Expression VisitParameter(ParameterExpression node)
			{
				// Replace PreambleParam with our local preamble expression
				if (ReferenceEquals(node, PreambleParam))
					return _preambleExpr;
				return base.VisitParameter(node);
			}

			public override Expression VisitSqlPlaceholderExpression(SqlPlaceholderExpression node)
			{
				if (node.Index != null && _placeholderMap.TryGetValue(node.Index.Value, out var pos))
				{
					var field = AccessValueTupleField(_bufferRowExpr, pos);
					return field.Type == node.ConvertType ? field : Expression.Convert(field, node.ConvertType);
				}
				return Expression.Default(node.ConvertType);
			}

			internal override Expression VisitSqlReaderIsNullExpression(SqlReaderIsNullExpression node)
			{
				if (node.Placeholder.Index != null && _placeholderMap.TryGetValue(node.Placeholder.Index.Value, out var pos))
				{
					var field = AccessValueTupleField(_bufferRowExpr, pos);
					if (field.Type.IsValueType && Nullable.GetUnderlyingType(field.Type) == null)
						return node.IsNot ? Expression.Constant(true) : Expression.Constant(false);
					return node.IsNot
						? (Expression)Expression.NotEqual(field, Expression.Constant(null, field.Type))
						: Expression.Equal(field, Expression.Constant(null, field.Type));
				}
				return Expression.Constant(!node.IsNot);
			}

			internal override Expression VisitSqlAdjustTypeExpression(SqlAdjustTypeExpression node)
			{
				// Visit the inner expression, then adjust type if needed
				var inner = Visit(node.Expression);
				if (inner.Type == node.Type)
					return inner;
				// Use soft type adjustment — don't convert incompatible types
				if (node.Type.IsAssignableFrom(inner.Type))
					return inner;
				if (inner.Type.IsAssignableFrom(node.Type))
					return Expression.Convert(inner, node.Type);
				// Return inner as-is if types are incompatible
				return inner;
			}

			internal override Expression VisitContextRefExpression(ContextRefExpression node)
			{
				// Should not appear in finalized expression — return default
				return Expression.Default(node.Type);
			}

			internal override Expression VisitSqlErrorExpression(SqlErrorExpression node)
			{
				return Expression.Default(node.Type);
			}

			internal override Expression VisitConvertFromDataReaderExpression(ConvertFromDataReaderExpression node)
			{
				// Should not appear before ToReadExpression — but handle just in case
				if (_placeholderMap.TryGetValue(node.Index, out var pos))
				{
					var field = AccessValueTupleField(_bufferRowExpr, pos);
					return field.Type == node.Type ? field : Expression.Convert(field, node.Type);
				}
				return Expression.Default(node.Type);
			}

			public override Expression VisitSqlGenericConstructorExpression(SqlGenericConstructorExpression node)
			{
				// This should have been resolved by FinalizeConstructors before we get here.
				// Visit children to resolve any nested placeholders.
				return base.VisitSqlGenericConstructorExpression(node);
			}

			internal override Expression VisitSqlGenericParamAccessExpression(SqlGenericParamAccessExpression node)
			{
				return Expression.Default(node.Type);
			}

			internal override Expression VisitSqlPathExpression(SqlPathExpression node)
			{
				return Expression.Default(node.Type);
			}

			public override Expression VisitSqlDefaultIfEmptyExpression(SqlDefaultIfEmptyExpression node)
			{
				return Visit(node.InnerExpression);
			}

			public override Expression VisitSqlValidateExpression(SqlValidateExpression node)
			{
				return Visit(node.InnerExpression);
			}

			public override Expression VisitChangeTypeExpression(ChangeTypeExpression node)
			{
				var inner = Visit(node.Expression);
				if (inner.Type == node.Type)
					return inner;
				return Expression.Convert(inner, node.Type);
			}

			public override Expression VisitDefaultValueExpression(DefaultValueExpression node)
			{
				return Expression.Default(node.Type);
			}

			public override Expression VisitMarkerExpression(MarkerExpression node)
			{
				return Visit(node.InnerExpression);
			}

			public override Expression VisitTagExpression(TagExpression node)
			{
				return Visit(node.InnerExpression);
			}

			public override Expression VisitSqlQueryRootExpression(SqlQueryRootExpression node)
			{
				return Expression.Default(node.Type);
			}

			public override Expression VisitConstantPlaceholder(ConstantPlaceholderExpression node)
			{
				return Expression.Default(node.Type);
			}

			internal override Expression VisitSqlEagerLoadExpression(SqlEagerLoadExpression node)
			{
				// Should have been resolved by CompleteEagerLoadingExpressions
				return Expression.Default(node.Type);
			}
		}

		sealed class NoOpPreamble : Preamble
		{
			public static readonly NoOpPreamble Instance = new();
			public override object Execute(IDataContext dc, IQueryExpressions expr, object?[]? ps, object?[]? preambles) => null!;
			public override Task<object> ExecuteAsync(IDataContext dc, IQueryExpressions expr, object?[]? ps, object[]? preambles, CancellationToken ct) => Task.FromResult<object>(null!);
			public override void GetUsedParametersAndValues(ICollection<SqlParameter> parameters, ICollection<SqlValue> values) { }
		}

		sealed class BufferMaterializePreamble<TBuffer> : Preamble
		{
			readonly Query<TBuffer>           _bufferQuery;
			readonly IPostQueryKeysPreamble[]  _keysPreambles;

			public BufferMaterializePreamble(Query<TBuffer> bufferQuery, IPostQueryKeysPreamble[] keysPreambles)
			{
				_bufferQuery   = bufferQuery;
				_keysPreambles = keysPreambles;
			}

			public override object Execute(IDataContext dataContext, IQueryExpressions expressions, object?[]? parameters, object?[]? preambles)
			{
				var buffer = _bufferQuery.GetResultEnumerable(dataContext, expressions, parameters, preambles).ToList();
				var ilist  = (IList)buffer;
				foreach (var kp in _keysPreambles)
					kp.SetKeysFromBuffer(ilist);
				return buffer;
			}

			public override async Task<object> ExecuteAsync(IDataContext dataContext, IQueryExpressions expressions, object?[]? parameters, object[]? preambles, CancellationToken cancellationToken)
			{
				var buffer = await _bufferQuery.GetResultEnumerable(dataContext, expressions, parameters, preambles)
					.ToListAsync(cancellationToken).ConfigureAwait(false);
				var ilist = (IList)buffer;
				foreach (var kp in _keysPreambles)
					kp.SetKeysFromBuffer(ilist);
				return buffer;
			}

			public override void GetUsedParametersAndValues(ICollection<SqlParameter> parameters, ICollection<SqlValue> values)
			{
				foreach (var q in _bufferQuery.Queries)
					QueryHelper.CollectParametersAndValues(q.Statement, parameters, values);
			}
		}

		sealed class BufferResultEnumerable<TBuffer, T> : IResultEnumerable<T>
		{
			readonly List<TBuffer>             _buffer;
			readonly Func<TBuffer, object?[], T> _reconstruct;
			readonly object?[]?                _preambles;

			public BufferResultEnumerable(List<TBuffer> buffer, Func<TBuffer, object?[], T> reconstruct, object?[]? preambles)
			{
				_buffer      = buffer;
				_reconstruct = reconstruct;
				_preambles   = preambles;
			}

			public IEnumerator<T> GetEnumerator()
			{
				var preambles = _preambles ?? Array.Empty<object>();
				foreach (var row in _buffer)
					yield return _reconstruct(row, preambles!);
			}

			IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

			public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
			{
				return new SyncToAsyncEnumerator(GetEnumerator());
			}

			sealed class SyncToAsyncEnumerator : IAsyncEnumerator<T>
			{
				readonly IEnumerator<T> _inner;
				public SyncToAsyncEnumerator(IEnumerator<T> inner) => _inner = inner;
				public T Current => _inner.Current;
				public ValueTask<bool> MoveNextAsync() => new(_inner.MoveNext());
				public ValueTask DisposeAsync() { _inner.Dispose(); return default; }
			}
		}

		#endregion

		/// <summary>
		/// Resolves the effective <see cref="EagerLoadingStrategy"/> for a given eager-load node,
		/// taking into account the per-association override and the global default from <see cref="LinqOptions"/>.
		/// Falls back from <see cref="EagerLoadingStrategy.CteUnion"/> to <see cref="EagerLoadingStrategy.PostQuery"/>
		/// when the provider does not support CTEs.
		/// </summary>
		EagerLoadingStrategy ResolveStrategy(SqlEagerLoadExpression eagerLoad)
		{
			var strategy = eagerLoad.Strategy != EagerLoadingStrategy.Default
				? eagerLoad.Strategy
				: DataContext.Options.LinqOptions.DefaultEagerLoadingStrategy;

			if (strategy == EagerLoadingStrategy.CteUnion && !DataContext.SqlProviderFlags.IsCommonTableExpressionsSupported)
				strategy = EagerLoadingStrategy.PostQuery;

			return strategy;
		}

		Expression CompleteEagerLoadingExpressions(
			Expression          expression,
			IBuildContext       buildContext,
			ParameterExpression queryParameter,
			ref List<Preamble>? preambles,
			Expression[]        previousKeys)
		{
			Dictionary<Expression, Expression>? eagerLoadingCache = null;

			var preamblesLocal = preambles;

			var updatedEagerLoading = expression.Transform(e =>
			{
				if (e.NodeType == ExpressionType.Extension && e is SqlEagerLoadExpression eagerLoad)
				{
					// Do not process eager loading fast mode
					if (!ValidateSubqueries)
						return SqlErrorExpression.EnsureError(eagerLoad.SequenceExpression, e.Type);

					eagerLoadingCache ??= new Dictionary<Expression, Expression>(ExpressionEqualityComparer.Instance);
					if (!eagerLoadingCache.TryGetValue(eagerLoad.SequenceExpression, out var preambleExpression))
					{
						preamblesLocal ??= [];

						var strategy = ResolveStrategy(eagerLoad);

						if (strategy == EagerLoadingStrategy.PostQuery)
						{
							preambleExpression = ProcessEagerLoadingPostQuery(
								buildContext, eagerLoad, queryParameter, preamblesLocal, previousKeys);
						}
						else
						{
							preambleExpression = ProcessEagerLoadingExpression(
								buildContext, eagerLoad, queryParameter, preamblesLocal, previousKeys);
						}

						eagerLoadingCache.Add(eagerLoad.SequenceExpression, preambleExpression);
					}

					return preambleExpression;
				}

				return e;
			});

			preambles = preamblesLocal;

			return updatedEagerLoading;
		}

		sealed class DatachedPreamble<T> : Preamble
		{
			readonly Query<T> _query;

			public DatachedPreamble(Query<T> query)
			{
				_query = query;
			}

			public override object Execute(IDataContext dataContext, IQueryExpressions expressions, object?[]? parameters, object?[]? preambles)
			{
				return _query.GetResultEnumerable(dataContext, expressions, preambles, preambles).ToList();
			}

			public override async Task<object> ExecuteAsync(IDataContext dataContext, IQueryExpressions expressions, object?[]? parameters, object[]? preambles, CancellationToken cancellationToken)
			{
				return await _query.GetResultEnumerable(dataContext, expressions, preambles, preambles).ToListAsync(cancellationToken).ConfigureAwait(false);
			}

			public override void GetUsedParametersAndValues(ICollection<SqlParameter> parameters, ICollection<SqlValue> values)
			{
				foreach (var query in _query.Queries)
				{
					QueryHelper.CollectParametersAndValues(query.Statement, parameters, values);
				}
			}
		}

		sealed class Preamble<TKey, T> : Preamble
			where TKey : notnull
		{
			readonly Query<KeyDetailEnvelope<TKey, T>> _query;

			public Preamble(Query<KeyDetailEnvelope<TKey, T>> query)
			{
				_query = query;
			}

			public override object Execute(IDataContext dataContext, IQueryExpressions expressions, object?[]? parameters, object?[]? preambles)
			{
				var result = new PreambleResult<TKey, T>();
				foreach (var e in _query.GetResultEnumerable(dataContext, expressions, preambles, preambles))
				{
					result.Add(e.Key, e.Detail);
				}

				return result;
			}

			public override async Task<object> ExecuteAsync(IDataContext dataContext, IQueryExpressions expressions, object?[]? parameters, object[]? preambles,
				CancellationToken                                        cancellationToken)
			{
				var result = new PreambleResult<TKey, T>();

				var enumerator = _query.GetResultEnumerable(dataContext, expressions, preambles, preambles)
					.GetAsyncEnumerator(cancellationToken);

				while (await enumerator.MoveNextAsync().ConfigureAwait(false))
				{
					var e = enumerator.Current;
					result.Add(e.Key, e.Detail);
				}

				return result;
			}

			public override void GetUsedParametersAndValues(ICollection<SqlParameter> parameters, ICollection<SqlValue> values)
			{
				foreach (var query in _query.Queries)
				{
					QueryHelper.CollectParametersAndValues(query.Statement, parameters, values);
				}
			}
		}

		sealed class PreambleResult<TKey, T>
			where TKey : notnull
		{
			Dictionary<TKey, List<T>>? _items;
			TKey                       _prevKey = default!;
			List<T>?                   _prevList;

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
						_items = new Dictionary<TKey, List<T>>(ValueComparer.GetDefaultValueComparer<TKey>(favorStructuralComparisons: true));
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
