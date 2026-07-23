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
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Metrics;

namespace LinqToDB.Internal.Linq.Builder
{
	partial class ExpressionBuilder
	{
		/// <summary>Marker interface for KeyedQueryKeysHarvester, used during buffer setup.</summary>
		interface IKeyedQueryKeysHarvester
		{
			/// <summary>Extract distinct keys from the buffer and write them into the child harvester's execution-context slot.</summary>
			void SetKeysFromBuffer(SqlCommandExecutionContext context, IList buffer);

			/// <summary>Index of the corresponding KeyedQueryChildHarvester in the harvesters list; also the context slot the keys are written to.</summary>
			int ChildHarvesterIndex { get; set; }
		}

		/// <summary>
		/// KeyedQuery strategy: joins child records to a local key collection (VALUES table)
		/// instead of re-querying the parent table. Keys are provided at runtime through the shared
		/// execution context, written by a key-extraction harvester and read by the child's VALUES parameter.
		/// Inner eager loads within the child query fall back to Default strategy.
		/// </summary>
		Expression? ProcessEagerLoadingKeyedQuery(
			IBuildContext          buildContext,
			SqlEagerLoadExpression eagerLoad,
			ParameterExpression    queryParameter,
			List<Harvester>         harvesters,
			Expression[]           previousKeys,
			EagerLoadState         state)
		{
			var cloningContext = new CloningContext();

			var itemType = eagerLoad.Type.GetItemType();

			if (itemType == null)
				throw new InvalidOperationException("Could not retrieve itemType for EagerLoading.");

			var dependencies = new HashSet<Expression>(ExpressionEqualityComparer.Instance);

			var sequenceExpression = eagerLoad.SequenceExpression;
			sequenceExpression     = ExpandContexts(buildContext, sequenceExpression);

			CollectDependencies(buildContext, sequenceExpression, dependencies);

			// For LoadWith associations, parent FK references live in eagerLoad.Predicate,
			// not in the sequence expression. Collect from both.
			if (eagerLoad.Predicate != null)
				CollectDependencies(buildContext, eagerLoad.Predicate, dependencies);

			// KeyedQuery requires all parent dependencies to appear exclusively inside simple
			// binary comparisons (==, >, >=, <, <=, !=) in the child's filter predicates.
			// Complex patterns (Contains/Any subqueries on parent collections, parent refs
			// in projections only) cannot be represented as VALUES keys.
			// Return null to trigger whole-strategy fallback to Default.
			if (dependencies.Count > 0 &&
				!HasOnlySimpleFilterDependencies(buildContext, sequenceExpression, eagerLoad.Predicate, dependencies, previousKeys))
			{
				state.FallbackReason = EagerLoadFallbackReason.ComplexParentReference;
				return null;
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

				var parameters = new object[] { detailSequence, queryParameter, harvesters };

				resultExpression = _buildHarvesterQueryDetachedMethodInfo
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

					// Also apply the predicate to the child/detail query.
					// For Concat/Union, each branch may have a discriminator predicate
					// (e.g., different association conditions per branch) that must filter
					// the child results to match only the correct branch.
					var childElementType = TypeHelper.GetEnumerableElementType(correctedSequence.Type)
						?? correctedSequence.Type;
					var childParam = Expression.Parameter(childElementType, "p_pred");
					var predicateLambda = Expression.Lambda(
						correctedPredicate.Transform(
							(clonedParentContext.ElementType, childParam),
							static (ctx, e) => e is ContextRefExpression cre && cre.BuildContext.ElementType == ctx.ElementType
								? ctx.childParam
								: e),
						childParam);

					if (typeof(IQueryable).IsAssignableFrom(correctedSequence.Type))
					{
						correctedSequence = Expression.Call(
							Methods.Queryable.Where.MakeGenericMethod(childElementType),
							correctedSequence, Expression.Quote(predicateLambda));
					}
					else
					{
						correctedSequence = Expression.Call(
							Methods.Enumerable.Where.MakeGenericMethod(childElementType),
							correctedSequence, predicateLambda);
					}
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

				// Reserve a dedicated execution-context slot for this keyed load's parent keys. The slot index is
				// baked as a scalar int constant into the keys accessor (below), which keeps sibling keyed children
				// as distinct cached queries — a reference-typed box would be parameterized out of the query-cache
				// key and collapse them. The slot is separate from the keys/child harvester slots, so buffer mode
				// (which repurposes the keys harvester slot) cannot clobber the keys.
				var keysDataIndex = harvesters.Count;
				harvesters.Add(new KeysDataSlotHarvester(keysDataIndex));

				// Source: local key collection read from that dedicated slot (written by the keys step).
				var sourceExpr = _buildKeyedQueryKeysSourceMethodInfo
					.MakeGenericMethod(keyType)
					.InvokeExt<Expression>(null, new object[] { keysDataIndex });

				Expression childQueryCall = null!;

				// Determine if we can use Contains optimization (single key with FK found as MemberExpression).
				// Also require that every reference to the detail key inside the child sequence appears as an
				// operand of an Equal binary — otherwise the Contains transform (which only rewrites Equal)
				// would leave a non-equality reference unresolved in the rewritten child query.
				var canUseContains = false;
				Expression? childFkExpr = null;
				if (mainKeys.Length == 1)
				{
					childFkExpr = FindChildFkExpression(correctedSequence, detailKeys[0]);

					if (childFkExpr is MemberExpression fkMember &&
						HasOnlyEqualityKeyReferences(correctedSequence, detailKeys[0]))
					{
						canUseContains = true;
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

					// Always wrap the terminal Select with KeyDetailEnvelope, extracting FK from the
					// entity parameter (pre-projection). This avoids member-name-based type introspection
					// and works uniformly whether FK is in the projection or not.
					{
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

					// A key with 8+ members is a ValueTuple whose 8th slot (Rest) is itself a nested
					// ValueTuple. Projecting the whole key element back through entity construction would
					// read that nested Rest as a single column and truncate the carried grouping key to its
					// first 7 members. Instead carry the key as an explicit tuple rebuilt from its leaf
					// accessors (the same New-based shape used for the VALUES keys), so every leaf is carried
					// as its own scalar column and the client reconstructs the full nested key by index.
					var carriedKey = (Expression)keyParameter;
					if (mainKeys.Length >= ValueTupleTypes.Length)
					{
						var keyLeaves = new Expression[mainKeys.Length];
						for (var li = 0; li < mainKeys.Length; li++)
							keyLeaves[li] = AccessValueTupleField(keyParameter, li);

						carriedKey = GenerateKeyExpression(keyLeaves, 0);
					}

					var keyDetailExpression = Expression.New(
						keyDetailType.GetConstructor([keyType, detailType])!,
						carriedKey,
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

				var parameters = new object?[] { detailSequence, mainKeyExpression, queryParameter, harvesters, orderByToApply, detailKeys, keysDataIndex, keyExtractionSequence, state };

				resultExpression = _buildKeyedQueryHarvesterAttachedMethodInfo
					.MakeGenericMethod(keyType, detailType)
					.InvokeExt<Expression>(this, parameters);
			}

			if (resultExpression is SqlErrorExpression errorExpression)
				return errorExpression.WithType(eagerLoad.Type);

			resultExpression = SqlAdjustTypeExpression.AdjustType(resultExpression, eagerLoad.Type, MappingSchema);
			return resultExpression;
		}

		/// <summary>
		/// Checks that all parent dependencies appear exclusively inside simple binary comparison
		/// expressions (<c>==</c>, <c>></c>, <c>>=</c>, <c>&lt;</c>, <c>&lt;=</c>, <c>!=</c>)
		/// within the child's filter predicates (Where lambdas and association predicate).
		/// <para>
		/// Returns <see langword="false"/> (= fall back to Default) when:
		/// <list type="bullet">
		/// <item>A dependency appears only in a projection, not in a filter.</item>
		/// <item>A dependency appears in a complex filter expression (e.g., <c>.Contains()</c>,
		///   <c>.Any()</c>, subqueries referencing parent navigation collections).</item>
		/// </list>
		/// </para>
		/// </summary>
		bool HasOnlySimpleFilterDependencies(
			IBuildContext       context,
			Expression          sequenceExpression,
			Expression?         predicate,
			HashSet<Expression> allDependencies,
			Expression[]        previousKeys)
		{
			var foundInSimpleBinary = new HashSet<Expression>(ExpressionEqualityComparer.Instance);
			foundInSimpleBinary.AddRange(previousKeys); // previousKeys are always acceptable

			// Check association predicate
			if (predicate != null)
				CollectSimpleBinaryDependencies(context, predicate, allDependencies, foundInSimpleBinary);

			// Walk Where lambdas in the method call chain
			var current = sequenceExpression;
			while (true)
			{
				if (current is MethodCallExpression mce)
				{
					if (!mce.IsQueryable)
						break;

					if (mce.Method.Name is nameof(Enumerable.Where) && mce.Arguments.Count >= 2)
					{
						var whereLambda = mce.Arguments[1].UnwrapLambda();
						if (whereLambda != null)
							CollectSimpleBinaryDependencies(context, whereLambda.Body, allDependencies, foundInSimpleBinary);
					}

					current = mce.Arguments[0];
				}
				else if (current is SqlAdjustTypeExpression adjustType)
				{
					current = adjustType.Expression;
				}
				else
				{
					break;
				}
			}

			// Every dependency must have been found in a simple binary comparison
			return allDependencies.All(foundInSimpleBinary.Contains);
		}

		/// <summary>
		/// Visits the expression and marks dependencies that appear as operands of simple binary
		/// comparison operators (Equal, GreaterThan, etc.). Dependencies found in other contexts
		/// (method calls, subqueries) are NOT marked.
		/// </summary>
		static void CollectSimpleBinaryDependencies(
			IBuildContext       context,
			Expression          expression,
			HashSet<Expression> allDependencies,
			HashSet<Expression> found)
		{
			expression.Visit((allDependencies, found), static (ctx, e) =>
			{
				if (e is BinaryExpression binary &&
					binary.NodeType is ExpressionType.Equal
						or ExpressionType.NotEqual
						or ExpressionType.GreaterThan
						or ExpressionType.GreaterThanOrEqual
						or ExpressionType.LessThan
						or ExpressionType.LessThanOrEqual)
				{
					if (ctx.allDependencies.Contains(binary.Left))
						ctx.found.Add(binary.Left);
					if (ctx.allDependencies.Contains(binary.Right))
						ctx.found.Add(binary.Right);
				}
			});
		}

		/// <summary>
		/// Returns <see langword="true"/> when every reference to <paramref name="detailKey"/>
		/// in <paramref name="sequence"/> appears as an operand of an
		/// <see cref="ExpressionType.Equal"/> binary (i.e. each occurrence will be rewritten by
		/// the Contains transform). Returns <see langword="false"/> when at least one reference
		/// appears in a non-equality context (e.g. <c>></c>, <c>>=</c>, method-call argument):
		/// such references would survive the rewrite and leave an unresolved parent-key reference
		/// in the child query, so the caller must fall back to the SelectMany + VALUES JOIN path.
		/// </summary>
		static bool HasOnlyEqualityKeyReferences(Expression sequence, Expression detailKey)
		{
			var unresolved = false;

			sequence.Visit(e =>
			{
				if (unresolved)
					return false;

				// Operands of an Equal binary will be rewritten — no need to descend into them.
				if (e is BinaryExpression { NodeType: ExpressionType.Equal })
					return false;

				if (ExpressionEqualityComparer.Instance.Equals(e, detailKey))
				{
					unresolved = true;
					return false;
				}

				return true;
			});

			return !unresolved;
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
				var detailAccess = Expression.Field(envelopeParam, "Detail");

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

		// Canonicalize key order so the inlined VALUES table is identical regardless of which extraction
		// path produced the keys (client-side buffer vs SQL key query) or what order the database returned
		// rows in. Without this, direct and remote (LinqService) execution can emit divergent SQL for the
		// same query (#5664). Keys whose type is not orderable (e.g. a composite with a byte[] component)
		// keep their source order — eager load compares keys structurally for equality only, so no orderable
		// contract is otherwise required.
		static TKey[] SortKeysDeterministically<TKey>(TKey[] keys)
		{
			if (keys.Length < 2)
				return keys;

			try
			{
				// Sort in place — every caller passes a freshly-allocated array (.ToArray()) that
				// nothing else holds, so mutating it is safe.
				Array.Sort(keys, DeterministicKeyComparer<TKey>.Instance);
			}
			catch (InvalidOperationException)
			{
				// TKey (or one of its composite components) does not implement IComparable.
				// Such keys are not deterministically orderable, so they keep source order — eager
				// load compares keys structurally for equality only, no ordering contract is required.
				// A partial Array.Sort here is harmless: it only reorders (never drops) elements, and
				// the order of a non-orderable key class is non-deterministic between paths regardless.
			}

			return keys;
		}

		// Ordinal, culture-invariant key comparer. Comparer<TKey>.Default orders string keys (and string
		// components of composite ValueTuple keys) with string.CompareTo, i.e. a culture-sensitive linguistic
		// comparison. That is unsuitable for canonicalizing SQL text: it is not a stable total order over
		// distinct strings (canonically-equal but distinct values can tie, and Array.Sort is not stable, so
		// the two extraction paths could still order them differently), and it is machine-culture-dependent
		// (baselines would reorder across cultures). Both defeat the deterministic ordering this
		// canonicalization exists to provide (#5664), so strings are compared ordinally.
		static class DeterministicKeyComparer<TKey>
		{
			public static readonly IComparer<TKey> Instance = Create();

			static IComparer<TKey> Create()
			{
				if (typeof(TKey) == typeof(string))
					return (IComparer<TKey>)(object)StringComparer.Ordinal;

				// Composite key (ValueTuple implements IStructuralComparable) that may contain string
				// components: compare element-wise with ordinal string handling. Arrays also implement
				// IStructuralComparable but are excluded — a byte[] key keeps the not-orderable fallback,
				// as before. Non-tuple, non-string keys use the default order, which is already
				// culture-invariant and a total order for the scalar types used as keys.
				if (typeof(IStructuralComparable).IsAssignableFrom(typeof(TKey)) && !typeof(TKey).IsArray)
					return new OrdinalKeyComparer();

				return Comparer<TKey>.Default;
			}

			// IComparer (non-generic) is implemented so IStructuralComparable.CompareTo can drive the
			// element-wise comparison recursively — it passes each component pair back through Compare.
			sealed class OrdinalKeyComparer : IComparer<TKey>, IComparer
			{
				public int Compare(TKey? x, TKey? y) => CompareElement(x, y);

				int IComparer.Compare(object? x, object? y) => CompareElement(x, y);

				int CompareElement(object? a, object? b)
				{
					if (a is null) return b is null ? 0 : -1;
					if (b is null) return 1;

					if (a is string sa && b is string sb)
						return string.CompareOrdinal(sa, sb);

					// Recurse into nested tuples (ValueTuple/Tuple implement IStructuralComparable) so
					// string components at any depth compare ordinally. Arrays are excluded: a byte[]
					// component is left to Comparer<object>.Default, which throws — Array.Sort wraps that
					// as InvalidOperationException, which SortKeysDeterministically catches to fall back to
					// source order, preserving the prior behaviour for non-orderable key classes.
					if (a is IStructuralComparable structural and not Array)
						return structural.CompareTo(b, this);

					return Comparer<object>.Default.Compare(a, b);
				}
			}
		}

		static readonly MethodInfo _getKeysAtMethodInfo =
			typeof(ExpressionBuilder).GetMethod(nameof(GetKeysAt), BindingFlags.Static | BindingFlags.NonPublic)
			?? throw new InvalidOperationException();

		// Reads a keyed child's parent keys from its dedicated execution-context slot (written by the keys step).
		// context is null only when the accessor is evaluated without a live execution (e.g. SQL-text generation);
		// the value is unused there, so return null.
		static TKey[]? GetKeysAt<TKey>(SqlCommandExecutionContext? context, int index)
			=> context != null ? (TKey[]?)context.GetResult(index) : null;

		static readonly MethodInfo _buildKeyedQueryKeysSourceMethodInfo =
			typeof(ExpressionBuilder).GetMethod(nameof(BuildKeyedQueryKeysSource), BindingFlags.Static | BindingFlags.NonPublic)
			?? throw new InvalidOperationException();

		// Builds the keyed child's keys-source expression: read the parent keys from the dedicated execution-context
		// slot (keysDataIndex). The index is baked as a scalar int constant, NOT a reference-typed box:
		// ExpressionCacheHelpers.ShouldRemoveConstantFromCache keeps scalar constants in the query-cache key, so
		// sibling keyed children stay distinct cached queries. A reference box is instead parameterized out of the
		// key, collapsing siblings onto one compiled query that reads the wrong slot.
		static Expression BuildKeyedQueryKeysSource<TKey>(int keysDataIndex)
		{
			var getKeys = _getKeysAtMethodInfo.MakeGenericMethod(typeof(TKey));
			return Expression.Call(getKeys, ExecutionContextParam, ExpressionInstances.Int32(keysDataIndex));
		}

		static readonly MethodInfo _buildKeyedQueryHarvesterAttachedMethodInfo =
			typeof(ExpressionBuilder).GetMethod(nameof(BuildKeyedQueryHarvesterAttached), BindingFlags.Instance | BindingFlags.NonPublic)
			?? throw new InvalidOperationException();

		Expression BuildKeyedQueryHarvesterAttached<TKey, T>(
			IBuildContext                   childSequence,
			Expression                      keyExpression,
			ParameterExpression             queryParameter,
			List<Harvester>                  harvesters,
			List<(LambdaExpression, bool)>? additionalOrderBy,
			Expression[]                    previousKeys,
			int                             keysDataIndex,
			IBuildContext                   keyExtractionSequence,
			EagerLoadState                  state)
			where TKey : notnull
		{
			// --- Step 1: Build key extraction harvester ---
			var keyQuery = new Query<TKey>(DataContext);
			keyQuery.Init(keyExtractionSequence);
			keyQuery.SetParametersAccessors(_parametersContext.CurrentSqlParameters.ToList());

			if (!BuildQuery(keyQuery, keyExtractionSequence, queryParameter, ref harvesters!, previousKeys))
				return keyQuery.ErrorExpression!;

			var keyHarvester = new KeyedQueryKeysHarvester<TKey>(keyQuery) { MainKeyExpression = keyExpression, KeysDataIndex = keysDataIndex };
			harvesters.Add(keyHarvester);

			// --- Step 2: Build child query harvester ---
			var childQuery = new Query<KeyDetailEnvelope<TKey, T>>(DataContext);
			childQuery.Init(childSequence);
			childQuery.SetParametersAccessors(_parametersContext.CurrentSqlParameters.ToList());

			if (!BuildQuery(childQuery, childSequence, queryParameter, ref harvesters!, Array.Empty<Expression>()))
				return childQuery.ErrorExpression!;

			// Signal the CURRENT level's BuildQuery to set up buffer materialization
			state.HasKeyedQueryHarvesters = true;

			var idx            = harvesters.Count;
			var childHarvester = new KeyedQueryChildHarvester<TKey, T>(childQuery, keysDataIndex);
			harvesters.Add(childHarvester);

			// Record the child harvester index on the key harvester so buffer setup can match the two by index.
			// The keys themselves live in the dedicated keysDataIndex slot (written by the keys step, read by the
			// child) — the child's own slot holds its HarvesterResult for the projection.
			keyHarvester.ChildHarvesterIndex = idx;

			var getListMethod = MemberHelper.MethodOf((HarvesterResult<TKey, T> c) => c.GetList(default!));

			Expression resultExpression =
				Expression.Call(
					Expression.Convert(Expression.Call(ExecutionContextParam, SqlCommandExecutionContext.GetResultMethodInfo, ExpressionInstances.Int32(idx)),
						typeof(HarvesterResult<TKey, T>)), getListMethod, keyExpression);

			if (additionalOrderBy != null)
			{
				resultExpression = ApplyEnumerableOrderBy(resultExpression, additionalOrderBy);
			}

			return resultExpression;
		}

		sealed class KeyedQueryKeysHarvester<TKey> : Harvester, IKeyedQueryKeysHarvester
			where TKey : notnull
		{
			readonly Query<TKey> _query;

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
			public int ChildHarvesterIndex { get; set; }

			/// <summary>Dedicated execution-context slot this harvester writes the extracted keys into; the child reads them from there.</summary>
			public int KeysDataIndex { get; set; }

			public KeyedQueryKeysHarvester(Query<TKey> query)
			{
				_query = query;
			}

			public override object Execute(IDataContext dataContext, IQueryExpressions expressions, SqlCommandExecutionContext? context)
			{
				var keys = SortKeysDeterministically(_query.GetResultEnumerable(dataContext, expressions, context).ToArray());
				// The child reads its parent keys from the dedicated KeysDataIndex slot. See BuildKeyedQueryHarvesterAttached.
				context?.SetResult(KeysDataIndex, keys);
				return keys;
			}

			public override async Task<object> ExecuteAsync(IDataContext dataContext, IQueryExpressions expressions, SqlCommandExecutionContext? context, CancellationToken cancellationToken)
			{
				var keys = SortKeysDeterministically(await _query.GetResultEnumerable(dataContext, expressions, context)
					.ToArrayAsync(cancellationToken).ConfigureAwait(false));
				context?.SetResult(KeysDataIndex, keys);
				return keys;
			}

			public void SetKeysFromBuffer(SqlCommandExecutionContext context, IList buffer)
			{
				if (BufferKeyExtractor == null)
				{
					// Extractor not set — buffer optimization not available for this harvester.
					// Fall back to SQL-based key extraction (Execute will be called normally).
					return;
				}

				var keySet = new HashSet<TKey>(ValueComparer.GetDefaultValueComparer<TKey>(favorStructuralComparisons: true));
				foreach (var row in buffer)
					keySet.Add(BufferKeyExtractor(row));

				// Write the (canonicalized) keys into the dedicated slot, exactly where the SQL key extraction
				// path would; the child reads them from there.
				context.SetResult(KeysDataIndex, SortKeysDeterministically(keySet.ToArray()));
			}

			public override void GetUsedParametersAndValues(ICollection<SqlParameter> parameters, ICollection<SqlValue> values)
			{
				QueryHelper.CollectParametersAndValues(_query.QueryInfo.Statement, parameters, values);
			}
		}

		sealed class KeyedQueryChildHarvester<TKey, T> : Harvester
			where TKey : notnull
		{
			readonly Query<KeyDetailEnvelope<TKey, T>> _query;
			readonly int                               _keysDataIndex;

			public KeyedQueryChildHarvester(
				Query<KeyDetailEnvelope<TKey, T>> query,
				int                               keysDataIndex)
			{
				_query         = query;
				_keysDataIndex = keysDataIndex;
			}

			public override object Execute(IDataContext dataContext, IQueryExpressions expressions, SqlCommandExecutionContext? context)
			{
				var keys   = context is null ? null : (TKey[]?)context.GetResult(_keysDataIndex);
				var result = new HarvesterResult<TKey, T>();

				// Skip child query when there are no parent keys — no rows to load.
				if (keys is not { Length: > 0 })
					return result;

				foreach (var e in _query.GetResultEnumerable(dataContext, expressions, context))
				{
					result.Add(e.Key, e.Detail);
				}

				return result;
			}

			public override async Task<object> ExecuteAsync(IDataContext dataContext, IQueryExpressions expressions, SqlCommandExecutionContext? context, CancellationToken cancellationToken)
			{
				var keys   = context is null ? null : (TKey[]?)context.GetResult(_keysDataIndex);
				var result = new HarvesterResult<TKey, T>();

				// Skip child query when there are no parent keys — no rows to load.
				if (keys is not { Length: > 0 })
					return result;

				await foreach (var e in _query.GetResultEnumerable(dataContext, expressions, context)
					.WithCancellation(cancellationToken)
					.ConfigureAwait(false))
				{
					result.Add(e.Key, e.Detail);
				}

				return result;
			}

			public override void GetUsedParametersAndValues(ICollection<SqlParameter> parameters, ICollection<SqlValue> values)
			{
				QueryHelper.CollectParametersAndValues(_query.QueryInfo.Statement, parameters, values);
			}
		}

		#region KeyedQuery Buffer Materialization

		/// <summary>
		/// Sets up buffer materialization: the main SQL runs once as a harvester producing ValueTuple rows,
		/// keys are extracted client-side, and the main query iterates the buffer to reconstruct T.
		/// Called from BuildQuery when the committed <see cref="EagerLoadState.HasKeyedQueryHarvesters"/> is true.
		/// </summary>
		void SetRunQueryWithKeyedQueryBuffer<T>(Query<T> query, IBuildContext sequence, Expression finalized, List<Harvester> harvesters, int harvesterStartIndex = 0)
		{
			var selectQuery = sequence.SelectQuery;

			// 1. Collect unique resolved SqlPlaceholderExpressions
			var placeholders  = new List<SqlPlaceholderExpression>();
			var seenIndices   = new HashSet<int>();
			finalized.Visit((placeholders, seenIndices), static (ctx, e) =>
			{
				if (e is SqlPlaceholderExpression p && p.Index != null && ctx.seenIndices.Add(p.Index.Value))
					ctx.placeholders.Add(p);

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
			_setupKeyedQueryBufferMethodInfo
				.MakeGenericMethod(typeof(T), bufferType)
				.InvokeExt(this, new object[] { query, sequence, finalized, harvesters, selectQuery, placeholders.ToArray(), colTypes, harvesterStartIndex });
		}

		static readonly MethodInfo _setupKeyedQueryBufferMethodInfo =
			typeof(ExpressionBuilder).GetMethod(nameof(SetupKeyedQueryBuffer), BindingFlags.Instance | BindingFlags.NonPublic)
			?? throw new InvalidOperationException();

		void SetupKeyedQueryBuffer<T, TBuffer>(
			Query<T>                    query,
			IBuildContext               sequence,
			Expression                  finalized,
			List<Harvester>              harvesters,
			SelectQuery                 selectQuery,
			SqlPlaceholderExpression[]  placeholders,
			Type[]                      colTypes,
			int                         harvesterStartIndex)
		{
			// 3. Build buffer mapper: new TBuffer(placeholder0, placeholder1, ...)
			var bufferBody  = BuildValueTupleNew(typeof(TBuffer), placeholders.Cast<Expression>().ToArray());
			var bufferMapper = BuildMapper<TBuffer>(selectQuery, bufferBody);

			// 4. Create Query<TBuffer> sharing the main SQL statement and parameter accessors.
			// Mark as parameter-dependent and continuous-run because the statement may contain
			// VALUES tables whose source is a SqlParameter (e.g., the keyed keys from the execution context).
			// Without this, the optimizer passes null ParameterValues to EvaluationContext,
			// making the SqlParameter unevaluable at SQL generation time.
			var bufferQuery = new Query<TBuffer>(DataContext);
			var bufferStatement = query.QueryInfo.Statement;
			bufferStatement.IsParameterDependent = true;
			bufferQuery.QueryInfo = new QueryInfo { Statement = bufferStatement, IsContinuousRun = true };
			QueryRunner.SetRunQuery(bufferQuery, bufferMapper);

			// 5. Build reconstruction using a visitor that handles all custom expression types.
			var placeholderMap = new Dictionary<int, int>();
			for (var i = 0; i < placeholders.Length; i++)
				placeholderMap[placeholders[i].Index!.Value] = i;

			var bufferRowParam = Expression.Parameter(typeof(TBuffer), "bufRow");
			var harvesterParam  = Expression.Parameter(typeof(SqlCommandExecutionContext), "pr");

			var visitor = new BufferReconstructionVisitor(placeholderMap, bufferRowParam, harvesterParam);
			var reconstructed = visitor.Visit(finalized)!;

			if (reconstructed.Type != typeof(T))
				reconstructed = Expression.Convert(reconstructed, typeof(T));

			var reconstructionLambda = Expression.Lambda<Func<IQueryExpressions, object?[]?, TBuffer, SqlCommandExecutionContext?, T>>(
				reconstructed,
				QueryExpressionContainerParam,
				ParametersParam,
				bufferRowParam, harvesterParam);
			var reconstructionFunc = reconstructionLambda.CompileExpression();

			// 6. Build key extractors for each KeyedQueryKeysHarvester and replace with buffer harvester
			// Only process harvesters at this BuildQuery level (from harvesterStartIndex onward).
			var keysHarvesters = new List<IKeyedQueryKeysHarvester>();
			var firstKeyIdx   = -1;

			for (var i = harvesterStartIndex; i < harvesters.Count; i++)
			{
				if (harvesters[i] is IKeyedQueryKeysHarvester kp)
				{
					keysHarvesters.Add(kp);
					if (firstKeyIdx == -1) firstKeyIdx = i;
				}
			}

			// Build key extractors: extract key expressions from the finalized expression
			// by finding HarvesterResult.GetList(keyExpr) calls for each child harvester index. The child-result
			// lookup is context.GetResult(idx) (was harvesterArray[idx] before the SqlCommandExecutionContext
			// threading), so match the GetResult call carrying the constant harvester index.
			var keyExpressions = new Dictionary<int, Expression>();
			finalized.Visit(keyExpressions, static (ctx, e) =>
			{
				if (e is MethodCallExpression { Method.Name: nameof(HarvesterResult<,>.GetList) } call
					&& call.Arguments.Count == 1
					&& call.Object is UnaryExpression { NodeType: ExpressionType.Convert, Operand: { } operand }
					&& operand is MethodCallExpression { Arguments: [ConstantExpression { Value: int idx }] } getResultCall
					&& getResultCall.Method == SqlCommandExecutionContext.GetResultMethodInfo)
				{
					ctx[idx] = call.Arguments[0];
				}

				return true;
			});

			// Verify ALL key harvesters at this level can get extractors. If not, skip buffer optimization.
			// Use ChildHarvesterIndex to find the corresponding GetList call (not pi + 1, since inner
			// harvesters may be inserted between the key harvester and its child harvester).
			var allExtractorsFound = true;
			for (var ki = 0; ki < keysHarvesters.Count && allExtractorsFound; ki++)
			{
				if (!keyExpressions.ContainsKey(keysHarvesters[ki].ChildHarvesterIndex))
					allExtractorsFound = false;
			}

			if (!allExtractorsFound)
			{
				// Can't build all key extractors — fall back to normal SetRunQuery
				sequence.SetRunQuery(query, finalized);
				return;
			}

			for (var ki = 0; ki < keysHarvesters.Count; ki++)
			{
				var kp     = keysHarvesters[ki];
				var kpType = kp.GetType();
				if (kpType.IsGenericType && kpType.GetGenericTypeDefinition() == typeof(KeyedQueryKeysHarvester<>))
				{
					var tKey = kpType.GetGenericArguments()[0];

					if (keyExpressions.TryGetValue(kp.ChildHarvesterIndex, out var keyExpr))
					{
						_setKeyExtractorMethodInfo
							.MakeGenericMethod(typeof(TBuffer), tKey)
							.InvokeExt(null, new object[] { kp, keyExpr, placeholderMap });
					}
				}
			}

			// Replace first key harvester with BufferMaterializeHarvester, rest become no-ops
			if (firstKeyIdx >= 0)
			{
				harvesters[firstKeyIdx] = new BufferMaterializeHarvester<TBuffer>(bufferQuery, query, keysHarvesters.ToArray());
				for (var i = firstKeyIdx + 1; i < harvesters.Count; i++)
				{
					if (harvesters[i] is IKeyedQueryKeysHarvester)
						harvesters[i] = NoOpHarvester.Instance;
				}
			}

			// 7. Override GetResultEnumerable to iterate buffer
			var bufferHarvesterIdx = firstKeyIdx;

			query.GetResultEnumerable = (db, expr, harvesterResults) =>
			{
				using var _ = ActivityService.Start(ActivityID.GetIEnumerable);
				var buffer = (List<TBuffer>)harvesterResults!.GetResult(bufferHarvesterIdx)!;
				return new BufferResultEnumerable<TBuffer, T>(buffer, expr, reconstructionFunc, harvesterResults);
			};

			// 8. Apply element-selection semantics from the calling sequence context.
			//    For First/Single/etc. this installs cardinality-aware delegates that wrap
			//    the buffer-iterating GetResultEnumerable above; for collection queries
			//    (.ToList() etc.) it's a no-op.
			sequence.SetElementSelection(query);
		}

		static readonly MethodInfo _setKeyExtractorMethodInfo =
			typeof(ExpressionBuilder).GetMethod(nameof(SetKeyExtractorFromBuffer), BindingFlags.Static | BindingFlags.NonPublic)
			?? throw new InvalidOperationException();

		/// <summary>
		/// Builds and sets the BufferKeyExtractor on a KeyedQueryKeysHarvester.
		/// The extractor takes a buffer row (TBuffer as object) and returns TKey.
		/// </summary>
		static void SetKeyExtractorFromBuffer<TBuffer, TKey>(KeyedQueryKeysHarvester<TKey> keysHarvester, Expression mainKeyExpression, Dictionary<int, int> placeholderMap)
			where TKey : notnull
		{
			var bufferRowParam = Expression.Parameter(typeof(object), "row");
			// Use a Convert expression as the "buffer row" so the visitor reads tuple fields from it
			var typedRow       = Expression.Convert(bufferRowParam, typeof(TBuffer));
			var dummyHarvester  = Expression.Parameter(typeof(SqlCommandExecutionContext), "unused");

			var visitor = new BufferReconstructionVisitor(placeholderMap, typedRow, dummyHarvester);
			var keyFromBuffer = visitor.Visit(mainKeyExpression)!;

			if (keyFromBuffer.Type != typeof(TKey))
				keyFromBuffer = Expression.Convert(keyFromBuffer, typeof(TKey));

			var lambda = Expression.Lambda<Func<object, TKey>>(keyFromBuffer, bufferRowParam);
			keysHarvester.BufferKeyExtractor = lambda.CompileExpression();
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
			readonly Expression            _harvesterExpr;

			public BufferReconstructionVisitor(
				Dictionary<int, int> placeholderMap,
				Expression           bufferRowExpr,
				Expression           harvesterExpr)
			{
				_placeholderMap = placeholderMap;
				_bufferRowExpr  = bufferRowExpr;
				_harvesterExpr  = harvesterExpr;
			}

			protected override Expression VisitParameter(ParameterExpression node)
			{
				// Replace ExecutionContextParam with our local execution-context expression
				if (ReferenceEquals(node, ExecutionContextParam))
					return _harvesterExpr;
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
				// Visit the inner expression, then re-apply the SAME type adjustment the node carries.
				var inner = Visit(node.Expression);
				if (inner.Type == node.Type)
					return inner;

				// Reuse SqlAdjustTypeExpression's own coercion (node.Update -> AdjustType) so the
				// adjustment reduces to the correct shape at compile time — e.g. an IOrderedEnumerable
				// inner targeting an IOrderedQueryable member becomes AsQueryable(...) + Convert, exactly
				// like the CteUnion reconstruction (which preserves the node via Transform and reduces it).
				// The previous "soft adjust" dropped the adjustment for unrelated interface pairs
				// (IOrderedEnumerable vs IOrderedQueryable), feeding a mistyped value into the enclosing
				// New/MemberInit and throwing ArgumentException at Expression.New construction.
				return node.Update(inner);
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

		sealed class NoOpHarvester : Harvester
		{
			public static readonly NoOpHarvester Instance = new();
			public override object Execute(IDataContext dataContext, IQueryExpressions expressions, SqlCommandExecutionContext? context) => null!;
			public override Task<object> ExecuteAsync(IDataContext dataContext, IQueryExpressions expressions, SqlCommandExecutionContext? context, CancellationToken cancellationToken) => Task.FromResult<object>(null!);
			public override void GetUsedParametersAndValues(ICollection<SqlParameter> parameters, ICollection<SqlValue> values) { }
		}

		// Placeholder occupying a keyed load's dedicated keys slot. Its result preserves whatever the keys step
		// wrote into that slot rather than clobbering it: in buffer mode the buffer materializer fills every keyed
		// load's slot from one earlier harvester, so a slot belonging to a later load is already populated by the
		// time the interpreter reaches this placeholder — returning the current slot value makes the interpreter's
		// SetResult a no-op. In the non-buffer path this runs before the keys step and carries the still-empty slot
		// forward until the keys step fills it.
		sealed class KeysDataSlotHarvester : Harvester
		{
			readonly int _index;

			public KeysDataSlotHarvester(int index) => _index = index;

			public override object Execute(IDataContext dataContext, IQueryExpressions expressions, SqlCommandExecutionContext? context) => context?.GetResult(_index)!;
			public override Task<object> ExecuteAsync(IDataContext dataContext, IQueryExpressions expressions, SqlCommandExecutionContext? context, CancellationToken cancellationToken) => Task.FromResult(context?.GetResult(_index)!);
			public override void GetUsedParametersAndValues(ICollection<SqlParameter> parameters, ICollection<SqlValue> values) { }
		}

		sealed class BufferMaterializeHarvester<TBuffer> : Harvester
		{
			readonly Query<TBuffer>           _bufferQuery;
			readonly Query                    _sourceQuery;
			readonly IKeyedQueryKeysHarvester[]  _keysHarvesters;

			public BufferMaterializeHarvester(Query<TBuffer> bufferQuery, Query sourceQuery, IKeyedQueryKeysHarvester[] keysHarvesters)
			{
				_bufferQuery   = bufferQuery;
				_sourceQuery   = sourceQuery;
				_keysHarvesters = keysHarvesters;
			}

			public override object Execute(IDataContext dataContext, IQueryExpressions expressions, SqlCommandExecutionContext? context)
			{
				// Copy parameter accessors lazily — they may not be finalized at construction time.
				_bufferQuery.SetParametersAccessors(_sourceQuery.ParameterAccessors);
				var buffer = _bufferQuery.GetResultEnumerable(dataContext, expressions, context).ToList();
				var ilist  = (IList)buffer;
				// Buffer mode always runs on the sequential path with a live context; guard defensively so a
				// null context (e.g. SQL-text generation) can't NRE — keys simply aren't forwarded then.
				if (context != null)
				{
					foreach (var kp in _keysHarvesters)
						kp.SetKeysFromBuffer(context, ilist);
				}

				return buffer;
			}

			public override async Task<object> ExecuteAsync(IDataContext dataContext, IQueryExpressions expressions, SqlCommandExecutionContext? context, CancellationToken cancellationToken)
			{
				_bufferQuery.SetParametersAccessors(_sourceQuery.ParameterAccessors);
				var buffer = await _bufferQuery.GetResultEnumerable(dataContext, expressions, context)
					.ToListAsync(cancellationToken).ConfigureAwait(false);
				var ilist = (IList)buffer;
				if (context != null)
				{
					foreach (var kp in _keysHarvesters)
						kp.SetKeysFromBuffer(context, ilist);
				}

				return buffer;
			}

			public override void GetUsedParametersAndValues(ICollection<SqlParameter> parameters, ICollection<SqlValue> values)
			{
				QueryHelper.CollectParametersAndValues(_bufferQuery.QueryInfo.Statement, parameters, values);
			}
		}

		sealed class BufferResultEnumerable<TBuffer, T> : IResultEnumerable<T>
		{
			readonly List<TBuffer>                                        _buffer;
			readonly IQueryExpressions                                    _expr;
			readonly Func<IQueryExpressions, object?[]?, TBuffer, SqlCommandExecutionContext?, T> _reconstruct;
			readonly SqlCommandExecutionContext?                         _context;

			public BufferResultEnumerable(
				List<TBuffer>                                                buffer,
				IQueryExpressions                                            expr,
				Func<IQueryExpressions, object?[]?, TBuffer, SqlCommandExecutionContext?, T>  reconstruct,
				SqlCommandExecutionContext?                                  context)
			{
				_buffer      = buffer;
				_expr        = expr;
				_reconstruct = reconstruct;
				_context     = context;
			}

			public IEnumerator<T> GetEnumerator()
			{
				// Mirror the pre-threading `?? Array.Empty<object>()` guard: a reconstruction that references the
				// execution context must never receive null, so substitute an empty context when none was supplied.
				var context = _context ?? new SqlCommandExecutionContext(0);
				foreach (var row in _buffer)
					yield return _reconstruct(_expr, context.Parameters, row, context);
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
