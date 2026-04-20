using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using LinqToDB.Expressions;
using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.Reflection;
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
	///   <item>Bulk source (<c>IEnumerable&lt;TSource&gt;</c> / <c>IQueryable&lt;TSource&gt;</c>) → MERGE.</item>
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
			// Generic arity 1 — single-entity form: Upsert<T>(ITable<T>, T, configure).
			// Generic arity 2 — bulk form:           Upsert<TTarget, TSource>(ITable<TTarget>, IEnumerable<TSource> | IQueryable<TSource>, configure).
			var genericArgs = methodCall.Method.GetGenericArguments();

			var singleItem   = genericArgs.Length == 1;
			var entityType   = genericArgs[0];                  // TTarget in both arities
			var sourceType   = singleItem ? entityType : genericArgs[1];
			var tableArg     = methodCall.Arguments[0];
			var itemArg      = methodCall.Arguments[1];        // single T / IEnumerable<TSource> / IQueryable<TSource>
			var configureArg = methodCall.Arguments[2];

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
				matchColumnExprs = TryParseMatchColumns(cfg.MatchCondition, entityParm)
					?? throw new LinqToDBException(
						"Upsert .Match(...) must be a conjunction of 'target.Member.Path == source.Member.Path' equalities over the target and source parameters.");

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
				return BuildAsMerge(builder, buildInfo, cfg, entityType, sourceType, singleItem, tableArg, itemArg, entityDescriptor, matchColumnExprs);
			}

			// ---- Native path (ON CONFLICT / MERGE / UPDATE-INSERT fallback) ----

			// Build sequence for the target table.
			builder.PushDisabledQueryFilters([entityType]);
			var sequence = builder.BuildSequence(new BuildInfo(buildInfo, tableArg));
			builder.PopDisabledFilter();

			var stmt = new SqlInsertOrUpdateStatement(sequence.SelectQuery);

			var tableContext = SequenceHelper.GetTableContext(sequence);
			if (tableContext == null)
				throw new LinqToDBException("Could not retrieve table information from query.");

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

				if (IsIgnored(canonicalField, cfg.RootIgnore) || IsIgnored(canonicalField, cfg.InsertIgnore))
					goto UpdateSide;

				if (cd.SkipOnInsert)
					goto UpdateSide;

				var fieldExpr = Expression.MakeMemberAccess(contextRef, cd.MemberInfo);
				var insertOverride = FindOverride(canonicalField, cfg.InsertSet)
				                  ?? FindOverride(canonicalField, cfg.RootSet);
				var valueExpr = insertOverride != null
					? InstantiateSetter(insertOverride, contextRef, itemConst)
					: cd.MemberAccessor.GetGetterExpression(itemConst);

				insertEnvelopes.Add(new UpdateBuilder.SetExpressionEnvelope(fieldExpr, valueExpr, forceParameter: false));

				UpdateSide:

				// SkipUpdate / Update(v => v.DoNothing()) — emit DO NOTHING by leaving Update.Items empty.
				if (cfg.SkipUpdate)
					continue;

				if (IsIgnored(canonicalField, cfg.RootIgnore) || IsIgnored(canonicalField, cfg.UpdateIgnore))
					continue;

				// PK columns participate as match keys in the ON CONFLICT clause, not in the SET list.
				if (cd.IsPrimaryKey)
					continue;

				if (cd.SkipOnUpdate)
					continue;

				var updFieldExpr = Expression.MakeMemberAccess(contextRef, cd.MemberInfo);

				var updateOverride = FindOverride(canonicalField, cfg.UpdateSet)
				                  ?? FindOverride(canonicalField, cfg.RootSet);
				var updValueExpr = updateOverride != null
					? InstantiateSetter(updateOverride, contextRef, itemConst)
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
				throw new LinqToDBException($"Upsert requires the '{table.NameForLogging}' table to have a primary key.");

			// Match validation is performed earlier; by this point we know matchMatchesPk is true
			// (otherwise the merge path would have been chosen). Nothing more to check here.

			var keyMatches = (
				from k in keys
				join i in stmt.Insert.Items on k equals i.Column
				select new { k, i }
			).ToList();

			var missedKey = keys.Except(keyMatches.Select(km => km.k)).FirstOrDefault();
			if (missedKey != null)
				throw new LinqToDBException(
					$"Upsert requires the '{table.NameForLogging}.{((SqlField)missedKey).Name}' field to be included in the insert setter.");

			stmt.Update.Keys.AddRange(keyMatches.Select(km => km.i));

			// ---- .When on the UPDATE branch → WHERE on DO UPDATE / WHEN MATCHED AND / … ----

			if (cfg.UpdateWhen != null && !cfg.SkipUpdate)
			{
				// Substitute (t, s) → (contextRef, itemConst) and build a SqlSearchCondition.
				var preparedBody = InstantiateSetter(cfg.UpdateWhen, contextRef, itemConst);
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
			Type                 entityType,            // TTarget
			Type                 sourceType,            // TSource (= TTarget for single-item upsert)
			bool                 singleItem,
			Expression           tableArg,
			Expression           sourceArg,             // single T / IEnumerable<TSource> / IQueryable<TSource>
			EntityDescriptor     entityDescriptor,
			List<Expression>?    matchColumnExprs)      // parsed target-side member-paths from .Match; null ⇒ default PK match
		{
			if (cfg.SkipInsert && cfg.SkipUpdate)
				throw new LinqToDBException("Upsert with both SkipInsert() and SkipUpdate() would do nothing — remove one.");

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
			else if (typeof(IQueryable<>).MakeGenericType(sourceType).IsAssignableFrom(sourceArg.Type))
			{
				mergeSource       = sourceArg;
				sourceIsQueryable = true;
			}
			else
			{
				// IEnumerable<TSource> — pass straight through to Using(IEnumerable<>).
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
				usingMethod.MakeGenericMethod(entityType, sourceType),
				expr, mergeSource);

			var matchLambda = cfg.MatchCondition ?? BuildDefaultPkMatchLambda(entityType, sourceType, entityDescriptor);
			expr = Expression.Call(null,
				Reflection.Methods.LinqToDB.Merge.OnMethodInfo2.MakeGenericMethod(entityType, sourceType),
				expr, Expression.Quote(matchLambda));

			if (!cfg.SkipInsert)
			{
				var insertSetter    = BuildInsertSetterLambda(cfg, entityType, sourceType, entityDescriptor);
				var insertPredicate = cfg.InsertWhen ?? BuildAlwaysTrueSingleParamLambda(sourceType, "s");

				expr = Expression.Call(null,
					Reflection.Methods.LinqToDB.Merge.InsertWhenNotMatchedAndMethodInfo.MakeGenericMethod(entityType, sourceType),
					expr, Expression.Quote(insertPredicate), Expression.Quote(insertSetter));
			}

			if (!cfg.SkipUpdate)
			{
				var updateSetter    = BuildUpdateSetterLambda(cfg, entityType, sourceType, entityDescriptor, matchColumns);
				var updatePredicate = cfg.UpdateWhen ?? BuildAlwaysTrueTwoParamLambda(entityType, sourceType, "t", "s");

				expr = Expression.Call(null,
					Reflection.Methods.LinqToDB.Merge.UpdateWhenMatchedAndMethodInfo.MakeGenericMethod(entityType, sourceType),
					expr, Expression.Quote(updatePredicate), Expression.Quote(updateSetter));
			}

			expr = Expression.Call(null,
				Reflection.Methods.LinqToDB.Merge.ExecuteMergeMethodInfo.MakeGenericMethod(entityType, sourceType),
				expr);

			var sequenceResult = builder.TryBuildSequence(new BuildInfo(buildInfo, expr));
			if (sequenceResult.BuildContext == null)
				return sequenceResult;

			return BuildSequenceResult.FromContext(sequenceResult.BuildContext);
		}

		/// <summary>Build <c>(t, s) =&gt; t.Pk1 == s.Pk1 &amp;&amp; t.Pk2 == s.Pk2 &amp;&amp; ...</c> from the PK columns.</summary>
		static LambdaExpression BuildDefaultPkMatchLambda(Type entityType, Type sourceType, EntityDescriptor entityDescriptor)
		{
			var pks = entityDescriptor.Columns.Where(c => c.IsPrimaryKey).ToList();
			if (pks.Count == 0)
				throw new LinqToDBException("Upsert requires either an explicit .Match(...) or a primary key on the target table.");

			var t = Expression.Parameter(entityType, "t");
			var s = Expression.Parameter(sourceType, "s");

			Expression? body = null;
			foreach (var pk in pks)
			{
				var sourceMember = sourceType.GetMemberEx(pk.MemberInfo)
					?? throw new LinqToDBException(
						$"Upsert default match: source type '{sourceType.Name}' has no member matching target PK '{pk.MemberInfo.Name}'. Provide an explicit .Match((t, s) => …).");

				var tCol = pk.MemberAccessor.GetGetterExpression(t);
				var sCol = Expression.MakeMemberAccess(s, sourceMember);
				var eq   = Expression.Equal(tCol, sCol);
				body = body == null ? eq : Expression.AndAlso(body, eq);
			}

			return Expression.Lambda(body!, t, s);
		}

		static LambdaExpression BuildAlwaysTrueSingleParamLambda(Type paramType, string paramName)
		{
			var p = Expression.Parameter(paramType, paramName);
			return Expression.Lambda(Expression.Constant(true), p);
		}

		static LambdaExpression BuildAlwaysTrueTwoParamLambda(Type p1Type, Type p2Type, string p1Name, string p2Name)
		{
			var p1 = Expression.Parameter(p1Type, p1Name);
			var p2 = Expression.Parameter(p2Type, p2Name);
			return Expression.Lambda(Expression.Constant(true), p1, p2);
		}

		/// <summary>
		/// Build <c>s =&gt; new TTarget { Col1 = v1, Col2 = v2, ... }</c> — whole-object defaults with
		/// root and per-branch <c>.Set</c> overrides overlaid and <c>.Ignore</c>d columns skipped.
		/// Columns without a matching member on <paramref name="sourceType"/> (and no user override)
		/// are dropped silently — the user explicitly takes ownership via <c>.Set</c> for those.
		/// </summary>
		static LambdaExpression BuildInsertSetterLambda(UpsertConfig cfg, Type entityType, Type sourceType, EntityDescriptor entityDescriptor)
		{
			var sParm    = Expression.Parameter(sourceType, "s");
			var bindings = new List<MemberBinding>();

			foreach (var cd in entityDescriptor.Columns)
			{
				if (cd.SkipOnInsert) continue;

				var canonicalField = cd.MemberAccessor.GetGetterExpression(cfg.EntityParm);
				if (IsIgnored(canonicalField, cfg.RootIgnore) || IsIgnored(canonicalField, cfg.InsertIgnore))
					continue;

				var @override = FindOverride(canonicalField, cfg.InsertSet)
				             ?? FindOverride(canonicalField, cfg.RootSet);

				Expression? value;
				if (@override != null)
				{
					value = InstantiateSetter(@override, sParm, sParm); // no target context for INSERT
				}
				else
				{
					var sourceMember = sourceType.GetMemberEx(cd.MemberInfo);
					if (sourceMember == null) continue;
					value = Expression.MakeMemberAccess(sParm, sourceMember);
				}

				bindings.Add(Expression.Bind(cd.MemberInfo, value));
			}

			var body = Expression.MemberInit(Expression.New(entityType), bindings);
			return Expression.Lambda(body, sParm);
		}

		/// <summary>
		/// Build <c>(t, s) =&gt; new TTarget { Col1 = v1, Col2 = v2, ... }</c>, skipping match-key,
		/// PK, and provider-marked non-updatable columns in the UPDATE set.
		/// </summary>
		static LambdaExpression BuildUpdateSetterLambda(UpsertConfig cfg, Type entityType, Type sourceType, EntityDescriptor entityDescriptor, HashSet<Expression> matchColumns)
		{
			var tParm    = Expression.Parameter(entityType, "t");
			var sParm    = Expression.Parameter(sourceType, "s");
			var bindings = new List<MemberBinding>();

			foreach (var cd in entityDescriptor.Columns)
			{
				if (cd.IsPrimaryKey) continue;
				if (cd.SkipOnUpdate) continue;

				var canonicalField = cd.MemberAccessor.GetGetterExpression(cfg.EntityParm);

				// Match columns appear in the ON clause — including them in UPDATE SET is
				// forbidden by Oracle (ORA-38104) and pointless elsewhere. Skip unless the
				// user explicitly opted in via .Update(v => v.Set(x => x.MatchCol, ...)).
				if (matchColumns.Contains(canonicalField) && FindOverride(canonicalField, cfg.UpdateSet) == null)
					continue;

				if (IsIgnored(canonicalField, cfg.RootIgnore) || IsIgnored(canonicalField, cfg.UpdateIgnore))
					continue;

				var @override = FindOverride(canonicalField, cfg.UpdateSet)
				             ?? FindOverride(canonicalField, cfg.RootSet);

				Expression? value;
				if (@override != null)
				{
					value = InstantiateSetter(@override, tParm, sParm);
				}
				else
				{
					var sourceMember = sourceType.GetMemberEx(cd.MemberInfo);
					if (sourceMember == null) continue;
					value = Expression.MakeMemberAccess(sParm, sourceMember);
				}

				bindings.Add(Expression.Bind(cd.MemberInfo, value));
			}

			var body = Expression.MemberInit(Expression.New(entityType), bindings);
			return Expression.Lambda(body, tParm, sParm);
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

			while (expr is MethodCallExpression mc)
			{
				switch (mc.Method.Name)
				{
					case nameof(LinqExtensions.Match):
						cfg.MatchCondition = mc.Arguments[1].UnwrapLambda();
						break;
					case nameof(LinqExtensions.Set):
						HandleSetCall(mc, cfg);
						break;
					case nameof(LinqExtensions.Ignore):
						HandleIgnoreCall(mc, cfg);
						break;
					case nameof(LinqExtensions.Insert):
						WalkBranch(mc.Arguments[1].UnwrapLambda().Body, cfg, insertBranch: true);
						break;
					case nameof(LinqExtensions.Update):
						WalkBranch(mc.Arguments[1].UnwrapLambda().Body, cfg, insertBranch: false);
						break;
					case nameof(LinqExtensions.SkipUpdate):
						cfg.SkipUpdate = true;
						break;
					case nameof(LinqExtensions.SkipInsert):
						cfg.SkipInsert = true;
						break;
					case nameof(LinqExtensions.When):
					case nameof(LinqExtensions.DoNothing):
						throw new LinqToDBException(
							$"Upsert configuration method '{mc.Method.Name}' is not yet supported.");
					default:
						throw new LinqToDBException(
							$"Unexpected method '{mc.Method.Name}' inside Upsert configure expression.");
				}

				expr = mc.Arguments[0];
			}

			// expr should now be the ParameterExpression (outer lambda's parameter).
			if (expr is not ParameterExpression)
				throw new LinqToDBException(
					"Upsert configure expression chain must start with the builder parameter; got " + expr.GetType().Name);
		}

		static void WalkBranch(Expression expr, UpsertConfig cfg, bool insertBranch)
		{
			while (expr is MethodCallExpression mc)
			{
				switch (mc.Method.Name)
				{
					case nameof(LinqExtensions.Set):
						HandleBranchSet(mc, cfg, insertBranch);
						break;
					case nameof(LinqExtensions.Ignore):
						HandleBranchIgnore(mc, cfg, insertBranch);
						break;
					case nameof(LinqExtensions.DoNothing) when insertBranch:
						cfg.SkipInsert = true;
						break;
					case nameof(LinqExtensions.DoNothing):
						cfg.SkipUpdate = true;
						break;
					case nameof(LinqExtensions.When) when insertBranch:
						cfg.InsertWhen = mc.Arguments[1].UnwrapLambda();
						break;
					case nameof(LinqExtensions.When):
						cfg.UpdateWhen = mc.Arguments[1].UnwrapLambda();
						break;
					default:
						throw new LinqToDBException(
							$"Unexpected method '{mc.Method.Name}' inside Upsert branch configure expression.");
				}

				expr = mc.Arguments[0];
			}

			if (expr is not ParameterExpression)
				throw new LinqToDBException(
					"Upsert branch configure expression chain must start with the builder parameter; got " + expr.GetType().Name);
		}

		static void HandleSetCall(MethodCallExpression mc, UpsertConfig cfg)
		{
			// Receiver type (first parameter) determines which list to append to.
			var receiverType = mc.Method.GetParameters()[0].ParameterType;
			var list =
				IsUpsertable(receiverType)            ? cfg.RootSet :
				IsUpsertInsertBuilder(receiverType)   ? cfg.InsertSet :
				IsUpsertUpdateBuilder(receiverType)   ? cfg.UpdateSet :
				throw new LinqToDBException($"Unexpected receiver type for Upsert.Set: {receiverType}");

			var fieldLambda = mc.Arguments[1].UnwrapLambda();
			var valueLambda = mc.Arguments[2].UnwrapLambda();

			list.Add((Canonicalise(fieldLambda, cfg.EntityParm), valueLambda));
		}

		static void HandleIgnoreCall(MethodCallExpression mc, UpsertConfig cfg)
		{
			var receiverType = mc.Method.GetParameters()[0].ParameterType;
			var list =
				IsUpsertable(receiverType)            ? cfg.RootIgnore :
				IsUpsertInsertBuilder(receiverType)   ? cfg.InsertIgnore :
				IsUpsertUpdateBuilder(receiverType)   ? cfg.UpdateIgnore :
				throw new LinqToDBException($"Unexpected receiver type for Upsert.Ignore: {receiverType}");

			var fieldLambda = mc.Arguments[1].UnwrapLambda();
			list.Add(Canonicalise(fieldLambda, cfg.EntityParm));
		}

		static void HandleBranchSet(MethodCallExpression mc, UpsertConfig cfg, bool insertBranch)
		{
			var fieldLambda = mc.Arguments[1].UnwrapLambda();
			var valueLambda = mc.Arguments[2].UnwrapLambda();
			(insertBranch ? cfg.InsertSet : cfg.UpdateSet).Add((Canonicalise(fieldLambda, cfg.EntityParm), valueLambda));
		}

		static void HandleBranchIgnore(MethodCallExpression mc, UpsertConfig cfg, bool insertBranch)
		{
			var fieldLambda = mc.Arguments[1].UnwrapLambda();
			(insertBranch ? cfg.InsertIgnore : cfg.UpdateIgnore).Add(Canonicalise(fieldLambda, cfg.EntityParm));
		}

		/// <summary>
		/// Rewrite a field-selector lambda <c>x =&gt; x.Col</c> so its body references the shared
		/// <paramref name="entityParm"/>. Two field selectors that referred to different source
		/// parameters now produce structurally-equal expressions, so <see cref="ExpressionEqualityComparer"/>
		/// can match them.
		/// </summary>
		static Expression Canonicalise(LambdaExpression fieldLambda, ParameterExpression entityParm)
			=> fieldLambda.GetBody(entityParm);

		static bool IsUpsertable(Type t) =>
			t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IUpsertable<,>);

		static bool IsUpsertInsertBuilder(Type t) =>
			t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IUpsertInsertBuilder<,>);

		static bool IsUpsertUpdateBuilder(Type t) =>
			t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IUpsertUpdateBuilder<,>);

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

		/// <summary>Format a canonical member-access chain like <c>entityParm.Name.FirstName</c> as <c>Name.FirstName</c> for error messages.</summary>
		static string PrintMemberPath(Expression expr)
		{
			var parts = new List<string>();
			while (expr is MemberExpression me)
			{
				parts.Add(me.Member.Name);
				expr = me.Expression!;
			}

			parts.Reverse();

#pragma warning disable MA0089 // Use an overload with char — not available on netstandard2.0/net462
			return parts.Count == 0 ? expr.ToString() : string.Join(".", parts);
#pragma warning restore MA0089
		}

		static bool IsIgnored(Expression canonicalField, List<Expression> list)
		{
			foreach (var e in list)
				if (ExpressionEqualityComparer.Instance.Equals(e, canonicalField)) return true;
			return false;
		}

		static LambdaExpression? FindOverride(Expression canonicalField, List<(Expression F, LambdaExpression V)> list)
		{
			// Later entries override earlier ones (branch-specific wins over root when merged externally).
			LambdaExpression? winner = null;
			foreach (var (f, v) in list)
				if (ExpressionEqualityComparer.Instance.Equals(f, canonicalField))
					winner = v;
			return winner;
		}

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
				if (Builder.DataContext.SqlProviderFlags.IsInsertOrUpdateSupported)
					QueryRunner.SetNonQueryQuery(query);
				else
					QueryRunner.MakeAlternativeInsertOrUpdate(Builder.DataContext.MappingSchema, query);
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

		/// <summary>
		/// Bind a user-provided setter lambda's parameters to our in-scope expressions and return its body.
		/// Supported arities:
		/// <list type="bullet">
		///   <item>0 params — context-free expression (<c>() =&gt; DateTime.UtcNow</c>); returns body unchanged.</item>
		///   <item>1 param — source row (<c>s =&gt; …</c>); binds to <paramref name="sourceItemConstant"/>.</item>
		///   <item>2 params — <c>(t, s) =&gt; …</c>; binds first to <paramref name="targetContextRef"/>, second to <paramref name="sourceItemConstant"/>.</item>
		/// </list>
		/// Uses <see cref="ExpressionExtensions.GetBody(LambdaExpression, Expression)"/> for the substitution.
		/// </summary>
		static Expression InstantiateSetter(LambdaExpression lambda, Expression targetContextRef, Expression sourceItemConstant)
			=> lambda.Parameters.Count switch
			{
				0 => lambda.Body,
				1 => lambda.GetBody(sourceItemConstant),
				2 => lambda.GetBody(targetContextRef, sourceItemConstant),
				_ => throw new LinqToDBException($"Unexpected upsert setter lambda arity: {lambda.Parameters.Count}"),
			};
	}
}
