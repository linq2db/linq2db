using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB.Expressions;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Linq;
using LinqToDB.Mapping;

namespace LinqToDB.Internal.Linq.Builder
{
	/// <summary>
	/// Translates <c>Upsert</c> / <c>UpsertAsync</c> calls (issue #2558) into either
	/// <see cref="SqlInsertOrUpdateStatement"/> (native ON&#160;CONFLICT / UPDATE-INSERT path)
	/// or a synthesised <c>Merge</c> expression tree that lowers to <see cref="SqlMergeStatement"/>.
	/// <para>
	/// Path selection is driven by the parsed <see cref="UpsertConfig"/>:
	/// </para>
	/// <list type="bullet">
	///   <item>Bulk source (<c>IEnumerable&lt;T&gt;</c> / <c>IQueryable&lt;T&gt;</c>) → MERGE.</item>
	///   <item><c>.SkipInsert()</c>, <c>.Insert(i =&gt; i.DoNothing())</c>, <c>.Insert(i =&gt; i.When(…))</c> → MERGE.</item>
	///   <item><c>.Match</c> not equal to the target primary key → MERGE.</item>
	///   <item>everything else → native <see cref="SqlInsertOrUpdateStatement"/>.</item>
	/// </list>
	/// </summary>
	[BuildsMethodCall(nameof(LinqExtensions.Upsert), nameof(LinqExtensions.UpsertAsync))]
	sealed class UpsertBuilder : MethodCallBuilder
	{
		public static bool CanBuildMethod(MethodCallExpression call) => call.IsQueryable;

		protected override BuildSequenceResult BuildMethodCall(
			ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			// All Upsert overloads have generic arity 1: Upsert<T>(ITable<T>, T | IEnumerable<T> | IQueryable<T>, configure).
			// Single- vs bulk-source is discriminated by inspecting the second argument's static type.
			var entityType   = methodCall.Method.GetGenericArguments()[0];
			var tableArg     = methodCall.Arguments[0];
			var itemArg      = methodCall.Arguments[1];        // single T / IEnumerable<T> / IQueryable<T>
			var configureArg = methodCall.Arguments[2];

			var singleItem = itemArg.Type == entityType;

			var configureLambda = configureArg.UnwrapLambda();

			// Shared parameter used to canonicalise every user-supplied field selector.
			// After canonicalisation all `x => x.Col` bodies become `entityParm.Col`,
			// so membership tests reduce to ExpressionEqualityComparer lookups.
			var entityParm = Expression.Parameter(entityType, "x");

			var cfg = ParseConfigure(configureLambda, entityParm);

			var entityDescriptor = builder.MappingSchema.GetEntityDescriptor(
				entityType, builder.DataContext.Options.ConnectionOptions.OnEntityDescriptorCreated);

			// Parse match columns (if any) early — feeds both the native-path PK check
			// and the MERGE-lowering decision below.
			List<Expression>? matchColumnExprs = null;
			var matchMatchesPk = true;
			if (cfg.MatchCondition != null)
			{
				matchColumnExprs = TryParseMatchColumns(cfg.MatchCondition, entityParm);
				if (matchColumnExprs == null)
				{
					return BuildSequenceResult.Error(
						buildInfo.Expression,
						"Upsert .Match(...) must be a conjunction of 'target.Member.Path == source.Member.Path' equalities over the target and source parameters.");
				}

				var pkExprs = entityDescriptor.Columns
					.Where(c => c.IsPrimaryKey)
					.Select(c => c.MemberAccessor.GetGetterExpression(entityParm))
					.ToList();

				var matchSet = new HashSet<Expression>(matchColumnExprs, ExpressionEqualityComparer.Instance);
				var pkSet    = new HashSet<Expression>(pkExprs,          ExpressionEqualityComparer.Instance);
				matchMatchesPk = pkSet.SetEquals(matchSet);
			}

			// MERGE-required whenever:
			//   - bulk source (IEnumerable / IQueryable) — ON CONFLICT is single-row-oriented here
			//   - .SkipInsert() / .Insert(i => i.DoNothing())
			//   - .Insert(i => i.When(...))
			//   - .Match on non-PK columns
			var needsMerge = !singleItem || cfg.SkipInsert || cfg.InsertWhen != null || !matchMatchesPk;

			if (needsMerge)
			{
				return BuildAsMerge(builder, buildInfo, cfg, entityType, singleItem, tableArg, itemArg, entityDescriptor, matchColumnExprs);
			}

			// ---- Native path (ON CONFLICT / MERGE / UPDATE-INSERT fallback) ----

			// Build sequence for the target table.
			builder.PushDisabledQueryFilters([entityType]);
			var sequence = builder.BuildSequence(new BuildInfo(buildInfo, tableArg));
			builder.PopDisabledFilter();

			var stmt = new SqlInsertOrUpdateStatement(sequence.SelectQuery);

			var tableContext = SequenceHelper.GetTableContext(sequence);
			if (tableContext == null)
				return BuildSequenceResult.Error(buildInfo.Expression, "Could not retrieve table information from query.");

			var contextRef  = new ContextRefExpression(entityType, sequence);
			var itemConst   = itemArg; // Already Expression.Constant(item)

			// ---- Build INSERT envelopes ----

			var insertEnvelopes = new List<UpdateBuilder.SetExpressionEnvelope>();
			var updateEnvelopes = new List<UpdateBuilder.SetExpressionEnvelope>();

			foreach (var cd in entityDescriptor.Columns)
			{
				// Canonical lookup key for this column — the full accessor path from the
				// shared entity parameter (handles nested columns like e.Name.FirstName the
				// same way). Matches the shape produced by Canonicalise(fieldLambda) for user
				// .Set/.Ignore field selectors.
				var canonicalField = cd.MemberAccessor.GetGetterExpression(entityParm);

				if (EntitySetterBuilder.IsIgnored(canonicalField, cfg.RootIgnore) || EntitySetterBuilder.IsIgnored(canonicalField, cfg.InsertIgnore))
					goto UpdateSide;

				if (cd.SkipOnInsert)
					goto UpdateSide;

				var fieldExpr = Expression.MakeMemberAccess(contextRef, cd.MemberInfo);
				var insertOverride = EntitySetterBuilder.FindOverride(canonicalField, cfg.InsertSet)
				                  ?? EntitySetterBuilder.FindOverride(canonicalField, cfg.RootSet);
				var valueExpr = insertOverride != null
					? EntitySetterBuilder.InstantiateSetter(insertOverride, contextRef, itemConst)
					: cd.MemberAccessor.GetGetterExpression(itemConst);

				insertEnvelopes.Add(new UpdateBuilder.SetExpressionEnvelope(fieldExpr, valueExpr, forceParameter: false));

				UpdateSide:

				// SkipUpdate / Update(v => v.DoNothing()) — emit DO NOTHING by leaving Update.Items empty.
				if (cfg.SkipUpdate)
					continue;

				if (EntitySetterBuilder.IsIgnored(canonicalField, cfg.RootIgnore) || EntitySetterBuilder.IsIgnored(canonicalField, cfg.UpdateIgnore))
					continue;

				// PK columns participate as match keys in the ON CONFLICT clause, not in the SET list.
				if (cd.IsPrimaryKey)
					continue;

				if (cd.SkipOnUpdate)
					continue;

				var updFieldExpr = Expression.MakeMemberAccess(contextRef, cd.MemberInfo);

				var updateOverride = EntitySetterBuilder.FindOverride(canonicalField, cfg.UpdateSet)
				                  ?? EntitySetterBuilder.FindOverride(canonicalField, cfg.RootSet);
				var updValueExpr = updateOverride != null
					? EntitySetterBuilder.InstantiateSetter(updateOverride, contextRef, itemConst)
					: cd.MemberAccessor.GetGetterExpression(itemConst);

				updateEnvelopes.Add(new UpdateBuilder.SetExpressionEnvelope(updFieldExpr, updValueExpr, forceParameter: false));
			}

			// ---- Populate statement ----

			UpdateBuilder.InitializeSetExpressions(builder, tableContext, sequence,
				insertEnvelopes, stmt.Insert.Items, createColumns: true);

			UpdateBuilder.InitializeSetExpressions(builder, tableContext, sequence,
				updateEnvelopes, stmt.Update.Items, createColumns: true);

			stmt.Insert.Into  = tableContext.SqlTable;
			stmt.Update.Table = tableContext.SqlTable;

			// ---- Match keys ----
			// Parse .Match content if provided. Phase 1 accepts only the case where the
			// match columns exactly equal the target table's primary key — which is what
			// ON CONFLICT / today's InsertOrUpdate natively supports. Other match shapes
			// fall into Phase 3 (MERGE) territory.

			var table = stmt.Insert.Into!;
			var keys  = table.GetKeys(false);

			if (keys == null || keys.Count == 0)
				return BuildSequenceResult.Error(buildInfo.Expression, $"Upsert requires the '{table.NameForLogging}' table to have a primary key.");

			// Match validation is performed earlier; by this point we know matchMatchesPk is true
			// (otherwise the merge path would have been chosen). Nothing more to check here.

			var keyMatches = (
				from k in keys
				join i in stmt.Insert.Items on k equals i.Column
				select new { k, i }
			).ToList();

			var missedKey = keys.Except(keyMatches.Select(km => km.k)).FirstOrDefault();
			if (missedKey != null)
			{
				return BuildSequenceResult.Error(
					buildInfo.Expression,
					$"Upsert requires the '{table.NameForLogging}.{((SqlField)missedKey).Name}' field to be included in the insert setter.");
			}

			stmt.Update.Keys.AddRange(keyMatches.Select(km => km.i));

			// ---- .When on the UPDATE branch → WHERE on DO UPDATE / WHEN MATCHED AND / … ----

			if (cfg.UpdateWhen != null && !cfg.SkipUpdate)
			{
				// Substitute (t, s) → (contextRef, itemConst) and build a SqlSearchCondition.
				var preparedBody = EntitySetterBuilder.InstantiateSetter(cfg.UpdateWhen, contextRef, itemConst);
				var searchCondition = new SqlSearchCondition(isOr: false);
				builder.BuildSearchCondition(sequence, preparedBody, searchCondition);
				stmt.UpdateWhere = searchCondition;
			}

			return BuildSequenceResult.FromContext(
				new UpsertContext(sequence.TranslationModifier, builder, sequence, stmt));
		}

		#region MERGE lowering (Phase 3)

		/// <summary>
		/// Build the upsert via the existing Merge pipeline. We synthesize an Expression tree
		/// equivalent to
		/// <code>
		/// table.Merge()
		///   .Using(new[] { item })
		///   .On((t, s) =&gt; matchCondition)
		///   [.InsertWhenNotMatchedAnd(insertPredicate, s =&gt; new TTarget { ... })]
		///   [.UpdateWhenMatchedAnd(updatePredicate, (t, s) =&gt; new TTarget { ... })]
		///   .Merge()
		/// </code>
		/// and hand it to <see cref="ExpressionBuilder.BuildSequence"/>. The existing
		/// <c>MergeBuilder</c> dispatchers pick up each call, produce a <see cref="SqlMergeStatement"/>,
		/// and let per-provider SQL builders emit the right MERGE dialect.
		/// </summary>
		static BuildSequenceResult BuildAsMerge(
			ExpressionBuilder    builder,
			BuildInfo            buildInfo,
			UpsertConfig         cfg,
			Type                 entityType,            // TTarget = TSource (always equal under the single-generic API)
			bool                 singleItem,
			Expression           tableArg,
			Expression           sourceArg,             // single T / IEnumerable<T> / IQueryable<T>
			EntityDescriptor     entityDescriptor,
			List<Expression>?    matchColumnExprs)      // parsed target-side member-paths from .Match; null ⇒ default PK match
		{
			if (cfg.SkipInsert && cfg.SkipUpdate)
				return BuildSequenceResult.Error(buildInfo.Expression, "Upsert with both SkipInsert() and SkipUpdate() would do nothing — remove one.");

			// Emulation-first: if the provider can't honor our synthesized two-branch MERGE, surface
			// a descriptive build error via BuildSequenceResult. Callers that want bulk / non-PK-match /
			// conditional-insert on such providers must switch providers or reshape the call.
			if (!builder.DataContext.SqlProviderFlags.IsUpsertWithMergeLoweringSupported)
				return BuildSequenceResult.Error(buildInfo.Expression, ErrorHelper.Error_Upsert_MergeLowering_NotSupported);

			// Provider supports basic MERGE lowering but not conditional branches (e.g. Firebird 2.5,
			// whose MERGE predates the WHEN [NOT] MATCHED AND <cond> form added in Firebird 3).
			if ((cfg.InsertWhen != null || cfg.UpdateWhen != null)
				&& !builder.DataContext.SqlProviderFlags.IsUpsertMergeWithPredicateSupported)
			{
				return BuildSequenceResult.Error(buildInfo.Expression, ErrorHelper.Error_Upsert_MergeWithPredicate_NotSupported);
			}

			// Default match requires a PK; surface early so the helper below doesn't have to throw.
			if (cfg.MatchCondition == null && !entityDescriptor.Columns.Any(c => c.IsPrimaryKey))
			{
				return BuildSequenceResult.Error(
					buildInfo.Expression,
					"Upsert requires either an explicit .Match(...) or a primary key on the target table.");
			}

			// Columns referenced by the ON clause must not appear in the UPDATE SET list
			// — Oracle rejects such MERGE (ORA-38104) and other providers do a pointless
			// self-assign. Build a set of excluded canonical-member expressions.
			var matchColumns = matchColumnExprs != null
				? new HashSet<Expression>(matchColumnExprs, ExpressionEqualityComparer.Instance)
				: new HashSet<Expression>(
					entityDescriptor.Columns.Where(c => c.IsPrimaryKey).Select(c => c.MemberAccessor.GetGetterExpression(cfg.EntityParm)),
					ExpressionEqualityComparer.Instance);

			// ---- USING: materialise the source ----

			Expression mergeSource;
			bool       sourceIsQueryable;

			if (singleItem)
			{
				// Single T item → wrap as a one-row IEnumerable via NewArrayInit.
				mergeSource       = Expression.NewArrayInit(entityType, sourceArg);
				sourceIsQueryable = false;
			}
			else if (typeof(IQueryable<>).MakeGenericType(entityType).IsAssignableFrom(sourceArg.Type))
			{
				mergeSource       = sourceArg;
				sourceIsQueryable = true;
			}
			else
			{
				// IEnumerable<T> — pass straight through to Using(IEnumerable<>).
				mergeSource       = sourceArg;
				sourceIsQueryable = false;
			}

			// ---- Synthesise the Merge chain ----

			var mergeTable = Reflection.Methods.LinqToDB.Merge.MergeMethodInfo2
				.GetGenericMethodDefinition()
				.MakeGenericMethod(entityType);

			Expression expr = Expression.Call(null, mergeTable, tableArg);

			var usingMethod = sourceIsQueryable
				? Reflection.Methods.LinqToDB.Merge.UsingMethodInfo1
				: Reflection.Methods.LinqToDB.Merge.UsingMethodInfo2;

			expr = Expression.Call(null,
				usingMethod.MakeGenericMethod(entityType, entityType),
				expr, mergeSource);

			var matchLambda = cfg.MatchCondition ?? BuildDefaultPkMatchLambda(entityType, entityDescriptor);
			expr = Expression.Call(null,
				Reflection.Methods.LinqToDB.Merge.OnMethodInfo2.MakeGenericMethod(entityType, entityType),
				expr, Expression.Quote(matchLambda));

			if (!cfg.SkipInsert)
			{
				// Merge root + branch overrides — branch entries appended last so EntitySetterBuilder.FindOverride picks them.
				var insertSetter = EntitySetterBuilder.BuildInsertSetter(
					entityType, entityDescriptor, cfg.EntityParm,
					setOverrides: [..cfg.RootSet,    ..cfg.InsertSet],
					ignoreList:   [..cfg.RootIgnore, ..cfg.InsertIgnore]);

				// No user predicate → pass null so BasicSqlBuilder emits plain 'WHEN NOT MATCHED THEN ...'
				// rather than 'WHEN NOT MATCHED AND 1 = 1'. Mirrors LinqExtensions.InsertWhenNotMatched.
				Expression insertPredicateArg = cfg.InsertWhen != null
					? Expression.Quote(cfg.InsertWhen)
					: Expression.Constant(null, typeof(Expression<>).MakeGenericType(typeof(Func<,>).MakeGenericType(entityType, typeof(bool))));

				expr = Expression.Call(null,
					Reflection.Methods.LinqToDB.Merge.InsertWhenNotMatchedAndMethodInfo.MakeGenericMethod(entityType, entityType),
					expr, insertPredicateArg, Expression.Quote(insertSetter));
			}

			if (!cfg.SkipUpdate)
			{
				var updateSetter = EntitySetterBuilder.BuildUpdateSetter(
					entityType, entityDescriptor, cfg.EntityParm,
					setOverrides: [..cfg.RootSet,    ..cfg.UpdateSet],
					ignoreList:   [..cfg.RootIgnore, ..cfg.UpdateIgnore],
					matchColumns: matchColumns);

				// Same story for the UPDATE branch — null predicate → plain 'WHEN MATCHED THEN UPDATE SET …'.
				Expression updatePredicateArg = cfg.UpdateWhen != null
					? Expression.Quote(cfg.UpdateWhen)
					: Expression.Constant(null, typeof(Expression<>).MakeGenericType(typeof(Func<,,>).MakeGenericType(entityType, entityType, typeof(bool))));

				expr = Expression.Call(null,
					Reflection.Methods.LinqToDB.Merge.UpdateWhenMatchedAndMethodInfo.MakeGenericMethod(entityType, entityType),
					expr, updatePredicateArg, Expression.Quote(updateSetter));
			}

			expr = Expression.Call(null,
				Reflection.Methods.LinqToDB.Merge.ExecuteMergeMethodInfo.MakeGenericMethod(entityType, entityType),
				expr);

			var sequenceResult = builder.TryBuildSequence(new BuildInfo(buildInfo, expr));
			if (sequenceResult.BuildContext == null)
				return sequenceResult;

			return BuildSequenceResult.FromContext(sequenceResult.BuildContext);
		}

		/// <summary>Build <c>(t, s) =&gt; t.Pk1 == s.Pk1 &amp;&amp; t.Pk2 == s.Pk2 &amp;&amp; ...</c> from the PK columns.</summary>
		static LambdaExpression BuildDefaultPkMatchLambda(Type entityType, EntityDescriptor entityDescriptor)
		{
			var pks = entityDescriptor.Columns.Where(c => c.IsPrimaryKey).ToList();
			if (pks.Count == 0)
				throw new LinqToDBException("Upsert requires either an explicit .Match(...) or a primary key on the target table.");

			var t = Expression.Parameter(entityType, "t");
			var s = Expression.Parameter(entityType, "s");

			Expression? body = null;
			foreach (var pk in pks)
			{
				var tCol = pk.MemberAccessor.GetGetterExpression(t);
				var sCol = pk.MemberAccessor.GetGetterExpression(s);
				var eq   = Expression.Equal(tCol, sCol);
				body = body == null ? eq : Expression.AndAlso(body, eq);
			}

			return Expression.Lambda(body!, t, s);
		}

		#endregion

		#region Configure-expression walker

		sealed class UpsertConfig
		{
			public LambdaExpression?                            MatchCondition;
			public readonly List<Expression>                    RootIgnore   = new();
			public readonly List<(Expression, LambdaExpression)> RootSet      = new();
			public readonly List<Expression>                    InsertIgnore = new();
			public readonly List<(Expression, LambdaExpression)> InsertSet    = new();
			public readonly List<Expression>                    UpdateIgnore = new();
			public readonly List<(Expression, LambdaExpression)> UpdateSet    = new();

			/// <summary>
			/// Set by root <c>SkipUpdate()</c> or by <c>Update(v =&gt; v.DoNothing())</c>.
			/// </summary>
			public bool                                         SkipUpdate;

			/// <summary>
			/// Set by root <c>SkipInsert()</c> or by <c>Insert(i =&gt; i.DoNothing())</c>.
			/// Implies MERGE-based lowering (ON CONFLICT can't express "don't insert").
			/// </summary>
			public bool                                         SkipInsert;

			/// <summary>
			/// Set by <c>.Update(v =&gt; v.When((t, s) =&gt; cond))</c>.
			/// </summary>
			public LambdaExpression?                            UpdateWhen;

			/// <summary>
			/// Set by <c>.Insert(i =&gt; i.When(s =&gt; cond))</c>. Implies MERGE-based lowering.
			/// </summary>
			public LambdaExpression?                            InsertWhen;

			public readonly ParameterExpression EntityParm;

			public UpsertConfig(ParameterExpression entityParm) => EntityParm = entityParm;
		}

		static UpsertConfig ParseConfigure(LambdaExpression configureLambda, ParameterExpression entityParm)
		{
			var cfg = new UpsertConfig(entityParm);
			WalkRoot(configureLambda.Body, cfg);
			return cfg;
		}

		static void WalkRoot(Expression expr, UpsertConfig cfg)
		{
			// Unwrap outer→inner. Each node is either a MethodCallExpression (chain step)
			// or the outer ParameterExpression (the `u` parameter of the configure lambda).
			// Chain methods are now interface members, so the receiver lives in mc.Object.

			while (expr is MethodCallExpression mc)
			{
				switch (mc.Method.Name)
				{
					case nameof(IEntityUpsertBuilder<>.Match):
						cfg.MatchCondition = mc.Arguments[0].UnwrapLambda();
						break;
					case nameof(IEntityUpsertBuilder<>.Set):
						cfg.RootSet.Add((EntityBuilderParser.Canonicalise(mc.Arguments[0].UnwrapLambda(), cfg.EntityParm), mc.Arguments[1].UnwrapLambda()));
						break;
					case nameof(IEntityUpsertBuilder<>.Ignore):
						cfg.RootIgnore.Add(EntityBuilderParser.Canonicalise(mc.Arguments[0].UnwrapLambda(), cfg.EntityParm));
						break;
					case nameof(IEntityUpsertBuilder<>.Insert):
						MergeBranch(EntityBuilderParser.Parse(mc.Arguments[0].UnwrapLambda(), cfg.EntityParm), cfg, insertBranch: true);
						break;
					case nameof(IEntityUpsertBuilder<>.Update):
						MergeBranch(EntityBuilderParser.Parse(mc.Arguments[0].UnwrapLambda(), cfg.EntityParm), cfg, insertBranch: false);
						break;
					case nameof(IEntityUpsertBuilder<>.SkipUpdate):
						cfg.SkipUpdate = true;
						break;
					case nameof(IEntityUpsertBuilder<>.SkipInsert):
						cfg.SkipInsert = true;
						break;
					default:
						throw new LinqToDBException(
							$"Unexpected method '{mc.Method.Name}' inside Upsert configure expression.");
				}

				expr = mc.Object!;
			}

			// expr should now be the ParameterExpression (outer lambda's parameter).
			if (expr is not ParameterExpression)
				throw new LinqToDBException(
					"Upsert configure expression chain must start with the builder parameter; got " + expr.GetType().Name);
		}

		/// <summary>
		/// Copy a parsed branch <see cref="EntityBuilderConfig"/> into the corresponding side of
		/// the Upsert <see cref="UpsertConfig"/>. <see cref="EntityBuilderConfig.DoNothing"/> on
		/// the insert branch implies <see cref="UpsertConfig.SkipInsert"/>; on the update branch,
		/// <see cref="UpsertConfig.SkipUpdate"/>.
		/// </summary>
		static void MergeBranch(EntityBuilderConfig branch, UpsertConfig cfg, bool insertBranch)
		{
			if (insertBranch)
			{
				cfg.InsertSet   .AddRange(branch.Set);
				cfg.InsertIgnore.AddRange(branch.Ignore);
				cfg.InsertWhen   = branch.When ?? cfg.InsertWhen;
				if (branch.DoNothing) cfg.SkipInsert = true;
			}
			else
			{
				cfg.UpdateSet   .AddRange(branch.Set);
				cfg.UpdateIgnore.AddRange(branch.Ignore);
				cfg.UpdateWhen   = branch.When ?? cfg.UpdateWhen;
				if (branch.DoNothing) cfg.SkipUpdate = true;
			}
		}

		/// <summary>
		/// Parse a <c>.Match((t, s) =&gt; t.Col1 == s.Col1 &amp;&amp; t.Nested.Col2 == s.Nested.Col2)</c>
		/// lambda body into the list of target-side member-access paths, canonicalised against
		/// <paramref name="entityParm"/>. Returns <see langword="null"/> when the body does not
		/// decompose into a conjunction of 'target.Path == source.Path' equalities where both sides
		/// are the same member path rooted at the lambda's target and source parameters respectively.
		/// </summary>
		static List<Expression>? TryParseMatchColumns(LambdaExpression match, ParameterExpression entityParm)
		{
			if (match.Parameters.Count != 2)
				return null;

			var targetParm = match.Parameters[0];
			var sourceParm = match.Parameters[1];

			var result = new List<Expression>();
			return TryWalk(match.Body, result) ? result : null;

			bool TryWalk(Expression node, List<Expression> acc)
			{
				switch (node.NodeType)
				{
					case ExpressionType.AndAlso:
					{
						var bin = (BinaryExpression)node;
						return TryWalk(bin.Left, acc) && TryWalk(bin.Right, acc);
					}

					case ExpressionType.Equal:
					{
						var bin = (BinaryExpression)node;
						var leftRoot  = TryGetPathRoot(bin.Left,  out var leftPath);
						var rightRoot = TryGetPathRoot(bin.Right, out var rightPath);
						if (leftRoot == null || rightRoot == null)
							return false;

						// Accept both (t.Path == s.Path) and (s.Path == t.Path); normalise so the
						// target-rooted side goes into the accumulator.
						Expression? targetPath = null;
						if (leftRoot == targetParm && rightRoot == sourceParm) targetPath = leftPath;
						else if (leftRoot == sourceParm && rightRoot == targetParm) targetPath = rightPath;
						else return false;

						// Compare the two paths as expressions after unifying their roots.
						var leftUnified  = RebaseRoot(leftPath!,  leftRoot,  entityParm);
						var rightUnified = RebaseRoot(rightPath!, rightRoot, entityParm);
						if (!ExpressionEqualityComparer.Instance.Equals(leftUnified, rightUnified))
							return false;

						acc.Add(RebaseRoot(targetPath!,
							targetPath! == leftPath ? leftRoot : rightRoot,
							entityParm));
						return true;
					}

					default:
						return false;
				}
			}
		}

		/// <summary>
		/// If <paramref name="e"/> (after peeling <c>Convert</c>) is a chain of <see cref="MemberExpression"/>
		/// nodes ending at a <see cref="ParameterExpression"/>, returns that root parameter and the full
		/// chain expression via <paramref name="path"/>. Otherwise returns <see langword="null"/>.
		/// </summary>
		static ParameterExpression? TryGetPathRoot(Expression e, out Expression? path)
		{
			while (e is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } u)
				e = u.Operand;

			path = e;
			var current = e;
			while (current is MemberExpression me)
				current = me.Expression!;

			return current as ParameterExpression;
		}

		/// <summary>Rewrite <paramref name="expr"/> replacing <paramref name="from"/> with <paramref name="to"/>.</summary>
		static Expression RebaseRoot(Expression expr, ParameterExpression from, ParameterExpression to)
			=> expr.Transform((from, to), static (ctx, e) => e == ctx.from ? ctx.to : e);

		#endregion

		#region UpsertContext

		sealed class UpsertContext : BuildContextBase
		{
			public override MappingSchema              MappingSchema => Context.MappingSchema;
			public          IBuildContext              Context       { get; }
			public          SqlInsertOrUpdateStatement Statement     { get; }

			public UpsertContext(
				TranslationModifier        translationModifier,
				ExpressionBuilder          builder,
				IBuildContext              sequence,
				SqlInsertOrUpdateStatement statement)
				: base(translationModifier, builder, typeof(object), sequence.SelectQuery)
			{
				Context   = sequence;
				Statement = statement;
			}

			public override Expression MakeExpression(Expression path, ProjectFlags flags)
			{
				if (SequenceHelper.IsSameContext(path, this))
					return Expression.Default(path.Type);
				throw new InvalidOperationException();
			}

			public override void SetRunQuery<T>(Query<T> query, Expression expr)
			{
				var flags = Builder.DataContext.SqlProviderFlags;

				// If the user attached an UPDATE predicate (Upsert.Update.When) and the provider's
				// native InsertOrUpdate emission can't honor it (e.g. MySQL ON DUPLICATE KEY UPDATE,
				// Sybase / SqlServer 2005 single-statement UPDATE+IF@@ROWCOUNT+INSERT), force the
				// alternative UPDATE→INSERT emulation, which carries UpdateWhere via the 3-query
				// orchestration in QueryRunner.
				var needsPredicateEmulation =
					Statement.UpdateWhere != null
					&& !flags.IsInsertOrUpdateWithPredicateSupported;

				var willEmulate = !flags.IsInsertOrUpdateSupported || needsPredicateEmulation;

				if (willEmulate && Builder.DataContext.Options.LinqOptions.ThrowOnUpsertEmulation)
				{
					throw new LinqToDBException(
						"Upsert cannot be expressed natively for this provider / configuration and would fall back to an emulated UPDATE+INSERT sequence. "
						+ "LinqOptions.ThrowOnUpsertEmulation is set — change the provider, adjust the Upsert configuration, or clear the flag to allow emulation.");
				}

				if (willEmulate)
					QueryRunner.MakeAlternativeInsertOrUpdate(Builder.DataContext.MappingSchema, query);
				else
					QueryRunner.SetNonQueryQuery(query);
			}

			public override SqlStatement GetResultStatement() => Statement;

			public override IBuildContext Clone(CloningContext context) =>
				new UpsertContext(
					TranslationModifier,
					Builder,
					context.CloneContext(Context),
					context.CloneElement(Statement));
		}

		#endregion
	}
}
