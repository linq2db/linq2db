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
			Expression[]        previousKeys,
			EagerLoadState      state)
		{
			// Nested CTE batch (previousKeys non-empty) is not supported —
			// the CTE would select ALL parent rows without correlation to the outer level.
			if (previousKeys.Length > 0)
				return null;

			// Collect all eager loads in this expression
			var cteUnionLoads = new List<SqlEagerLoadExpression>();

			expression.Visit(cteUnionLoads, static (ctx, e) =>
			{
				if (e.NodeType == ExpressionType.Extension && e is SqlEagerLoadExpression eagerLoad)
					ctx.Add(eagerLoad);
			});

			if (cteUnionLoads.Count == 0)
				return null;

			// Pass 1: Collect dependencies from expanded sequences (lightweight — no BuildSequence).
			var allDependencies = new List<Expression>();
			var allDepsSet      = new HashSet<Expression>(ExpressionEqualityComparer.Instance);

			var branchInfos = new List<BranchInfo>();

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

				foreach (var dep in dependencies)
				{
					if (allDepsSet.Add(dep))
						allDependencies.Add(dep);
				}

				Expression? expandedPredicate = null;
				if (eagerLoad.Predicate != null)
				{
					expandedPredicate = ExpandContexts(buildContext, eagerLoad.Predicate);
					var predicateDeps = new HashSet<Expression>(ExpressionEqualityComparer.Instance);
					CollectDependencies(buildContext, expandedPredicate, predicateDeps);

					foreach (var dep in predicateDeps)
					{
						dependencies.Add(dep);
						if (allDepsSet.Add(dep))
							allDependencies.Add(dep);
					}
				}

				Expression mainKeyExpression = mainKeys.Length == 1
					? mainKeys[0]
					: GenerateKeyExpression(mainKeys, 0);

				branchInfos.Add(new BranchInfo
				{
					EagerLoad          = eagerLoad,
					ExpandedSequence   = expandedSequence,
					ExpandedPredicate  = expandedPredicate,
					DetailType         = detailType,
					MainKeys           = mainKeys,
					MainKeyExpression  = mainKeyExpression,
					OrderBy            = CollectOrderBy(sequenceExpression),
				});
			}

			if (branchInfos.Count == 0 || allDependencies.Count == 0)
				return null;

			// Build root CTE — buildContext is used ONLY here. After this, everything uses CTE contexts.
			var sourceType           = buildContext.ElementType;
			var wrappedBuildContext  = new EagerContext(buildContext, sourceType);
			var mainExpression       = new ContextRefExpression(typeof(IQueryable<>).MakeGenericType(sourceType), wrappedBuildContext);
			var mainCteExpression    = Expression.Call(Methods.LinqToDB.AsCte.MakeGenericMethod(sourceType), mainExpression);

			var rootCteCtx      = BuildSequence(new BuildInfo((IBuildContext?)null, mainCteExpression, new SelectQuery()));
			var rootCteTableCtx = (CteTableContext)rootCteCtx;

			// Register all dependencies as key virtual fields on root CTE
			var keyVirtualFields = new MemberExpression[allDependencies.Count];
			for (int i = 0; i < allDependencies.Count; i++)
				keyVirtualFields[i] = rootCteTableCtx.RegisterVirtualField(allDependencies[i]);

			var keyTypes   = keyVirtualFields.Select(vf => vf.Type).ToArray();
			var cteKeyType = keyTypes.Length == 1 ? keyTypes[0] : BuildValueTupleType(keyTypes);

			// Register RN
			MemberExpression rnVirtualField;
			{
				var rnOrderByList = new List<(Expression expr, bool descending)>();
				for (int i = 0; i < keyVirtualFields.Length; i++)
					rnOrderByList.Add((keyVirtualFields[i], false));

				Expression rnExpr = rnOrderByList.Count > 0
					? WindowFunctionHelpers.BuildRowNumber([], rnOrderByList.ToArray())
					: Expression.Constant(0L);

				rnVirtualField = rootCteTableCtx.RegisterVirtualField(rnExpr);
			}

			// Build replacement map: dependency → CTE virtual field
			var depToVF = new Dictionary<Expression, Expression>(ExpressionEqualityComparer.Instance);
			for (int i = 0; i < allDependencies.Count; i++)
				depToVF[allDependencies[i]] = keyVirtualFields[i];

			// Pass 2: Per-branch CTE-aware detail building.
			// Each branch gets its own CteTable. Virtual fields stored instead of placeholders.
			var branches = new List<CteUnionBranch>();

			foreach (var info in branchInfos)
			{
				// Build full branch CTE: rootCTE.SelectDistinct().SelectMany(kd => child, (kd, d) => KeyDetailEnvelope(key, d)).AsCte()
				// 1. Replace parent deps with CTE virtual fields, retarget to per-branch CTE
				var cteChildSequence = info.ExpandedSequence.Transform(
					depToVF,
					static (map, e) => map.TryGetValue(e, out var replacement) ? replacement : e);

				var branchRootCtx = BuildSequence(new BuildInfo((IBuildContext?)null, mainCteExpression, new SelectQuery()));
				cteChildSequence = SequenceHelper.ReplaceContext(cteChildSequence, rootCteTableCtx, branchRootCtx);

				// 2. Build SelectMany with KeyDetailEnvelope result selector
				var branchRootRef   = new ContextRefExpression(typeof(IQueryable<>).MakeGenericType(sourceType), branchRootCtx);
				var smSourceParam   = Expression.Parameter(sourceType, "kd");
				var detailParameter = Expression.Parameter(info.DetailType, "d");

				// Key body: use ALL dependencies (not per-branch mainKeys) so key type
				// matches carrierKeyType across all branches in the UNION ALL.
				// Use virtual fields from branchRootCtx — they reference the SelectMany source
				// context and resolve through CteTableContext.MakeExpression.
				var branchRootTableCtx = (CteTableContext)branchRootCtx;
				var branchKeyVFs = new Expression[allDependencies.Count];
				for (int k = 0; k < allDependencies.Count; k++)
					branchKeyVFs[k] = branchRootTableCtx.RegisterVirtualField(allDependencies[k]);

				Expression branchKeyBody = branchKeyVFs.Length == 1
					? branchKeyVFs[0]
					: BuildValueTupleNew(cteKeyType, branchKeyVFs);

				var branchEnvType        = typeof(KeyDetailEnvelope<,>).MakeGenericType(cteKeyType, info.DetailType);
				var branchEnvKeyField    = branchEnvType.GetField(nameof(KeyDetailEnvelope<,>.Key))!;
				var branchEnvDetailField = branchEnvType.GetField(nameof(KeyDetailEnvelope<,>.Detail))!;

				var envNew = Expression.New(
					branchEnvType.GetConstructor([cteKeyType, info.DetailType])!,
					[branchKeyBody, detailParameter],
					[branchEnvKeyField, branchEnvDetailField]);

				// Virtual fields in cteChildSequence reference branchRootCtx (the SelectMany source).
				// Don't replace ContextRef → smSourceParam — virtual fields need the CTE context
				// for resolution. The SelectMany builder handles ContextRef(source) correctly.

				var distinctSrcExpr = Expression.Call(
					Methods.LinqToDB.SelectDistinct.MakeGenericMethod(sourceType), branchRootRef);

				var detailSelector = _buildSelectManyDetailSelectorInfo
					.MakeGenericMethod(sourceType, info.DetailType)
					.InvokeExt<LambdaExpression>(null, [cteChildSequence, smSourceParam]);

				var resultSelector = Expression.Lambda(envNew, smSourceParam, detailParameter);

				var selectManyExpr = Expression.Call(
					Methods.Queryable.SelectManyProjection.MakeGenericMethod(sourceType, info.DetailType, branchEnvType),
					distinctSrcExpr,
					Expression.Quote(detailSelector!), Expression.Quote(resultSelector));

				var branchCteExpr = Expression.Call(
					Methods.LinqToDB.AsCte.MakeGenericMethod(branchEnvType), selectManyExpr);

				// 3. Build branch CTE context and collect detail placeholders as virtual fields
				var branchCteCtx      = BuildSequence(new BuildInfo((IBuildContext?)null, branchCteExpr, new SelectQuery()));
				var branchCteTableCtx = (CteTableContext)branchCteCtx;

				var cteDetailRef = Expression.Field(
					new ContextRefExpression(branchEnvType, branchCteTableCtx), branchEnvDetailField);
				var builtDetail  = BuildSqlExpression(branchCteTableCtx, cteDetailRef, BuildPurpose.Expression, BuildFlags.ForSetProjection);

				var rawPlaceholders = CollectDistinctPlaceholders(builtDetail, false);

				if (rawPlaceholders.Count == 0)
					continue;

				var placeholderVFs = new List<MemberExpression>(rawPlaceholders.Count);
				for (int c = 0; c < rawPlaceholders.Count; c++)
					placeholderVFs.Add(branchCteTableCtx.RegisterVirtualField(rawPlaceholders[c]));

				// Register RN as virtual field using PlaceholderVFs for ORDER BY
				MemberExpression? branchRnVF = null;
				if (info.OrderBy != null && info.OrderBy.Count > 0)
				{
					var rnDetailRef = Expression.Field(
						new ContextRefExpression(branchEnvType, branchCteTableCtx), branchEnvDetailField);

					var rnOrderByList = new List<(Expression expr, bool descending)>();
					var rnParam = Expression.Parameter(info.DetailType, "rn_d");
					foreach (var (lambda, descending) in info.OrderBy)
					{
						// Build ORDER BY body: replace lambda param with CTE Detail field ref
						var body = lambda.GetBody(rnParam);
						body = body.Transform(
							(rnParam, rnDetailRef),
							static (ctx, e) => e == ctx.rnParam ? ctx.rnDetailRef : e);
						rnOrderByList.Add((body, descending));
					}

					var rnExpr = WindowFunctionHelpers.BuildRowNumber([], rnOrderByList.ToArray());
					branchRnVF = branchCteTableCtx.RegisterVirtualField(rnExpr);
				}

				var parentBranchIdx = branches.Count;
				branches.Add(new CteUnionBranch
				{
					EagerLoad            = info.EagerLoad,
					BuiltDetailExpr      = builtDetail,
					DetailContext        = branchCteCtx,
					DetailType           = info.DetailType,
					KeyType              = info.MainKeyExpression.Type,
					MainKeys             = info.MainKeys,
					Placeholders         = rawPlaceholders,
					PlaceholderVFs       = placeholderVFs,
					BranchCteExpr        = branchCteExpr,
					BranchCteType        = branchEnvType,
					BranchEnvKeyField    = branchEnvKeyField,
					BranchEnvDetailField = branchEnvDetailField,
					RnVirtualField       = branchRnVF,
					OrderBy              = info.OrderBy,
				});

				// Detect nested eager loads
				var pendingNested = new Queue<(int parentIdx, Expression detail, IBuildContext ctx)>();
				pendingNested.Enqueue((parentBranchIdx, builtDetail, branchCteCtx));

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

						var nestedExpandedSeq = ExpandContexts(curCtx, nestedEL.SequenceExpression);

						var nestedDeps = new HashSet<Expression>(ExpressionEqualityComparer.Instance);
						CollectDependencies(curCtx, nestedExpandedSeq, nestedDeps);

						if (nestedDeps.Count == 0)
							throw new LinqToDBException("CteUnion: Cannot determine nested correlation — no dependencies found.");

						var nestedMainKeys = new Expression[nestedDeps.Count];
						{
							int nki = 0;
							foreach (var dep in nestedDeps)
								nestedMainKeys[nki++] = dep;
						}

						Expression nestedMainKeyExpression = nestedMainKeys.Length == 1
							? nestedMainKeys[0]
							: GenerateKeyExpression(nestedMainKeys, 0);

						// Build nested branch CTE: parentBranchCTE.SelectMany(pd => child, (pd, d) => KeyDetailEnvelope(key, d)).AsCte()
						var parentBranch     = branches[curParentIdx];
						var parentCteType    = parentBranch.BranchCteType!;

						// Build per-nested-branch CTE source from parent branch CTE
						var nestedSrcCtx = BuildSequence(new BuildInfo((IBuildContext?)null, parentBranch.BranchCteExpr!, new SelectQuery()));
						var nestedSrcRef = new ContextRefExpression(typeof(IQueryable<>).MakeGenericType(parentCteType), nestedSrcCtx);
						var nestedSmParam = Expression.Parameter(parentCteType, "pd");
						var nestedDetailParam = Expression.Parameter(nestedDetailType, "nd");

						// Register nested deps as virtual fields on nestedSrcCtx (parent branch CTE).
						// Same pattern as non-nested: virtual fields resolve through CTE context.
						var nestedSrcTableCtx = (CteTableContext)nestedSrcCtx;
						var nestedDepVFs = new Expression[nestedMainKeys.Length];
						for (int nk = 0; nk < nestedMainKeys.Length; nk++)
							nestedDepVFs[nk] = nestedSrcTableCtx.RegisterVirtualField(nestedMainKeys[nk]);

						var nestedDepToVF = new Dictionary<Expression, Expression>(ExpressionEqualityComparer.Instance);
						for (int nk = 0; nk < nestedMainKeys.Length; nk++)
							nestedDepToVF[nestedMainKeys[nk]] = nestedDepVFs[nk];

						var nestedChildSeq = nestedExpandedSeq.Transform(
							nestedDepToVF,
							static (map, e) => map.TryGetValue(e, out var r) ? r : e);

						// Key body from virtual fields
						var nestedCteKeyType = nestedMainKeyExpression.Type;
						Expression nestedKeyBody = nestedDepVFs.Length == 1
							? nestedDepVFs[0]
							: BuildValueTupleNew(nestedCteKeyType, nestedDepVFs);

						// Build KeyDetailEnvelope
						var nestedEnvType        = typeof(KeyDetailEnvelope<,>).MakeGenericType(nestedCteKeyType, nestedDetailType);
						var nestedEnvKeyField    = nestedEnvType.GetField(nameof(KeyDetailEnvelope<,>.Key))!;
						var nestedEnvDetailField = nestedEnvType.GetField(nameof(KeyDetailEnvelope<,>.Detail))!;

						var nestedEnvNew = Expression.New(
							nestedEnvType.GetConstructor([nestedCteKeyType, nestedDetailType])!,
							[nestedKeyBody, nestedDetailParam],
							[nestedEnvKeyField, nestedEnvDetailField]);

						var nestedDistinctSrc = Expression.Call(
							Methods.LinqToDB.SelectDistinct.MakeGenericMethod(parentCteType), nestedSrcRef);

						var nestedDetailSelector = _buildSelectManyDetailSelectorInfo
							.MakeGenericMethod(parentCteType, nestedDetailType)
							.InvokeExt<LambdaExpression>(null, [nestedChildSeq, nestedSmParam]);

						var nestedResultSelector = Expression.Lambda(nestedEnvNew, nestedSmParam, nestedDetailParam);

						var nestedSelectManyExpr = Expression.Call(
							Methods.Queryable.SelectManyProjection.MakeGenericMethod(parentCteType, nestedDetailType, nestedEnvType),
							nestedDistinctSrc,
							Expression.Quote(nestedDetailSelector!), Expression.Quote(nestedResultSelector));

						var nestedBranchCteExpr = Expression.Call(
							Methods.LinqToDB.AsCte.MakeGenericMethod(nestedEnvType), nestedSelectManyExpr);

						// Build nested branch CTE context
						var nestedBranchCteCtx      = BuildSequence(new BuildInfo((IBuildContext?)null, nestedBranchCteExpr, new SelectQuery()));
						var nestedBranchCteTableCtx = (CteTableContext)nestedBranchCteCtx;

						var nestedCteDetailRef = Expression.Field(
							new ContextRefExpression(nestedEnvType, nestedBranchCteTableCtx), nestedEnvDetailField);
						var nestedBuiltDetail = BuildSqlExpression(nestedBranchCteTableCtx, nestedCteDetailRef, BuildPurpose.Expression, BuildFlags.ForSetProjection);

						var nestedPlaceholders = CollectDistinctPlaceholders(nestedBuiltDetail, false);
						if (nestedPlaceholders.Count == 0) continue;

						var nestedPlaceholderVFs = new List<MemberExpression>(nestedPlaceholders.Count);
						for (int nc = 0; nc < nestedPlaceholders.Count; nc++)
							nestedPlaceholderVFs.Add(nestedBranchCteTableCtx.RegisterVirtualField(nestedPlaceholders[nc]));

						// Register RN for nested branch
						var nestedOrderBy = CollectOrderBy(nestedEL.SequenceExpression);
						MemberExpression? nestedRnVF = null;
						if (nestedOrderBy != null && nestedOrderBy.Count > 0)
						{
							var nestedRnDetailRef = Expression.Field(
								new ContextRefExpression(nestedEnvType, nestedBranchCteTableCtx), nestedEnvDetailField);

							var nestedRnOrderBy = new List<(Expression expr, bool descending)>();
							var nestedRnParam = Expression.Parameter(nestedDetailType, "rn_nd");
							foreach (var (lambda, descending) in nestedOrderBy)
							{
								var body = lambda.GetBody(nestedRnParam);
								body = body.Transform(
									(nestedRnParam, nestedRnDetailRef),
									static (ctx, e) => e == ctx.nestedRnParam ? ctx.nestedRnDetailRef : e);
								nestedRnOrderBy.Add((body, descending));
							}

							nestedRnVF = nestedBranchCteTableCtx.RegisterVirtualField(
								WindowFunctionHelpers.BuildRowNumber([], nestedRnOrderBy.ToArray()));
						}

						// Compute MainKeyPlaceholderIndices: for each nested dep, find its
						// index in the parent's PlaceholderVFs by registering it on the same
						// shared CteContext and matching by reference equality.
						// Match nested dep VFs to parent PlaceholderVFs by virtual field property name.
						// Both share the same CteContext (same CteClause), so VF names uniquely
						// identify CTE fields. Different CteTableContext instances produce different
						// MemberExpression objects but with the same SpecialPropertyInfo name.
						var nestedKeyPhIdxs = new int[nestedMainKeys.Length];
						for (int nk = 0; nk < nestedMainKeys.Length; nk++)
						{
							nestedKeyPhIdxs[nk] = -1;
							var nestedVFName = ((MemberExpression)nestedDepVFs[nk]).Member.Name;
							var parentVFs = curParentBranch.PlaceholderVFs;
							for (int pvi = 0; pvi < parentVFs.Count; pvi++)
							{
								if (string.Equals(parentVFs[pvi].Member.Name, nestedVFName, StringComparison.Ordinal))
								{
									nestedKeyPhIdxs[nk] = pvi;
									break;
								}
							}
						}

						var nestedBranchIdx = branches.Count;
						branches.Add(new CteUnionBranch
						{
							EagerLoad            = nestedEL,
							BuiltDetailExpr      = nestedBuiltDetail,
							DetailContext        = nestedBranchCteCtx,
							DetailType           = nestedDetailType,
							KeyType              = nestedMainKeyExpression.Type,
							MainKeys             = nestedMainKeys,
							MainKeyPlaceholderIndices = nestedKeyPhIdxs,
							Placeholders         = nestedPlaceholders,
							PlaceholderVFs       = nestedPlaceholderVFs,
							BranchCteExpr        = nestedBranchCteExpr,
							BranchCteType        = nestedEnvType,
							BranchEnvKeyField    = nestedEnvKeyField,
							BranchEnvDetailField = nestedEnvDetailField,
							RnVirtualField       = nestedRnVF,
							OrderBy              = nestedOrderBy,
							IsNested             = true,
						});

						curParentBranch.NestedEagerLoads.Add(new NestedEagerLoadInfo
						{
							EagerLoad         = nestedEL,
							NestedBranchIndex = nestedBranchIdx,
						});

						pendingNested.Enqueue((nestedBranchIdx, nestedBuiltDetail, nestedBranchCteCtx));
					}
				}
			}

			if (branches.Count == 0)
				return null;

			// SqlPlaceholderExpressions in allDependencies come from Concat/SetOperations.
			// They reference the SetOperation's SQL fields directly.
			// They are added to KDE keys and remapped to CTE later.

			// Verify all branches share the same key type
			var firstKeyType = branches[0].KeyType;
			if (branches.Exists(b => b.KeyType != firstKeyType))
				return null;

			// Phase 3a: Build main expression to discover parent SQL placeholders.
			// The parent branch in the UNION ALL carries these columns (non-eager-load fields).
			var mainRef          = new ContextRefExpression(sourceType, rootCteTableCtx);
			var mainBuiltExpr    = BuildSqlExpression(rootCteTableCtx, mainRef, BuildPurpose.Expression, BuildFlags.ForSetProjection);
			var mainPlaceholders = CollectDistinctPlaceholders(mainBuiltExpr, false);

			// Note: allDependencies only contains actual correlation columns (from CollectDependencies).
			// Parent entity data columns are in CTE.Data — they don't need to be in the Key.

			// Carrier key type matches CTE key type
			var carrierKeyType = cteKeyType;

			// Phase 3b: Build carrier type with slot reuse
			// Slots: 0=setId, 1=key, 2=RN. Data slots start at 3.
			const int dataSlotOffset = 3;
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
						if (slotOwners[s] != b && slotTypes[s + dataSlotOffset] == colType)
						{
							// Check this slot isn't already used by this branch
							var alreadyUsed = false;
							for (int pc = 0; pc < c; pc++)
							{
								if (slotMaps[b][pc] == s + dataSlotOffset)
								{
									alreadyUsed = true;
									break;
								}
							}

							if (!alreadyUsed)
							{
								reusedSlot = s + dataSlotOffset;
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

					// Build the lookup key using pre-computed MainKeyPlaceholderIndices (no member name matching).
					var lookupKeyExprs = new List<Expression>();
					var keyPhIdxs = nestedBranch.MainKeyPlaceholderIndices;
					for (int ki = 0; ki < nestedBranch.MainKeys.Length; ki++)
					{
						if (keyPhIdxs != null && ki < keyPhIdxs.Length && keyPhIdxs[ki] >= 0)
							lookupKeyExprs.Add(branch.Placeholders[keyPhIdxs[ki]]);
						else
							lookupKeyExprs.Add(nestedBranch.MainKeys[ki]);
					}

					var lookupKey = lookupKeyExprs.Count == 1
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
						[typeof(object), nestedBranch.DetailType],
						lookupExpr, castLambda);

					lookupExpr = Expression.Call(
						typeof(Enumerable), nameof(Enumerable.ToList),
						[nestedBranch.DetailType],
						lookupExpr);

					if (nestedBranch.OrderBy != null)
						lookupExpr = ApplyEnumerableOrderBy(lookupExpr, nestedBranch.OrderBy);

					lookupExpr = SqlAdjustTypeExpression.AdjustType(lookupExpr, nested.EagerLoad.Type, MappingSchema);

					branch.BuiltDetailExpr = branch.BuiltDetailExpr.Transform(
						(nested.EagerLoad, lookupExpr),
						static (ctx, e) => e is SqlEagerLoadExpression sel && sel == ctx.EagerLoad ? ctx.lookupExpr : e);
				}
			}

			// Phase 5: Build per-branch CTEs and UNION ALL carrier.
			// Each branch: rootCTE.SelectMany(…, (kd,d) => d) → .AsCte() → .Select(…) → carrier
			// Nested branches chain from parent branch's CTE instead of the root parent CTE.

			Expression? concatExpr = null;

			// Phase 5: Build carrier from pre-built branch CTEs.
			// Branch CTEs were already constructed in Phase 2b with KeyDetailEnvelope.
			for (int b = 0; b < branches.Count; b++)
			{
				var branch         = branches[b];
				var branchEnvType  = branch.BranchCteType!;
				var branchEnvKeyField   = branch.BranchEnvKeyField!;

				// Build carrier from the pre-built branch CTE context
				var cteTableCtx = (CteTableContext)branch.DetailContext;

				var cSelectParam = Expression.Parameter(branchEnvType, "c");

				var args = new Expression[carrierTypes.Length];
				args[0] = Expression.Constant(b); // setId

				// Key: direct field access on carrier lambda parameter
				var cKeyAccess = Expression.Field(cSelectParam, branchEnvKeyField);
				args[1] = cKeyAccess.Type != carrierKeyType ? Expression.Convert(cKeyAccess, carrierKeyType) : cKeyAccess;

				// RN: register as virtual field
				// RN: use pre-registered RnVirtualField from Phase 2b (if available),
				// otherwise register constant 0L
				MemberExpression branchRnVF;
				if (branch.RnVirtualField != null)
				{
					branchRnVF = branch.RnVirtualField;
				}
				else
				{
					branchRnVF = cteTableCtx.RegisterVirtualField(Expression.Constant(0L));
				}

				args[2] = branchRnVF;

				// Data: use pre-registered PlaceholderVFs from Phase 2b
				for (int s = dataSlotOffset; s < args.Length; s++)
					args[s] = Expression.Default(carrierTypes[s]);

				for (int c = 0; c < branch.PlaceholderVFs.Count; c++)
				{
					var slotIdx = slotMaps[b][c];
					Expression access = branch.PlaceholderVFs[c];
					if (access.Type != carrierTypes[slotIdx])
						access = Expression.Convert(access, carrierTypes[slotIdx]);
					args[slotIdx] = access;
				}

				// Ensure all carrier args match expected types
				for (int s = 0; s < args.Length; s++)
				{
					if (args[s].Type != carrierTypes[s])
						args[s] = Expression.Convert(args[s], carrierTypes[s]);
				}

				var carrierNew = BuildValueTupleNew(carrierType, args);
				var carrierSelectLambda = Expression.Lambda(carrierNew, cSelectParam);

				var branchCarrierQuery = Expression.Call(
					Methods.Queryable.Select.MakeGenericMethod(branchEnvType, carrierType),
					new ContextRefExpression(typeof(IQueryable<>).MakeGenericType(branchEnvType), cteTableCtx),
					Expression.Quote(carrierSelectLambda));

				concatExpr = concatExpr == null
					? branchCarrierQuery
					: Expression.Call(Methods.Queryable.Concat.MakeGenericMethod(carrierType), concatExpr, branchCarrierQuery);
			}

			if (concatExpr == null)
				throw new LinqToDBException("CteUnion: No branches produced carrier expressions.");

			// Phase 5b: Add parent branch (setId = parentSetId, LAST in UNION ALL).
			// Parent data comes from the root CTE (which wraps buildContext — entity or SetOperation).
			var useParentBranch = mainPlaceholders.Count > 0;

			if (!useParentBranch)
				parentSetId = -1;

			if (useParentBranch)
			{
				// Parent data from root CTE via virtual fields (works for both entity and SetOperation).
				var parentBranchCtx      = BuildSequence(new BuildInfo((IBuildContext?)null, mainCteExpression, new SelectQuery()));
				var parentBranchTableCtx = (CteTableContext)parentBranchCtx;

				var parentParam = Expression.Parameter(sourceType, "p");
				var parentArgs  = new Expression[carrierTypes.Length];
				parentArgs[0] = Expression.Constant(parentSetId);

				// Key: register allDependencies as virtual fields on parent CTE context
				var parentKeyVFs = new Expression[allDependencies.Count];
				for (int k = 0; k < allDependencies.Count; k++)
					parentKeyVFs[k] = parentBranchTableCtx.RegisterVirtualField(allDependencies[k]);

				parentArgs[1] = parentKeyVFs.Length == 1
					? parentKeyVFs[0]
					: GenerateKeyExpression(parentKeyVFs, 0);

				// RN: register from Phase 4 rnVirtualField pattern
				{
					var parentRnOrderBy = new List<(Expression expr, bool descending)>();
					for (int k = 0; k < parentKeyVFs.Length; k++)
						parentRnOrderBy.Add((parentKeyVFs[k], false));

					var parentRnExpr = parentRnOrderBy.Count > 0
						? WindowFunctionHelpers.BuildRowNumber([], parentRnOrderBy.ToArray())
						: Expression.Constant(0L);

					parentArgs[2] = parentBranchTableCtx.RegisterVirtualField(parentRnExpr);
				}

				for (int s = dataSlotOffset; s < parentArgs.Length; s++)
					parentArgs[s] = Expression.Default(carrierTypes[s]);

				// Data: build from the parent CTE context itself (not external mainPlaceholders)
				{
					var parentCteRef   = new ContextRefExpression(sourceType, parentBranchTableCtx);
					var parentBuiltCte = BuildSqlExpression(parentBranchTableCtx, parentCteRef, BuildPurpose.Expression, BuildFlags.ForSetProjection);
					var parentCtePhs   = CollectDistinctPlaceholders(parentBuiltCte, false);

					for (int c = 0; c < parentCtePhs.Count && c < mainPlaceholders.Count; c++)
					{
						var slotIdx = parentSlotMap[c];
						Expression access = parentBranchTableCtx.RegisterVirtualField(parentCtePhs[c]);

						if (access.Type != carrierTypes[slotIdx])
							access = Expression.Convert(access, carrierTypes[slotIdx]);
						parentArgs[slotIdx] = access;
					}
				}

				// Ensure all carrier args match expected types
				for (int s = 0; s < parentArgs.Length; s++)
				{
					if (parentArgs[s].Type != carrierTypes[s])
						parentArgs[s] = Expression.Convert(parentArgs[s], carrierTypes[s]);
				}

				var parentCarrierNew = BuildValueTupleNew(carrierType, parentArgs);
				var parentSelectLambda = Expression.Lambda(parentCarrierNew, parentParam);

				var parentBranchQuery = Expression.Call(
					Methods.Queryable.Select.MakeGenericMethod(sourceType, carrierType),
					new ContextRefExpression(typeof(IQueryable<>).MakeGenericType(sourceType), parentBranchCtx),
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

			if (useParentBranch)
			{
				// Phase 7a: Single-query mode — store UNION ALL info for Phase 2 (applied in BuildQuery).
				state.CteUnionInfo = new CteUnionPhase2Info
				{
					CombinedSequence   = combinedSequence,
					KeyType            = cteKeyType,
					CarrierType        = carrierType,
					CarrierTypes       = carrierTypes,
					Branches           = branches.ToArray(),
					SlotMaps           = slotMaps,
					ParentSetId        = parentSetId,
					ParentSlotMap      = parentSlotMap,
				};
				state.HasCteUnionQuery = true;

				// Request that the main query be marked finalized on commit — its statement shares the
				// SelectQuery with the CTE body and must not be finalized separately (mutations like
				// Oracle 11's ReplaceTakeSkipWithRowNum would corrupt the shared CTE body). The union
				// query built in SetupCteUnionQuery has its own independent finalization. The actual
				// _query.IsFinalized = true is applied only when CompleteEagerLoadingExpressions commits.
				state.QueryFinalizedRequested = true;
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

					// Use full allDependencies key to match carrier key
					var keyExpr = allDependencies.Count == 1
						? allDependencies[0]
						: GenerateKeyExpression(allDependencies.ToArray(), 0);

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
						[typeof(object), detailType],
						resultExpr, castLambda);

					resultExpr = Expression.Call(
						typeof(Enumerable), nameof(Enumerable.ToList),
						[detailType],
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
							allDependencies.Count == 1 ? allDependencies[0] : GenerateKeyExpression(allDependencies.ToArray(), 0),
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
					[typeof(object), detailType],
					resultExpr, castLambda);

				// ToList() to match expected List<T> type
				resultExpr = Expression.Call(
					typeof(Enumerable), nameof(Enumerable.ToList),
					[detailType],
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
			ParameterExpression queryParameter,
			CteUnionPhase2Info  info)
		{
			try
			{
				_setupCteUnionQueryMethodInfo
					.MakeGenericMethod(typeof(T), info.KeyType, info.CarrierType)
					.InvokeExt(this, [query, sequence, finalized, preambles, preambleStartIndex, info, queryParameter]);
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
			//    expression with carrier slot access by positional matching.
			//    Collect placeholders from finalized in order — they correspond 1:1 to mainPlaceholders.
			var parentCarrierParam = Expression.Parameter(typeof(TCarrier), "pvt");

			var finalizedPlaceholders = CollectDistinctPlaceholders(finalized, false);
			var phToSlot = new Dictionary<SqlPlaceholderExpression, int>(finalizedPlaceholders.Count);
			for (int i = 0; i < finalizedPlaceholders.Count && i < info.ParentSlotMap.Length; i++)
				phToSlot[finalizedPlaceholders[i]] = info.ParentSlotMap[i];

			var parentReconstructed = finalized.Transform(
				(phToSlot, parentCarrierParam),
				static (ctx, e) =>
				{
					if (e is SqlPlaceholderExpression spe && ctx.phToSlot.TryGetValue(spe, out var slotIdx))
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

			var parentMapper = Expression.Lambda<Func<IQueryExpressions, object?[]?, TCarrier, object?[], T>>(
				parentReconstructed,
				QueryExpressionContainerParam,
				ParametersParam,
				parentCarrierParam, preambleArrayVar).CompileExpression();

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
					nestedSetIds0 = [..nestedIds0];
					nestedIds0.Sort();
					nestedIds0.Reverse(); // deepest first (BFS assigns higher indices to deeper levels)
					nestedProcessingOrder0 = nestedIds0.ToArray();
				}
			}

			// 6. Replace GetResultEnumerable with UNION ALL-based iterator.
			var preambleIdx0   = preambleStartIndex;
			var branchCount0   = branches.Length;
			var parentSetId0   = info.ParentSetId;

			// Override GetResultEnumerable (no SetRunQuery: CteUnion uses path-based reconstruction,
			// not column indices, so the standard mapper is never needed)
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
					carriers, setIdExtractor, parentSetId0, expr, ps, parentMapper, preambleResults!);
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
						return parentMapper(expr, ps, carrier, preambleResults!);
				}

				return default;
			};
		}

		sealed class CteUnionResultEnumerable<T, TCarrier> : IResultEnumerable<T>
		{
			readonly List<TCarrier>                                               _carriers;
			readonly Func<TCarrier, int>                                          _getSetId;
			readonly int                                                           _parentSetId;
			readonly IQueryExpressions                                             _expr;
			readonly object?[]?                                                    _ps;
			readonly Func<IQueryExpressions, object?[]?, TCarrier, object?[], T>  _parentMapper;
			readonly object?[]                                                     _preambleResults;

			public CteUnionResultEnumerable(
				List<TCarrier>                                                     carriers,
				Func<TCarrier, int>                                                getSetId,
				int                                                                parentSetId,
				IQueryExpressions                                                  expr,
				object?[]?                                                         ps,
				Func<IQueryExpressions, object?[]?, TCarrier, object?[], T>       parentMapper,
				object?[]                                                          preambleResults)
			{
				_carriers        = carriers;
				_getSetId        = getSetId;
				_parentSetId     = parentSetId;
				_expr            = expr;
				_ps              = ps;
				_parentMapper    = parentMapper;
				_preambleResults = preambleResults;
			}

			public IEnumerator<T> GetEnumerator()
			{
				foreach (var carrier in _carriers)
				{
					if (_getSetId(carrier) == _parentSetId)
						yield return _parentMapper(_expr, _ps, carrier, _preambleResults);
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
			public override bool IsInlined => true;

			public override object Execute(IDataContext dataContext, IQueryExpressions expressions, object?[]? parameters, object?[]? preambles)
				=> Array.Empty<object?>();

			public override Task<object> ExecuteAsync(IDataContext dataContext, IQueryExpressions expressions, object?[]? parameters, object[]? preambles, CancellationToken cancellationToken)
				=> Task.FromResult<object>(Array.Empty<object?>());

			public override void GetUsedParametersAndValues(ICollection<SqlParameter> parameters, ICollection<SqlValue> values) { }
		}

		/// <summary>Pass 1 branch metadata collected before root CTE creation.</summary>
		sealed class BranchInfo
		{
			public SqlEagerLoadExpression              EagerLoad          = null!;
			public Expression                          ExpandedSequence   = null!;
			public Expression?                         ExpandedPredicate;
			public Type                                DetailType         = null!;
			public Expression[]                        MainKeys           = null!;
			public Expression                          MainKeyExpression  = null!;
			public List<(LambdaExpression, bool)>?     OrderBy;
		}

		/// <summary>Pass 2 branch with pre-built CTE and virtual fields.</summary>
		sealed class CteUnionBranch
		{
			public SqlEagerLoadExpression              EagerLoad          = null!;
			public Expression                          BuiltDetailExpr    = null!;
			public IBuildContext                        DetailContext      = null!;
			public Type                                DetailType         = null!;
			public Type                                KeyType            = null!;
			public Expression[]                        MainKeys           = null!;
			public int[]?                              MainKeyPlaceholderIndices; // indices into parent Placeholders
			public List<SqlPlaceholderExpression>       Placeholders       = null!;
			public List<MemberExpression>              PlaceholderVFs     = null!;
			public Expression?                         BranchCteExpr;
			public Type?                               BranchCteType;
			public FieldInfo?                          BranchEnvKeyField;
			public FieldInfo?                          BranchEnvDetailField;
			public MemberExpression?                   RnVirtualField;
			public List<(LambdaExpression, bool)>?     OrderBy;
			public bool                                IsNested;
			public List<NestedEagerLoadInfo>?          NestedEagerLoads;
		}

		sealed class NestedEagerLoadInfo
		{
			public SqlEagerLoadExpression EagerLoad    = null!;
			public int                    NestedBranchIndex;
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

	}
}
