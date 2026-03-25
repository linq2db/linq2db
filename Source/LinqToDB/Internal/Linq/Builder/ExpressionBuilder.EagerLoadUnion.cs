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

			// Phase 2: For each eager load, prepare its branch info with its OWN cloning context
			var branches = new List<CteUnionBranch>();

			foreach (var eagerLoad in cteUnionLoads)
			{
				var itemType = eagerLoad.Type.GetItemType();
				if (itemType == null)
					continue;

				var cloningCtx         = new CloningContext();
				var clonedParentCtx    = cloningCtx.CloneContext(buildContext);
				clonedParentCtx        = new EagerContext(new SubQueryContext(clonedParentCtx), buildContext.ElementType);

				var dependencies = new HashSet<Expression>(ExpressionEqualityComparer.Instance);

				var sequenceExpression = eagerLoad.SequenceExpression;
				sequenceExpression = ExpandContexts(buildContext, sequenceExpression);

				CollectDependencies(buildContext, sequenceExpression, dependencies);

				var correctedSequence  = cloningCtx.CloneExpression(sequenceExpression);
				var correctedPredicate = cloningCtx.CloneExpression(eagerLoad.Predicate);

				dependencies.AddRange(previousKeys);

				if (dependencies.Count == 0)
					continue;

				var mainKeys   = new Expression[dependencies.Count];
				var detailKeys = new Expression[dependencies.Count];

				int i = 0;
				foreach (var dependency in dependencies)
				{
					mainKeys[i]   = dependency;
					detailKeys[i] = cloningCtx.CloneExpression(dependency);
					++i;
				}

				var mainType   = clonedParentCtx.ElementType;
				var detailType = TypeHelper.GetEnumerableElementType(eagerLoad.Type);

				// Apply predicate if present
				if (correctedPredicate != null)
				{
					var predicateExpr = BuildSqlExpression(clonedParentCtx, correctedPredicate);

					if (predicateExpr is SqlPlaceholderExpression { Sql: ISqlPredicate predicateSql })
						clonedParentCtx.SelectQuery.Where.EnsureConjunction().Add(predicateSql);

					var childElementType = TypeHelper.GetEnumerableElementType(correctedSequence.Type) ?? correctedSequence.Type;
					var childParam       = Expression.Parameter(childElementType, "p_pred");
					var predicateLambda  = Expression.Lambda(
						correctedPredicate.Transform(
							(clonedParentCtx.ElementType, childParam),
							static (ctx, e) => e is ContextRefExpression cre && cre.BuildContext.ElementType == ctx.ElementType
								? ctx.childParam
								: e),
						childParam);

					correctedSequence = typeof(IQueryable).IsAssignableFrom(correctedSequence.Type)
						? Expression.Call(Methods.Queryable.Where.MakeGenericMethod(childElementType), correctedSequence, Expression.Quote(predicateLambda))
						: Expression.Call(Methods.Enumerable.Where.MakeGenericMethod(childElementType), correctedSequence, predicateLambda);
				}

				Expression mainKeyExpression = mainKeys.Length == 1
					? mainKeys[0]
					: GenerateKeyExpression(mainKeys, 0);

				var entityDescriptor = MappingSchema.GetEntityDescriptor(detailType, DataOptions.ConnectionOptions.OnEntityDescriptorCreated);

				branches.Add(new CteUnionBranch
				{
					EagerLoad          = eagerLoad,
					CloningContext     = cloningCtx,
					ClonedParentContext = clonedParentCtx,
					CorrectedSequence  = correctedSequence,
					MainType           = mainType,
					DetailType         = detailType,
					KeyType            = mainKeyExpression.Type,
					MainKeyExpression  = mainKeyExpression,
					DetailKeys         = detailKeys,
					OrderBy            = CollectOrderBy(correctedSequence),
					EntityColumns      = entityDescriptor.Columns,
				});
			}

			if (branches.Count < 2)
				return null;

			// Verify all branches share the same key type
			var firstKeyType = branches[0].KeyType;
			if (branches.Any(b => b.KeyType != firstKeyType))
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
				var cols = branches[b].EntityColumns;
				slotMaps[b] = new int[cols.Count];

				for (int c = 0; c < cols.Count; c++)
				{
					var colType = cols[c].MemberType;
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

			// Phase 4: Build UNION ALL branches — each branch uses its own cloned parent context
			var mainType0 = branches[0].MainType;
			Expression? concatExpr = null;

			for (int b = 0; b < branches.Count; b++)
			{
				var branch          = branches[b];
				var mainParameter   = Expression.Parameter(branch.MainType, "m");
				var detailParameter = Expression.Parameter(branch.DetailType, "d");

				// Build branch-specific parent source
				var branchContextRef = new ContextRefExpression(
					typeof(IQueryable<>).MakeGenericType(branch.ClonedParentContext.ElementType), branch.ClonedParentContext);

				Expression branchSource = branchContextRef;
				if (!typeof(IQueryable<>).IsSameOrParentOf(branchSource.Type))
					branchSource = Expression.Call(Methods.Queryable.AsQueryable.MakeGenericMethod(branch.MainType), branchSource);

				branchSource = Expression.Call(Methods.LinqToDB.SelectDistinct.MakeGenericMethod(branch.MainType), branchSource);

				// Build carrier arguments
				var args = new Expression[carrierTypes.Length];
				args[0] = Expression.Constant(b); // setId

				// Use cloned detail key expression
				args[1] = branch.DetailKeys.Length == 1
					? branch.DetailKeys[0]
					: GenerateKeyExpression(branch.DetailKeys, 0);

				for (int s = 2; s < args.Length; s++)
					args[s] = Expression.Default(carrierTypes[s]);

				for (int c = 0; c < branch.EntityColumns.Count; c++)
				{
					var col     = branch.EntityColumns[c];
					var access  = Expression.MakeMemberAccess(detailParameter, col.MemberInfo);
					var slotIdx = slotMaps[b][c]; // Use slot map for reuse

					args[slotIdx] = access.Type != carrierTypes[slotIdx]
						? Expression.Convert(access, carrierTypes[slotIdx])
						: access;
				}

				var carrierNew = BuildValueTupleNew(carrierType, args);

				var resultSelector = Expression.Lambda(carrierNew, mainParameter, detailParameter);

				var detailSelector = _buildSelectManyDetailSelectorInfo
					.MakeGenericMethod(branch.MainType, branch.DetailType)
					.InvokeExt<LambdaExpression>(null, new object[] { branch.CorrectedSequence, mainParameter });

				var branchQuery = Expression.Call(
					Methods.Queryable.SelectManyProjection.MakeGenericMethod(branch.MainType, branch.DetailType, carrierType),
					branchSource, Expression.Quote(detailSelector), Expression.Quote(resultSelector));

				concatExpr = concatExpr == null
					? branchQuery
					: Expression.Call(Methods.Queryable.Concat.MakeGenericMethod(carrierType), concatExpr, branchQuery);
			}

			if (concatExpr == null)
				return null;

			// Phase 5: Build combined sequence using the first branch's cloning context
			var firstBranchCloningCtx = branches[0].CloningContext;
			var saveVisitor = _buildVisitor;
			_buildVisitor = _buildVisitor.Clone(firstBranchCloningCtx);
			firstBranchCloningCtx.UpdateContextParents();

			var combinedSequence = BuildSequence(new BuildInfo((IBuildContext?)null, concatExpr,
				branches[0].ClonedParentContext.SelectQuery));

			_buildVisitor = saveVisitor;

			// Phase 6: Create preamble via reflection
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

			// Build detail extractors per branch
			var detailExtractors = new Func<TCarrier, object>[branches.Length];

			for (int b = 0; b < branches.Length; b++)
			{
				var branch         = branches[b];
				var cp             = Expression.Parameter(typeof(TCarrier), "vt");
				var memberBindings = new List<MemberBinding>();

				for (int c = 0; c < branch.EntityColumns.Count; c++)
				{
					var col     = branch.EntityColumns[c];
					var slotIdx = slotMaps[b][c];
					var access  = AccessValueTupleField(cp, slotIdx);

					if (Nullable.GetUnderlyingType(access.Type) != null && col.MemberType.IsValueType && Nullable.GetUnderlyingType(col.MemberType) == null)
						access = Expression.Convert(access, col.MemberType);

					memberBindings.Add(Expression.Bind(col.MemberInfo, access));
				}

				var detailNew = Expression.MemberInit(Expression.New(branch.DetailType), memberBindings);
				detailExtractors[b] = Expression.Lambda<Func<TCarrier, object>>(
					Expression.Convert(detailNew, typeof(object)), cp).CompileExpression();
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
				var getListMethod      = preambleResultType.GetMethod(nameof(PreambleResult<int, int>.GetList))!;

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
			public CloningContext                      CloningContext     = null!;
			public IBuildContext                        ClonedParentContext = null!;
			public Expression                          CorrectedSequence  = null!;
			public Type                                MainType           = null!;
			public Type                                DetailType         = null!;
			public Type                                KeyType            = null!;
			public Expression                          MainKeyExpression  = null!;
			public Expression[]                        DetailKeys         = null!;
			public List<(LambdaExpression, bool)>?     OrderBy;
			public IReadOnlyList<ColumnDescriptor>     EntityColumns      = null!;
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
