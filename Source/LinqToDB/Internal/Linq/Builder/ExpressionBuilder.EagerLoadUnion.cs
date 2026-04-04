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

			var branchInfos = new List<(
				SqlEagerLoadExpression eagerLoad,
				Expression expandedSequence,
				Expression? expandedPredicate,
				Type detailType,
				Expression[] mainKeys,
				Expression mainKeyExpression,
				List<(LambdaExpression, bool)>? orderBy
			)>();

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

				branchInfos.Add((eagerLoad, expandedSequence, expandedPredicate, detailType, mainKeys, mainKeyExpression, CollectOrderBy(sequenceExpression)));
			}

			if (branchInfos.Count == 0 || allDependencies.Count == 0)
				return null;

			// Build root CTE — buildContext is used ONLY here. After this, everything uses CTE contexts.
			var sourceType        = buildContext.ElementType;
			var mainExpression    = new ContextRefExpression(typeof(IQueryable<>).MakeGenericType(sourceType), buildContext);
			var mainCteExpression = Expression.Call(Methods.LinqToDB.AsCte.MakeGenericMethod(sourceType), mainExpression);

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
				var cteChildSequence = info.expandedSequence.Transform(
					depToVF,
					static (map, e) => map.TryGetValue(e, out var replacement) ? replacement : e);

				var branchRootCtx = BuildSequence(new BuildInfo((IBuildContext?)null, mainCteExpression, new SelectQuery()));
				cteChildSequence = SequenceHelper.ReplaceContext(cteChildSequence, rootCteTableCtx, branchRootCtx);

				// 2. Build SelectMany with KeyDetailEnvelope result selector
				var branchRootRef   = new ContextRefExpression(typeof(IQueryable<>).MakeGenericType(sourceType), branchRootCtx);
				var smSourceParam   = Expression.Parameter(sourceType, "kd");
				var detailParameter = Expression.Parameter(info.detailType, "d");

				// Key body: kd.Member for each dependency
				var branchCteKeyType = info.mainKeyExpression.Type;
				var remappedKeys     = new Expression[info.mainKeys.Length];
				for (int k = 0; k < info.mainKeys.Length; k++)
				{
					var dep = info.mainKeys[k];
					if (dep is MemberExpression me && me.Expression is ContextRefExpression)
					{
						var member = sourceType.GetProperty(me.Member.Name)
							?? (MemberInfo?)sourceType.GetField(me.Member.Name);
						remappedKeys[k] = member != null
							? Expression.MakeMemberAccess(smSourceParam, member)
							: dep;
					}
					else
					{
						remappedKeys[k] = dep;
					}
				}

				Expression branchKeyBody = remappedKeys.Length == 1
					? remappedKeys[0]
					: BuildValueTupleNew(branchCteKeyType, remappedKeys);

				var branchEnvType        = typeof(KeyDetailEnvelope<,>).MakeGenericType(branchCteKeyType, info.detailType);
				var branchEnvKeyField    = branchEnvType.GetField(nameof(KeyDetailEnvelope<int, int>.Key))!;
				var branchEnvDetailField = branchEnvType.GetField(nameof(KeyDetailEnvelope<int, int>.Detail))!;

				var envNew = Expression.New(
					branchEnvType.GetConstructor(new[] { branchCteKeyType, info.detailType })!,
					new Expression[] { branchKeyBody, detailParameter },
					new MemberInfo[] { branchEnvKeyField, branchEnvDetailField });

				// Virtual fields in cteChildSequence reference branchRootCtx (the SelectMany source).
				// Don't replace ContextRef → smSourceParam — virtual fields need the CTE context
				// for resolution. The SelectMany builder handles ContextRef(source) correctly.

				var distinctSrcExpr = Expression.Call(
					Methods.LinqToDB.SelectDistinct.MakeGenericMethod(sourceType), branchRootRef);

				var detailSelector = _buildSelectManyDetailSelectorInfo
					.MakeGenericMethod(sourceType, info.detailType)
					.InvokeExt<LambdaExpression>(null, new object[] { cteChildSequence, smSourceParam });

				var resultSelector = Expression.Lambda(envNew, smSourceParam, detailParameter);

				var selectManyExpr = Expression.Call(
					Methods.Queryable.SelectManyProjection.MakeGenericMethod(sourceType, info.detailType, branchEnvType),
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

				var parentBranchIdx = branches.Count;
				branches.Add(new CteUnionBranch
				{
					EagerLoad          = info.eagerLoad,
					ExpandedSequence   = cteChildSequence,
					ExpandedPredicate  = info.expandedPredicate,
					BuiltDetailExpr    = builtDetail,
					DetailContext      = branchCteCtx,
					DetailType         = info.detailType,
					KeyType            = info.mainKeyExpression.Type,
					MainKeyExpression  = info.mainKeyExpression,
					MainKeys           = info.mainKeys,
					Placeholders       = rawPlaceholders,
					PlaceholderVFs     = placeholderVFs,
					BranchRootCtx      = branchRootCtx,
					BranchCteExpr      = branchCteExpr,
					BranchCteType      = branchEnvType,
					BranchEnvKeyField  = branchEnvKeyField,
					BranchEnvDetailField = branchEnvDetailField,
					OrderBy            = info.orderBy,
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
						if (nestedDetailType == null)
						{
							return null;
						}

						var nestedExpandedSeq = ExpandContexts(curCtx, nestedEL.SequenceExpression);

						var nestedDetailCtx   = BuildSequence(new BuildInfo((IBuildContext?)null, nestedExpandedSeq, new SelectQuery()));
						var nestedDetailRef   = new ContextRefExpression(nestedDetailType, nestedDetailCtx);
						var nestedBuiltDetail = BuildSqlExpression(nestedDetailCtx, nestedDetailRef, BuildPurpose.Expression, BuildFlags.ForSetProjection);

						var nestedPlaceholders = CollectDistinctPlaceholders(nestedBuiltDetail, false);
						if (nestedPlaceholders.Count == 0) continue;

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

						pendingNested.Enqueue((nestedBranchIdx, nestedBuiltDetail, nestedDetailCtx));
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

			// Phase 5: Build per-branch CTEs and UNION ALL carrier.
			// Each branch: rootCTE.SelectMany(…, (kd,d) => d) → .AsCte() → .Select(…) → carrier
			// Nested branches chain from parent branch's CTE instead of the root parent CTE.

			Expression? concatExpr = null;

			// Per-branch CTE expressions and types (needed for nested branches to chain from parent branch CTE)
			var branchCteExpressions = new Expression?[branches.Count];
			var branchCteTypes       = new Type[branches.Count];

			for (int b = 0; b < branches.Count; b++)
			{
				var branch = branches[b];

				// --- Step 1: Build retargeted child sequence through parent CTE ---
				var branchCtx = BuildSequence(new BuildInfo((IBuildContext?)null, mainCteExpression, new SelectQuery()));
				var branchRef = new ContextRefExpression(sourceType, branchCtx);

				Expression retargetedSequence;
				Type       branchSourceType;

				if (branch.IsNested && branch.ExpandedNestedSequence != null)
				{
					// Nested branch chains from parent branch's CTE (KeyDetailEnvelope type).
					// Access parent entity via .Detail field.
					var parentBranchIdx  = branch.ParentBranchIndex;
					var parentBranchCte  = branchCteExpressions[parentBranchIdx]!;
					var parentBranchType = branchCteTypes[parentBranchIdx]; // KeyDetailEnvelope<TKey, TParentDetail>

					var parentDetailField = parentBranchType.GetField(nameof(KeyDetailEnvelope<int, int>.Detail))!;
					var pdParam = Expression.Parameter(parentBranchType, "pd");
					var pdDetail = Expression.Field(pdParam, parentDetailField);

					// Collect parent ref types from branch.MainKeys (the correlation keys).
					var parentRefTypes = new HashSet<Type>();
					parentRefTypes.Add(parentDetailField.FieldType);
					foreach (var mk in branch.MainKeys)
					{
						if (mk is MemberExpression me2 && me2.Expression is ContextRefExpression cre2)
							parentRefTypes.Add(cre2.Type);
						else if (mk is ContextRefExpression cre3)
							parentRefTypes.Add(cre3.Type);
					}

					// Replace ContextRef(parentType).Member → pd.Detail.Member
					var nestedSeqBody = branch.ExpandedNestedSequence.Transform(
						(parentRefTypes, pdDetail),
						static (ctx, e) =>
						{
							if (e is MemberExpression me && me.Expression is ContextRefExpression cre
								&& ctx.parentRefTypes.Contains(cre.Type))
							{
								var member = (MemberInfo?)ctx.pdDetail.Type.GetProperty(me.Member.Name)
									?? ctx.pdDetail.Type.GetField(me.Member.Name)
									?? me.Member;
								return Expression.MakeMemberAccess(ctx.pdDetail, member);
							}

							if (e is ContextRefExpression cre2 && ctx.parentRefTypes.Contains(cre2.Type))
								return ctx.pdDetail;
							return e;
						});

					nestedSeqBody = StripMaterialization(nestedSeqBody);

					retargetedSequence = nestedSeqBody;
					branchSourceType   = branch.DetailType;
				}
				else
				{
					// Non-nested: replace ALL parent refs with CTE virtual fields using depToVF,
					// then retarget virtual fields from rootCteTableCtx → branchCtx so they
					// reference the SqlCteTable that's in the SelectMany's FROM clause.
					retargetedSequence = branch.ExpandedSequence.Transform(
						depToVF,
						static (map, e) => map.TryGetValue(e, out var replacement) ? replacement : e);

					retargetedSequence = SequenceHelper.ReplaceContext(retargetedSequence, rootCteTableCtx, branchCtx);

					if (branch.ExpandedPredicate != null)
					{
						var hasParentRefs = branch.ExpandedPredicate.Find(
							depToVF,
							static (map, e) => map.ContainsKey(e)) != null;

						if (hasParentRefs)
						{
							var retargetedPredicate = branch.ExpandedPredicate.Transform(
								depToVF,
								static (map, e) => map.TryGetValue(e, out var replacement) ? replacement : e);

							retargetedPredicate = SequenceHelper.ReplaceContext(retargetedPredicate, rootCteTableCtx, branchCtx);

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

				// --- Step 2: Build per-branch CTE with KeyDetailEnvelope ---
				// Result selector: (kd, d) => new KeyDetailEnvelope(key, d)
				// CTE element type = KeyDetailEnvelope<TKey, TDetail>

				Expression selectManySrcExpr;
				Type       selectManySrcType;
				Type       branchCteKeyType;

				if (branch.IsNested && branch.ExpandedNestedSequence != null)
				{
					var parentBranchCte  = branchCteExpressions[branch.ParentBranchIndex]!;
					var parentBranchType = branchCteTypes[branch.ParentBranchIndex];
					selectManySrcType = parentBranchType;
					branchCteKeyType  = branch.KeyType;

					var parentBranchCteCtx = BuildSequence(new BuildInfo((IBuildContext?)null, parentBranchCte, new SelectQuery()));
					selectManySrcExpr = new ContextRefExpression(typeof(IQueryable<>).MakeGenericType(parentBranchType), parentBranchCteCtx);
				}
				else
				{
					selectManySrcType = sourceType;
					branchCteKeyType  = cteKeyType;
					selectManySrcExpr = new ContextRefExpression(typeof(IQueryable<>).MakeGenericType(sourceType), branchCtx);
				}

				var smSourceParam   = Expression.Parameter(selectManySrcType, branch.IsNested ? "pd" : "kd");
				var detailParameter = Expression.Parameter(branchSourceType, "d");

				// Build key body for KeyDetailEnvelope
				Expression branchKeyBody;

				if (branch.IsNested && branch.ExpandedNestedSequence != null)
				{
					// Nested key: pd.Detail.Member for each correlation column
					var parentDetailField = selectManySrcType.GetField(nameof(KeyDetailEnvelope<int, int>.Detail))!;
					var smDetail = Expression.Field(smSourceParam, parentDetailField);
					var parentDataType = parentDetailField.FieldType;

					var nestedKeyExprs = new List<Expression>();
					foreach (var dep in branch.MainKeys)
					{
						if (dep is MemberExpression me && me.Expression is ContextRefExpression)
						{
							var member = (MemberInfo?)parentDataType.GetProperty(me.Member.Name)
								?? parentDataType.GetField(me.Member.Name)
								?? me.Member;
							nestedKeyExprs.Add(Expression.MakeMemberAccess(smDetail, member));
						}
						else
							nestedKeyExprs.Add(dep);
					}

					branchKeyBody = nestedKeyExprs.Count == 1
						? nestedKeyExprs[0]
						: BuildValueTupleNew(branchCteKeyType, nestedKeyExprs.ToArray());

					// Remap retargetedSequence: pdParam → smSourceParam
					retargetedSequence = retargetedSequence.Transform(
						(smSourceParam, selectManySrcType),
						static (ctx, e) =>
						{
							if (e is ParameterExpression pe && pe.Type == ctx.selectManySrcType && pe != ctx.smSourceParam)
								return ctx.smSourceParam;
							return e;
						});
				}
				else
				{
					// Non-nested key: kd.Member for each allParentRef
					var remappedKeys = new Expression[allDependencies.Count];
					for (int k = 0; k < allDependencies.Count; k++)
					{
						var dep = allDependencies[k];
						if (dep is MemberExpression me && me.Expression is ContextRefExpression)
						{
							var member = selectManySrcType.GetProperty(me.Member.Name)
								?? (MemberInfo?)selectManySrcType.GetField(me.Member.Name);
							remappedKeys[k] = member != null
								? Expression.MakeMemberAccess(smSourceParam, member)
								: dep;
						}
						else
						{
							remappedKeys[k] = dep;
						}
					}

					branchKeyBody = remappedKeys.Length == 1
						? remappedKeys[0]
						: BuildValueTupleNew(cteKeyType, remappedKeys);

					// Remap retargetedSequence: branchRef → smSourceParam
					retargetedSequence = retargetedSequence.Transform(
						(branchRef, smSourceParam),
						static (ctx, e) =>
						{
							if (e is ContextRefExpression cre && cre.BuildContext == ctx.branchRef.BuildContext)
								return ctx.smSourceParam;
							return e;
						});
				}

				// Build KeyDetailEnvelope type and result selector
				var branchEnvType       = typeof(KeyDetailEnvelope<,>).MakeGenericType(branchCteKeyType, branchSourceType);
				var branchEnvKeyField   = branchEnvType.GetField(nameof(KeyDetailEnvelope<int, int>.Key))!;
				var branchEnvDetailField = branchEnvType.GetField(nameof(KeyDetailEnvelope<int, int>.Detail))!;

				var envNew = Expression.New(
					branchEnvType.GetConstructor(new[] { branchCteKeyType, branchSourceType })!,
					new Expression[] { branchKeyBody, detailParameter },
					new MemberInfo[] { branchEnvKeyField, branchEnvDetailField });

				// Apply SelectDistinct to prevent duplicates from parent key duplication
				var distinctSrcExpr = Expression.Call(
					Methods.LinqToDB.SelectDistinct.MakeGenericMethod(selectManySrcType),
					selectManySrcExpr);

				// Build SelectMany with result selector (kd, d) => new KeyDetailEnvelope(key, d)
				var detailSelector = _buildSelectManyDetailSelectorInfo
					.MakeGenericMethod(selectManySrcType, branchSourceType)
					.InvokeExt<LambdaExpression>(null, new object[] { retargetedSequence, smSourceParam });

				var resultSelector = Expression.Lambda(envNew, smSourceParam, detailParameter);

				var selectManyExpr = Expression.Call(
					Methods.Queryable.SelectManyProjection.MakeGenericMethod(selectManySrcType, branchSourceType, branchEnvType),
					distinctSrcExpr,
					Expression.Quote(detailSelector!), Expression.Quote(resultSelector));

				// Wrap in .AsCte() → per-branch CTE (element type = KeyDetailEnvelope)
				var branchCteExpr = Expression.Call(
					Methods.LinqToDB.AsCte.MakeGenericMethod(branchEnvType), selectManyExpr);

				branchCteExpressions[b] = branchCteExpr;
				branchCteTypes[b]       = branchEnvType;

				// --- Step 3: Build carrier from per-branch CTE ---
				// Key via envelope Key field, Data via envelope Detail field, RN via RegisterVirtualField.
				var branchCteCtx = BuildSequence(new BuildInfo((IBuildContext?)null, branchCteExpr, new SelectQuery()));
				var cteTableCtx  = (CteTableContext)branchCteCtx;

				var cSelectParam = Expression.Parameter(branchEnvType, "c");
				var ctxRef       = new ContextRefExpression(branchEnvType, cteTableCtx);

				var args = new Expression[carrierTypes.Length];
				args[0] = Expression.Constant(b); // setId

				// Key: direct field access on carrier lambda parameter (resolved by CTE MakeExpression)
				var cKeyAccess = Expression.Field(cSelectParam, branchEnvKeyField);
				args[1] = cKeyAccess.Type != carrierKeyType ? Expression.Convert(cKeyAccess, carrierKeyType) : cKeyAccess;

				// RN: register as virtual field via the CTE's Detail entity columns
				MemberExpression branchRnVF;
				{
					var detailRef = Expression.Field(ctxRef, branchEnvDetailField);

					var rnOrderByList = new List<(Expression expr, bool descending)>();
					if (branch.OrderBy != null && branch.OrderBy.Count > 0)
					{
						foreach (var (lambda, descending) in branch.OrderBy)
						{
							var body = lambda.GetBody(detailParameter);
							// Remap ORDER BY body: detailParameter → CTE Detail field access
							if (body is MemberExpression orderMe)
							{
								var detailMember = branchSourceType.GetProperty(orderMe.Member.Name)
									?? (MemberInfo?)branchSourceType.GetField(orderMe.Member.Name);
								if (detailMember != null)
									body = Expression.MakeMemberAccess(detailRef, detailMember);
							}
							rnOrderByList.Add((body, descending));
						}
					}

					Expression rnExpr = rnOrderByList.Count > 0
						? WindowFunctionHelpers.BuildRowNumber([], rnOrderByList.ToArray())
						: Expression.Constant(0L);

					branchRnVF = cteTableCtx.RegisterVirtualField(rnExpr);
				}

				args[2] = branchRnVF;

				// Data: build from CTE Detail field and register as virtual fields (positional match)
				var cteDetailRef     = Expression.Field(ctxRef, branchEnvDetailField);
				var builtFromCte     = BuildSqlExpression(cteTableCtx, cteDetailRef, BuildPurpose.Expression, BuildFlags.ForSetProjection);
				var ctePlaceholders  = CollectDistinctPlaceholders(builtFromCte, false);

				for (int s = DataSlotOffset; s < args.Length; s++)
					args[s] = Expression.Default(carrierTypes[s]);

				for (int c = 0; c < ctePlaceholders.Count && c < branch.Placeholders.Count; c++)
				{
					var slotIdx = slotMaps[b][c];
					Expression access = cteTableCtx.RegisterVirtualField(ctePlaceholders[c]);
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
					new ContextRefExpression(typeof(IQueryable<>).MakeGenericType(branchEnvType), branchCteCtx),
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

			// Use parent branch if we have placeholders to carry
			var useParentBranch = mainPlaceholders.Count > 0;

			if (!useParentBranch)
				parentSetId = -1;

			// SetOperation parents (Concat/Union) use cloning; entity CTE parents use RegisterVirtualField.
			var useCloneSourceParent = useParentBranch && isSetOpParent;

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

				// Build key: find the cloned mainPlaceholder(s) that correspond to allDependencies
				var parentKeyArgs = new Expression[allDependencies.Count];
				for (int k = 0; k < allDependencies.Count; k++)
				{
					var dep = allDependencies[k];
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
						parentKeyArgs[k] = clonedPh.Type != cteKeyType && allDependencies.Count == 1
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
				// Entity CTE parent branch (non-SetOperation): parent data from root CTE via virtual fields
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

					Expression parentRnExpr = parentRnOrderBy.Count > 0
						? WindowFunctionHelpers.BuildRowNumber([], parentRnOrderBy.ToArray())
						: Expression.Constant(0L);

					parentArgs[2] = parentBranchTableCtx.RegisterVirtualField(parentRnExpr);
				}

				for (int s = DataSlotOffset; s < parentArgs.Length; s++)
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
					BuildContext       = buildContext,
					RootCteTableCtx    = rootCteTableCtx,
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

					// Use full allDependencies key to match carrier key
					Expression keyExpr = allDependencies.Count == 1
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

			// Map finalized SqlPlaceholderExpressions to parent carrier slots via Path matching.
			// Finalized has buildContext paths; mainPlaceholders have rootCteTableCtx paths.
			// Transform finalized to remap paths from buildContext → rootCteTableCtx before matching.
			var parentReconstructed = finalized.Transform(
				(pathToSlot, parentCarrierParam, info.BuildContext, info.RootCteTableCtx),
				static (ctx, e) =>
				{
					if (e is SqlPlaceholderExpression spe && spe.Path != null)
					{
						var remappedPath = SequenceHelper.ReplaceContext(spe.Path, ctx.BuildContext, ctx.RootCteTableCtx);
						if (ctx.pathToSlot.TryGetValue(remappedPath, out var slotIdx))
						{
							var access = AccessValueTupleField(ctx.parentCarrierParam, slotIdx);
							if (access.Type != spe.ConvertType)
								access = Expression.Convert(access, spe.ConvertType);
							return access;
						}
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
			public IBuildContext                     BuildContext       = null!;
			public IBuildContext                     RootCteTableCtx    = null!;
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
			public List<MemberExpression>              PlaceholderVFs     = null!; // virtual fields on branch CTE
			public IBuildContext?                       BranchRootCtx;             // per-branch root CTE context
			public Expression?                         BranchCteExpr;             // branch CTE expression (AsCte)
			public Type?                               BranchCteType;             // KeyDetailEnvelope<TKey, TDetail>
			public FieldInfo?                          BranchEnvKeyField;         // KeyDetailEnvelope.Key field
			public FieldInfo?                          BranchEnvDetailField;      // KeyDetailEnvelope.Detail field
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
