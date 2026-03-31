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
			// Phase 1: Collect ALL eager loads. If any uses CteUnion, all go through this batch.
			var cteUnionLoads = new List<SqlEagerLoadExpression>();
			var hasCteUnion   = false;

			expression.Visit((cteUnionLoads, builder: this, buildContext), static (ctx, e) =>
			{
				if (e.NodeType == ExpressionType.Extension && e is SqlEagerLoadExpression eagerLoad)
				{
					ctx.cteUnionLoads.Add(eagerLoad);
				}
			});

			if (cteUnionLoads.Count == 0)
				return null;

			foreach (var load in cteUnionLoads)
			{
				if (ResolveStrategy(load, buildContext) == EagerLoadingStrategy.CteUnion)
				{
					hasCteUnion = true;
					break;
				}
			}

			if (!hasCteUnion)
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

				var parentBranchIdx = branches.Count;
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

				// Detect nested eager loads in the detail expression and recursively
				// collect all levels as additional UNION ALL branches.
				// Uses a work queue to handle arbitrary nesting depth (e.g., Company → Dept → Emp → Task).
				var pendingNested = new Queue<(int parentIdx, Expression detail, IBuildContext ctx)>();
				pendingNested.Enqueue((parentBranchIdx, builtDetail, detailCtx));

				while (pendingNested.Count > 0)
				{
					var (curParentIdx, curDetail, curCtx) = pendingNested.Dequeue();

					var curNestedELs = new List<SqlEagerLoadExpression>();
					curDetail.Visit(curNestedELs, static (list, e) =>
					{
						if (e is SqlEagerLoadExpression el) list.Add(el);
					});

					if (curNestedELs.Count == 0)
						continue;

					var curParentBranch = branches[curParentIdx];
					curParentBranch.NestedEagerLoads ??= new List<NestedEagerLoadInfo>();

					foreach (var nestedEL in curNestedELs)
					{
						var nestedDetailType = TypeHelper.GetEnumerableElementType(nestedEL.Type);
						if (nestedDetailType == null)
						{
							return null; // Can't determine nested type, bail out
						}

						// Expand nested sequence through the parent detail context
						var nestedExpandedSeq = ExpandContexts(curCtx, nestedEL.SequenceExpression);

						// Build nested detail context
						var nestedDetailCtx   = BuildSequence(new BuildInfo((IBuildContext?)null, nestedExpandedSeq, new SelectQuery()));
						var nestedDetailRef   = new ContextRefExpression(nestedDetailType, nestedDetailCtx);
						var nestedBuiltDetail = BuildSqlExpression(nestedDetailCtx, nestedDetailRef, BuildPurpose.Expression, BuildFlags.ForSetProjection);

						var nestedPlaceholders = CollectDistinctPlaceholders(nestedBuiltDetail, false);
						if (nestedPlaceholders.Count == 0) continue;

						// Collect parent references from expanded nested sequence.
						// These are ContextRef(parentDetailCtx).Member — the nested branch's correlation keys.
						var nestedDeps = new HashSet<Expression>(ExpressionEqualityComparer.Instance);
						CollectDependencies(curCtx, nestedExpandedSeq, nestedDeps);

						if (nestedDeps.Count == 0)
							throw new LinqToDBException("CteUnion: Cannot determine nested correlation — no dependencies found.");

						// Build nested branch keys from nestedDeps
						var nestedMainKeys = new Expression[nestedDeps.Count];
						{
							int nki = 0;
							foreach (var dep in nestedDeps)
								nestedMainKeys[nki++] = dep;
						}
						Expression nestedMainKeyExpression = nestedMainKeys.Length == 1
							? nestedMainKeys[0]
							: GenerateKeyExpression(nestedMainKeys, 0);

						var nestedBranchIdx = branches.Count;
						branches.Add(new CteUnionBranch
						{
							EagerLoad              = nestedEL,
							ExpandedSequence       = nestedExpandedSeq,
							BuiltDetailExpr        = nestedBuiltDetail,
							DetailContext          = nestedDetailCtx,
							DetailType             = nestedDetailType,
							KeyType                = nestedMainKeyExpression.Type,
							MainKeyExpression      = nestedMainKeyExpression,
							MainKeys               = nestedMainKeys,
							Placeholders           = nestedPlaceholders,
							OrderBy                = CollectOrderBy(nestedEL.SequenceExpression),
							IsNested               = true,
							ParentBranchIndex      = curParentIdx,
							OriginalNestedSequence = nestedEL.SequenceExpression,
							ExpandedNestedSequence = nestedExpandedSeq,
						});

						curParentBranch.NestedEagerLoads.Add(new NestedEagerLoadInfo
						{
							EagerLoad         = nestedEL,
							NestedBranchIndex = nestedBranchIdx,
						});

						// Enqueue for further nesting detection (handles 3+ levels)
						pendingNested.Enqueue((nestedBranchIdx, nestedBuiltDetail, nestedDetailCtx));
					}
				}
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

			// Note: allParentRefs only contains actual correlation columns (from CollectDependencies).
			// Parent entity data columns are in CTE.Data — they don't need to be in the Key.

			// Build carrier key type from allParentRefs (full key ensures uniqueness across Concat branches)
			var carrierKeyTypes = allParentRefs.Select(r => r.Type).ToArray();
			var carrierKeyType  = carrierKeyTypes.Length == 1 ? carrierKeyTypes[0] : BuildValueTupleType(carrierKeyTypes);

			// Phase 3b: Build carrier type with slot reuse
			// Slots: 0=setId, 1=key, 2=RN. Data slots start at 3.
			const int DataSlotOffset = 3;
			var slotTypes = new List<Type> { typeof(int), carrierKeyType, typeof(long) };

			// For each branch, slotMap[b][c] = carrier slot index for column c of branch b
			var slotMaps = new int[branches.Count + 1][]; // +1 for parent branch

			// Track which slots are "occupied" by which branch (-1 = free)
			var slotOwners = new List<int>(); // parallel to slotTypes, starting from index DataSlotOffset

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
						if (slotOwners[s] != b && slotTypes[s + DataSlotOffset] == colType)
						{
							// Check this slot isn't already used by this branch
							var alreadyUsed = false;
							for (int pc = 0; pc < c; pc++)
							{
								if (slotMaps[b][pc] == s + DataSlotOffset)
								{
									alreadyUsed = true;
									break;
								}
							}

							if (!alreadyUsed)
							{
								reusedSlot = s + DataSlotOffset;
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

			// Phase 3c: Replace nested SqlEagerLoadExpressions in parent branches' BuiltDetailExpr
			// with runtime PreambleResult lookups. Each nested branch's CTE Key = its correlation
			// columns (e.g., Department.Id). The lookup key is the corresponding parent placeholder.
			for (int b = 0; b < branches.Count; b++)
			{
				var branch = branches[b];
				if (branch.NestedEagerLoads == null || branch.NestedEagerLoads.Count == 0)
					continue;

				foreach (var nested in branch.NestedEagerLoads)
				{
					var nestedBranch = branches[nested.NestedBranchIndex];
					var nestedSetId  = nested.NestedBranchIndex;

					// Build the lookup key from parent placeholders matching the nested branch's MainKeys.
					// E.g., nested MainKeys = [ContextRef(deptCtx).Id] → find parent placeholder for Department.Id
					var lookupKeyExprs = new List<Expression>();
					foreach (var nestedKey in nestedBranch.MainKeys)
					{
						if (nestedKey is MemberExpression me && me.Expression is ContextRefExpression)
						{
							// Find matching parent placeholder by member name
							SqlPlaceholderExpression? matchedPh = null;
							for (int pi = 0; pi < branch.Placeholders.Count; pi++)
							{
								if (branch.Placeholders[pi].Path is MemberExpression pme
									&& string.Equals(pme.Member.Name, me.Member.Name, StringComparison.Ordinal))
								{
									matchedPh = branch.Placeholders[pi];
									break;
								}
							}
							if (matchedPh != null)
								lookupKeyExprs.Add(matchedPh);
							else
								throw new LinqToDBException($"CteUnion: Cannot find parent placeholder for nested key '{me.Member.Name}'.");
						}
						else
						{
							lookupKeyExprs.Add(nestedKey);
						}
					}

					Expression lookupKey = lookupKeyExprs.Count == 1
						? lookupKeyExprs[0]
						: GenerateKeyExpression(lookupKeyExprs.ToArray(), 0);

					var preambleResultType = typeof(PreambleResult<,>).MakeGenericType(typeof(object), typeof(object));
					var getListMethod      = preambleResultType.GetMethod(nameof(PreambleResult<,>.GetList))!;

					if (lookupKey.Type.IsValueType)
						lookupKey = Expression.Convert(lookupKey, typeof(object));

					Expression lookupExpr = Expression.Call(
						Expression.Convert(
							new NestedPreambleLookupExpression(nestedSetId, preambleResultType),
							preambleResultType),
						getListMethod,
						lookupKey);

					var objParam   = Expression.Parameter(typeof(object), "nested_o");
					var castLambda = Expression.Lambda(Expression.Convert(objParam, nestedBranch.DetailType), objParam);
					lookupExpr = Expression.Call(
						typeof(Enumerable), nameof(Enumerable.Select),
						new[] { typeof(object), nestedBranch.DetailType },
						lookupExpr, castLambda);

					lookupExpr = Expression.Call(
						typeof(Enumerable), nameof(Enumerable.ToList),
						new[] { nestedBranch.DetailType },
						lookupExpr);

					if (nestedBranch.OrderBy != null)
						lookupExpr = ApplyEnumerableOrderBy(lookupExpr, nestedBranch.OrderBy);

					lookupExpr = SqlAdjustTypeExpression.AdjustType(lookupExpr, nested.EagerLoad.Type, MappingSchema);

					branch.BuiltDetailExpr = branch.BuiltDetailExpr.Transform(
						(nested.EagerLoad, lookupExpr),
						static (ctx, e) => e is SqlEagerLoadExpression sel && sel == ctx.EagerLoad ? ctx.lookupExpr : e);
				}
			}

			// Phase 4: Build CTE with CteUnionEnvelope projection
			// RN  = ROW_NUMBER() OVER (ORDER BY Key) — deterministic ordering
			// Key  = ValueTuple(allParentRefs...) — all parent-referencing expressions
			// Data = x (the source entity/row)
			var cloningContext  = new CloningContext();
			var parentBuildCtx  = parentCtxRef?.BuildContext ?? buildContext;
			var cteSourceCtx    = cloningContext.CloneContext(parentBuildCtx);
			var sourceType      = cteSourceCtx.ElementType;
			var mainExpression  = new ContextRefExpression(typeof(IQueryable<>).MakeGenericType(sourceType), cteSourceCtx);

			// Strip ORDER BY from CTE — some providers don't support ORDER BY in CTE top level
			// and the optimizer will remove it. ROW_NUMBER preserves ordering instead.
			cteSourceCtx.SelectQuery.OrderBy.Items.Clear();

			// Build Key type from allParentRefs
			var keyTypes = allParentRefs.Select(r => r.Type).ToArray();
			var cteKeyType = keyTypes.Length == 1 ? keyTypes[0] : BuildValueTupleType(keyTypes);

			var envelopeType    = typeof(CteUnionEnvelope<,>).MakeGenericType(cteKeyType, sourceType);
			var envelopeRnField   = envelopeType.GetField(nameof(CteUnionEnvelope<,>.RN))!;
			var envelopeKeyField  = envelopeType.GetField(nameof(CteUnionEnvelope<,>.Key))!;
			var envelopeDataField = envelopeType.GetField(nameof(CteUnionEnvelope<,>.Data))!;

			var cteType = envelopeType;

			// Build Select lambda: cte_x => new CteUnionEnvelope { RN = ROW_NUMBER(), Key = VT(ref1, ...), Data = cte_x }
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

			// Build ROW_NUMBER() OVER (ORDER BY key columns) for deterministic ordering.
			// Only use simple member accesses (selectParam.Member) for ROW_NUMBER ordering.
			// SqlPlaceholderExpression (from Concat/SetOperation) can't be used in window functions —
			// fall back to constant 0L (ordering by setId alone is sufficient for those cases).
			Expression rnExpr;
			{
				var rnOrderByList = new List<(Expression expr, bool descending)>();
				for (int i = 0; i < keyArgs.Length; i++)
				{
					if (keyArgs[i] is MemberExpression { Expression: ParameterExpression })
						rnOrderByList.Add((keyArgs[i], false));
				}

				rnExpr = rnOrderByList.Count > 0
					? WindowFunctionHelpers.BuildRowNumber([], rnOrderByList.ToArray())
					: Expression.Constant(0L);
			}

			var envelopeNew = Expression.New(
				envelopeType.GetConstructor(new[] { typeof(long), cteKeyType, sourceType })!,
				new[] { rnExpr, keyBody, selectParam },
				new MemberInfo[] { envelopeRnField, envelopeKeyField, envelopeDataField });

			var envelopeSelectExpr = Expression.Call(
				Methods.Queryable.Select.MakeGenericMethod(sourceType, envelopeType),
				mainExpression, Expression.Quote(Expression.Lambda(envelopeNew, selectParam)));

			var mainCteExpression = Expression.Call(
				Methods.LinqToDB.AsCte.MakeGenericMethod(cteType), envelopeSelectExpr);

			// Build CTE ref mapping with dummy parameter:
			// parentRef → dummyCteParam.Key.ItemN (for key refs)
			// parentRef → dummyCteParam.Data.Member (for entity member refs)
			var dummyCteParam = Expression.Parameter(cteType, "cte_dummy");
			var cteRefMap     = new Dictionary<Expression, Expression>(ExpressionEqualityComparer.Instance);

			Expression dummyKeyAccess  = Expression.Field(dummyCteParam, envelopeKeyField);
			Expression dummyDataAccess = Expression.Field(dummyCteParam, envelopeDataField);

			for (int i = 0; i < allParentRefs.Count; i++)
			{
				var dep = allParentRefs[i];
				// Map to Key.ItemN (or Key directly for single-key)
				Expression keyFieldAccess = keyArgs.Length == 1
					? dummyKeyAccess
					: AccessValueTupleField(dummyKeyAccess, i);

				cteRefMap[dep] = keyFieldAccess;
			}

			// Also map ALL parent entity member accesses and placeholders to CTE Data member paths.
			// This ensures non-correlation columns (e.g., Company.Name) are also retargeted through the CTE
			// rather than referencing the original table directly (which causes "Table not found" errors).
			{
				// Map parent ContextRef → cte.Data
				if (parentCtxRef != null)
					cteRefMap.TryAdd(parentCtxRef, dummyDataAccess);

				// Map parent member accesses → cte.Data.Member
				// Use mainPlaceholders (all parent entity columns) to find member paths.
				// Add both original path and parentCtxRef-based path to handle different ContextRef instances.
				foreach (var ph in mainPlaceholders)
				{
					if (ph.Path is MemberExpression me && me.Expression is ContextRefExpression)
					{
						var member = (MemberInfo?)sourceType.GetProperty(me.Member.Name)
							?? sourceType.GetField(me.Member.Name);
						if (member == null) continue;

						var dataMemberAccess = Expression.MakeMemberAccess(dummyDataAccess, member);

						// Map original path
						cteRefMap.TryAdd(ph.Path, dataMemberAccess);

						// Map parentCtxRef-based path (used in parent branch carrier lookup)
						if (parentCtxRef != null && !me.Expression.Equals(parentCtxRef))
						{
							var altPath = Expression.MakeMemberAccess(parentCtxRef, me.Member);
							cteRefMap.TryAdd(altPath, dataMemberAccess);
						}

						// Map the placeholder itself
						cteRefMap.TryAdd(ph, dataMemberAccess);
					}
				}
			}

			// Phase 5: Build per-branch CTEs and UNION ALL carrier.
			// Each branch: parentCTE.SelectMany(…) → Envelope → .AsCte() → .Select(…) → carrier
			// Nested branches chain from parent branch's CTE instead of the root parent CTE.
			var saveVisitor = _buildVisitor;
			_buildVisitor = _buildVisitor.Clone(cloningContext);
			cloningContext.UpdateContextParents();

			Expression? concatExpr = null;

			// Per-branch CTE expressions and types (needed for nested branches to chain from parent branch CTE)
			var branchCteExpressions = new Expression?[branches.Count];
			var branchCteTypes       = new Type[branches.Count];

			for (int b = 0; b < branches.Count; b++)
			{
				var branch = branches[b];

				// --- Step 1: Build retargeted child sequence through parent CTE ---
				var branchCtx = BuildSequence(new BuildInfo((IBuildContext?)null, mainCteExpression, new SelectQuery()));
				var branchRef = new ContextRefExpression(envelopeType, branchCtx);

				Expression retargetedSequence;
				Type       branchSourceType; // entity type for this branch's CTE Data

				if (branch.IsNested && branch.ExpandedNestedSequence != null)
				{
					// Nested branch chains from parent branch's CTE, not the root parent CTE.
					// E.g., Employees chains from CTE_2 (Departments), not CTE_1 (Company).
					// Build: parentBranchCTE.SelectMany(pd => Emp.Where(e => e.DeptId == pd.Data.Id), ...)
					var parentBranchIdx  = branch.ParentBranchIndex;
					var parentBranchCte  = branchCteExpressions[parentBranchIdx]!;
					var parentBranchType = branchCteTypes[parentBranchIdx]; // Envelope<TKey, TParentData>

					// The parent branch CTE element is Envelope<TKey, TParentData>.
					// We need pd.Data to get the parent entity, then build the nested query from it.
					var parentBranchEnvDataField = parentBranchType.GetField(nameof(CteUnionEnvelope<,>.Data))!;

					var pdParam    = Expression.Parameter(parentBranchType, "pd");
					var pdData     = Expression.Field(pdParam, parentBranchEnvDataField);

					// Use the EXPANDED nested sequence (not OriginalNestedSequence).
					// The expanded form has association navigation resolved to Where() calls
					// with explicit ContextRef.Member parent refs — no navigation properties.
					// Collect parent ref types from branch.MainKeys (the correlation keys).
					var parentRefTypes = new HashSet<Type>();
					parentRefTypes.Add(parentBranchEnvDataField.FieldType);
					foreach (var mk in branch.MainKeys)
					{
						if (mk is MemberExpression me2 && me2.Expression is ContextRefExpression cre2)
							parentRefTypes.Add(cre2.Type);
						else if (mk is ContextRefExpression cre3)
							parentRefTypes.Add(cre3.Type);
					}

					// Replace ContextRef(parentType).Member → pd.Data.Member in expanded nested sequence
					var nestedSeqBody = branch.ExpandedNestedSequence.Transform(
						(parentRefTypes, pdData),
						static (ctx, e) =>
						{
							if (e is MemberExpression me && me.Expression is ContextRefExpression cre
								&& ctx.parentRefTypes.Contains(cre.Type))
							{
								var member = (MemberInfo?)ctx.pdData.Type.GetProperty(me.Member.Name)
									?? ctx.pdData.Type.GetField(me.Member.Name)
									?? me.Member;
								return Expression.MakeMemberAccess(ctx.pdData, member);
							}
							if (e is ContextRefExpression cre2 && ctx.parentRefTypes.Contains(cre2.Type))
								return ctx.pdData;
							return e;
						});

					nestedSeqBody = StripMaterialization(nestedSeqBody);

					retargetedSequence = nestedSeqBody;
					branchSourceType   = branch.DetailType;
				}
				else
				{
					retargetedSequence = RetargetThroughCteMap(branch.ExpandedSequence, cteRefMap, dummyCteParam, branchRef);

					if (branch.ExpandedPredicate != null)
					{
						// Only apply the predicate when it references CTE-mapped parent expressions
						// (e.g., association correlation from LoadWith: ContextRef(parent).Member).
						// Skip when hasCteRefs is false — this means the predicate is a set-distinguishing
						// predicate from SetOperationBuilder (SqlPlaceholderExpression referencing SetOperation
						// SQL fields, not in cteRefMap). CteUnion handles set distinction via branch setId.
						var hasCteRefs = branch.ExpandedPredicate.Find(
							cteRefMap,
							static (map, e) => map.ContainsKey(e)) != null;

						if (hasCteRefs)
						{
							var retargetedPredicate = RetargetThroughCteMap(branch.ExpandedPredicate, cteRefMap, dummyCteParam, branchRef);

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
					}

					branchSourceType = TypeHelper.GetEnumerableElementType(retargetedSequence.Type) ?? branch.DetailType;
				}

				// --- Step 2: Build per-branch CTE envelope ---
				// For non-nested: parentCTE.SelectMany(kd => retargetedSequence, (kd, d) => Envelope{Key, RN=0, Data=d}).AsCte()
				// For nested:     parentBranchCTE.SelectMany(pd => retargetedSequence, (pd, e) => Envelope{Key, RN=0, Data=e}).AsCte()

				Expression    selectManySrcExpr; // IQueryable source for SelectMany
				Type          selectManySrcType; // element type of source
				Expression    branchKeyBody;     // Key expression for envelope
				Type          branchCteKeyType;  // Key type for this branch's CTE

				// Determine source type first so we can create smSourceParam before building key/sequence
				if (branch.IsNested && branch.ExpandedNestedSequence != null)
				{
					var parentBranchCte  = branchCteExpressions[branch.ParentBranchIndex]!;
					var parentBranchType = branchCteTypes[branch.ParentBranchIndex];
					selectManySrcType = parentBranchType;

					var parentBranchCteCtx = BuildSequence(new BuildInfo((IBuildContext?)null, parentBranchCte, new SelectQuery()));
					selectManySrcExpr = new ContextRefExpression(typeof(IQueryable<>).MakeGenericType(parentBranchType), parentBranchCteCtx);
				}
				else
				{
					selectManySrcType = cteType;
					selectManySrcExpr = new ContextRefExpression(typeof(IQueryable<>).MakeGenericType(cteType), branchCtx);
				}

				// Create smSourceParam ONCE — used in key, retargetedSequence, and result selector
				var smSourceParam   = Expression.Parameter(selectManySrcType, branch.IsNested ? "pd" : "kd");
				var detailParameter = Expression.Parameter(branchSourceType, "d");

				// Build key body and finalize retargetedSequence using smSourceParam
				if (branch.IsNested && branch.ExpandedNestedSequence != null)
				{
					// Key = nested correlation from parent branch CTE Data (e.g., pd.Data.Id)
					var parentBranchEnvDataField = selectManySrcType.GetField(nameof(CteUnionEnvelope<,>.Data))!;
					var smData = Expression.Field(smSourceParam, parentBranchEnvDataField);

					// Use CTE Data field type (entity type), not DetailType (projected type)
					var parentDataType = parentBranchEnvDataField.FieldType;
					var nestedKeyExprs = new List<Expression>();
					foreach (var dep in branch.MainKeys)
					{
						if (dep is MemberExpression me && me.Expression is ContextRefExpression)
						{
							// Resolve member on the parent branch's Data type (e.g., Department)
							var member = (MemberInfo?)parentDataType.GetProperty(me.Member.Name)
								?? parentDataType.GetField(me.Member.Name)
								?? me.Member;
							nestedKeyExprs.Add(Expression.MakeMemberAccess(smData, member));
						}
						else
							nestedKeyExprs.Add(dep);
					}

					branchCteKeyType = nestedKeyExprs.Count == 1
						? nestedKeyExprs[0].Type
						: BuildValueTupleType(nestedKeyExprs.Select(e => e.Type).ToArray());

					branchKeyBody = nestedKeyExprs.Count == 1
						? nestedKeyExprs[0]
						: BuildValueTupleNew(branchCteKeyType, nestedKeyExprs.ToArray());

					// Remap retargetedSequence: pdParam/pdData → smSourceParam/smData
					retargetedSequence = retargetedSequence.Transform(
						(smSourceParam, smData, selectManySrcType, parentBranchEnvDataField),
						static (ctx, e) =>
						{
							if (e is MemberExpression me && me.Member == ctx.parentBranchEnvDataField
								&& me.Expression is ParameterExpression pe && pe.Type == ctx.selectManySrcType && pe != ctx.smSourceParam)
								return ctx.smData;
							if (e is ParameterExpression pe2 && pe2.Type == ctx.selectManySrcType && pe2 != ctx.smSourceParam)
								return ctx.smSourceParam;
							return e;
						});
				}
				else
				{
					// Key = allParentRefs remapped from root CTE → smSourceParam
					branchCteKeyType = cteKeyType;

					var remappedFullKeys = new Expression[allParentRefs.Count];
					for (int k = 0; k < allParentRefs.Count; k++)
					{
						if (cteRefMap.TryGetValue(allParentRefs[k], out var mapped))
						{
							remappedFullKeys[k] = mapped.Transform(
								(dummyCteParam, smSourceParam),
								static (ctx, inner) => inner == ctx.dummyCteParam ? ctx.smSourceParam : inner);
						}
						else
						{
							remappedFullKeys[k] = allParentRefs[k];
						}
					}

					branchKeyBody = remappedFullKeys.Length == 1
						? remappedFullKeys[0]
						: BuildValueTupleNew(cteKeyType, remappedFullKeys);

						// Remap retargetedSequence: branchRef → smSourceParam
						// RetargetThroughCteMap replaced parent refs with branchRef (ContextRefExpression),
						// but the SelectMany lambda parameter is smSourceParam (ParameterExpression).
						retargetedSequence = retargetedSequence.Transform(
							(branchRef, smSourceParam, envelopeType),
							static (ctx, e) =>
							{
								if (e is ContextRefExpression cre && cre.Type == ctx.envelopeType)
									return ctx.smSourceParam;
								return e;
							});
				}

				// Build envelope type and fields
				var branchEnvelopeType = typeof(CteUnionEnvelope<,>).MakeGenericType(branchCteKeyType, branchSourceType);
				var branchEnvKeyField  = branchEnvelopeType.GetField(nameof(CteUnionEnvelope<,>.Key))!;
				var branchEnvRnField   = branchEnvelopeType.GetField(nameof(CteUnionEnvelope<,>.RN))!;
				var branchEnvDataField = branchEnvelopeType.GetField(nameof(CteUnionEnvelope<,>.Data))!;

				// Build ROW_NUMBER for branch envelope using branch.OrderBy
				Expression branchRnExpr;
				if (branch.OrderBy != null && branch.OrderBy.Count > 0)
				{
					var rnOrderBy = new List<(Expression expr, bool descending)>();
					foreach (var (lambda, descending) in branch.OrderBy)
					{
						var body = lambda.GetBody(detailParameter);
						rnOrderBy.Add((body, descending));
					}
					branchRnExpr = WindowFunctionHelpers.BuildRowNumber([], rnOrderBy.ToArray());
				}
				else
				{
					branchRnExpr = Expression.Constant(0L);
				}

				var branchEnvelopeNew = Expression.New(
					branchEnvelopeType.GetConstructor(new[] { typeof(long), branchCteKeyType, branchSourceType })!,
					new Expression[] { branchRnExpr, branchKeyBody, detailParameter },
					new MemberInfo[] { branchEnvRnField, branchEnvKeyField, branchEnvDataField });

				// Apply SelectDistinct to the source CTE to prevent duplicate detail rows
				// when the parent CTE has duplicate key values.
				var distinctSrcExpr = Expression.Call(
					Methods.LinqToDB.SelectDistinct.MakeGenericMethod(selectManySrcType),
					selectManySrcExpr);

				// Build SelectMany
				var detailSelector = _buildSelectManyDetailSelectorInfo
					.MakeGenericMethod(selectManySrcType, branchSourceType)
					.InvokeExt<LambdaExpression>(null, new object[] { retargetedSequence, smSourceParam });

				var resultSelector = Expression.Lambda(branchEnvelopeNew, smSourceParam, detailParameter);

				var selectManyExpr = Expression.Call(
					Methods.Queryable.SelectManyProjection.MakeGenericMethod(selectManySrcType, branchSourceType, branchEnvelopeType),
					distinctSrcExpr,
					Expression.Quote(detailSelector!), Expression.Quote(resultSelector));


				// Wrap in .AsCte() → per-branch CTE
				var branchCteExpr = Expression.Call(
					Methods.LinqToDB.AsCte.MakeGenericMethod(branchEnvelopeType), selectManyExpr);

				branchCteExpressions[b] = branchCteExpr;
				branchCteTypes[b]       = branchEnvelopeType;

				// --- Step 3: Build carrier from per-branch CTE ---
				// branchCte.Select(c => (setId, c.Key, c.Data.Col1, c.Data.Col2, ...))
				var branchCteCtx = BuildSequence(new BuildInfo((IBuildContext?)null, branchCteExpr, new SelectQuery()));
				var cSelectParam = Expression.Parameter(branchEnvelopeType, "c");
				var cDataAccess  = Expression.Field(cSelectParam, branchEnvDataField);
				var cKeyAccess   = Expression.Field(cSelectParam, branchEnvKeyField);

				var args = new Expression[carrierTypes.Length];
				args[0] = Expression.Constant(b); // setId

				// Carrier key: each branch projects its own CTE key into slot 1.
				// For non-nested, the key matches carrierKeyType directly.
				// For nested, the key type may differ — convert if needed.
				if (cKeyAccess.Type == carrierKeyType)
					args[1] = cKeyAccess;
				else
					args[1] = Expression.Convert(cKeyAccess, carrierKeyType);

				args[2] = Expression.Field(cSelectParam, branchEnvRnField); // RN → slot 2

				// Fill data slots with defaults
				for (int s = DataSlotOffset; s < args.Length; s++)
					args[s] = Expression.Default(carrierTypes[s]);

				// Build placeholder→projected member mapping from BuiltDetailExpr.
				// The SqlGenericConstructorExpression assignments map anonymous type members
				// (e.g., DeptName) to SqlPlaceholderExpressions whose paths may reference
				// underlying entity members (e.g., Department.Name) or scalar subqueries
				// (e.g., MethodCallExpression for Count()). This mapping lets us resolve
				// the correct CTE Data member regardless of renaming or path type.
				var phToMember = new Dictionary<SqlPlaceholderExpression, MemberInfo>();
				branch.BuiltDetailExpr.Visit(phToMember, static (map, e) =>
				{
					if (e is SqlGenericConstructorExpression sgce)
					{
						foreach (var assignment in sgce.Assignments)
						{
							if (assignment.Expression is SqlPlaceholderExpression spe)
								map.TryAdd(spe, assignment.MemberInfo);
						}
					}
				});

				// Fill data slots from branch CTE Data members
				for (int c = 0; c < branch.Placeholders.Count; c++)
				{
					var ph      = branch.Placeholders[c];
					var slotIdx = slotMaps[b][c];

					MemberInfo? targetMember = null;

					// Try BuiltDetailExpr assignment mapping first (handles renamed members + scalar subqueries)
					if (phToMember.TryGetValue(ph, out var projectedMemberInfo))
					{
						targetMember = (MemberInfo?)branchSourceType.GetProperty(projectedMemberInfo.Name)
							?? branchSourceType.GetField(projectedMemberInfo.Name);
					}

					// Fallback: match by placeholder path member name
					if (targetMember == null && ph.Path is MemberExpression mePath)
					{
						var member = mePath.Member;
						if (member.DeclaringType != branchSourceType)
						{
							targetMember = (MemberInfo?)branchSourceType.GetProperty(member.Name)
								?? branchSourceType.GetField(member.Name)
								?? member;
						}
						else
						{
							targetMember = member;
						}
					}

					if (targetMember == null || targetMember.DeclaringType == null
						|| !targetMember.DeclaringType.IsAssignableFrom(branchSourceType))
					{
						throw new LinqToDBException(
							$"CteUnion: Cannot resolve placeholder to CTE Data member. " +
							$"Path={ph.Path?.GetType().Name}, branchSourceType={branchSourceType.Name}");
					}

					Expression access = Expression.MakeMemberAccess(cDataAccess, targetMember);

					if (access.Type != carrierTypes[slotIdx])
						access = Expression.Convert(access, carrierTypes[slotIdx]);
					args[slotIdx] = access;
				}

				var carrierNew = BuildValueTupleNew(carrierType, args);
				var carrierSelectLambda = Expression.Lambda(carrierNew, cSelectParam);

				var branchCarrierQuery = Expression.Call(
					Methods.Queryable.Select.MakeGenericMethod(branchEnvelopeType, carrierType),
					new ContextRefExpression(typeof(IQueryable<>).MakeGenericType(branchEnvelopeType), branchCteCtx),
					Expression.Quote(carrierSelectLambda));

				concatExpr = concatExpr == null
					? branchCarrierQuery
					: Expression.Call(Methods.Queryable.Concat.MakeGenericMethod(carrierType), concatExpr, branchCarrierQuery);
			}

			if (concatExpr == null)
				throw new LinqToDBException("CteUnion: No branches produced carrier expressions.");

			// Phase 5b: Add parent branch (setId = parentSetId, LAST in UNION ALL)
			// For SetOperation (Concat/Union) contexts, the parent data comes from the SetOperation query
			// itself (which may have 2N rows for Concat), not from the entity CTE (N rows).
			// Detect by checking if mainPlaceholders use SqlPathExpression paths (SetOperation signature).
			var isSetOpParent = mainPlaceholders.Exists(ph => ph.Path is SqlPathExpression);

			// Determine if all placeholders can be resolved from the CTE map (entity CTE path)
			var canUseCteParent = !isSetOpParent && mainPlaceholders.Count > 0
				&& mainPlaceholders.TrueForAll(ph =>
					ph.Path is MemberExpression { Expression: ContextRefExpression }
					|| parentRefSet.Contains(ph)
					|| CanResolveFromCteMap(ph, cteRefMap));

			// Use parent branch if we have placeholders AND can resolve them (CTE or clone-source)
			var useParentBranch = mainPlaceholders.Count > 0;

			if (!useParentBranch)
				parentSetId = -1;

			// When CTE resolution fails (e.g., scalar subqueries like Count()), fall back to
			// cloning the source buildContext — same approach as SetOperation parents.
			var useCloneSourceParent = useParentBranch && !isSetOpParent && !canUseCteParent;

			if (useParentBranch && (isSetOpParent || useCloneSourceParent))
			{
				// SetOperation parent branch: clone the buildContext (Concat/Union query) and use
				// cloned placeholders directly. This produces 2N rows for Concat (correct row count).
				var setOpCloningCtx = new CloningContext();
				var clonedBuildCtx  = setOpCloningCtx.CloneContext(buildContext);
				setOpCloningCtx.UpdateContextParents();

				var clonedElementType = clonedBuildCtx.ElementType;
				var clonedSrcRef      = new ContextRefExpression(typeof(IQueryable<>).MakeGenericType(clonedElementType), clonedBuildCtx);
				var parentParam       = Expression.Parameter(clonedElementType, "p_setop");
				var parentArgs        = new Expression[carrierTypes.Length];
				parentArgs[0] = Expression.Constant(parentSetId);

				// Build key: find the cloned mainPlaceholder(s) that correspond to allParentRefs
				var parentKeyArgs = new Expression[allParentRefs.Count];
				for (int k = 0; k < allParentRefs.Count; k++)
				{
					var dep = allParentRefs[k];
					string? memberName = null;
					if (dep is MemberExpression me && me.Expression is ContextRefExpression)
						memberName = me.Member.Name;

					// Find the mainPlaceholder matching this correlation key
					SqlPlaceholderExpression? matchedPh = null;
					if (memberName != null)
					{
						for (int pi = 0; pi < mainPlaceholders.Count; pi++)
						{
							var ph = mainPlaceholders[pi];
							if (MatchesPlaceholderMemberName(ph, memberName))
							{
								matchedPh = ph;
								break;
							}
						}
					}

					if (matchedPh != null)
					{
						var clonedPh = setOpCloningCtx.CloneExpression(matchedPh);
						parentKeyArgs[k] = clonedPh.Type != cteKeyType && allParentRefs.Count == 1
							? Expression.Convert(clonedPh, cteKeyType)
							: (Expression)clonedPh;
					}
					else
					{
						// Fallback: clone the dependency expression directly
						parentKeyArgs[k] = setOpCloningCtx.CloneExpression(dep);
					}
				}

				parentArgs[1] = parentKeyArgs.Length == 1
					? parentKeyArgs[0]
					: GenerateKeyExpression(parentKeyArgs, 0);

				parentArgs[2] = Expression.Constant(0L); // RN — no ordering for SetOperation parent

				// Fill all data slots with defaults first
				for (int s = DataSlotOffset; s < parentArgs.Length; s++)
					parentArgs[s] = Expression.Default(carrierTypes[s]);

				// Fill parent data slots from cloned mainPlaceholders
				for (int c = 0; c < mainPlaceholders.Count; c++)
				{
					var slotIdx  = parentSlotMap[c];
					var clonedPh = setOpCloningCtx.CloneExpression(mainPlaceholders[c]);

					if (clonedPh.Type != carrierTypes[slotIdx])
						parentArgs[slotIdx] = Expression.Convert(clonedPh, carrierTypes[slotIdx]);
					else
						parentArgs[slotIdx] = clonedPh;
				}

				var parentCarrierNew    = BuildValueTupleNew(carrierType, parentArgs);
				var parentSelectLambda  = Expression.Lambda(parentCarrierNew, parentParam);

				var parentBranchQuery = Expression.Call(
					Methods.Queryable.Select.MakeGenericMethod(clonedElementType, carrierType),
					clonedSrcRef,
					Expression.Quote(parentSelectLambda));

				concatExpr = Expression.Call(Methods.Queryable.Concat.MakeGenericMethod(carrierType), concatExpr!, parentBranchQuery);
			}
			else if (useParentBranch)
			{
				// Entity CTE parent branch (non-SetOperation): parent data from CTE envelope
				var parentBranchCtx = BuildSequence(new BuildInfo((IBuildContext?)null, mainCteExpression, new SelectQuery()));
				var parentBranchRef = new ContextRefExpression(cteType, parentBranchCtx);

				var parentParam = Expression.Parameter(cteType, "p");
				var parentArgs  = new Expression[carrierTypes.Length];
				parentArgs[0] = Expression.Constant(parentSetId);

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

				parentArgs[2] = Expression.Field(parentParam, envelopeRnField); // RN → slot 2

				for (int s = DataSlotOffset; s < parentArgs.Length; s++)
					parentArgs[s] = Expression.Default(carrierTypes[s]);

				for (int c = 0; c < mainPlaceholders.Count; c++)
				{
					var slotIdx = parentSlotMap[c];
					var ph      = mainPlaceholders[c];

					Expression? mappedPath = null;

					if (ph.Path is MemberExpression me && me.Expression is ContextRefExpression)
					{
						var lookupPath = me.Expression.Equals(parentCtxRef)
							? ph.Path
							: Expression.MakeMemberAccess(parentCtxRef!, me.Member);

						cteRefMap.TryGetValue(lookupPath, out mappedPath);
					}
					else
					{
						cteRefMap.TryGetValue(ph, out mappedPath);
					}

					if (mappedPath != null)
					{
						var cteAccess = mappedPath.Transform(
							(dummyCteParam, parentParam),
							static (ctx, inner) => inner == ctx.dummyCteParam ? ctx.parentParam : inner);

						if (cteAccess.Type != carrierTypes[slotIdx])
							cteAccess = Expression.Convert(cteAccess, carrierTypes[slotIdx]);
						parentArgs[slotIdx] = cteAccess;
					}
					else if (ph.Path != null)
					{
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

			// Phase 5c: ORDER BY setId, key, RN for deterministic dispatch and row ordering.
			// Key is included between setId and RN so that the SQL builder encounters carrier slots
			// in sequential order (0, 1, 2). Without this, the ORDER BY on slots 0 and 2 causes the
			// SQL builder to create column references out of positional order, producing wrapper SELECTs
			// where column names don't match positional order — breaking Oracle/Informix which resolve
			// UNION ALL column references by position.
			{
				var orderParam  = Expression.Parameter(carrierType, "ord");
				var setIdAccess = AccessValueTupleField(orderParam, 0);
				var keyAccess   = AccessValueTupleField(orderParam, 1); // slot 1 = key
				var rnAccess    = AccessValueTupleField(orderParam, 2); // slot 2 = RN

				concatExpr = Expression.Call(
					Methods.Queryable.OrderBy.MakeGenericMethod(carrierType, typeof(int)),
					concatExpr!,
					Expression.Quote(Expression.Lambda(setIdAccess, orderParam)));

				concatExpr = Expression.Call(
					Methods.Queryable.ThenBy.MakeGenericMethod(carrierType, carrierKeyType),
					concatExpr,
					Expression.Quote(Expression.Lambda(keyAccess, orderParam)));

				if (rnAccess.Type != typeof(long))
					rnAccess = Expression.Convert(rnAccess, typeof(long));

				concatExpr = Expression.Call(
					Methods.Queryable.ThenBy.MakeGenericMethod(carrierType, typeof(long)),
					concatExpr,
					Expression.Quote(Expression.Lambda(rnAccess, orderParam)));
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
					var getListMethod      = preambleResultType.GetMethod(nameof(PreambleResult<,>.GetList))!;

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

				var preambleIdx = preambles.Count;
				var reconstructed = branch.BuiltDetailExpr.Transform(
					(placeholderToSlot, cp, pa, preambleIdx),
					static (ctx, e) =>
					{
						if (e is SqlPlaceholderExpression spe && ctx.placeholderToSlot.TryGetValue(spe, out var slotIdx))
						{
							var access = AccessValueTupleField(ctx.cp, slotIdx);

							if (access.Type != spe.ConvertType)
								access = Expression.Convert(access, spe.ConvertType);

							return access;
						}

						// Resolve NestedPreambleLookupExpression — mirrors SetupCteUnionQuery
						if (e is NestedPreambleLookupExpression nple)
						{
							return Expression.Convert(
								Expression.ArrayIndex(
									Expression.Convert(
										Expression.ArrayIndex(ctx.pa, ExpressionInstances.Int32(ctx.preambleIdx)),
										typeof(object?[])),
									ExpressionInstances.Int32(nple.NestedSetId)),
								nple.PreambleResultType);
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

			// Create preamble — compute nested processing order (deepest first)
			int[]? nestedProcessingOrder = null;
			{
				List<int>? nestedIds = null;
				for (int b = 0; b < branches.Length; b++)
				{
					if (branches[b].IsNested)
					{
						nestedIds ??= new List<int>();
						nestedIds.Add(b);
					}
				}

				if (nestedIds != null)
				{
					nestedIds.Sort();
					nestedIds.Reverse(); // deepest first (BFS assigns higher indices to deeper levels)
					nestedProcessingOrder = nestedIds.ToArray();
				}
			}

			var idx      = preambles.Count;
			var preamble = new CteUnionPreamble<TKey, TCarrier>(query, setIdExtractor, keyExtractor, detailExtractors, branches.Length, nestedProcessingOrder, idx);
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

			try
			{
				_setupCteUnionQueryMethodInfo
					.MakeGenericMethod(typeof(T), info.KeyType, info.CarrierType)
					.InvokeExt(this, new object[]
					{
						query, sequence, finalized, preambles, preambleStartIndex, info, queryParameter,
					});
			}
			catch (Exception ex) when (ex is not LinqToDBException)
			{
				throw new LinqToDBException($"CteUnion SetupCteUnionQuery failed: {ex.GetType().Name}: {ex.Message}\nInner: {ex.InnerException?.Message}", ex);
			}
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
			try
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
			catch (Exception ex)
			{
				throw new LinqToDBException($"CteUnion: Force carrier fields failed: {ex.Message}", ex);
			}

			// Build the UNION ALL mapper
			try
			{
				if (!BuildQuery(unionQuery, info.CombinedSequence, queryParameter, ref preambles!, []))
					throw new LinqToDBException("Failed to build CteUnion combined query.");
			}
			catch (LinqToDBException) { throw; }
			catch (Exception ex)
			{
				throw new LinqToDBException($"CteUnion: BuildQuery(unionQuery) failed: {ex.Message}", ex);
			}

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

				var preambleIdx = preambleStartIndex;
				var reconstructed = branch.BuiltDetailExpr.Transform(
					(placeholderToSlot, cp, pa, preambleIdx),
					static (ctx, e) =>
					{
						if (e is SqlPlaceholderExpression spe && ctx.placeholderToSlot.TryGetValue(spe, out var slotIdx))
						{
							var access = AccessValueTupleField(ctx.cp, slotIdx);
							if (access.Type != spe.ConvertType)
								access = Expression.Convert(access, spe.ConvertType);
							return access;
						}

						// Resolve NestedPreambleLookupExpression:
						// Access: ((PreambleResult<TKey, object>)((object?[])pa[preambleIdx])[nestedSetId])
						// pa = preambleResults array
						// preambleResults[preambleIdx] = childResults (object?[])
						// childResults[nestedSetId] = PreambleResult for that nested branch
						if (e is NestedPreambleLookupExpression nple)
						{
							return Expression.Convert(
								Expression.ArrayIndex(
									Expression.Convert(
										Expression.ArrayIndex(ctx.pa, ExpressionInstances.Int32(ctx.preambleIdx)),
										typeof(object?[])),
									ExpressionInstances.Int32(nple.NestedSetId)),
								nple.PreambleResultType);
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

			// 5. Determine which branches are nested and compute processing order (deepest first)
			HashSet<int>? nestedSetIds0 = null;
			int[]? nestedProcessingOrder0 = null;
			{
				List<int>? nestedIds0 = null;
				for (int b0 = 0; b0 < branches.Length; b0++)
				{
					if (branches[b0].IsNested)
					{
						nestedIds0 ??= new List<int>();
						nestedIds0.Add(b0);
					}
				}

				if (nestedIds0 != null)
				{
					nestedSetIds0 = new HashSet<int>(nestedIds0);
					nestedIds0.Sort();
					nestedIds0.Reverse(); // deepest first (BFS assigns higher indices to deeper levels)
					nestedProcessingOrder0 = nestedIds0.ToArray();
				}
			}

			// 6. Replace GetResultEnumerable with UNION ALL-based iterator.
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
				{
					if (nestedSetIds0 != null && nestedSetIds0.Contains(i))
						childResults[i] = new PreambleResult<object, object>(EqualityComparer<object>.Default);
					else
						childResults[i] = new PreambleResult<TKey, object>();
				}

				// Store in the preambles array so PreambleResult.GetList calls work
				if (preambleResults != null && preambleIdx0 < preambleResults.Length)
					preambleResults[preambleIdx0] = childResults;

				// Execute the UNION ALL query and buffer all rows
				var carriers = unionQuery.GetResultEnumerable(db, expr, ps, preambleResults).ToList();

				if (nestedProcessingOrder0 != null)
				{
					// Multi-pass: process nested branches in reverse depth order (deepest first)
					foreach (var nestedSetId in nestedProcessingOrder0)
					{
						foreach (var carrier in carriers)
						{
							var setId = setIdExtractor(carrier);
							if (setId == nestedSetId)
							{
								var key    = (object)keyExtractor(carrier)!;
								var detail = detailExtractors[setId](carrier, preambleResults);
								((PreambleResult<object, object>)childResults[setId]!).Add(key, detail);
							}
						}
					}

					// Non-nested branches
					foreach (var carrier in carriers)
					{
						var setId = setIdExtractor(carrier);
						if (setId >= 0 && setId < branchCount0 && !nestedSetIds0!.Contains(setId))
						{
							var key    = keyExtractor(carrier);
							var detail = detailExtractors[setId](carrier, preambleResults);
							((PreambleResult<TKey, object>)childResults[setId]!).Add(key, detail);
						}
					}
				}
				else
				{
					// Single pass: no nested branches
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
				}

				// Yield parent rows (reconstructed with PreambleResults)
				return new CteUnionResultEnumerable<T, TCarrier>(
					carriers, setIdExtractor, parentSetId0, parentMapper, preambleResults!);
			};

			// Override GetElement for FirstOrDefault/Single
			query.GetElement = (db, expr, ps, preambleResults) =>
			{
				var childResults = new object?[branchCount0];
				for (int i = 0; i < branchCount0; i++)
				{
					if (nestedSetIds0 != null && nestedSetIds0.Contains(i))
						childResults[i] = new PreambleResult<object, object>(EqualityComparer<object>.Default);
					else
						childResults[i] = new PreambleResult<TKey, object>();
				}

				if (preambleResults != null && preambleIdx0 < preambleResults.Length)
					preambleResults[preambleIdx0] = childResults;

				var carriers = unionQuery.GetResultEnumerable(db, expr, ps, preambleResults).ToList();

				if (nestedProcessingOrder0 != null)
				{
					foreach (var nestedSetId in nestedProcessingOrder0)
					{
						foreach (var carrier in carriers)
						{
							var setId = setIdExtractor(carrier);
							if (setId == nestedSetId)
							{
								var key    = (object)keyExtractor(carrier)!;
								var detail = detailExtractors[setId](carrier, preambleResults);
								((PreambleResult<object, object>)childResults[setId]!).Add(key, detail);
							}
						}
					}

					foreach (var carrier in carriers)
					{
						var setId = setIdExtractor(carrier);
						if (setId >= 0 && setId < branchCount0 && !nestedSetIds0!.Contains(setId))
						{
							var key    = keyExtractor(carrier);
							var detail = detailExtractors[setId](carrier, preambleResults);
							((PreambleResult<TKey, object>)childResults[setId]!).Add(key, detail);
						}
					}
				}
				else
				{
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
				}

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

		/// <summary>
		/// Marker expression used in parent branch detail expressions to represent a nested PreambleResult
		/// lookup. Resolved during detail extractor compilation by replacing with the actual childResults
		/// array access.
		/// </summary>
		sealed class NestedPreambleLookupExpression : Expression
		{
			public int  NestedSetId        { get; }
			public Type PreambleResultType { get; }

			public NestedPreambleLookupExpression(int nestedSetId, Type preambleResultType)
			{
				NestedSetId        = nestedSetId;
				PreambleResultType = preambleResultType;
			}

			public override ExpressionType NodeType => ExpressionType.Extension;
			public override Type           Type     => PreambleResultType;
			public override bool           CanReduce => false;

			protected override Expression VisitChildren(ExpressionVisitor visitor) => this;
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
		/// <summary>Strips .ToList() / .ToArray() from the end of an expression (for SelectMany compatibility).</summary>
		static Expression StripMaterialization(Expression expr)
		{
			if (expr is MethodCallExpression mc && mc.Arguments.Count == 1
				&& (string.Equals(mc.Method.Name, "ToList", StringComparison.Ordinal) || string.Equals(mc.Method.Name, "ToArray", StringComparison.Ordinal)))
			{
				return mc.Arguments[0];
			}

			return expr;
		}

		/// <summary>
		/// Strips trailing Select, OrderBy, ThenBy, ThenByDescending, AsUnionQuery, AsSeparateQuery
		/// from a method call chain, leaving just the base query (typically Where + source).
		/// </summary>
		static Expression StripToBaseQuery(Expression expr)
		{
			while (expr is MethodCallExpression mc && mc.Arguments.Count >= 1)
			{
				if (mc.Method.Name is "Select" or "OrderBy" or "OrderByDescending"
					or "ThenBy" or "ThenByDescending" or "AsUnionQuery" or "AsSeparateQuery" or "AsKeyedQuery")
				{
					expr = mc.Arguments[0];
					continue;
				}

				break;
			}

			return expr;
		}


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

		/// <summary>
		/// Checks if a SqlPlaceholderExpression corresponds to a given member name.
		/// For SetOperation placeholders, the Path is a SqlPathExpression whose Path array
		/// may contain ConstantExpression(MemberInfo) entries from the SetOperationBuilder's visitor.
		/// Also checks the placeholder's Alias as a fallback.
		/// </summary>
		static bool MatchesPlaceholderMemberName(SqlPlaceholderExpression ph, string memberName)
		{
			// Check alias first (most reliable if set)
			if (string.Equals(ph.Alias, memberName, StringComparison.Ordinal))
				return true;

			// Check SqlPathExpression for MemberInfo entries matching the member name
			if (ph.Path is SqlPathExpression spe)
			{
				for (int i = spe.Path.Length - 1; i >= 0; i--)
				{
					if (spe.Path[i] is ConstantExpression ce)
					{
						if (ce.Value is MemberInfo mi && string.Equals(mi.Name, memberName, StringComparison.Ordinal))
							return true;
						if (ce.Value is string s && string.Equals(s, memberName, StringComparison.Ordinal))
							return true;
					}
				}
			}

			// Check MemberExpression path (normal non-SetOperation case)
			if (ph.Path is MemberExpression me2 && string.Equals(me2.Member.Name, memberName, StringComparison.Ordinal))
				return true;

			return false;
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

			// Nested eager load support
			public bool                                IsNested;
			public int                                 ParentBranchIndex  = -1;
			/// <summary>Original (unexpanded) nested sequence — has ContextRef references to parent detail context.</summary>
			public Expression?                         OriginalNestedSequence;
			/// <summary>Expanded nested sequence — association navs resolved to Where() with ContextRef parent refs.</summary>
			public Expression?                         ExpandedNestedSequence;

			// Nested eager loads found in this branch's BuiltDetailExpr (set on parent branches)
			public List<NestedEagerLoadInfo>?          NestedEagerLoads;
		}

		sealed class NestedEagerLoadInfo
		{
			public SqlEagerLoadExpression EagerLoad    = null!;
			public int                    NestedBranchIndex;
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
			readonly int[]?                     _nestedProcessingOrder; // nested setIds in reverse depth order (deepest first)
			readonly int                        _preambleIndex;

			public CteUnionPreamble(
				Query<TCarrier>                      query,
				Func<TCarrier, int>                  getSetId,
				Func<TCarrier, TKey>                 getKey,
				Func<TCarrier, object?[]?, object>[] detailExtractors,
				int                                  branchCount,
				int[]?                               nestedProcessingOrder,
				int                                  preambleIndex)
			{
				_query                  = query;
				_getSetId               = getSetId;
				_getKey                 = getKey;
				_detailExtractors       = detailExtractors;
				_branchCount            = branchCount;
				_nestedProcessingOrder  = nestedProcessingOrder;
				_preambleIndex          = preambleIndex;
			}

			public override object Execute(IDataContext dataContext, IQueryExpressions expressions, object?[]? parameters, object?[]? preambles)
			{
				var nestedSetIds = _nestedProcessingOrder != null ? new HashSet<int>(_nestedProcessingOrder) : null;

				var results = new object?[_branchCount];
				for (int i = 0; i < _branchCount; i++)
				{
					results[i] = nestedSetIds != null && nestedSetIds.Contains(i)
						? new PreambleResult<object, object>(EqualityComparer<object>.Default)
						: new PreambleResult<TKey, object>();
				}

				// Store results in preambles early so nested branch detail extractors
				// can access them via preambles[_preambleIndex]
				if (preambles != null && _preambleIndex < preambles.Length)
					preambles[_preambleIndex] = results;

				if (_nestedProcessingOrder != null)
				{
					// Multi-pass: buffer all carriers, process nested branches in reverse depth
					// order (deepest first), then non-nested. This ensures nested PreambleResults
					// are populated before parent detail extractors try to look them up.
					var carriers = _query.GetResultEnumerable(dataContext, expressions, parameters, preambles).ToList();

					foreach (var nestedSetId in _nestedProcessingOrder)
					{
						foreach (var carrier in carriers)
						{
							var setId = _getSetId(carrier);
							if (setId == nestedSetId)
							{
								var key    = (object)_getKey(carrier)!;
								var detail = _detailExtractors[setId](carrier, preambles);
								((PreambleResult<object, object>)results[setId]!).Add(key, detail);
							}
						}
					}

					foreach (var carrier in carriers)
					{
						var setId = _getSetId(carrier);
						if (setId >= 0 && setId < _branchCount && !nestedSetIds!.Contains(setId))
						{
							var key    = _getKey(carrier);
							var detail = _detailExtractors[setId](carrier, preambles);
							((PreambleResult<TKey, object>)results[setId]!).Add(key, detail);
						}
					}
				}
				else
				{
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
				}

				return results;
			}

			public override async Task<object> ExecuteAsync(IDataContext dataContext, IQueryExpressions expressions, object?[]? parameters, object[]? preambles,
				CancellationToken cancellationToken)
			{
				var nestedSetIds = _nestedProcessingOrder != null ? new HashSet<int>(_nestedProcessingOrder) : null;

				var results = new object?[_branchCount];
				for (int i = 0; i < _branchCount; i++)
				{
					results[i] = nestedSetIds != null && nestedSetIds.Contains(i)
						? new PreambleResult<object, object>(EqualityComparer<object>.Default)
						: new PreambleResult<TKey, object>();
				}

				// Store results in preambles early so nested branch detail extractors
				// can access them via preambles[_preambleIndex]
				if (preambles != null && _preambleIndex < preambles.Length)
					preambles[_preambleIndex] = results;

				if (_nestedProcessingOrder != null)
				{
					var carriers = new List<TCarrier>();
					var enumerator = _query.GetResultEnumerable(dataContext, expressions, parameters, preambles)
						.GetAsyncEnumerator(cancellationToken);
					while (await enumerator.MoveNextAsync().ConfigureAwait(false))
						carriers.Add(enumerator.Current);

					foreach (var nestedSetId in _nestedProcessingOrder)
					{
						foreach (var carrier in carriers)
						{
							var setId = _getSetId(carrier);
							if (setId == nestedSetId)
							{
								var key    = (object)_getKey(carrier)!;
								var detail = _detailExtractors[setId](carrier, preambles);
								((PreambleResult<object, object>)results[setId]!).Add(key, detail);
							}
						}
					}

					foreach (var carrier in carriers)
					{
						var setId = _getSetId(carrier);
						if (setId >= 0 && setId < _branchCount && !nestedSetIds!.Contains(setId))
						{
							var key    = _getKey(carrier);
							var detail = _detailExtractors[setId](carrier, preambles);
							((PreambleResult<TKey, object>)results[setId]!).Add(key, detail);
						}
					}
				}
				else
				{
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
