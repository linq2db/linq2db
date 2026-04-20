using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Linq;
using LinqToDB.Mapping;

namespace LinqToDB.Internal.Linq.Builder
{
	/// <summary>
	/// Translates <c>Upsert&lt;T&gt;(ITable&lt;T&gt;, T, Expression&lt;Func&lt;IUpsertable&lt;T,T&gt;, IUpsertable&lt;T,T&gt;&gt;&gt;)</c>
	/// (and the matching Async overload) into a <see cref="SqlInsertOrUpdateStatement"/>.
	///
	/// Phase 1 scope (issue #2558):
	/// - Only the single-entity overloads (generic arity = 1) are handled.
	/// - Supported chain methods: <c>.Match</c> (content currently ignored; PK is used as keys),
	///   root <c>.Set</c>/<c>.Ignore</c>, and <c>.Insert(i => i.Set/Ignore)</c> / <c>.Update(v => v.Set/Ignore)</c>.
	/// - Rejected with <see cref="LinqToDBException"/>:
	///   <c>.When</c>, <c>.DoNothing</c>, <c>.SkipInsert</c>, <c>.SkipUpdate</c>.
	/// - IEnumerable / IQueryable source overloads (generic arity = 2) throw
	///   <see cref="LinqToDBException"/> for now (Phase 4 territory).
	/// </summary>
	[BuildsMethodCall("Upsert", "UpsertAsync")]
	sealed class UpsertBuilder : MethodCallBuilder
	{
		public static bool CanBuildMethod(MethodCallExpression call) => call.IsQueryable;

		protected override BuildSequenceResult BuildMethodCall(
			ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			// Reject bulk / IQueryable / mirror overloads for Phase 1.
			var genericArgs = methodCall.Method.GetGenericArguments();
			if (genericArgs.Length != 1)
				throw new LinqToDBException(
					"Upsert with bulk IEnumerable / IQueryable source is not yet implemented. " +
					"Use the single-entity overload Upsert(item, configure).");

			var entityType   = genericArgs[0];
			var tableArg     = methodCall.Arguments[0];
			var itemArg      = methodCall.Arguments[1];      // ConstantExpression holding the T item
			var configureArg = methodCall.Arguments[2];      // UnaryExpression(Quote, LambdaExpression)

			var configureLambda = configureArg.UnwrapLambda();

			var cfg = ParseConfigure(configureLambda);

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

			var entityDescriptor = builder.MappingSchema.GetEntityDescriptor(
				entityType, builder.DataContext.Options.ConnectionOptions.OnEntityDescriptorCreated);

			// ---- Build INSERT envelopes ----

			var insertEnvelopes = new List<UpdateBuilder.SetExpressionEnvelope>();
			var updateEnvelopes = new List<UpdateBuilder.SetExpressionEnvelope>();

			foreach (var cd in entityDescriptor.Columns)
			{
				if (IsIgnored(cd.MemberInfo, cfg.RootIgnore) || IsIgnored(cd.MemberInfo, cfg.InsertIgnore))
					goto UpdateSide;

				if (cd.SkipOnInsert)
					goto UpdateSide;

				var fieldExpr = Expression.MakeMemberAccess(contextRef, cd.MemberInfo);
				var insertBody = FindOverride(cd.MemberInfo, cfg.InsertSet)?.Body
				              ?? FindOverride(cd.MemberInfo, cfg.RootSet)?.Body;
				var valueExpr = insertBody != null
					? UpsertExpressionHelpers.SubstituteSource(insertBody, itemConst)
					: cd.MemberAccessor.GetGetterExpression(itemConst);

				insertEnvelopes.Add(new UpdateBuilder.SetExpressionEnvelope(fieldExpr, valueExpr, forceParameter: false));

				UpdateSide:

				if (IsIgnored(cd.MemberInfo, cfg.RootIgnore) || IsIgnored(cd.MemberInfo, cfg.UpdateIgnore))
					continue;

				// PK columns participate as match keys in the ON CONFLICT clause, not in the SET list.
				if (cd.IsPrimaryKey)
					continue;

				if (cd.SkipOnUpdate)
					continue;

				var updFieldExpr = Expression.MakeMemberAccess(contextRef, cd.MemberInfo);
				Expression updValueExpr;

				var updateOverride = FindOverride(cd.MemberInfo, cfg.UpdateSet);
				if (updateOverride != null)
				{
					// Update-side .Set may have (t, s) lambdas; substitute t → contextRef, s → itemConst.
					updValueExpr = UpsertExpressionHelpers.SubstituteUpdateParams(
						updateOverride.Body, contextRef, itemConst, updateOverride.Parameters);
				}
				else
				{
					var rootOverride = FindOverride(cd.MemberInfo, cfg.RootSet);
					updValueExpr = rootOverride != null
						? UpsertExpressionHelpers.SubstituteSource(rootOverride.Body, itemConst)
						: cd.MemberAccessor.GetGetterExpression(itemConst);
				}

				updateEnvelopes.Add(new UpdateBuilder.SetExpressionEnvelope(updFieldExpr, updValueExpr, forceParameter: false));
			}

			// ---- Populate statement ----

			UpdateBuilder.InitializeSetExpressions(builder, tableContext, sequence,
				insertEnvelopes, stmt.Insert.Items, createColumns: true);

			UpdateBuilder.InitializeSetExpressions(builder, tableContext, sequence,
				updateEnvelopes, stmt.Update.Items, createColumns: true);

			stmt.Insert.Into  = tableContext.SqlTable;
			stmt.Update.Table = tableContext.SqlTable;

			// ---- Match keys (Phase 1: PK-based, match expression content is ignored) ----

			var table = stmt.Insert.Into!;
			var keys  = table.GetKeys(false);

			if (keys == null || keys.Count == 0)
				throw new LinqToDBException($"Upsert requires the '{table.NameForLogging}' table to have a primary key.");

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

			return BuildSequenceResult.FromContext(
				new UpsertContext(sequence.TranslationModifier, builder, sequence, stmt));
		}

		#region Configure-expression walker

		sealed class UpsertConfig
		{
			public LambdaExpression?                             MatchCondition;
			public readonly List<MemberInfo>                     RootIgnore   = new();
			public readonly List<(MemberInfo, LambdaExpression)> RootSet      = new();
			public readonly List<MemberInfo>                     InsertIgnore = new();
			public readonly List<(MemberInfo, LambdaExpression)> InsertSet    = new();
			public readonly List<MemberInfo>                     UpdateIgnore = new();
			public readonly List<(MemberInfo, LambdaExpression)> UpdateSet    = new();
		}

		static UpsertConfig ParseConfigure(LambdaExpression configureLambda)
		{
			var cfg = new UpsertConfig();
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
					case "Match":
						cfg.MatchCondition = mc.Arguments[1].UnwrapLambda();
						break;

					case "Set":
						HandleSetCall(mc, cfg.RootSet, cfg.InsertSet, cfg.UpdateSet);
						break;

					case "Ignore":
						HandleIgnoreCall(mc, cfg.RootIgnore, cfg.InsertIgnore, cfg.UpdateIgnore);
						break;

					case "Insert":
					{
						var innerLambda = mc.Arguments[1].UnwrapLambda();
						WalkBranch(innerLambda.Body, cfg, insertBranch: true);
						break;
					}

					case "Update":
					{
						var innerLambda = mc.Arguments[1].UnwrapLambda();
						WalkBranch(innerLambda.Body, cfg, insertBranch: false);
						break;
					}

					case "SkipInsert":
					case "SkipUpdate":
					case "When":
					case "DoNothing":
						throw new LinqToDBException(
							$"Upsert configuration method '{mc.Method.Name}' is not yet implemented (Phase 1 supports .Match, .Set, .Ignore, .Insert, .Update only).");

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
					case "Set":
						HandleBranchSet(mc, cfg, insertBranch);
						break;

					case "Ignore":
						HandleBranchIgnore(mc, cfg, insertBranch);
						break;

					case "When":
					case "DoNothing":
						throw new LinqToDBException(
							$"Upsert branch method '.{mc.Method.Name}' is not yet implemented (Phase 1 supports .Set and .Ignore only inside .Insert / .Update).");

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

		static void HandleSetCall(
			MethodCallExpression mc,
			List<(MemberInfo, LambdaExpression)> rootList,
			List<(MemberInfo, LambdaExpression)> insertList,
			List<(MemberInfo, LambdaExpression)> updateList)
		{
			// Receiver type (first parameter) determines which list to append to.
			var receiverType = mc.Method.GetParameters()[0].ParameterType;
			var list =
				IsUpsertable(receiverType)            ? rootList :
				IsUpsertInsertBuilder(receiverType)   ? insertList :
				IsUpsertUpdateBuilder(receiverType)   ? updateList :
				throw new LinqToDBException($"Unexpected receiver type for Upsert.Set: {receiverType}");

			var fieldLambda = mc.Arguments[1].UnwrapLambda();
			var valueLambda = mc.Arguments[2].UnwrapLambda();

			var member = ExtractMember(fieldLambda);
			list.Add((member, valueLambda));
		}

		static void HandleIgnoreCall(
			MethodCallExpression mc,
			List<MemberInfo> rootList,
			List<MemberInfo> insertList,
			List<MemberInfo> updateList)
		{
			var receiverType = mc.Method.GetParameters()[0].ParameterType;
			var list =
				IsUpsertable(receiverType)            ? rootList :
				IsUpsertInsertBuilder(receiverType)   ? insertList :
				IsUpsertUpdateBuilder(receiverType)   ? updateList :
				throw new LinqToDBException($"Unexpected receiver type for Upsert.Ignore: {receiverType}");

			var fieldLambda = mc.Arguments[1].UnwrapLambda();
			list.Add(ExtractMember(fieldLambda));
		}

		static void HandleBranchSet(MethodCallExpression mc, UpsertConfig cfg, bool insertBranch)
		{
			var fieldLambda = mc.Arguments[1].UnwrapLambda();
			var valueLambda = mc.Arguments[2].UnwrapLambda();
			var member      = ExtractMember(fieldLambda);
			(insertBranch ? cfg.InsertSet : cfg.UpdateSet).Add((member, valueLambda));
		}

		static void HandleBranchIgnore(MethodCallExpression mc, UpsertConfig cfg, bool insertBranch)
		{
			var fieldLambda = mc.Arguments[1].UnwrapLambda();
			var member      = ExtractMember(fieldLambda);
			(insertBranch ? cfg.InsertIgnore : cfg.UpdateIgnore).Add(member);
		}

		static bool IsUpsertable(Type t) =>
			t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IUpsertable<,>);

		static bool IsUpsertInsertBuilder(Type t) =>
			t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IUpsertInsertBuilder<,>);

		static bool IsUpsertUpdateBuilder(Type t) =>
			t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IUpsertUpdateBuilder<,>);

		static MemberInfo ExtractMember(LambdaExpression fieldLambda)
		{
			var body = fieldLambda.Body;
			while (body is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } u)
				body = u.Operand;

			if (body is MemberExpression me)
				return me.Member;

			throw new LinqToDBException("Expected a simple x => x.Member expression but got " + fieldLambda);
		}

		static bool IsIgnored(MemberInfo member, List<MemberInfo> list)
		{
			foreach (var m in list)
				if (MemberInfoEquals(m, member)) return true;
			return false;
		}

		static LambdaExpression? FindOverride(MemberInfo member, List<(MemberInfo M, LambdaExpression V)> list)
		{
			// Later entries override earlier ones (branch-specific wins over root when merged externally).
			LambdaExpression? winner = null;
			foreach (var (m, v) in list)
				if (MemberInfoEquals(m, member))
					winner = v;
			return winner;
		}

		static bool MemberInfoEquals(MemberInfo a, MemberInfo b)
		{
			if (a == b) return true;
			if (!string.Equals(a.Name, b.Name, StringComparison.Ordinal)) return false;
			// Compare by declaring type's metadata so reflected-vs-declared instances match.
			return a.Module == b.Module && a.MetadataToken == b.MetadataToken;
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
	}

	file static class UpsertExpressionHelpers
	{
		/// <summary>
		/// Substitute a single-param source lambda: <c>s =&gt; s.Something</c> body using <paramref name="itemConstant"/>
		/// wherever <c>s</c> appears. If the lambda is parameter-less (<c>() =&gt; val</c>), returns the body unchanged.
		/// </summary>
		public static Expression SubstituteSource(Expression lambdaBody, Expression itemConstant)
		{
			return new SourceParmReplacer(itemConstant).Visit(lambdaBody);
		}

		/// <summary>
		/// Substitute params for a setter lambda that may have 0, 1 (source-only), or 2 ((target, source)) parameters.
		/// </summary>
		public static Expression SubstituteUpdateParams(
			Expression                                                            lambdaBody,
			Expression                                                            targetContextRef,
			Expression                                                            sourceItemConstant,
			System.Collections.ObjectModel.ReadOnlyCollection<ParameterExpression> lambdaParameters)
		{
			if (lambdaParameters.Count == 0)
				return lambdaBody;

			var replacements = new Dictionary<ParameterExpression, Expression>(lambdaParameters.Count);

			if (lambdaParameters.Count == 1)
			{
				// Single-param: source-only (s => …)
				replacements[lambdaParameters[0]] = sourceItemConstant;
			}
			else
			{
				// Two-param: (t, s) => …
				replacements[lambdaParameters[0]] = targetContextRef;
				replacements[lambdaParameters[1]] = sourceItemConstant;
			}

			return new ParamReplacer(replacements).Visit(lambdaBody);
		}

		sealed class SourceParmReplacer : ExpressionVisitor
		{
			readonly Expression _replacement;
			public SourceParmReplacer(Expression replacement) => _replacement = replacement;

			protected override Expression VisitParameter(ParameterExpression node)
			{
				// Replace any parameter with the source/item — single-param lambdas are
				// the only case that reaches this path for insert-side and root Set.
				return _replacement.Type == node.Type
					? _replacement
					: Expression.Convert(_replacement, node.Type);
			}
		}

		sealed class ParamReplacer : ExpressionVisitor
		{
			readonly Dictionary<ParameterExpression, Expression> _map;
			public ParamReplacer(Dictionary<ParameterExpression, Expression> map) => _map = map;

			protected override Expression VisitParameter(ParameterExpression node) =>
				_map.TryGetValue(node, out var replacement)
					? (replacement.Type == node.Type ? replacement : Expression.Convert(replacement, node.Type))
					: base.VisitParameter(node);
		}
	}
}
