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

			if (cteUnionLoads.Count == 0)
				return null;

			// Nested CTE batch (previousKeys non-empty) is not supported —
			// the CTE would select ALL parent rows without correlation to the outer level.
			if (previousKeys.Length > 0)
				return null;

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

			if (branches.Count == 0)
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

			// Phase 3a: Build main expression to discover parent SQL placeholders.
			// The parent branch in the UNION ALL carries these columns (non-eager-load fields).
			var mainRef          = new ContextRefExpression(buildContext.ElementType, buildContext);
			var mainBuiltExpr    = BuildSqlExpression(buildContext, mainRef, BuildPurpose.Expression, BuildFlags.ForSetProjection);
			var mainPlaceholders = CollectDistinctPlaceholders(mainBuiltExpr, false);

			// Also collect ContextRef patterns from the main expression's SQL placeholders.
			// The placeholders reference the parent entity's columns (e.g., Department.Id, Department.Name).
			// We need these as ContextRefExpression patterns for the CTE projection.
			if (parentCtxRef != null)
			{
				var parentCtx    = parentCtxRef.BuildContext;
				var parentEntity = parentCtxRef;

				foreach (var ph in mainPlaceholders)
				{
					// Use the Path property which has ContextRefExpression-based member access
					if (ph.Path is MemberExpression me && me.Expression is ContextRefExpression)
					{
						if (parentRefSet.Add(ph.Path))
							allParentRefs.Add(ph.Path);
					}
				}
			}

			// Phase 3b: Build carrier type with slot reuse
			// Slots 0=setId, 1=key. Data slots start at 2.
			var slotTypes = new List<Type> { typeof(int), firstKeyType };

			// For each branch, slotMap[b][c] = carrier slot index for column c of branch b
			var slotMaps = new int[branches.Count + 1][]; // +1 for parent branch

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

			// Allocate parent slots (for parent branch in UNION ALL)
			var parentSetId  = branches.Count; // parent is the LAST setId
			var parentSlotMap = new int[mainPlaceholders.Count];
			slotMaps[branches.Count] = parentSlotMap;

			for (int c = 0; c < mainPlaceholders.Count; c++)
			{
				var colType = mainPlaceholders[c].ConvertType;
				if (colType.IsValueType && Nullable.GetUnderlyingType(colType) == null)
					colType = typeof(Nullable<>).MakeGenericType(colType);

				parentSlotMap[c] = slotTypes.Count;
				slotTypes.Add(colType);
			}

			var maxColumns = DataContext.SqlProviderFlags.MaxColumnCount;
			if (maxColumns > 0 && slotTypes.Count > maxColumns)
				return null;

			var carrierTypes = slotTypes.ToArray();
			var carrierType  = BuildValueTupleType(carrierTypes);

			// Phase 4: Build entity-type CTE (no Select wrapper — fields register lazily)
			// Clone the actual parent entity context for the CTE body
			var cloningContext  = new CloningContext();
			var parentBuildCtx  = parentCtxRef?.BuildContext ?? buildContext;
			var cteSourceCtx    = cloningContext.CloneContext(parentBuildCtx);
			var mainExpression  = new ContextRefExpression(typeof(IQueryable<>).MakeGenericType(mainType), cteSourceCtx);

			var cteType = mainType; // CTE has entity type — fields register lazily as accessed

			var mainCteExpression = Expression.Call(
				Methods.LinqToDB.AsCte.MakeGenericMethod(cteType), mainExpression);

			// Build CTE ref mapping with dummy parameter:
			// parentRef → dummyCteParam.Member (same member access on CTE element)
			// Then per-branch: swap dummyCteParam → ContextRefExpression(branchCtx)
			var dummyCteParam = Expression.Parameter(cteType, "cte_dummy");
			var cteRefMap     = new Dictionary<Expression, Expression>(ExpressionEqualityComparer.Instance);
			for (int i = 0; i < allParentRefs.Count; i++)
			{
				var dep = allParentRefs[i];
				if (dep is MemberExpression me && me.Expression is ContextRefExpression)
				{
					var member = mainType.GetProperty(me.Member.Name) ?? (MemberInfo?)mainType.GetField(me.Member.Name);
					if (member == null)
						return null;
					cteRefMap[dep] = Expression.MakeMemberAccess(dummyCteParam, member);
				}
				else if (dep is ContextRefExpression)
				{
					cteRefMap[dep] = dummyCteParam;
				}
				else
				{
					cteRefMap[dep] = dep.Transform(
						dummyCteParam,
						static (param, e) => e is ContextRefExpression ? param : e);
				}
			}

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

			// Phase 5b: Add parent branch (setId = parentSetId, LAST in UNION ALL)
			// Only enable parent-in-UNION when ALL main placeholders have simple column paths
			// (MemberExpression on ContextRefExpression). Correlated subqueries, scalar functions,
			// etc. can't be projected into the CTE carrier.
			var useParentBranch = mainPlaceholders.Count > 0
				&& mainPlaceholders.All(ph => ph.Path is MemberExpression { Expression: ContextRefExpression });
			if (!useParentBranch)
				parentSetId = -1; // No parent branch
			// Parent branch: cte.Select(kd => new Carrier(parentSetId, key, ..., parentCol1, parentCol2, ...))
			if (useParentBranch)
			{
				var parentBranchCtx = BuildSequence(new BuildInfo((IBuildContext?)null, mainCteExpression, new SelectQuery()));
				var parentBranchRef = new ContextRefExpression(cteType, parentBranchCtx);

				var parentParam = Expression.Parameter(cteType, "p");
				var parentArgs  = new Expression[carrierTypes.Length];
				parentArgs[0] = Expression.Constant(parentSetId);

				// Key from CTE
				var remappedKey = branches[0].MainKeys.Length == 1
					? branches[0].MainKeys[0]
					: GenerateKeyExpression(branches[0].MainKeys, 0);

				if (cteRefMap.TryGetValue(remappedKey, out var mappedKey) || branches[0].MainKeys.Length == 1 && cteRefMap.TryGetValue(branches[0].MainKeys[0], out mappedKey))
				{
					remappedKey = mappedKey.Transform(
						(dummyCteParam, parentBranchRef),
						static (ctx, inner) => inner == ctx.dummyCteParam ? ctx.parentBranchRef : inner);
				}
				parentArgs[1] = remappedKey;

				// Fill all non-key slots with defaults
				for (int s = 2; s < parentArgs.Length; s++)
					parentArgs[s] = Expression.Default(carrierTypes[s]);

				// Fill parent slots from CTE columns — access entity members directly
				for (int c = 0; c < mainPlaceholders.Count; c++)
				{
					var slotIdx = parentSlotMap[c];
					var ph      = mainPlaceholders[c];

					// Find the matching CTE ref map entry via the placeholder's Path
					if (ph.Path is MemberExpression me && me.Expression is ContextRefExpression
						&& cteRefMap.TryGetValue(ph.Path, out var mappedPath))
					{
						// Swap dummyCteParam → parentParam
						var cteAccess = mappedPath.Transform(
							(dummyCteParam, parentParam),
							static (ctx, inner) => inner == ctx.dummyCteParam ? ctx.parentParam : inner);

						if (cteAccess.Type != carrierTypes[slotIdx])
							cteAccess = Expression.Convert(cteAccess, carrierTypes[slotIdx]);
						parentArgs[slotIdx] = cteAccess;
					}
				}

				var parentCarrierNew = BuildValueTupleNew(carrierType, parentArgs);
				var parentSelectLambda = Expression.Lambda(parentCarrierNew, parentParam);

				var parentBranchQuery = Expression.Call(
					Methods.Queryable.Select.MakeGenericMethod(cteType, carrierType),
					new ContextRefExpression(typeof(IQueryable<>).MakeGenericType(cteType), parentBranchCtx),
					Expression.Quote(parentSelectLambda));

				concatExpr = Expression.Call(Methods.Queryable.Concat.MakeGenericMethod(carrierType), concatExpr!, parentBranchQuery);
			}

			// Phase 6: Build UNION ALL combined sequence
			var combinedSequence = BuildSequence(new BuildInfo((IBuildContext?)null, concatExpr,
				new SelectQuery()));

			// Force all carrier fields into the UNION ALL's SELECT columns.
			// Without this, the SetOperationBuilder only registers columns from the first branch,
			// and the optimizer strips parent-only columns (NULL in child branches).
		// (Column forcing moved to Phase 2 — SetupCteUnionQuery)

			_buildVisitor = saveVisitor;

			if (useParentBranch)
			{
				// Phase 7a: Single-query mode — store UNION ALL info for Phase 2 (applied in BuildQuery).
				_cteUnionInfo = new CteUnionPhase2Info
				{
					CombinedSequence   = combinedSequence,
					KeyType            = firstKeyType,
					CarrierType        = carrierType,
					CarrierTypes       = carrierTypes,
					Branches           = branches.ToArray(),
					SlotMaps           = slotMaps,
					ParentSetId        = parentSetId,
					ParentSlotMap      = parentSlotMap,
					MainPlaceholders   = mainPlaceholders,
					MainBuiltExpr      = mainBuiltExpr,
				};
				_hasCteUnionQuery = true;
			}

			// Phase 7b: Build result expressions
			var results = new Dictionary<Expression, Expression>(ExpressionEqualityComparer.Instance);

			if (useParentBranch)
			{
				// Single-query mode: PreambleResult access is resolved at runtime in Phase 2
				for (int b = 0; b < branches.Count; b++)
				{
					var branch     = branches[b];
					var detailType = branch.DetailType;

					Expression keyExpr = branch.MainKeys.Length == 1
						? branch.MainKeys[0]
						: GenerateKeyExpression(branch.MainKeys, 0);

					var preambleResultType = typeof(PreambleResult<,>).MakeGenericType(firstKeyType, typeof(object));
					var getListMethod      = preambleResultType.GetMethod(nameof(PreambleResult<int, object>.GetList))!;

					Expression preambleAccess = Expression.Convert(
						Expression.ArrayIndex(
							Expression.Convert(
								Expression.ArrayIndex(PreambleParam, ExpressionInstances.Int32(preambles.Count)),
								typeof(object?[])),
							ExpressionInstances.Int32(b)),
						preambleResultType);

					Expression resultExpr = Expression.Call(preambleAccess, getListMethod, keyExpr);

					var objParam = Expression.Parameter(typeof(object), "o");
					var castLambda = Expression.Lambda(Expression.Convert(objParam, detailType), objParam);
					resultExpr = Expression.Call(
						typeof(Enumerable), nameof(Enumerable.Select),
						new[] { typeof(object), detailType },
						resultExpr, castLambda);

					resultExpr = Expression.Call(
						typeof(Enumerable), nameof(Enumerable.ToList),
						new[] { detailType },
						resultExpr);

					if (branch.OrderBy != null)
						resultExpr = ApplyEnumerableOrderBy(resultExpr, branch.OrderBy);

					resultExpr = SqlAdjustTypeExpression.AdjustType(resultExpr, branch.EagerLoad.Type, MappingSchema);
					results[branch.EagerLoad.SequenceExpression] = resultExpr;
				}

				preambles.Add(new CteUnionPlaceholderPreamble());
			}
			else
			{
				// Preamble mode: create CteUnionPreamble that executes the UNION ALL as a child query
				var result = (Dictionary<Expression, Expression>)_buildCteUnionPreambleMethodInfo
					.MakeGenericMethod(firstKeyType, carrierType)
					.InvokeExt<object>(this, new object?[]
					{
						combinedSequence, branches[0].MainKeyExpression, queryParameter, preambles,
						branches.ToArray(), slotMaps, carrierTypes,
					})!;

				foreach (var kvp in result)
					results[kvp.Key] = kvp.Value;
			}

			return results;
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

		/// <summary>
		/// Phase 2: Called from BuildQuery. Replaces the main query's GetResultEnumerable
		/// with a streaming iterator over the UNION ALL result.
		/// </summary>
		void SetRunQueryWithCteUnion<T>(
			Query<T>            query,
			IBuildContext       sequence,
			Expression          finalized,
			List<Preamble>      preambles,
			int                 preambleStartIndex,
			ParameterExpression queryParameter)
		{
			var info = _cteUnionInfo!;

			_setupCteUnionQueryMethodInfo
				.MakeGenericMethod(typeof(T), info.KeyType, info.CarrierType)
				.InvokeExt(this, new object[]
				{
					query, sequence, finalized, preambles, preambleStartIndex, info, queryParameter,
				});
		}

		static readonly MethodInfo _setupCteUnionQueryMethodInfo =
			typeof(ExpressionBuilder).GetMethod(nameof(SetupCteUnionQuery), BindingFlags.Instance | BindingFlags.NonPublic)
			?? throw new InvalidOperationException();

		void SetupCteUnionQuery<T, TKey, TCarrier>(
			Query<T>            query,
			IBuildContext       sequence,
			Expression          finalized,
			List<Preamble>      preambles,
			int                 preambleStartIndex,
			CteUnionPhase2Info  info,
			ParameterExpression queryParameter)
			where TKey : notnull
		{
			var branches     = info.Branches;
			var carrierTypes = info.CarrierTypes;

			// 1. Build UNION ALL query
			// Clear the flag BEFORE BuildQuery to prevent re-entrant ProcessCteUnionBatch
			_hasCteUnionQuery = false;
			_cteUnionInfo     = null;

			var unionQuery = new Query<TCarrier>(DataContext);
			unionQuery.Init(info.CombinedSequence);
			unionQuery.SetParametersAccessors(_parametersContext.CurrentSqlParameters.ToList());

			// Force all carrier fields into the UNION ALL's SELECT columns BEFORE BuildQuery.
			// Without this, SetOperationBuilder only registers columns from the first branch,
			// and parent-only columns get stripped by the optimizer.
			{
				var combinedRef = new ContextRefExpression(typeof(TCarrier), info.CombinedSequence);
				for (int f = 0; f < info.CarrierTypes.Length; f++)
				{
					var fieldAccess = AccessValueTupleField(combinedRef, f);
					var built = BuildSqlExpression(info.CombinedSequence, fieldAccess, BuildPurpose.Sql, BuildFlags.ForKeys);
					if (built is SqlPlaceholderExpression ph)
						ToColumns(info.CombinedSequence.SelectQuery, ph);
				}
			}

			// Build the UNION ALL mapper
			if (!BuildQuery(unionQuery, info.CombinedSequence, queryParameter, ref preambles!, []))
				throw new LinqToDBException("Failed to build CteUnion combined query.");

			// 2. Build carrier extractors
			var carrierParam   = Expression.Parameter(typeof(TCarrier), "vt");
			var setIdAccess    = AccessValueTupleField(carrierParam, 0);
			var setIdExtractor = Expression.Lambda<Func<TCarrier, int>>(setIdAccess, carrierParam).CompileExpression();

			var keyAccess = AccessValueTupleField(carrierParam, 1);
			if (keyAccess.Type != typeof(TKey))
				keyAccess = Expression.Convert(keyAccess, typeof(TKey));
			var keyExtractor = Expression.Lambda<Func<TCarrier, TKey>>(keyAccess, carrierParam).CompileExpression();

			// 3. Build detail extractors per child branch
			var detailExtractors = new Func<TCarrier, object>[branches.Length];

			for (int b = 0; b < branches.Length; b++)
			{
				var branch = branches[b];
				var cp     = Expression.Parameter(typeof(TCarrier), "vt");

				var placeholderToSlot = new Dictionary<SqlPlaceholderExpression, int>(branch.Placeholders.Count);
				for (int c = 0; c < branch.Placeholders.Count; c++)
					placeholderToSlot[branch.Placeholders[c]] = info.SlotMaps[b][c];

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

				reconstructed = FinalizeConstructors(branch.DetailContext, reconstructed);

				if (reconstructed.Type != branch.DetailType)
					reconstructed = Expression.Convert(reconstructed, branch.DetailType);

				detailExtractors[b] = Expression.Lambda<Func<TCarrier, object>>(
					Expression.Convert(reconstructed, typeof(object)), cp).CompileExpression();
			}

			// 4. Build parent row reconstruction: replace SqlPlaceholderExpressions in the main
			//    expression with carrier slot access, replace PreambleResult access with runtime lookups
			var parentCarrierParam = Expression.Parameter(typeof(TCarrier), "pvt");

			// Build path-to-slot mapping for parent placeholders
			var pathToSlot = new Dictionary<Expression, int>(ExpressionEqualityComparer.Instance);
			for (int i = 0; i < info.MainPlaceholders.Count; i++)
			{
				var ph = info.MainPlaceholders[i];
				if (ph.Path != null)
					pathToSlot[ph.Path] = info.ParentSlotMap[i];
			}

			// Map finalized SqlPlaceholderExpressions to parent carrier slots via Path matching
			var parentReconstructed = finalized.Transform(
				(pathToSlot, parentCarrierParam),
				static (ctx, e) =>
				{
					if (e is SqlPlaceholderExpression spe && spe.Path != null
						&& ctx.pathToSlot.TryGetValue(spe.Path, out var slotIdx))
					{
						var access = AccessValueTupleField(ctx.parentCarrierParam, slotIdx);
						if (access.Type != spe.ConvertType)
							access = Expression.Convert(access, spe.ConvertType);
						return access;
					}

					return e;
				});

			// Replace PreambleParam with a closure variable (populated at runtime)
			var preambleArrayVar = Expression.Variable(typeof(object?[]), "preambleArray");
			parentReconstructed = parentReconstructed.Transform(
				preambleArrayVar,
				static (ctx, e) => e == PreambleParam ? ctx : e);

			var parentMapper = Expression.Lambda<Func<TCarrier, object?[], T>>(
				parentReconstructed, parentCarrierParam, preambleArrayVar).CompileExpression();

			// 5. Replace GetResultEnumerable with UNION ALL-based iterator.
			// The UNION ALL carries both child and parent rows. We buffer all rows,
			// populate PreambleResults from child rows, then yield parent rows.
			var preambleIdx0   = preambleStartIndex;
			var branchCount0   = branches.Length;
			var parentSetId0   = info.ParentSetId;

			// First, set the normal mapper so the query infrastructure is wired
			sequence.SetRunQuery(query, finalized);

			// Then override GetResultEnumerable
			query.GetResultEnumerable = (db, expr, ps, preambleResults) =>
			{
				// Create PreambleResults for child branches
				var childResults = new object?[branchCount0];
				for (int i = 0; i < branchCount0; i++)
					childResults[i] = new PreambleResult<TKey, object>();

				// Store in the preambles array so PreambleResult.GetList calls work
				if (preambleResults != null && preambleIdx0 < preambleResults.Length)
					preambleResults[preambleIdx0] = childResults;

				// Execute the UNION ALL query and buffer all rows
				var carriers = unionQuery.GetResultEnumerable(db, expr, ps, preambleResults).ToList();

				// First pass: populate PreambleResults from child rows
				foreach (var carrier in carriers)
				{
					var setId = setIdExtractor(carrier);
					if (setId >= 0 && setId < branchCount0)
					{
						var key    = keyExtractor(carrier);
						var detail = detailExtractors[setId](carrier);
						((PreambleResult<TKey, object>)childResults[setId]!).Add(key, detail);
					}
				}

				// Second pass: yield parent rows (reconstructed with PreambleResults)
				return new CteUnionResultEnumerable<T, TCarrier>(
					carriers, setIdExtractor, parentSetId0, parentMapper, preambleResults!);
			};

			// Override GetElement for FirstOrDefault/Single
			query.GetElement = (db, expr, ps, preambleResults) =>
			{
				var childResults = new object?[branchCount0];
				for (int i = 0; i < branchCount0; i++)
					childResults[i] = new PreambleResult<TKey, object>();

				if (preambleResults != null && preambleIdx0 < preambleResults.Length)
					preambleResults[preambleIdx0] = childResults;

				var carriers = unionQuery.GetResultEnumerable(db, expr, ps, preambleResults).ToList();

				foreach (var carrier in carriers)
				{
					var setId = setIdExtractor(carrier);
					if (setId >= 0 && setId < branchCount0)
					{
						var key    = keyExtractor(carrier);
						var detail = detailExtractors[setId](carrier);
						((PreambleResult<TKey, object>)childResults[setId]!).Add(key, detail);
					}
				}

				// Return first parent row
				foreach (var carrier in carriers)
				{
					if (setIdExtractor(carrier) == parentSetId0)
						return parentMapper(carrier, preambleResults!);
				}

				return default;
			};
		}

		sealed class CteUnionResultEnumerable<T, TCarrier> : IResultEnumerable<T>
		{
			readonly List<TCarrier>       _carriers;
			readonly Func<TCarrier, int>  _getSetId;
			readonly int                  _parentSetId;
			readonly Func<TCarrier, object?[], T> _parentMapper;
			readonly object?[]            _preambleResults;

			public CteUnionResultEnumerable(
				List<TCarrier> carriers,
				Func<TCarrier, int> getSetId,
				int parentSetId,
				Func<TCarrier, object?[], T> parentMapper,
				object?[] preambleResults)
			{
				_carriers        = carriers;
				_getSetId        = getSetId;
				_parentSetId     = parentSetId;
				_parentMapper    = parentMapper;
				_preambleResults = preambleResults;
			}

			public IEnumerator<T> GetEnumerator()
			{
				foreach (var carrier in _carriers)
				{
					if (_getSetId(carrier) == _parentSetId)
						yield return _parentMapper(carrier, _preambleResults);
				}
			}

			System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

			public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
			{
				return new AsyncEnumeratorWrapper<T>(GetEnumerator(), cancellationToken);
			}
		}

		sealed class AsyncEnumeratorWrapper<T> : IAsyncEnumerator<T>
		{
			readonly IEnumerator<T>    _inner;
			readonly CancellationToken _ct;

			public AsyncEnumeratorWrapper(IEnumerator<T> inner, CancellationToken ct)
			{
				_inner = inner;
				_ct    = ct;
			}

			public T Current => _inner.Current;

			public ValueTask<bool> MoveNextAsync()
			{
				_ct.ThrowIfCancellationRequested();
				return new ValueTask<bool>(_inner.MoveNext());
			}

			public ValueTask DisposeAsync()
			{
				_inner.Dispose();
				return default;
			}
		}

		// Fields for Phase 2 (applied in BuildQuery)
		bool             _hasCteUnionQuery;
		CteUnionPhase2Info? _cteUnionInfo;

		sealed class CteUnionPhase2Info
		{
			public IBuildContext                     CombinedSequence   = null!;
			public Type                              KeyType            = null!;
			public Type                              CarrierType        = null!;
			public Type[]                            CarrierTypes       = null!;
			public CteUnionBranch[]                  Branches           = null!;
			public int[][]                           SlotMaps           = null!;
			public int                               ParentSetId;
			public int[]                             ParentSlotMap      = null!;
			public List<SqlPlaceholderExpression>     MainPlaceholders   = null!;
			public Expression                        MainBuiltExpr      = null!;
		}

		/// <summary>Placeholder preamble that reserves a slot. Phase 2 fills it at runtime.</summary>
		sealed class CteUnionPlaceholderPreamble : Preamble
		{
			public override object Execute(IDataContext dataContext, IQueryExpressions expressions, object?[]? parameters, object?[]? preambles)
				=> Array.Empty<object?>();

			public override Task<object> ExecuteAsync(IDataContext dataContext, IQueryExpressions expressions, object?[]? parameters, object[]? preambles, CancellationToken cancellationToken)
				=> Task.FromResult<object>(Array.Empty<object?>());

			public override void GetUsedParametersAndValues(ICollection<SqlParameter> parameters, ICollection<SqlValue> values) { }
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
