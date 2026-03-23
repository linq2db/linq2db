using System;
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
		/// PostQuery strategy: loads child records by joining a local key collection
		/// (extracted via a key-extraction preamble) to the child table via SelectMany.
		/// The local key collection is provided at runtime through a <see cref="PostQueryKeysHolder{TKey}"/>.
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
				// Replace references to cloned parent-side keys in correctedSequence
				// with references to a local key parameter. This transforms:
				//   childTable.Where(c => c.FK == parentKey && c.Active)
				// into:
				//   childTable.Where(c => c.FK == localKey && c.Active)
				// Then wrap in SelectMany with the local key collection.

				var correctedSequenceWithLocalKey = ReplaceDetailKeysWithParameter(
					correctedSequence, detailKeys, keyParameter);

				// Build KeyDetailEnvelope selector: (k, d) => new KeyDetailEnvelope(k, d)
				var keyDetailExpression = Expression.New(
					keyDetailType.GetConstructor([keyType, detailType])!,
					keyParameter,
					detailParameter);
				var selector = Expression.Lambda(keyDetailExpression, keyParameter, detailParameter);

				// Build detail selector: k => correctedSequenceWithLocalKey
				var detailSelector = _buildSelectManyDetailSelectorInfo
					.MakeGenericMethod(keyType, detailType)
					.InvokeExt<LambdaExpression>(null, new object[] { correctedSequenceWithLocalKey, keyParameter });

				// Source: local key collection from PostQueryKeysHolder, wrapped as IQueryable
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

				// --- Build key extraction query: SELECT DISTINCT key FROM parent ---
				var clonedParentContextRef = new ContextRefExpression(
					typeof(IQueryable<>).MakeGenericType(clonedParentContext.ElementType), clonedParentContext);

				Expression keyExtractionQuery = clonedParentContextRef;
				if (!typeof(IQueryable<>).IsSameOrParentOf(keyExtractionQuery.Type))
					keyExtractionQuery = Expression.Call(Methods.Queryable.AsQueryable.MakeGenericMethod(mainType), keyExtractionQuery);

				var mainParameter = Expression.Parameter(mainType, "m");
				var keySelector   = Expression.Lambda(detailKeyExpression, mainParameter);

				keyExtractionQuery = Expression.Call(
					Methods.Queryable.Select.MakeGenericMethod(mainType, keyType),
					keyExtractionQuery,
					Expression.Quote(keySelector));

				keyExtractionQuery = Expression.Call(Methods.LinqToDB.SelectDistinct.MakeGenericMethod(keyType), keyExtractionQuery);

				var saveVisitor = _buildVisitor;
				_buildVisitor = _buildVisitor.Clone(cloningContext);

				cloningContext.UpdateContextParents();

				// Build key extraction sequence
				var keyExtractionSequence = BuildSequence(new BuildInfo((IBuildContext?)null, keyExtractionQuery,
					clonedParentContextRef.BuildContext.SelectQuery));

				// Build child query sequence
				var detailSequence = BuildSequence(new BuildInfo((IBuildContext?)null, selectManyCall,
					new SelectQuery()));

				var parameters = new object?[] { detailSequence, mainKeyExpression, queryParameter, preambles, orderByToApply, detailKeys, holderAndSourceExpr.holder, keyExtractionSequence };

				resultExpression = _buildPostQueryPreambleAttachedMethodInfo
					.MakeGenericMethod(keyType, detailType)
					.InvokeExt<Expression>(this, parameters);

				_buildVisitor = saveVisitor;
			}

			if (resultExpression is SqlErrorExpression errorExpression)
				return errorExpression.WithType(eagerLoad.Type);

			resultExpression = SqlAdjustTypeExpression.AdjustType(resultExpression, eagerLoad.Type, MappingSchema);
			return resultExpression;
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

			var keyPreamble = new PostQueryKeysPreamble<TKey>(keyQuery, holder);
			preambles.Add(keyPreamble);

			// --- Step 2: Build child query preamble ---
			var childQuery = new Query<KeyDetailEnvelope<TKey, T>>(DataContext);
			childQuery.Init(childSequence);
			childQuery.SetParametersAccessors(_parametersContext.CurrentSqlParameters.ToList());

			if (!BuildQuery(childQuery, childSequence, queryParameter, ref preambles!, previousKeys))
				return childQuery.ErrorExpression!;

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

		sealed class PostQueryKeysPreamble<TKey> : Preamble
			where TKey : notnull
		{
			readonly Query<TKey>               _query;
			readonly PostQueryKeysHolder<TKey>  _holder;

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
