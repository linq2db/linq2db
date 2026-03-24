using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Common;
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
		/// <summary>Set by ProcessEagerLoadingPostQuery to signal BuildQuery that buffer materialization is needed.</summary>
		bool _hasPostQueryPreambles;

		/// <summary>Tracks PostQuery nesting depth. Buffer materialization only at depth 0 (outermost level).</summary>
		int _postQueryNestingDepth;

		/// <summary>Marker interface for PostQueryKeysPreamble, used during buffer setup.</summary>
		interface IPostQueryKeysPreamble
		{
			/// <summary>Extract distinct keys from the buffer and populate the holder.</summary>
			void SetKeysFromBuffer(IList buffer);

			/// <summary>Index of the corresponding PostQueryChildPreamble in the preambles list.</summary>
			int ChildPreambleIndex { get; set; }
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

			// Detect projection-only parent references (e.g., c.Name in child Select projection).
			// These are dependencies not used in filter predicates — they make the composite key
			// unnecessarily wide and cause issues on some providers.
			// Fall back to Default strategy for this child if detected.
			if (dependencies.Count > 0)
			{
				var filterDependencies = new HashSet<Expression>(ExpressionEqualityComparer.Instance);
				CollectFilterDependencies(buildContext, sequenceExpression, eagerLoad.Predicate, filterDependencies);
				filterDependencies.AddRange(previousKeys);

				if (dependencies.Count + previousKeys.Length > filterDependencies.Count)
				{
					return ProcessEagerLoadingExpression(
						buildContext, eagerLoad, queryParameter, preambles, previousKeys);
				}
			}

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

				// For single-key associations, use the bare type instead of ValueTuple<T> wrapping.
				// This avoids ITuple.Length appearing as a spurious column in VALUES tables.
				Expression mainKeyExpression;
				Expression detailKeyExpression;

				if (mainKeys.Length == 1)
				{
					mainKeyExpression   = mainKeys[0];
					detailKeyExpression = detailKeys[0];
				}
				else
				{
					mainKeyExpression   = GenerateKeyExpression(mainKeys, 0);
					detailKeyExpression = GenerateKeyExpression(detailKeys, 0);
				}

				var keyType          = mainKeyExpression.Type;
				var keyDetailType    = typeof(KeyDetailEnvelope<,>).MakeGenericType(keyType, detailType);
				var keyParameter     = Expression.Parameter(keyType, "k");
				var detailParameter  = Expression.Parameter(detailType, "d");

				// Source: local key collection from PostQueryKeysHolder
				var (holder, sourceExpr) = _buildPostQueryKeysSourceMethodInfo
					.MakeGenericMethod(keyType)
					.InvokeExt<(object holder, Expression sourceExpr)>(null, Array.Empty<object>());

				Expression childQueryCall = null!;

				// Determine if we can use Contains optimization (single key with FK found as MemberExpression).
				var canUseContains = false;
				Expression? childFkExpr = null;
				var fkIsInProjection = false;

				if (mainKeys.Length == 1)
				{
					childFkExpr = FindChildFkExpression(correctedSequence, detailKeys[0]);

					if (childFkExpr is MemberExpression fkMember)
					{
						canUseContains = true;

						// Check whether the FK member is accessible from the final element type (detailType).
						// When a Select projection strips the FK (e.g., .Select(d => new { d.Id, ... })),
						// we handle it by modifying the terminal Select to wrap in KeyDetailEnvelope.
						var declaringType = fkMember.Expression?.Type;
						fkIsInProjection = declaringType == detailType
							|| detailType.GetProperty(fkMember.Member.Name) != null
							|| detailType.GetField(fkMember.Member.Name) != null;
					}
				}

				if (canUseContains)
				{
					// --- Single key: use Contains → WHERE FK IN (...) ---
					// Replace the equality (childFK == parentKey) with keys.Contains(childFK),
					// then project to KeyDetailEnvelope using the child FK as the grouping key.
					var containsMethod = Methods.Enumerable.Contains.MakeGenericMethod(keyType);

					// Replace the equality with Contains
					var capturedFk = childFkExpr!;
					var modifiedSequence = correctedSequence.Transform(
						(detailKey: detailKeys[0], containsMethod, sourceExpr, capturedFk),
						static (ctx, e) =>
						{
							if (e is BinaryExpression { NodeType: ExpressionType.Equal } binary)
							{
								if (ExpressionEqualityComparer.Instance.Equals(binary.Right, ctx.detailKey))
									return Expression.Call(ctx.containsMethod, ctx.sourceExpr, ctx.capturedFk);

								if (ExpressionEqualityComparer.Instance.Equals(binary.Left, ctx.detailKey))
									return Expression.Call(ctx.containsMethod, ctx.sourceExpr, ctx.capturedFk);
							}

							return e;
						});

					if (fkIsInProjection)
					{
						// FK is in the final projected type — append Select(d => new KeyDetailEnvelope(d.FK, d))
						var selectParam = Expression.Parameter(detailType, "d_sel");
						Expression selectFk;

						if (childFkExpr is MemberExpression { Expression: ParameterExpression } me)
						{
							selectFk = Expression.MakeMemberAccess(selectParam, me.Member);
						}
						else
						{
							selectFk = childFkExpr!.Transform(
								(detailType, selectParam),
								static (ctx, e) => e is ParameterExpression pe && pe.Type == ctx.detailType
									? ctx.selectParam
									: e);
						}

						if (selectFk.Type != keyType)
							selectFk = Expression.Convert(selectFk, keyType);

						var envelopeCtor = keyDetailType.GetConstructor([keyType, detailType])!;
						var envelopeNew  = Expression.New(envelopeCtor, selectFk, selectParam);
						var selectLambda = Expression.Lambda(envelopeNew, selectParam);

						// Use Queryable.Select or Enumerable.Select depending on the source type
						// (navigation properties produce IEnumerable, table queries produce IQueryable).
						if (typeof(IQueryable).IsAssignableFrom(modifiedSequence.Type))
						{
							childQueryCall = Expression.Call(
								Methods.Queryable.Select.MakeGenericMethod(detailType, keyDetailType),
								modifiedSequence,
								Expression.Quote(selectLambda));
						}
						else
						{
							childQueryCall = Expression.Call(
								Methods.Enumerable.Select.MakeGenericMethod(detailType, keyDetailType),
								modifiedSequence,
								selectLambda);
						}
					}
					else
					{
						// FK was stripped by a terminal Select projection.
						// Modify that Select to wrap its body in KeyDetailEnvelope, including FK from the entity parameter.
						var (terminalSelect, _) = UnwrapOrderingToSelect(modifiedSequence);

						if (terminalSelect != null)
						{
							childQueryCall = WrapTerminalSelectWithEnvelope(
								modifiedSequence, (MemberExpression)childFkExpr!, keyType, detailType, keyDetailType);
						}
						else
						{
							// No terminal Select found (e.g., FK is inside a subquery like Any()).
							// Fall back to SelectMany + VALUES JOIN.
							canUseContains = false;
						}
					}
				}

				if (!canUseContains)
				{
					// --- Composite key or inaccessible FK: SelectMany + VALUES JOIN ---
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

					Expression sourceQuery = Expression.Call(
						Methods.Queryable.AsQueryable.MakeGenericMethod(keyType),
						sourceExpr);

					childQueryCall = Expression.Call(
						Methods.Queryable.SelectManyProjection.MakeGenericMethod(keyType, detailType, keyDetailType),
						sourceQuery, Expression.Quote(detailSelector), Expression.Quote(selector));
				}

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

				var keyDetailKeyExpression = keyDetailKeys.Length == 1
					? keyDetailKeys[0]
					: GenerateKeyExpression(keyDetailKeys, 0);

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

				var detailSequence = BuildSequence(new BuildInfo((IBuildContext?)null, childQueryCall,
					new SelectQuery()));

				var parameters = new object?[] { detailSequence, mainKeyExpression, queryParameter, preambles, orderByToApply, detailKeys, holder, keyExtractionSequence };

				resultExpression = _buildPostQueryPreambleAttachedMethodInfo
					.MakeGenericMethod(keyType, detailType)
					.InvokeExt<Expression>(this, parameters);
			}

			if (resultExpression is SqlErrorExpression errorExpression)
				return errorExpression.WithType(eagerLoad.Type);

			resultExpression = SqlAdjustTypeExpression.AdjustType(resultExpression, eagerLoad.Type, MappingSchema);
			return resultExpression;
		}

		/// <summary>
		/// Collects parent dependencies used only in filter predicates (Where lambdas and association predicate),
		/// excluding projection-only references. Used to detect when child projections reference non-key parent
		/// fields (e.g., <c>c.Name</c> in a child Select), which requires fallback to Default strategy.
		/// </summary>
		void CollectFilterDependencies(
			IBuildContext   context,
			Expression      sequenceExpression,
			Expression?     predicate,
			HashSet<Expression> filterDependencies)
		{
			// Collect from association predicate
			if (predicate != null)
				CollectDependencies(context, predicate, filterDependencies);

			// Walk the method call chain and collect dependencies from Where lambda bodies only
			var current = sequenceExpression;
			while (current is MethodCallExpression mce)
			{
				if (mce.Method.Name is nameof(Enumerable.Where) && mce.Arguments.Count >= 2)
				{
					var whereLambda = mce.Arguments[1].UnwrapLambda();
					if (whereLambda != null)
						CollectDependencies(context, whereLambda.Body, filterDependencies);
				}

				current = mce.Arguments[0];
			}
		}

		/// <summary>
		/// Finds the child FK expression from a pure equality like <c>childFK == parentKey</c>
		/// by matching the parentKey side against <paramref name="detailKey"/>.
		/// Does NOT descend into OrElse/Or nodes — Contains optimization only works when the
		/// equality is the sole/AND-conjunction predicate connecting parent and child.
		/// </summary>
		static Expression? FindChildFkExpression(Expression sequence, Expression detailKey)
		{
			Expression? result = null;

			sequence.Visit(e =>
			{
				if (result != null)
					return false;

				// Do not look for equalities inside OR predicates.
				// With OR, a single Contains cannot correctly group results by parent key.
				if (e is BinaryExpression { NodeType: ExpressionType.OrElse or ExpressionType.Or })
					return false;

				if (e is BinaryExpression { NodeType: ExpressionType.Equal } binary)
				{
					if (ExpressionEqualityComparer.Instance.Equals(binary.Right, detailKey))
					{
						result = binary.Left;
						return false;
					}

					if (ExpressionEqualityComparer.Instance.Equals(binary.Left, detailKey))
					{
						result = binary.Right;
						return false;
					}
				}

				return true;
			});

			return result;
		}

		/// <summary>
		/// When the FK is not in the final projected type (stripped by a terminal Select),
		/// modifies that Select to wrap its body in <c>KeyDetailEnvelope</c> using the FK
		/// from the pre-projection entity parameter.
		/// </summary>
		/// <example>
		/// Input:  source.Where(...).Select(d => new { d.Id, d.Name })
		/// Output: source.Where(...).Select(d => new KeyDetailEnvelope(d.CompanyId, new { d.Id, d.Name }))
		/// </example>
		static Expression WrapTerminalSelectWithEnvelope(
			Expression       modifiedSequence,
			MemberExpression childFkMember,
			Type             keyType,
			Type             detailType,
			Type             keyDetailType)
		{
			// Walk past OrderBy/ThenBy/ThenByDescending to find the terminal Select.
			// Caller must ensure terminal Select exists (checked via UnwrapOrderingToSelect beforehand).
			var (terminalSelect, outerChain) = UnwrapOrderingToSelect(modifiedSequence);

			var selectLambda = terminalSelect!.Arguments[1].UnwrapLambda()!;

			// selectLambda: d => new { d.Id, d.Name, ... }
			// selectLambda.Parameters[0] is the pre-projection entity parameter
			var entityParam = selectLambda.Parameters[0];

			// Build FK access: entityParam.FK (e.g., d.CompanyId)
			Expression fkAccess = Expression.MakeMemberAccess(entityParam, childFkMember.Member);
			if (fkAccess.Type != keyType)
				fkAccess = Expression.Convert(fkAccess, keyType);

			// Build: new KeyDetailEnvelope(d.CompanyId, <original body producing detailType>)
			var envelopeCtor = keyDetailType.GetConstructor([keyType, detailType])!;
			var envelopeBody = Expression.New(envelopeCtor, fkAccess, selectLambda.Body);

			// New lambda: d => new KeyDetailEnvelope(d.CompanyId, new { d.Id, d.Name, ... })
			var newLambda = Expression.Lambda(envelopeBody, entityParam);

			// The source of the original Select (everything before it)
			var selectSource = terminalSelect.Arguments[0];
			var entityType   = entityParam.Type;

			// Build new Select call producing KeyDetailEnvelope instead of detailType.
			// Reuse the original Select's generic definition (Queryable.Select or Enumerable.Select)
			// to handle both IQueryable and IEnumerable sources (e.g., navigation properties).
			var selectGenericDef  = terminalSelect.Method.GetGenericMethodDefinition();
			var isQueryableSelect = terminalSelect.Method.DeclaringType == typeof(Queryable);

			Expression newSelect = Expression.Call(
				selectGenericDef.MakeGenericMethod(entityType, keyDetailType),
				selectSource,
				isQueryableSelect ? Expression.Quote(newLambda) : newLambda);

			// Re-apply any OrderBy/ThenBy chain that was on top of the Select
			foreach (var (method, lambda) in outerChain)
			{
				// Ordering methods have signature: OrderBy<TSource, TKey>(source, keySelector)
				// The TSource was detailType, now it's keyDetailType — we need to rebuild the key selector
				// to access the Detail property of KeyDetailEnvelope.
				var origParam = lambda.Parameters[0]; // was of type detailType
				var envelopeParam = Expression.Parameter(keyDetailType, origParam.Name);
				var detailAccess = Expression.Property(envelopeParam, nameof(KeyDetailEnvelope<int, int>.Detail));

				// Replace original parameter with envelope.Detail access
				var newKeyBody = lambda.Body.Transform(
					(origParam, detailAccess),
					static (ctx, e) => e == ctx.origParam ? ctx.detailAccess : e);

				var newKeyLambda = Expression.Lambda(newKeyBody, envelopeParam);

				// Rebuild the ordering call with the new source type.
				// Preserve original declaring type (Queryable vs Enumerable).
				var isQueryableOrdering = method.DeclaringType == typeof(Queryable);
				var genericMethod = method.GetGenericMethodDefinition()
					.MakeGenericMethod(keyDetailType, lambda.Body.Type);

				newSelect = Expression.Call(
					genericMethod,
					newSelect,
					isQueryableOrdering ? Expression.Quote(newKeyLambda) : newKeyLambda);
			}

			return newSelect;
		}

		/// <summary>
		/// Unwraps OrderBy/ThenBy/OrderByDescending/ThenByDescending calls to find the underlying Select call.
		/// Returns the Select MethodCallExpression and the chain of ordering operations to re-apply.
		/// </summary>
		static (MethodCallExpression? select, List<(MethodInfo method, LambdaExpression lambda)> orderingChain) UnwrapOrderingToSelect(
			Expression expression)
		{
			var chain = new List<(MethodInfo, LambdaExpression)>();

			var current = expression;
			while (current is MethodCallExpression mce)
			{
				if (mce.IsOrderByMethodName)
				{
					chain.Add((mce.Method, mce.Arguments[1].UnwrapLambda()!));
					current = mce.Arguments[0];
				}
				else if (mce.Method.Name is nameof(Enumerable.Select))
				{
					chain.Reverse(); // restore original order (innermost first → outermost first)
					return (mce, chain);
				}
				else
				{
					break;
				}
			}

			return (null, chain);
		}

		static Expression ReplaceDetailKeysWithParameter(
			Expression           expression,
			Expression[]         detailKeys,
			ParameterExpression  keyParameter)
		{
			var replacements = new Dictionary<Expression, Expression>(ExpressionEqualityComparer.Instance);
			for (var idx = 0; idx < detailKeys.Length; idx++)
			{
				// For single keys, keyParameter is the bare value; for composite, access ValueTuple fields.
				Expression keyAccess = detailKeys.Length == 1
					? keyParameter
					: AccessValueTupleField(keyParameter, idx);

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
			var keysExpr   = Expression.Property(holderExpr, nameof(PostQueryKeysHolder<>.Keys));

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

			// --- Step 2: Build child query preamble ---
			// Increment nesting depth to prevent buffer materialization at inner levels.
			// Inner-level SQL contains VALUES tables whose source expressions get parametrized
			// during finalization, breaking buffer query execution at runtime. Only the
			// outermost level (depth == 0) triggers buffer materialization in BuildQuery.
			// TODO: eliminate inner SELECT DISTINCT by fixing VALUES source preservation.
			_postQueryNestingDepth++;

			var childQuery = new Query<KeyDetailEnvelope<TKey, T>>(DataContext);
			childQuery.Init(childSequence);
			childQuery.SetParametersAccessors(_parametersContext.CurrentSqlParameters.ToList());

			if (!BuildQuery(childQuery, childSequence, queryParameter, ref preambles!, Array.Empty<Expression>()))
			{
				_postQueryNestingDepth--;
				return childQuery.ErrorExpression!;
			}

			_postQueryNestingDepth--;

			// Signal the CURRENT level's BuildQuery to set up buffer materialization
			_hasPostQueryPreambles = true;

			var idx          = preambles.Count;
			var childPreamble = new PostQueryChildPreamble<TKey, T>(childQuery, holder);
			preambles.Add(childPreamble);

			// Record child preamble index on the key preamble for buffer setup matching
			keyPreamble.ChildPreambleIndex = idx;

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

			/// <inheritdoc />
			public int ChildPreambleIndex { get; set; }

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

					// Skip child query when there are no parent keys — no rows to load.
					if (_holder.Keys is not { Length: > 0 })
						return result;

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

					// Skip child query when there are no parent keys — no rows to load.
					if (_holder.Keys is not { Length: > 0 })
						return result;

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
		void SetRunQueryWithPostQueryBuffer<T>(Query<T> query, IBuildContext sequence, Expression finalized, List<Preamble> preambles, int preambleStartIndex = 0)
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
				.InvokeExt(this, new object[] { query, sequence, finalized, preambles, selectQuery, placeholders.ToArray(), colTypes, preambleStartIndex });
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
			Type[]                      colTypes,
			int                         preambleStartIndex)
		{
			// 3. Build buffer mapper: new TBuffer(placeholder0, placeholder1, ...)
			var bufferBody  = BuildValueTupleNew(typeof(TBuffer), placeholders.Cast<Expression>().ToArray());
			var bufferMapper = BuildMapper<TBuffer>(selectQuery, bufferBody);

			// 4. Create Query<TBuffer> sharing the main SQL statement and parameter accessors.
			// Mark as parameter-dependent and continuous-run because the statement may contain
			// VALUES tables whose source is a SqlParameter (e.g., PostQueryKeysHolder.Keys).
			// Without this, the optimizer passes null ParameterValues to EvaluationContext,
			// making the SqlParameter unevaluable at SQL generation time.
			var bufferQuery = new Query<TBuffer>(DataContext);
			var bufferStatement = query.Queries[0].Statement;
			bufferStatement.IsParameterDependent = true;
			bufferQuery.Queries.Add(new QueryInfo { Statement = bufferStatement, IsContinuousRun = true });
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
			var reconstructionFunc   = reconstructionLambda.CompileExpression();

			// 6. Build key extractors for each PostQueryKeysPreamble and replace with buffer preamble
			// Only process preambles at this BuildQuery level (from preambleStartIndex onward).
			var keysPreambles = new List<IPostQueryKeysPreamble>();
			var firstKeyIdx   = -1;

			for (var i = preambleStartIndex; i < preambles.Count; i++)
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
				if (e is MethodCallExpression { Method.Name: nameof(PreambleResult<int, int>.GetList) } call
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

			// Verify ALL key preambles at this level can get extractors. If not, skip buffer optimization.
			// Use ChildPreambleIndex to find the corresponding GetList call (not pi + 1, since inner
			// preambles may be inserted between the key preamble and its child preamble).
			var allExtractorsFound = true;
			for (var ki = 0; ki < keysPreambles.Count && allExtractorsFound; ki++)
			{
				if (!keyExpressions.ContainsKey(keysPreambles[ki].ChildPreambleIndex))
					allExtractorsFound = false;
			}

			if (!allExtractorsFound)
			{
				// Can't build all key extractors — fall back to normal SetRunQuery
				sequence.SetRunQuery(query, finalized);
				return;
			}

			for (var ki = 0; ki < keysPreambles.Count; ki++)
			{
				var kp     = keysPreambles[ki];
				var kpType = kp.GetType();
				if (kpType.IsGenericType && kpType.GetGenericTypeDefinition() == typeof(PostQueryKeysPreamble<>))
				{
					var tKey = kpType.GetGenericArguments()[0];

					if (keyExpressions.TryGetValue(kp.ChildPreambleIndex, out var keyExpr))
					{
						_setKeyExtractorMethodInfo
							.MakeGenericMethod(typeof(TBuffer), tKey)
							.InvokeExt(null, new object[] { kp, keyExpr, placeholderMap });
					}
				}
			}

			// Replace first key preamble with BufferMaterializePreamble, rest become no-ops
			if (firstKeyIdx >= 0)
			{
				preambles[firstKeyIdx] = new BufferMaterializePreamble<TBuffer>(bufferQuery, query, keysPreambles.ToArray());
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

			// 8. Override GetElement/GetElementAsync for FirstOrDefault/Single etc.
			query.GetElement = (db, expr, ps, preambleResults) =>
			{
				var buffer = (List<TBuffer>)preambleResults![bufferPreambleIdx]!;
				if (buffer.Count == 0)
					return default(T);

				return reconstructionFunc(buffer[0], preambleResults!);
			};

			query.GetElementAsync = (db, expr, ps, preambleResults, token) =>
			{
				var buffer = (List<TBuffer>)preambleResults![bufferPreambleIdx]!;
				if (buffer.Count == 0)
					return Task.FromResult<object?>(default(T));

				return Task.FromResult<object?>(reconstructionFunc(buffer[0], preambleResults!));
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
			keysPreamble.BufferKeyExtractor = lambda.CompileExpression();
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
			public override object Execute(IDataContext dataContext, IQueryExpressions expressions, object?[]? parameters, object?[]? preambles) => null!;
			public override Task<object> ExecuteAsync(IDataContext dataContext, IQueryExpressions expressions, object?[]? parameters, object[]? preambles, CancellationToken cancellationToken) => Task.FromResult<object>(null!);
			public override void GetUsedParametersAndValues(ICollection<SqlParameter> parameters, ICollection<SqlValue> values) { }
		}

		sealed class BufferMaterializePreamble<TBuffer> : Preamble
		{
			readonly Query<TBuffer>           _bufferQuery;
			readonly Query                    _sourceQuery;
			readonly IPostQueryKeysPreamble[]  _keysPreambles;

			public BufferMaterializePreamble(Query<TBuffer> bufferQuery, Query sourceQuery, IPostQueryKeysPreamble[] keysPreambles)
			{
				_bufferQuery   = bufferQuery;
				_sourceQuery   = sourceQuery;
				_keysPreambles = keysPreambles;
			}

			public override object Execute(IDataContext dataContext, IQueryExpressions expressions, object?[]? parameters, object?[]? preambles)
			{
				// Copy parameter accessors lazily — they may not be finalized at construction time.
				_bufferQuery.SetParametersAccessors(_sourceQuery.ParameterAccessors);
				var buffer = _bufferQuery.GetResultEnumerable(dataContext, expressions, parameters, preambles).ToList();
				var ilist  = (IList)buffer;
				foreach (var kp in _keysPreambles)
					kp.SetKeysFromBuffer(ilist);
				return buffer;
			}

			public override async Task<object> ExecuteAsync(IDataContext dataContext, IQueryExpressions expressions, object?[]? parameters, object[]? preambles, CancellationToken cancellationToken)
			{
				_bufferQuery.SetParametersAccessors(_sourceQuery.ParameterAccessors);
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
	}
}
