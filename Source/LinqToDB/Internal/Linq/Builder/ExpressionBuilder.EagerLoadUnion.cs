using System;
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
using LinqToDB.Mapping;

namespace LinqToDB.Internal.Linq.Builder
{
	partial class ExpressionBuilder
	{
		/// <summary>
		/// Batch-processes all CteUnion eager loads in an expression tree into a single UNION ALL query.
		/// Returns a mapping from each eager load's sequence expression to its preamble access expression,
		/// or null if batch processing is not possible.
		/// </summary>
		Dictionary<Expression, Expression>? ProcessCteUnionBatch(
			Expression          expression,
			IBuildContext       buildContext,
			ParameterExpression queryParameter,
			List<Preamble>      preambles,
			Expression[]        previousKeys)
		{
			// Phase 1: Collect all CteUnion eager loads
			var cteUnionLoads = new List<SqlEagerLoadExpression>();

			expression.Visit((cteUnionLoads, builder: this), static (ctx, e) =>
			{
				if (e.NodeType == ExpressionType.Extension && e is SqlEagerLoadExpression eagerLoad
					&& ctx.builder.ResolveStrategy(eagerLoad) == EagerLoadingStrategy.CteUnion)
				{
					ctx.cteUnionLoads.Add(eagerLoad);
				}
			});

			if (cteUnionLoads.Count < 2)
				return null; // Single eager load doesn't benefit from UNION ALL

			// Phase 2: Collect branch info using EXPANDED sequences
			// Note: buildContext.ElementType may be a projected type (e.g., anonymous type from Concat).
			// We derive the actual parent entity type from collected parent refs later.
			var mainType = buildContext.ElementType;
			var branches = new List<CteUnionBranch>();

			// Collect ALL parent-referencing expressions across all branches for CTE projection
			var allParentRefs   = new List<Expression>();
			var parentRefSet    = new HashSet<Expression>(ExpressionEqualityComparer.Instance);
			var allDependencies = new HashSet<Expression>(ExpressionEqualityComparer.Instance);

			foreach (var eagerLoad in cteUnionLoads)
			{
				var itemType = eagerLoad.Type.GetItemType();
				if (itemType == null)
					continue;

				var dependencies = new HashSet<Expression>(ExpressionEqualityComparer.Instance);

				var sequenceExpression = eagerLoad.SequenceExpression;
				var expandedSequence   = ExpandContexts(buildContext, sequenceExpression);

				CollectDependencies(buildContext, expandedSequence, dependencies);

				dependencies.AddRange(previousKeys);

				if (dependencies.Count == 0)
					continue;

				var mainKeys = new Expression[dependencies.Count];
				int i = 0;
				foreach (var dependency in dependencies)
				{
					mainKeys[i] = dependency;
					++i;
				}

				var detailType = TypeHelper.GetEnumerableElementType(eagerLoad.Type);

				// Expand predicate and collect its parent refs too
				Expression? expandedPredicate = null;
				if (eagerLoad.Predicate != null)
				{
					expandedPredicate = ExpandContexts(buildContext, eagerLoad.Predicate);
					CollectDependencies(buildContext, expandedPredicate, dependencies);
				}

				// Add all dependencies to the CTE projection set
				foreach (var dep in dependencies)
				{
					allDependencies.Add(dep);
					if (parentRefSet.Add(dep))
						allParentRefs.Add(dep);
				}

				Expression mainKeyExpression = mainKeys.Length == 1
					? mainKeys[0]
					: GenerateKeyExpression(mainKeys, 0);

				// Build detail sequence to discover actual SQL placeholders.
				// This handles complex projections (Select(d => new { ... })), not just entities.
				var detailCtx    = BuildSequence(new BuildInfo((IBuildContext?)null, expandedSequence, new SelectQuery()));
				var detailRef    = new ContextRefExpression(detailType, detailCtx);
				var builtDetail  = BuildSqlExpression(detailCtx, detailRef, BuildPurpose.Expression, BuildFlags.ForSetProjection);
				var placeholders = CollectDistinctPlaceholders(builtDetail, false);

				if (placeholders.Count == 0)
					continue;

				// Check for nested eager loads in the detail expression.
				// These can't be handled in the UNION ALL carrier — fall back for this batch.
				if (builtDetail.Find(0, static (_, e) => e is SqlEagerLoadExpression) != null)
					return null;

				// Collect ContextRef patterns from predicate for CTE projection
				if (expandedPredicate != null)
				{
					expandedPredicate.Visit((parentRefSet, allParentRefs), static (ctx, e) =>
					{
						if (e is MemberExpression me && me.Expression is ContextRefExpression && ctx.parentRefSet.Add(e))
							ctx.allParentRefs.Add(e);
						else if (e is ContextRefExpression && ctx.parentRefSet.Add(e))
							ctx.allParentRefs.Add(e);
					});
				}

				branches.Add(new CteUnionBranch
				{
					EagerLoad          = eagerLoad,
					ExpandedSequence   = expandedSequence,
					ExpandedPredicate  = expandedPredicate,
					BuiltDetailExpr    = builtDetail,
					DetailContext      = detailCtx,
					DetailType         = detailType,
					KeyType            = mainKeyExpression.Type,
					MainKeyExpression  = mainKeyExpression,
					MainKeys           = mainKeys,
					Placeholders       = placeholders,
					OrderBy            = CollectOrderBy(sequenceExpression),
				});
			}

			if (branches.Count < 2)
				return null;

			if (allParentRefs.Count == 0)
				return null;

			// Derive the actual parent entity type from collected ContextRefExpressions.
			// buildContext.ElementType may be a projected/anonymous type (e.g., from Concat),
			// but the CTE must wrap the actual entity table.
			var parentCtxRef = allParentRefs
				.Select(r => r is MemberExpression me ? me.Expression as ContextRefExpression : r as ContextRefExpression)
				.FirstOrDefault(c => c != null);

			if (parentCtxRef != null)
				mainType = parentCtxRef.BuildContext.ElementType;
			else if (allParentRefs.Exists(r => r is SqlPlaceholderExpression))
				return null; // SQL placeholders from Concat/SetOperations — can't wrap in CTE

			// Verify all branches share the same key type
			var firstKeyType = branches[0].KeyType;
			if (branches.Exists(b => b.KeyType != firstKeyType))
				return null;

			// Phase 3: Build carrier type with slot reuse
			// Slots 0=setId, 1=key. Data slots start at 2.
			var slotTypes = new List<Type> { typeof(int), firstKeyType };

			// For each branch, slotMap[b][c] = carrier slot index for column c of branch b
			var slotMaps = new int[branches.Count][];

			// Track which slots are "occupied" by which branch (-1 = free)
			var slotOwners = new List<int>(); // parallel to slotTypes, starting from index 2

			for (int b = 0; b < branches.Count; b++)
			{
				var phs = branches[b].Placeholders;
				slotMaps[b] = new int[phs.Count];

				for (int c = 0; c < phs.Count; c++)
				{
					var colType = phs[c].ConvertType;
					if (colType.IsValueType && Nullable.GetUnderlyingType(colType) == null)
						colType = typeof(Nullable<>).MakeGenericType(colType);

					// Try to reuse a slot from a different branch with the same nullable CLR type
					var reusedSlot = -1;
					for (int s = 0; s < slotOwners.Count; s++)
					{
						if (slotOwners[s] != b && slotTypes[s + 2] == colType)
						{
							// Check this slot isn't already used by this branch
							var alreadyUsed = false;
							for (int pc = 0; pc < c; pc++)
							{
								if (slotMaps[b][pc] == s + 2)
								{
									alreadyUsed = true;
									break;
								}
							}

							if (!alreadyUsed)
							{
								reusedSlot = s + 2;
								slotOwners[s] = -1; // Mark as shared (used by multiple branches)
								break;
							}
						}
					}

					if (reusedSlot >= 0)
					{
						slotMaps[b][c] = reusedSlot;
					}
					else
					{
						slotMaps[b][c] = slotTypes.Count;
						slotOwners.Add(b);
						slotTypes.Add(colType);
					}
				}
			}

			var maxColumns = DataContext.SqlProviderFlags.MaxColumnCount;
			if (maxColumns > 0 && slotTypes.Count > maxColumns)
				return null;

			var carrierTypes = slotTypes.ToArray();
			var carrierType  = BuildValueTupleType(carrierTypes);

			// Phase 4: Build CTE with ValueTuple projection of ALL parent refs
			// Clone the actual parent entity context (not buildContext, which may be a Concat/projection)
			var cloningContext  = new CloningContext();
			var parentBuildCtx  = parentCtxRef?.BuildContext ?? buildContext;
			var cteSourceCtx    = cloningContext.CloneContext(parentBuildCtx);
			var mainExpression  = new ContextRefExpression(typeof(IQueryable<>).MakeGenericType(mainType), cteSourceCtx);

			// Build CTE projection type: VT<parentRef0Type, parentRef1Type, ...>
			var cteColTypes = allParentRefs.Select(r => r.Type).ToArray();
			var cteType     = BuildValueTupleType(cteColTypes);

			// Build Select lambda: cte_x => new VT(cte_x.Field1, cte_x.Field2, ...)
			// Manually replace ContextRefExpression with the lambda parameter for each dependency
			var cteParam = Expression.Parameter(mainType, "cte_x");
			var cteArgs  = new Expression[cteColTypes.Length];

			for (int ci = 0; ci < allParentRefs.Count; ci++)
			{
				var dep = allParentRefs[ci];

				if (dep is MemberExpression me && me.Expression is ContextRefExpression)
				{
					// ContextRef.Field → cte_x.Field
					// The member might be from a different type (e.g., anonymous type projection),
					// so look up the member by name on the actual mainType
					var member = mainType.GetProperty(me.Member.Name) ?? (MemberInfo?)mainType.GetField(me.Member.Name);
					if (member == null)
						return null; // Member not found on mainType — can't project into CTE
					cteArgs[ci] = Expression.MakeMemberAccess(cteParam, member);
				}
				else if (dep is ContextRefExpression)
				{
					// ContextRef itself → cte_x
					cteArgs[ci] = cteParam;
				}
				else
				{
					// Nested member access (e.g., ContextRef.Nav.Field) → deep replace
					cteArgs[ci] = dep.Transform(
						cteParam,
						static (param, e) => e is ContextRefExpression ? param : e);
				}
			}

			var cteBody = BuildValueTupleNew(cteType, cteArgs);

			var cteSelectLambda = Expression.Lambda(cteBody, cteParam);
			var cteSelectExpr   = Expression.Call(
				Methods.Queryable.Select.MakeGenericMethod(mainType, cteType),
				mainExpression, Expression.Quote(cteSelectLambda));

			var mainCteExpression = Expression.Call(
				Methods.LinqToDB.AsCte.MakeGenericMethod(cteType), cteSelectExpr);

			// Build CTE ref mapping with dummy parameter:
			// parentRef → AccessValueTupleField(dummyCteParam, slotIdx)
			// Then per-branch: swap dummyCteParam → ContextRefExpression(branchCtx)
			var dummyCteParam = Expression.Parameter(cteType, "cte_dummy");
			var cteRefMap     = new Dictionary<Expression, Expression>(ExpressionEqualityComparer.Instance);
			for (int i = 0; i < allParentRefs.Count; i++)
				cteRefMap[allParentRefs[i]] = AccessValueTupleField(dummyCteParam, i);

			// Phase 5: Build UNION ALL branches — use cloned visitor for CTE building
			var saveVisitor = _buildVisitor;
			_buildVisitor = _buildVisitor.Clone(cloningContext);
			cloningContext.UpdateContextParents();

			Expression? concatExpr = null;

			for (int b = 0; b < branches.Count; b++)
			{
				var branch          = branches[b];
				var mainParameter   = Expression.Parameter(cteType, "kd");
				var detailParameter = Expression.Parameter(branch.DetailType, "d");

				// Build a new CteTableContext for this branch
				var branchCtx = BuildSequence(new BuildInfo((IBuildContext?)null, mainCteExpression, new SelectQuery()));
				var branchRef = new ContextRefExpression(cteType, branchCtx);

				// Remap expanded sequence: replace parent refs with CTE field access
				var retargetedSequence = RetargetThroughCteMap(branch.ExpandedSequence, cteRefMap, dummyCteParam, branchRef);

				// Apply predicate if present — retarget parent refs through CTE, then wrap in .Where()
				if (branch.ExpandedPredicate != null)
				{
					var retargetedPredicate = RetargetThroughCteMap(branch.ExpandedPredicate, cteRefMap, dummyCteParam, branchRef);

					var childElementType = TypeHelper.GetEnumerableElementType(retargetedSequence.Type) ?? retargetedSequence.Type;
					var predParam        = Expression.Parameter(childElementType, "p_pred");
					var predicateLambda  = Expression.Lambda(
						retargetedPredicate.Transform(
							(cteType, predParam),
							static (ctx, e) => e is ContextRefExpression cre && cre.BuildContext.ElementType == ctx.cteType
								? ctx.predParam
								: e),
						predParam);

					retargetedSequence = typeof(IQueryable).IsAssignableFrom(retargetedSequence.Type)
						? Expression.Call(Methods.Queryable.Where.MakeGenericMethod(childElementType), retargetedSequence, Expression.Quote(predicateLambda))
						: Expression.Call(Methods.Enumerable.Where.MakeGenericMethod(childElementType), retargetedSequence, predicateLambda);
				}

				// Build carrier arguments
				var args = new Expression[carrierTypes.Length];
				args[0] = Expression.Constant(b); // setId

				// Key from CTE — remap mainKeys through dummy mapping, then swap
				var remappedKeys = new Expression[branch.MainKeys.Length];
				for (int k = 0; k < branch.MainKeys.Length; k++)
				{
					if (cteRefMap.TryGetValue(branch.MainKeys[k], out var mapped))
					{
						remappedKeys[k] = mapped.Transform(
							(dummyCteParam, branchRef),
							static (ctx, inner) => inner == ctx.dummyCteParam ? ctx.branchRef : inner);
					}
					else
					{
						remappedKeys[k] = branch.MainKeys[k];
					}
				}

				args[1] = remappedKeys.Length == 1
					? remappedKeys[0]
					: GenerateKeyExpression(remappedKeys, 0);

				for (int s = 2; s < args.Length; s++)
					args[s] = Expression.Default(carrierTypes[s]);

				for (int c = 0; c < branch.Placeholders.Count; c++)
				{
					var ph      = branch.Placeholders[c];
					var slotIdx = slotMaps[b][c];

					// Use the placeholder's path to build an access on detailParameter
					Expression access;
					if (ph.Path is MemberExpression mePath)
					{
						// Reconstruct member access on the result selector's detail parameter
						access = Expression.MakeMemberAccess(detailParameter, mePath.Member);
					}
					else
					{
						access = ph; // fallback: use placeholder directly
					}

					if (access.Type != carrierTypes[slotIdx])
						access = Expression.Convert(access, carrierTypes[slotIdx]);
					args[slotIdx] = access;
				}

				var carrierNew = BuildValueTupleNew(carrierType, args);

				var resultSelector = Expression.Lambda(carrierNew, mainParameter, detailParameter);

				var detailSelector = _buildSelectManyDetailSelectorInfo
					.MakeGenericMethod(cteType, branch.DetailType)
					.InvokeExt<LambdaExpression>(null, new object[] { retargetedSequence, mainParameter });

				var branchQuery = Expression.Call(
					Methods.Queryable.SelectManyProjection.MakeGenericMethod(cteType, branch.DetailType, carrierType),
					new ContextRefExpression(typeof(IQueryable<>).MakeGenericType(cteType), branchCtx),
					Expression.Quote(detailSelector), Expression.Quote(resultSelector));

				concatExpr = concatExpr == null
					? branchQuery
					: Expression.Call(Methods.Queryable.Concat.MakeGenericMethod(carrierType), concatExpr, branchQuery);
			}

			if (concatExpr == null)
				return null;

			// Phase 6: Build UNION ALL combined sequence
			var combinedSequence = BuildSequence(new BuildInfo((IBuildContext?)null, concatExpr,
				new SelectQuery()));

			_buildVisitor = saveVisitor;

			// Phase 7: Create preamble via reflection
			var result = (Dictionary<Expression, Expression>)_buildCteUnionPreambleMethodInfo
				.MakeGenericMethod(firstKeyType, carrierType)
				.InvokeExt<object>(this, new object?[]
				{
					combinedSequence, branches[0].MainKeyExpression, queryParameter, preambles,
					branches.ToArray(), slotMaps, carrierTypes,
				})!;

			return result;
		}

		static readonly MethodInfo _buildCteUnionPreambleMethodInfo =
			typeof(ExpressionBuilder).GetMethod(nameof(BuildCteUnionPreamble), BindingFlags.Instance | BindingFlags.NonPublic)
			?? throw new InvalidOperationException();

		Dictionary<Expression, Expression> BuildCteUnionPreamble<TKey, TCarrier>(
			IBuildContext       combinedSequence,
			Expression          keyExpression,
			ParameterExpression queryParameter,
			List<Preamble>      preambles,
			CteUnionBranch[]    branches,
			int[][]             slotMaps,
			Type[]              carrierTypes)
			where TKey : notnull
		{
			var query = new Query<TCarrier>(DataContext);
			query.Init(combinedSequence);
			query.SetParametersAccessors(_parametersContext.CurrentSqlParameters.ToList());

			if (!BuildQuery(query, combinedSequence, queryParameter, ref preambles!, []))
				throw new LinqToDBException("Failed to build CteUnion combined query.");

			// Build setId extractor
			var carrierParam   = Expression.Parameter(typeof(TCarrier), "vt");
			var setIdAccess    = AccessValueTupleField(carrierParam, 0);
			var setIdExtractor = Expression.Lambda<Func<TCarrier, int>>(setIdAccess, carrierParam).CompileExpression();

			// Build key extractor
			var keyAccess = AccessValueTupleField(carrierParam, 1);
			if (keyAccess.Type != typeof(TKey))
				keyAccess = Expression.Convert(keyAccess, typeof(TKey));
			var keyExtractor = Expression.Lambda<Func<TCarrier, TKey>>(keyAccess, carrierParam).CompileExpression();

			// Build detail extractors per branch — reconstruct detail from carrier VT slots
			var detailExtractors = new Func<TCarrier, object>[branches.Length];

			for (int b = 0; b < branches.Length; b++)
			{
				var branch = branches[b];
				var cp     = Expression.Parameter(typeof(TCarrier), "vt");

				// Reconstruct using builtDetailExpr: replace each SqlPlaceholderExpression
				// with the corresponding carrier VT field access
				var placeholderToSlot = new Dictionary<SqlPlaceholderExpression, int>(branch.Placeholders.Count);
				for (int c = 0; c < branch.Placeholders.Count; c++)
					placeholderToSlot[branch.Placeholders[c]] = slotMaps[b][c];

				var reconstructed = branch.BuiltDetailExpr.Transform(
					(placeholderToSlot, cp),
					static (ctx, e) =>
					{
						if (e is SqlPlaceholderExpression spe && ctx.placeholderToSlot.TryGetValue(spe, out var slotIdx))
						{
							var access = AccessValueTupleField(ctx.cp, slotIdx);

							if (access.Type != spe.ConvertType)
								access = Expression.Convert(access, spe.ConvertType);

							return access;
						}

						return e;
					});

				// Finalize SqlGenericConstructorExpression nodes into compilable MemberInit/New
				reconstructed = FinalizeConstructors(branch.DetailContext, reconstructed);

				if (reconstructed.Type != branch.DetailType)
					reconstructed = Expression.Convert(reconstructed, branch.DetailType);

				detailExtractors[b] = Expression.Lambda<Func<TCarrier, object>>(
					Expression.Convert(reconstructed, typeof(object)), cp).CompileExpression();
			}

			// Create preamble
			var idx      = preambles.Count;
			var preamble = new CteUnionPreamble<TKey, TCarrier>(query, setIdExtractor, keyExtractor, detailExtractors, branches.Length);
			preambles.Add(preamble);

			// Build result expressions for each branch
			var results = new Dictionary<Expression, Expression>(ExpressionEqualityComparer.Instance);

			for (int b = 0; b < branches.Length; b++)
			{
				var branch     = branches[b];
				var detailType = branch.DetailType;

				// Access: ((PreambleResult<TKey, object>)((object?[])preambles[idx])[b]).GetList(key)
				// Then cast each element to detailType
				var preambleResultType = typeof(PreambleResult<,>).MakeGenericType(typeof(TKey), typeof(object));
				var getListMethod      = preambleResultType.GetMethod("GetList")!;

				Expression preambleAccess = Expression.Convert(
					Expression.ArrayIndex(
						Expression.Convert(
							Expression.ArrayIndex(PreambleParam, ExpressionInstances.Int32(idx)),
							typeof(object?[])),
						ExpressionInstances.Int32(b)),
					preambleResultType);

				Expression resultExpr = Expression.Call(preambleAccess, getListMethod, keyExpression);

				// Cast List<object> to List<DetailType> via Select + Cast
				var objParam = Expression.Parameter(typeof(object), "o");
				var castLambda = Expression.Lambda(Expression.Convert(objParam, detailType), objParam);
				resultExpr = Expression.Call(
					typeof(Enumerable), nameof(Enumerable.Select),
					new[] { typeof(object), detailType },
					resultExpr, castLambda);

				// ToList() to match expected List<T> type
				resultExpr = Expression.Call(
					typeof(Enumerable), nameof(Enumerable.ToList),
					new[] { detailType },
					resultExpr);

				if (branch.OrderBy != null)
					resultExpr = ApplyEnumerableOrderBy(resultExpr, branch.OrderBy);

				resultExpr = SqlAdjustTypeExpression.AdjustType(resultExpr, branch.EagerLoad.Type, MappingSchema);

				results[branch.EagerLoad.SequenceExpression] = resultExpr;
			}

			return results;
		}

		sealed class CteUnionBranch
		{
			public SqlEagerLoadExpression              EagerLoad          = null!;
			public Expression                          ExpandedSequence   = null!;
			public Expression?                         ExpandedPredicate;
			public Expression                          BuiltDetailExpr    = null!;
			public IBuildContext                        DetailContext      = null!;
			public Type                                DetailType         = null!;
			public Type                                KeyType            = null!;
			public Expression                          MainKeyExpression  = null!;
			public Expression[]                        MainKeys           = null!;
			public List<SqlPlaceholderExpression>       Placeholders       = null!;
			public List<(LambdaExpression, bool)>?     OrderBy;
		}

		/// <summary>
		/// Remaps an expression through the CTE reference map: replaces parent references with
		/// CTE field access paths, then substitutes the dummy CTE parameter with the actual branch reference.
		/// </summary>
		static Expression RetargetThroughCteMap(
			Expression                                expression,
			Dictionary<Expression, Expression>        cteRefMap,
			ParameterExpression                       dummyCteParam,
			ContextRefExpression                      branchRef)
		{
			return expression.Transform(
				(cteRefMap, dummyCteParam, branchRef),
				static (ctx, e) =>
				{
					if (ctx.cteRefMap.TryGetValue(e, out var mapped))
					{
						return mapped.Transform(
							(ctx.dummyCteParam, ctx.branchRef),
							static (ctx2, inner) => inner == ctx2.dummyCteParam ? ctx2.branchRef : inner);
					}

					return e;
				});
		}

		sealed class CteUnionPreamble<TKey, TCarrier> : Preamble
			where TKey : notnull
		{
			readonly Query<TCarrier>            _query;
			readonly Func<TCarrier, int>        _getSetId;
			readonly Func<TCarrier, TKey>       _getKey;
			readonly Func<TCarrier, object>[]   _detailExtractors;
			readonly int                        _branchCount;

			public CteUnionPreamble(
				Query<TCarrier>           query,
				Func<TCarrier, int>       getSetId,
				Func<TCarrier, TKey>      getKey,
				Func<TCarrier, object>[]  detailExtractors,
				int                       branchCount)
			{
				_query            = query;
				_getSetId         = getSetId;
				_getKey           = getKey;
				_detailExtractors = detailExtractors;
				_branchCount      = branchCount;
			}

			public override object Execute(IDataContext dataContext, IQueryExpressions expressions, object?[]? parameters, object?[]? preambles)
			{
				var results = new object?[_branchCount];
				for (int i = 0; i < _branchCount; i++)
					results[i] = new PreambleResult<TKey, object>();

				foreach (var carrier in _query.GetResultEnumerable(dataContext, expressions, parameters, preambles))
				{
					var setId = _getSetId(carrier);
					if (setId >= 0 && setId < _branchCount)
					{
						var key    = _getKey(carrier);
						var detail = _detailExtractors[setId](carrier);
						((PreambleResult<TKey, object>)results[setId]!).Add(key, detail);
					}
				}

				return results;
			}

			public override async Task<object> ExecuteAsync(IDataContext dataContext, IQueryExpressions expressions, object?[]? parameters, object[]? preambles,
				CancellationToken cancellationToken)
			{
				var results = new object?[_branchCount];
				for (int i = 0; i < _branchCount; i++)
					results[i] = new PreambleResult<TKey, object>();

				var enumerator = _query.GetResultEnumerable(dataContext, expressions, parameters, preambles)
					.GetAsyncEnumerator(cancellationToken);

				while (await enumerator.MoveNextAsync().ConfigureAwait(false))
				{
					var carrier = enumerator.Current;
					var setId   = _getSetId(carrier);
					if (setId >= 0 && setId < _branchCount)
					{
						var key    = _getKey(carrier);
						var detail = _detailExtractors[setId](carrier);
						((PreambleResult<TKey, object>)results[setId]!).Add(key, detail);
					}
				}

				return results;
			}

			public override void GetUsedParametersAndValues(ICollection<SqlParameter> parameters, ICollection<SqlValue> values)
			{
				foreach (var query in _query.Queries)
					QueryHelper.CollectParametersAndValues(query.Statement, parameters, values);
			}
		}

		/// <summary>
		/// Fallback: processes a single CteUnion eager load individually (like Default strategy).
		/// Used when batch processing is not possible (single eager load or mixed key types).
		/// </summary>
		Expression ProcessEagerLoadingCteUnion(
			IBuildContext          buildContext,
			SqlEagerLoadExpression eagerLoad,
			ParameterExpression    queryParameter,
			List<Preamble>         preambles,
			Expression[]           previousKeys)
		{
			return ProcessEagerLoadingExpression(buildContext, eagerLoad, queryParameter, preambles, previousKeys);
		}
	}
}
