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

			expression.Visit((cteUnionLoads, builder: this, buildContext), static (ctx, e) =>
			{
				if (e.NodeType == ExpressionType.Extension && e is SqlEagerLoadExpression eagerLoad
					&& ctx.builder.ResolveStrategy(eagerLoad, ctx.buildContext) == EagerLoadingStrategy.CteUnion)
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

				// Add sequence dependencies to the CTE projection set (parent refs only)
				foreach (var dep in dependencies)
				{
					allDependencies.Add(dep);
					if (parentRefSet.Add(dep))
						allParentRefs.Add(dep);
				}

				// Expand predicate — collect its parent refs separately
				// (predicate may contain child entity refs like d.IsActive that shouldn't be in the CTE key)
				Expression? expandedPredicate = null;
				if (eagerLoad.Predicate != null)
				{
					expandedPredicate = ExpandContexts(buildContext, eagerLoad.Predicate);
					var predicateDeps = new HashSet<Expression>(ExpressionEqualityComparer.Instance);
					CollectDependencies(buildContext, expandedPredicate, predicateDeps);

					foreach (var dep in predicateDeps)
					{
						dependencies.Add(dep);
						allDependencies.Add(dep);
					}
				}

				Expression mainKeyExpression = mainKeys.Length == 1
					? mainKeys[0]
					: GenerateKeyExpression(mainKeys, 0);

				// Build detail sequence to discover actual SQL placeholders.
				// This handles complex projections (Select(d => new { ... })), not just entities.
				var detailCtx    = BuildSequence(new BuildInfo((IBuildContext?)null, expandedSequence, new SelectQuery()));
				var detailRef    = new ContextRefExpression(detailType, detailCtx);
				var builtDetail  = BuildSqlExpression(detailCtx, detailRef, BuildPurpose.Expression, BuildFlags.ForSetProjection);

				// If detail contains nested eager loads, bail out — can't batch them in CteUnion yet
				if (builtDetail.Find(0, static (_, e) => e is SqlEagerLoadExpression) != null)
					return null;

				var placeholders = CollectDistinctPlaceholders(builtDetail, false);

				if (placeholders.Count == 0)
					continue;

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

			// SqlPlaceholderExpressions in allParentRefs come from Concat/SetOperations.
			// They reference the SetOperation's SQL fields directly.
			// They are added to KDE keys and remapped to CTE later.

			// Verify all branches share the same key type
			var firstKeyType = branches[0].KeyType;
			if (branches.Exists(b => b.KeyType != firstKeyType))
				return null;

			// Phase 3a: Build main expression to discover parent SQL placeholders.
			// The parent branch in the UNION ALL carries these columns (non-eager-load fields).
			var mainRef          = new ContextRefExpression(buildContext.ElementType, buildContext);
			var mainBuiltExpr    = BuildSqlExpression(buildContext, mainRef, BuildPurpose.Expression, BuildFlags.ForSetProjection);
			var mainPlaceholders = CollectDistinctPlaceholders(mainBuiltExpr, false);

			// Also collect parent column references from the main expression's SQL placeholders.
			// These are projected into the CTE Key so the parent branch can carry them in the UNION ALL.
			if (parentCtxRef != null)
			{
				foreach (var ph in mainPlaceholders)
				{
					// Use the Path property which has ContextRefExpression-based member access.
					// Normalize to parentCtxRef so the path matches the form used by CollectDependencies,
					// preventing duplicate entries when the projection context differs from the table context.
					if (ph.Path is MemberExpression me && me.Expression is ContextRefExpression)
					{
						var normalizedPath = me.Expression.Equals(parentCtxRef)
							? ph.Path
							: Expression.MakeMemberAccess(parentCtxRef, me.Member);
						if (parentRefSet.Add(normalizedPath))
							allParentRefs.Add(normalizedPath);
					}
				}
			}
			else
			{
				// SetOperation / Concat case: placeholders have SqlPathExpression paths, not ContextRef members.
				// Add the SqlPlaceholderExpressions themselves as parent refs so they're projected into KDE Key.
				// Skip internal SetOperation placeholders (filter conditions, comparisons) — only include
				// actual projected columns (SqlPathExpression paths that reference constructor members).
				foreach (var ph in mainPlaceholders)
				{
					if (ph.Path is not SqlPathExpression)
						continue;
					if (parentRefSet.Add(ph))
						allParentRefs.Add(ph);
				}
			}

			// Build carrier key type from allParentRefs (full key ensures uniqueness across Concat branches)
			var carrierKeyTypes = allParentRefs.Select(r => r.Type).ToArray();
			var carrierKeyType  = carrierKeyTypes.Length == 1 ? carrierKeyTypes[0] : BuildValueTupleType(carrierKeyTypes);

			// Phase 3b: Build carrier type with slot reuse
			// Slots 0=setId, 1=key. Data slots start at 2.
			var slotTypes = new List<Type> { typeof(int), carrierKeyType };

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

			// Phase 4: Build CTE with KeyDetailEnvelope projection
			// Key = ValueTuple(allParentRefs...) — all parent-referencing expressions
			// Data = x (the source entity/row)
			var cloningContext  = new CloningContext();
			var parentBuildCtx  = parentCtxRef?.BuildContext ?? buildContext;
			var cteSourceCtx    = cloningContext.CloneContext(parentBuildCtx);
			var sourceType      = cteSourceCtx.ElementType;
			var mainExpression  = new ContextRefExpression(typeof(IQueryable<>).MakeGenericType(sourceType), cteSourceCtx);

			// Build Key type from allParentRefs
			var keyTypes = allParentRefs.Select(r => r.Type).ToArray();
			var cteKeyType = keyTypes.Length == 1 ? keyTypes[0] : BuildValueTupleType(keyTypes);

			var kdeType      = typeof(KeyDetailEnvelope<,>).MakeGenericType(cteKeyType, sourceType);
			var kdeKeyField  = kdeType.GetField(nameof(KeyDetailEnvelope<int, object>.Key))!;
			var kdeDataField = kdeType.GetField(nameof(KeyDetailEnvelope<int, object>.Detail))!;

			var cteType = kdeType;

			// Build Select lambda: cte_x => new KDE { Key = VT(ref1, ref2, ...), Data = cte_x }
			var selectParam = Expression.Parameter(sourceType, "cte_x");

			// Build key body: clone allParentRefs and replace ContextRefExpressions with selectParam
			var keyArgs = new Expression[allParentRefs.Count];
			for (int i = 0; i < allParentRefs.Count; i++)
			{
				var dep = allParentRefs[i];
				if (dep is MemberExpression me && me.Expression is ContextRefExpression)
				{
					var member = sourceType.GetProperty(me.Member.Name) ?? (MemberInfo?)sourceType.GetField(me.Member.Name);
					if (member != null)
						keyArgs[i] = Expression.MakeMemberAccess(selectParam, member);
					else
						keyArgs[i] = cloningContext.CloneExpression(dep);
				}
				else if (dep is ContextRefExpression)
				{
					keyArgs[i] = selectParam;
				}
				else
				{
					// SqlPlaceholderExpression or other — clone through cloningContext
					keyArgs[i] = cloningContext.CloneExpression(dep);
				}
			}

			Expression keyBody = keyArgs.Length == 1 ? keyArgs[0] : BuildValueTupleNew(cteKeyType, keyArgs);

			var kdeNew = Expression.New(
				kdeType.GetConstructor(new[] { cteKeyType, sourceType })!,
				new[] { keyBody, selectParam },
				new MemberInfo[] { kdeKeyField, kdeDataField });

			var kdeSelectExpr = Expression.Call(
				Methods.Queryable.Select.MakeGenericMethod(sourceType, kdeType),
				mainExpression, Expression.Quote(Expression.Lambda(kdeNew, selectParam)));

			var mainCteExpression = Expression.Call(
				Methods.LinqToDB.AsCte.MakeGenericMethod(cteType), kdeSelectExpr);

			// Build CTE ref mapping with dummy parameter:
			// parentRef → dummyCteParam.Key.ItemN (for key refs)
			// parentRef → dummyCteParam.Detail.Member (for entity member refs)
			var dummyCteParam = Expression.Parameter(cteType, "cte_dummy");
			var cteRefMap     = new Dictionary<Expression, Expression>(ExpressionEqualityComparer.Instance);

			Expression dummyKeyAccess = Expression.Field(dummyCteParam, kdeKeyField);
			Expression dummyDataAccess = Expression.Field(dummyCteParam, kdeDataField);

			for (int i = 0; i < allParentRefs.Count; i++)
			{
				var dep = allParentRefs[i];
				// Map to Key.ItemN (or Key directly for single-key)
				Expression keyFieldAccess = keyArgs.Length == 1
					? dummyKeyAccess
					: AccessValueTupleField(dummyKeyAccess, i);

				cteRefMap[dep] = keyFieldAccess;
			}

			// Phase 5: Build UNION ALL branches — use cloned visitor for CTE building
			var saveVisitor = _buildVisitor;
			_buildVisitor = _buildVisitor.Clone(cloningContext);
			cloningContext.UpdateContextParents();

			Expression? concatExpr = null;

			for (int b = 0; b < branches.Count; b++)
			{
				var branch          = branches[b];
				var mainParameter   = Expression.Parameter(kdeType, "kd");
				var detailParameter = Expression.Parameter(branch.DetailType, "d");

				// Build a new CteTableContext for this branch
				var branchCtx = BuildSequence(new BuildInfo((IBuildContext?)null, mainCteExpression, new SelectQuery()));
				var branchRef = new ContextRefExpression(kdeType, branchCtx);

				// Remap expanded sequence: replace parent refs with CTE field access
				var retargetedSequence = RetargetThroughCteMap(branch.ExpandedSequence, cteRefMap, dummyCteParam, branchRef);

				// Apply predicate if present — retarget parent refs through CTE, then wrap in .Where()
				// Only retarget if the predicate contains expressions from cteRefMap (parent refs).
				// Pure child predicates (like d.IsActive) should not be retargeted.
				if (branch.ExpandedPredicate != null)
				{
					var hasCteRefs = branch.ExpandedPredicate.Find(
						cteRefMap,
						static (map, e) => map.ContainsKey(e)) != null;

					var retargetedPredicate = hasCteRefs
						? RetargetThroughCteMap(branch.ExpandedPredicate, cteRefMap, dummyCteParam, branchRef)
						: branch.ExpandedPredicate;

					var childElementType = TypeHelper.GetEnumerableElementType(retargetedSequence.Type) ?? retargetedSequence.Type;
					var predParam        = Expression.Parameter(childElementType, "p_pred");
					var predicateLambda  = Expression.Lambda(
						retargetedPredicate.Transform(
							(childElementType, predParam),
							static (ctx, e) => e is ContextRefExpression cre && cre.Type == ctx.childElementType
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

				// Key from CTE — remap ALL allParentRefs through cteRefMap.
				// Using full allParentRefs ensures key uniqueness across Concat branches.
				var remappedFullKeys = new Expression[allParentRefs.Count];
				for (int k = 0; k < allParentRefs.Count; k++)
				{
					if (cteRefMap.TryGetValue(allParentRefs[k], out var mapped))
					{
						remappedFullKeys[k] = mapped.Transform(
							(dummyCteParam, branchRef),
							static (ctx, inner) => inner == ctx.dummyCteParam ? ctx.branchRef : inner);
					}
					else
					{
						remappedFullKeys[k] = allParentRefs[k];
					}
				}

				args[1] = remappedFullKeys.Length == 1
					? remappedFullKeys[0]
					: GenerateKeyExpression(remappedFullKeys, 0);

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
						// Reconstruct member access on the result selector's detail parameter.
						// When detail is a projected type (e.g., anonymous type from Select),
						// the placeholder member may be from the entity type — look up by name.
						var member = mePath.Member;
						if (member.DeclaringType != detailParameter.Type)
						{
							member = (System.Reflection.MemberInfo?)detailParameter.Type.GetProperty(member.Name)
								?? detailParameter.Type.GetField(member.Name)
								?? member;
						}
						access = Expression.MakeMemberAccess(detailParameter, member);
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
			// Check if we can embed parent data in the UNION ALL carrier (single-query mode).
			// Requires all mainPlaceholders to be resolvable from parent entity columns or CTE refs.
			// Computed placeholders (e.g., bool discriminators like Label == "ActiveOnly") are OK
			// as long as their constituent parts are in the CTE.
			var useParentBranch = mainPlaceholders.Count > 0
				&& mainPlaceholders.All(ph =>
					ph.Path is MemberExpression { Expression: ContextRefExpression }
					|| parentRefSet.Contains(ph)
					|| CanResolveFromCteMap(ph, cteRefMap));
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

				// Key from CTE — full allParentRefs key (matches child branch key)
				var parentRemappedKeys = new Expression[allParentRefs.Count];
				for (int k = 0; k < allParentRefs.Count; k++)
				{
					if (cteRefMap.TryGetValue(allParentRefs[k], out var mappedKey))
					{
						parentRemappedKeys[k] = mappedKey.Transform(
							(dummyCteParam, parentBranchRef),
							static (ctx, inner) => inner == ctx.dummyCteParam ? ctx.parentBranchRef : inner);
					}
					else
					{
						parentRemappedKeys[k] = allParentRefs[k];
					}
				}
				parentArgs[1] = parentRemappedKeys.Length == 1
					? parentRemappedKeys[0]
					: GenerateKeyExpression(parentRemappedKeys, 0);

				// Fill all non-key slots with defaults
				for (int s = 2; s < parentArgs.Length; s++)
					parentArgs[s] = Expression.Default(carrierTypes[s]);

				// Fill parent slots from CTE columns — access entity members directly
				for (int c = 0; c < mainPlaceholders.Count; c++)
				{
					var slotIdx = parentSlotMap[c];
					var ph      = mainPlaceholders[c];

					// Find the matching CTE ref map entry via the placeholder's Path or the placeholder itself.
					Expression? mappedPath = null;

					if (ph.Path is MemberExpression me && me.Expression is ContextRefExpression)
					{
						// Normalize ph.Path to parentCtxRef (same normalization as Phase 3a)
						var lookupPath = me.Expression.Equals(parentCtxRef)
							? ph.Path
							: Expression.MakeMemberAccess(parentCtxRef!, me.Member);

						cteRefMap.TryGetValue(lookupPath, out mappedPath);
					}
					else
					{
						// SetOperation/Concat: placeholder itself was added to allParentRefs and cteRefMap
						cteRefMap.TryGetValue(ph, out mappedPath);
					}

					if (mappedPath != null)
					{
						// Swap dummyCteParam → parentParam
						var cteAccess = mappedPath.Transform(
							(dummyCteParam, parentParam),
							static (ctx, inner) => inner == ctx.dummyCteParam ? ctx.parentParam : inner);

						if (cteAccess.Type != carrierTypes[slotIdx])
							cteAccess = Expression.Convert(cteAccess, carrierTypes[slotIdx]);
						parentArgs[slotIdx] = cteAccess;
					}
					else if (ph.Path != null)
					{
						// Computed placeholder (e.g., Label == "ActiveOnly"): resolve nested
						// SqlPlaceholderExpressions through cteRefMap and rebuild the expression.
						var resolved = ph.Path.Transform(
							(cteRefMap, dummyCteParam, parentParam),
							static (ctx, e) =>
							{
								if (e is SqlPlaceholderExpression nested && ctx.cteRefMap.TryGetValue(nested, out var mapped))
								{
									return mapped.Transform(
										(ctx.dummyCteParam, ctx.parentParam),
										static (ctx2, inner) => inner == ctx2.dummyCteParam ? ctx2.parentParam : inner);
								}
								return e;
							});

						if (resolved.Type != carrierTypes[slotIdx])
							resolved = Expression.Convert(resolved, carrierTypes[slotIdx]);
						parentArgs[slotIdx] = resolved;
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
					KeyType            = cteKeyType,
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

					// Use full allParentRefs key to match carrier key
					Expression keyExpr = allParentRefs.Count == 1
						? allParentRefs[0]
						: GenerateKeyExpression(allParentRefs.ToArray(), 0);

					var preambleResultType = typeof(PreambleResult<,>).MakeGenericType(cteKeyType, typeof(object));
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
					.MakeGenericMethod(cteKeyType, carrierType)
					.InvokeExt<object>(this, new object?[]
					{
						combinedSequence,
							allParentRefs.Count == 1 ? allParentRefs[0] : GenerateKeyExpression(allParentRefs.ToArray(), 0),
							queryParameter, preambles,
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
			var detailExtractors = new Func<TCarrier, object?[]?, object>[branches.Length];

			for (int b = 0; b < branches.Length; b++)
			{
				var branch = branches[b];
				var cp     = Expression.Parameter(typeof(TCarrier), "vt");
				var pa     = Expression.Parameter(typeof(object?[]), "pa");

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
				reconstructed = reconstructed.Transform(pa, static (ctx, e) => e == PreambleParam ? ctx : e);

				if (reconstructed.Type != branch.DetailType)
					reconstructed = Expression.Convert(reconstructed, branch.DetailType);

				detailExtractors[b] = Expression.Lambda<Func<TCarrier, object?[]?, object>>(
					Expression.Convert(reconstructed, typeof(object)), cp, pa).CompileExpression();
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

			// 3. Build detail extractors per child branch.
			// Extractors take (carrier, preambleResults) to support nested eager loads
			// whose PreambleParam references are resolved at runtime.
			var detailExtractors = new Func<TCarrier, object?[]?, object>[branches.Length];

			for (int b = 0; b < branches.Length; b++)
			{
				var branch = branches[b];
				var cp     = Expression.Parameter(typeof(TCarrier), "vt");
				var pa     = Expression.Parameter(typeof(object?[]), "pa");

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
				reconstructed = reconstructed.Transform(pa, static (ctx, e) => e == PreambleParam ? ctx : e);

				if (reconstructed.Type != branch.DetailType)
					reconstructed = Expression.Convert(reconstructed, branch.DetailType);

				detailExtractors[b] = Expression.Lambda<Func<TCarrier, object?[]?, object>>(
					Expression.Convert(reconstructed, typeof(object)), cp, pa).CompileExpression();
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
						var detail = detailExtractors[setId](carrier, preambleResults);
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
						var detail = detailExtractors[setId](carrier, preambleResults);
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

		/// <summary>
		/// Checks whether a computed placeholder (e.g., bool discriminator like Label == "ActiveOnly")
		/// can be resolved from CTE columns. Returns true if all nested SqlPlaceholderExpressions
		/// within the placeholder's Path are present in the cteRefMap.
		/// </summary>
		static bool CanResolveFromCteMap(SqlPlaceholderExpression ph, Dictionary<Expression, Expression> cteRefMap)
		{
			if (ph.Path == null)
				return false;

			// Find any nested SqlPlaceholderExpression that is NOT in cteRefMap
			var unresolvable = ph.Path.Find(
				cteRefMap,
				static (map, e) => e is SqlPlaceholderExpression nested && !map.ContainsKey(nested));

			// Also check there's at least one nested placeholder (it's a computed expression)
			var hasNested = ph.Path.Find(
				0, static (_, e) => e is SqlPlaceholderExpression) != null;

			return hasNested && unresolvable == null;
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
			readonly Func<TCarrier, object?[]?, object>[] _detailExtractors;
			readonly int                        _branchCount;

			public CteUnionPreamble(
				Query<TCarrier>                      query,
				Func<TCarrier, int>                  getSetId,
				Func<TCarrier, TKey>                 getKey,
				Func<TCarrier, object?[]?, object>[] detailExtractors,
				int                                  branchCount)
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
						var detail = _detailExtractors[setId](carrier, preambles);
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
						var detail = _detailExtractors[setId](carrier, preambles);
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
		/// <summary>
		/// Processes a single CteUnion eager load that wasn't handled by the batch.
		/// Returns <see langword="null"/> to trigger fallback to the next strategy in the chain
		/// (<c>CteUnion → PostQuery → Default</c>).
		/// </summary>
		Expression? ProcessEagerLoadingCteUnion(
			IBuildContext          buildContext,
			SqlEagerLoadExpression eagerLoad,
			ParameterExpression    queryParameter,
			List<Preamble>         preambles,
			Expression[]           previousKeys)
		{
			return null;
		}
	}
}
