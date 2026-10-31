using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Async;
using LinqToDB.Expressions;
using LinqToDB.Internal.Async;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.Reflection;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Internal.SqlQuery.Visitors;
using LinqToDB.Mapping;

namespace LinqToDB.Internal.Linq.Builder
{
	[BuildsMethodCall("GroupBy")]
	sealed class GroupByBuilder : MethodCallBuilder
	{
		static readonly MethodInfo[] GroupingSetMethods = { Methods.LinqToDB.GroupBy.Rollup, Methods.LinqToDB.GroupBy.Cube, Methods.LinqToDB.GroupBy.GroupingSets };

		#region Builder Methods

		public static bool CanBuildMethod(MethodCallExpression call)
		{
			if (!call.IsQueryable)
				return false;

			var body = ((LambdaExpression)call.Arguments[1].Unwrap()).Body.Unwrap();
			if (body.NodeType == ExpressionType.MemberInit)
			{
				var mi = (MemberInitExpression)body;
				if (mi.NewExpression.Arguments.Count > 0 || 
					mi.Bindings.Count == 0 ||
					mi.Bindings.Any(b => b.BindingType != MemberBindingType.Assignment))
				{
					throw new NotSupportedException($"Explicit construction of entity type '{body.Type}' in group by is not allowed.");
				}
			}

			return (call.Arguments[call.Arguments.Count - 1].Unwrap().NodeType == ExpressionType.Lambda);
		}

		static IEnumerable<Expression> EnumGroupingSets(Expression expression)
		{
			if (expression is NewExpression newExpression)
			{
				foreach (var arg in newExpression.Arguments)
				{
					yield return arg;
				}
			}
			else if (expression is SqlGenericConstructorExpression generic)
			{
				foreach (var arg in generic.Assignments)
				{
					yield return arg.Expression;
				}
			}
		}

		/*

		-- describing subqueries when building GroupByContext

		 SELECT GroupByContext.*
		 FROM
		 (
		    SELECT
				GroupByContext.SubQuery.Field1,
				GroupByContext.SubQuery.Field2,
				Count(*),
				SUM(GroupByContext.SubQuery.Field3)
		    FROM
			(
				SELECT dataSubquery.*
				FROM (
				   SELECT dataSequence.*
				   FROM dataSequence
				   -- all associations are attached here
				) dataSubquery
		    ) GroupByContext.SubQuery	-- groupingSubquery
		    GROUP BY
					GroupByContext.SubQuery.Field1,
					GroupByContext.SubQuery.Field2
		 ) GroupByContext

		 OUTER APPLY (  -- applying complex aggregates
			SELECT Count(*) FROM dataSubquery
			WHERE dataSubquery.Field > 10 AND
				-- filter by grouping key
				dataSubquery.Field1 == GroupByContext.Field1 AND dataSubquery.Field2 == GroupByContext.Field2
		 )

		 */

		protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var sequenceExpr    = methodCall.Arguments[0];
			var groupingKind    = GroupingType.Default;

			var dataSequenceResult = builder.TryBuildSequence(new BuildInfo(buildInfo, sequenceExpr));
			if (dataSequenceResult.BuildContext == null)
				return dataSequenceResult;

			var dataSequence = dataSequenceResult.BuildContext;

			var dataSubquery     = new SubQueryContext(dataSequence);
			var groupingSubquery = new SubQueryContext(dataSubquery);

			var keySequence     = dataSequence;

			var groupingType = methodCall.Type.GetGenericArguments()[0];
			var keySelector  = methodCall.Arguments[1].UnwrapLambda();

			// Detecting Grouping Sets
			//
			var keySelectorBody = keySelector.Body.Unwrap();

			if (keySelectorBody.NodeType == ExpressionType.Call)
			{
				var mc = (MethodCallExpression)keySelectorBody;
				if (mc.IsSameGenericMethod(GroupingSetMethods))
				{
					var groupingKey = mc.Arguments[0].Unwrap();
					if (mc.IsSameGenericMethod(Methods.LinqToDB.GroupBy.Rollup))
						groupingKind = GroupingType.Rollup;
					else if (mc.IsSameGenericMethod(Methods.LinqToDB.GroupBy.Cube))
						groupingKind = GroupingType.Cube;
					else if (mc.IsSameGenericMethod(Methods.LinqToDB.GroupBy.GroupingSets))
						groupingKind = GroupingType.GroupBySets;
					else throw new InvalidOperationException();

					keySelector = Expression.Lambda(groupingKey, keySelector.Parameters);
				}
			}

			var resultSelector  = SequenceHelper.GetArgumentLambda(methodCall, "resultSelector");
			var elementSelector = SequenceHelper.GetArgumentLambda(methodCall, "elementSelector");

			if (elementSelector == null)
			{
				var param = Expression.Parameter(methodCall.Method.GetGenericArguments()[0], "selector");
				elementSelector = Expression.Lambda(param, param);
			}

			var key                 = new KeyContext(groupingSubquery, keySelector, keySequence, buildInfo.IsSubQuery);
			var keyRef              = new ContextRefExpression(key.Body.Type, key);
			var currentPlaceholders = new Dictionary<Expression, SqlPlaceholderExpression>(ExpressionEqualityComparer.Instance);

			var element = new ElementContext(buildInfo.Parent, elementSelector, dataSubquery, buildInfo.IsSubQuery);
			var groupBy = new GroupByContext(groupingSubquery, sequenceExpr, groupingType, key, keyRef, currentPlaceholders, element,
				!builder.DataOptions.LinqOptions.GuardGrouping || builder.IsGroupingGuardDisabled, true);

			// Will be used for completing GroupBy part
			key.GroupByContext = groupBy;

			groupingSubquery.SelectQuery.GroupBy.GroupingType = groupingKind;

			var groupContextRef = new ContextRefExpression(groupBy.GetInterfaceGroupingType(), groupBy);
			var keyExpr         = Expression.PropertyOrField(groupContextRef, nameof(IGrouping<,>.Key));

			var groupingKeys = builder.BuildSqlExpression(groupBy, keyExpr, BuildPurpose.Sql, BuildFlags.ForKeys);

			if (groupingKeys is SqlErrorExpression)
			{
				return BuildSequenceResult.Error(groupingKeys);
			}

			KeepOrderByOfGroupingKeys(builder, dataSequence, groupingKeys, groupingKind);

#if DEBUG
			System.Diagnostics.Debug.WriteLine("BuildMethodCall GroupBy:\n" + groupBy.SelectQuery);
#endif

			if (resultSelector != null)
			{
				var newBody = resultSelector.Body.Replace(resultSelector.Parameters[0], keyExpr);

				if (resultSelector.Parameters.Count > 1)
				{
					newBody = newBody.Replace(resultSelector.Parameters[1], groupContextRef.WithType(resultSelector.Parameters[1].Type));
				}

				var result  = new SelectContext(buildInfo.Parent, newBody, groupBy, false);
				return BuildSequenceResult.FromContext(result);
			}

			return BuildSequenceResult.FromContext(groupBy);
		}

		// Groups are ordered by the source's ordering as far as that ordering reads only grouping keys. An entry that is a key
		// is replaced by the key, one computed from keys is rebuilt over them (both stay valid above the GROUP BY), and an
		// outer-correlated entry is constant per row and dropped. At the first other entry the ordering is kept only when the
		// key entries before it cover every grouping key; otherwise it does not order the groups and is dropped as a whole.
		static void KeepOrderByOfGroupingKeys(ExpressionBuilder builder, IBuildContext dataSequence, Expression groupingKeys, GroupingType groupingKind)
		{
			if (builder.CurrentOrderBy is not { Count: > 0 } captured)
				return;

			var keys = ExpressionBuilder.CollectDistinctPlaceholders(groupingKeys, false)
				.FindAll(static k => !QueryHelper.IsConstant(k.Sql));

			var keyChains = keys.ConvertAll(static k => CollectColumnChain(k.Sql));
			var within    = new QuerySources(dataSequence.SelectQuery);
			var kept      = new List<(Expression expr, bool descending, Sql.NullsPosition nulls)>();
			var covered   = new HashSet<SqlPlaceholderExpression>();
			var cut       = false;

			foreach (var (expr, descending, nulls) in captured)
			{
				var orderSql = builder.BuildSqlExpression(dataSequence, expr, BuildPurpose.Sql, BuildFlags.ForKeys);

				if (!SequenceHelper.IsSqlReady(orderSql))
				{
					cut = true;
					break;
				}

				var entry = new List<(SqlPlaceholderExpression placeholder, bool isKey)>();

				foreach (var placeholder in ExpressionBuilder.CollectDistinctPlaceholders(orderSql, false))
				{
					if (QueryHelper.IsConstant(placeholder.Sql))
						continue;

					if (placeholder.SelectQuery == null)
					{
						cut = true;
						break;
					}

					if (!within.Queries.Contains(placeholder.SelectQuery))
						continue;

					var rewritten = RewriteOverKeys(placeholder.Sql, keys, keyChains);

					if (keys.Find(k => ReferenceEquals(k.Sql, rewritten)) is { } key)
					{
						entry.Add((key, true));
					}
					else if (groupingKind == GroupingType.Default && keys.Count > 0 && IsBuiltFromKeys(rewritten, keys, within))
					{
						entry.Add((ExpressionBuilder.CreatePlaceholder(keys[0].SelectQuery, rewritten, placeholder.Path, trackingPath: placeholder.TrackingPath), false));
					}
					else
					{
						cut = true;
						break;
					}
				}

				if (cut)
					break;

				foreach (var (placeholder, isKey) in entry)
				{
					kept.Add((placeholder, descending, nulls));

					if (isKey)
						covered.Add(placeholder);
				}
			}

			if (cut && !keys.TrueForAll(covered.Contains))
				kept.Clear();

			builder.RemoveOrderByEntries(static _ => true);

			foreach (var (expr, descending, nulls) in kept)
			{
				builder.RegisterOrderBy(expr, descending, nulls, reset: false);
			}
		}

		// A column re-exposes its expression per row, so two expressions whose column chains share an element are the same
		// value. A set-operation column takes its value from every leg, so the chain stops there.
		static List<ISqlExpression> CollectColumnChain(ISqlExpression sql)
		{
			var chain   = new List<ISqlExpression>();
			var current = Unwrap(sql);

			chain.Add(current);

			while (current is SqlColumn { Parent: not { HasSetOperators: true } } column)
			{
				current = Unwrap(column.Expression);
				chain.Add(current);
			}

			return chain;

			static ISqlExpression Unwrap(ISqlExpression expr)
			{
				expr = QueryHelper.UnwrapExpression(expr, true);
				return expr is SqlNullabilityExpression nullability ? Unwrap(nullability.SqlExpression) : expr;
			}
		}

		static SqlPlaceholderExpression? FindKey(ISqlExpression sql, List<SqlPlaceholderExpression> keys, List<List<ISqlExpression>> keyChains)
		{
			var chain = CollectColumnChain(sql);

			for (var i = 0; i < keys.Count; i++)
			{
				var keyChain = keyChains[i];

				if (chain.Exists(e => keyChain.Exists(k => k.Equals(e))))
					return keys[i];
			}

			return null;
		}

		// Replaces every sub-expression that is a grouping key with the key's SQL. A second pass also expands a column of a
		// single-source query without set operators into its expression, so that a key function projected below the GroupBy
		// is seen; it runs after the first because Convert is post-order, and an expanded child would hide a key its parent is.
		static ISqlExpression RewriteOverKeys(ISqlExpression sql, List<SqlPlaceholderExpression> keys, List<List<ISqlExpression>> keyChains)
		{
			var matched = sql.Convert((keys, keyChains), static (visitor, element) =>
				IsOperand(visitor, element) && element is ISqlExpression expr && FindKey(expr, visitor.Context.keys, visitor.Context.keyChains) is { } key
					? key.Sql
					: element,
				withStack: true);

			if (keys.Exists(k => ReferenceEquals(k.Sql, matched)))
				return matched;

			return matched.Convert((keys, keyChains), static (visitor, element) =>
			{
				if (!IsOperand(visitor, element) || element is not ISqlExpression expr)
					return element;

				if (FindKey(expr, visitor.Context.keys, visitor.Context.keyChains) is { } key)
					return key.Sql;

				if (expr is SqlColumn { Parent: { HasSetOperators: false } parent } column && !QueryHelper.EnumerateLevelSources(parent).Skip(1).Any())
					return QueryHelper.UnwrapExpression(column.Expression, true);

				return element;
			},
			withStack: true);
		}

		// Only a node that is the root or an operand of an expression is replaced: the visitor also reaches nodes in structural
		// positions (the column of a CTE field, a predicate slot), which must keep their type.
		static bool IsOperand(SqlQueryConvertVisitorBase visitor, IQueryElement element)
		{
			if (visitor.ParentElement is null)
				return true;

			return element is not ISqlPredicate && visitor.ParentElement
				is SqlBinaryExpression
				or SqlUnaryExpression
				or SqlNullabilityExpression
				or SqlCastExpression
				or SqlCoalesceExpression
				or SqlParameterizedExpressionBase
				or SqlConditionExpression
				or SqlCaseExpression
				or ISqlPredicate;
		}

		// A closed test: every node must be a key, a constant, a value of an outer row, or one of the composite kinds below
		// over such nodes. Aggregate and window functions would change meaning above the GROUP BY, as in QueryHelper.IsConstant.
		static bool IsBuiltFromKeys(ISqlExpression sql, List<SqlPlaceholderExpression> keys, QuerySources within)
		{
			var context = new BuiltFromKeysContext(keys, within);

			sql.VisitParentFirst(context, static (ctx, element) =>
			{
				if (!ctx.Result)
					return false;

				if (element is ISqlExpression expr && ctx.Keys.Exists(k => ReferenceEquals(k.Sql, expr)))
					return false;

				switch (element)
				{
					case SqlField field:
						ctx.Result = field.Table != null && !ctx.Within.Sources.Contains(field.Table);
						return false;
					case SqlCteTableField cteField:
						ctx.Result = cteField.Table != null && !ctx.Within.Sources.Contains(cteField.Table);
						return false;
					case SqlColumn column:
						ctx.Result = column.Parent != null && !ctx.Within.Queries.Contains(column.Parent) || QueryHelper.IsConstant(column);
						return false;
					case SqlParameterizedExpressionBase parameterized:
						ctx.Result = parameterized.IsPure && (parameterized.Flags & (SqlFlags.IsAggregate | SqlFlags.IsWindowFunction)) == 0;
						return ctx.Result;
				}

				switch (element.ElementType)
				{
					case QueryElementType.SqlValue:
					case QueryElementType.SqlParameter:
					case QueryElementType.SqlParameterCast:
					case QueryElementType.SqlDefaultValueExpression:
					case QueryElementType.SqlDataType:
					case QueryElementType.TruePredicate:
					case QueryElementType.FalsePredicate:
						return false;
					case QueryElementType.SqlBinaryExpression:
					case QueryElementType.SqlUnaryExpression:
					case QueryElementType.SqlNullabilityExpression:
					case QueryElementType.SqlCast:
					case QueryElementType.SqlCoalesce:
					case QueryElementType.SqlCondition:
					case QueryElementType.SqlCase:
					case QueryElementType.SearchCondition:
					case QueryElementType.NotPredicate:
					case QueryElementType.ExprPredicate:
					case QueryElementType.ExprExprPredicate:
					case QueryElementType.IsNullPredicate:
					case QueryElementType.IsTruePredicate:
					case QueryElementType.IsDistinctPredicate:
					case QueryElementType.BetweenPredicate:
					case QueryElementType.InListPredicate:
						return true;
				}

				ctx.Result = false;
				return false;
			});

			return context.Result;
		}

		sealed class BuiltFromKeysContext(List<SqlPlaceholderExpression> keys, QuerySources within)
		{
			public List<SqlPlaceholderExpression> Keys   { get; } = keys;
			public QuerySources                   Within { get; } = within;
			public bool                           Result { get; set; } = true;
		}

		/// <summary>
		/// The queries and table sources reachable from a query through its FROM and JOIN sources, entering subqueries and
		/// CTE bodies — what a placeholder anchored at or below that query can reference.
		/// </summary>
		internal sealed class QuerySources
		{
			public HashSet<SelectQuery>     Queries { get; } = new();
			public HashSet<ISqlTableSource> Sources { get; } = new();

			public QuerySources(SelectQuery root)
			{
				var pending = new Stack<SelectQuery>();

				pending.Push(root);

				while (pending.Count > 0)
				{
					var query = pending.Pop();

					if (!Queries.Add(query))
						continue;

					Sources.Add(query);

					foreach (var table in query.From.Tables)
						Add(table, pending);

					if (query.HasSetOperators)
					{
						foreach (var setOperator in query.SetOperators)
							pending.Push(setOperator.SelectQuery);
					}
				}
			}

			void Add(SqlTableSource tableSource, Stack<SelectQuery> pending)
			{
				var source = tableSource.Source;

				Sources.Add(source);

				switch (source)
				{
					case SelectQuery subQuery:
						pending.Push(subQuery);
						break;
					case SqlCteTable { Cte.Body: { } body }:
						pending.Push(body);
						break;
				}

				foreach (var join in tableSource.Joins)
					Add(join.Table, pending);
			}
		}

		/// <summary>
		/// Appends GroupBy items to <paramref name="sequence"/> SelectQuery.
		/// </summary>
		/// <param name="builder"></param>
		/// <param name="keyContext"></param>
		/// <param name="sequence">Context which contains groping query.</param>
		/// <param name="currentPlaceholders"></param>
		/// <param name="onSequence">Context from which level we want to get groping SQL.</param>
		/// <param name="path"></param>
		/// <param name="groupingExpr">Actual expression which should be translated to grouping keys.</param>
		/// <param name="groupingKind"></param>
		/// <param name="translated"></param>
		/// <param name="errorExpression"></param>
		static bool AppendGrouping(ExpressionBuilder         builder,
			KeyContext                                       keyContext,
			IBuildContext                                    sequence,
			Dictionary<Expression, SqlPlaceholderExpression> currentPlaceholders,
			IBuildContext                                    onSequence,
			Expression                                       path,
			Expression                                       groupingExpr,
			GroupingType                                     groupingKind,
			out                      Expression              translated,
			[NotNullWhen(false)] out Expression?             errorExpression)
		{
			errorExpression = null;
			translated      = groupingExpr;

			if (groupingKind == GroupingType.GroupBySets)
			{
				var hasSets  = false;
				foreach (var groupingSet in EnumGroupingSets(groupingExpr))
				{
					hasSets = true;
					var setExpr = builder.BuildSqlExpression(onSequence, groupingSet,
						buildPurpose: BuildPurpose.Sql, buildFlags: BuildFlags.ForKeys);

					if (!SequenceHelper.IsSqlReady(setExpr))
					{
						errorExpression = SqlErrorExpression.EnsureError(setExpr, groupingExpr.Type);
						return false;
					}

					setExpr = builder.UpdateNesting(sequence, setExpr);

					var placeholders = ExpressionBuilder.CollectPlaceholders(setExpr, true);

					sequence.SelectQuery.GroupBy.Items.Add(new SqlGroupingSet(placeholders.Select(p => p.Sql)));
				}

				if (!hasSets)
					throw new LinqToDBException($"Invalid grouping sets expression '{groupingExpr}'.");
			}
			else
			{
				if (!SequenceHelper.IsSqlReady(groupingExpr))
				{
					var sqLError = groupingExpr.Find(e => e is SqlErrorExpression);
					errorExpression = sqLError ?? SqlErrorExpression.EnsureError(groupingExpr, groupingExpr.Type);
					return false;
				}

				translated = AppendGroupBy(builder, keyContext, path, currentPlaceholders, sequence.SelectQuery, groupingExpr);
			}

			return true;
		}

		static Expression AppendGroupBy(ExpressionBuilder builder, IBuildContext keyContext, Expression path, Dictionary<Expression, SqlPlaceholderExpression> currentPlaceholders, SelectQuery query, Expression groupByExpression)
		{
			var placeholders = ExpressionBuildVisitor.CollectPlaceholders(groupByExpression);

			if (placeholders.Count == 1 && SequenceHelper.IsSameContext(path, keyContext))
			{
				var alreadyTranslated = placeholders[0];
				if (QueryHelper.IsConstantFast(alreadyTranslated.Sql))
				{
					// it is a case whe we do not group elements
					var newPlaceholder = builder.UpdateNesting(query, alreadyTranslated)
						.WithSelectQuery(query);

					return newPlaceholder;
				}

				placeholders.Clear();
				placeholders.Add(alreadyTranslated.WithPath(path));
			}

			var transformed = groupByExpression;

			foreach (var p in placeholders)
			{
				var traversed = builder.BuildTraverseExpression(p.Path);
				if (!currentPlaceholders.TryGetValue(traversed, out var placeholder))
				{
					placeholder = builder.UpdateNesting(query, p);
					currentPlaceholders.Add(traversed, placeholder);

					query.GroupBy.Items.Add(placeholder.Sql);
				}

				transformed = transformed.Replace(p, placeholder);
			}

			return transformed;
		}

		#endregion

		#region Element Context

		internal sealed class ElementContext : SelectContext
		{
			public ElementContext(IBuildContext? parent, LambdaExpression lambda, IBuildContext sequence, bool isSubQuery) :
				base(parent, SequenceHelper.PrepareBody(lambda, sequence), sequence, isSubQuery)
			{
				Lambda   = lambda;
				Sequence = sequence;
			}

			public LambdaExpression Lambda   { get; set; }
			public IBuildContext    Sequence { get; }

			public override Expression MakeExpression(Expression path, ProjectFlags flags)
			{
				if (flags.IsRoot() && SequenceHelper.IsSameContext(path, this))
					return path;

				var newExpr = base.MakeExpression(path, flags);

				return newExpr;
			}

			public override IBuildContext Clone(CloningContext context)
			{
				return new ElementContext(null, context.CloneExpression(Lambda), context.CloneContext(Sequence), IsSubQuery);
			}
		}

		#endregion

		#region KeyContext

		internal sealed class KeyContext : SelectContext
		{
			public KeyContext(IBuildContext? parent, LambdaExpression lambda, IBuildContext sequence, bool isSubQuery) :
				base(parent, SequenceHelper.PrepareBody(lambda, sequence), sequence, isSubQuery)
			{
				Lambda   = lambda;
				Sequence = sequence;
			}

			public LambdaExpression Lambda         { get; }
			public IBuildContext    Sequence       { get; }
			public GroupByContext   GroupByContext { get; set; } = null!;

			public override Expression MakeExpression(Expression path, ProjectFlags flags)
			{
				if (flags.IsRoot() || flags.IsAssociationRoot())
				{
					if (SequenceHelper.IsSameContext(path, this))
						return path;

					var root = base.MakeExpression(path, flags);
					return root;
				}

				if (flags.IsKeys() && SequenceHelper.IsSameContext(path, this))
				{
					if (Body.Type != path.Type && flags.IsSql())
					{
						using var outside = Builder.UsingOutsideProjectionValue();

						var resultExpr = Builder.BuildSqlExpression(this, Body);
						return resultExpr;
					}
				}

				var projected = base.MakeExpression(path, flags);
				var result    = projected;

				if (flags.IsAggregationRoot() || flags.IsTraverse())
					return result;

				if (!ExpressionEqualityComparer.Instance.Equals(result, path) && (flags.IsSql() || flags.IsExpression() || flags.IsExtractProjection() || flags.IsExpand()))
				{
					// Use Expand only for a member-root navigation off a non-scalar key (g.Key.<association>),
					// so the key's associations are built and the navigation resolves. Whole-entity key
					// materialization must stay Sql: under Expand a ForKeys build loses the Keys flag in
					// GetProjectFlags, so the key would select every column instead of reducing to its PK
					// (the Value1 over-fetch). A scalar key stays Sql too — under Expand a bare scalar comes
					// back as an unresolved column reference, breaking eager-load correlation
					// (detail.Where(d => d.X == g.Key)).
					var keyPurpose = flags.IsExpand() && flags.IsMemberRoot() && !Builder.MappingSchema.IsScalarType(Body.Type)
						? BuildPurpose.Expand
						: BuildPurpose.Sql;

					// The key is the GROUP BY's: a value of the projection reads it, it is not calculated for it.
					using (Builder.UsingOutsideProjectionValue())
						result = Builder.BuildSqlExpression(this, result, keyPurpose, !flags.IsExpression() ? BuildFlags.ForKeys : BuildFlags.None);

					if (result is SqlErrorExpression)
						return SqlErrorExpression.EnsureError(result, path.Type);

					if (GroupByContext.SubQuery.SelectQuery.GroupBy.IsEmpty || GroupByContext.SubQuery.SelectQuery.GroupBy.GroupingType != GroupingType.GroupBySets)
					{
						// appending missing keys
						if (!AppendGrouping(Builder,     this, GroupByContext.SubQuery, GroupByContext.CurrentPlaceholders,
							    GroupByContext.SubQuery, path, result,                  GroupByContext.SubQuery.SelectQuery.GroupBy.GroupingType, out var translated, out var error))
						{
							return path;
						}

						result = translated;
					}

					// we return SQL nested as GroupByContext.SubQuery
					result = Builder.UpdateNesting(GroupByContext.SubQuery, result);
				}

				return result;
			}

			public override IBuildContext Clone(CloningContext context)
			{
				return new KeyContext(null, context.CloneExpression(Lambda), context.CloneContext(Sequence!), IsSubQuery);
			}
		}

		#endregion

		#region GroupByContext

		internal sealed class GroupByContext : SubQueryContext
		{
			public GroupByContext(
				IBuildContext                                    sequence,
				Expression                                       sequenceExpr,
				Type                                             groupingType,
				KeyContext                                       key,
				ContextRefExpression                             keyRef,
				Dictionary<Expression, SqlPlaceholderExpression> currentPlaceholders,
				ElementContext                                   element,
				bool                                             isGroupingGuardDisabled,
				bool                                             addToSql)
				: this(sequence, new SelectQuery(), sequenceExpr, groupingType, key, keyRef, currentPlaceholders, element, isGroupingGuardDisabled, addToSql)
			{
			}

			public GroupByContext(
				IBuildContext                                    sequence,
				SelectQuery                                      selectQuery,
				Expression                                       sequenceExpr,
				Type                                             groupingType,
				KeyContext                                       key,
				ContextRefExpression                             keyRef,
				Dictionary<Expression, SqlPlaceholderExpression> currentPlaceholders,
				ElementContext                                   element,
				bool                                             isGroupingGuardDisabled,
				bool                                             addToSql)
				: base(sequence, selectQuery, addToSql)
			{
				_sequenceExpr       = sequenceExpr;
				_key                = key;
				_keyRef             = keyRef;
				CurrentPlaceholders = currentPlaceholders;
				Element             = element;
				_groupingType       = groupingType;

				IsGroupingGuardDisabled = isGroupingGuardDisabled;

				key.GroupByContext = this;
				key.Parent         = this;
			}

			public Dictionary<Expression, SqlPlaceholderExpression> CurrentPlaceholders { get; }

			readonly Expression           _sequenceExpr;
			readonly KeyContext           _key;
			readonly ContextRefExpression _keyRef;
			readonly Type                 _groupingType;
			public   bool                 IsGroupingGuardDisabled { get; }

			public ElementContext Element { get; }

			public override Type ElementType => _groupingType;

			public override Expression MakeExpression(Expression path, ProjectFlags flags)
			{
				if (flags.IsTraverse())
					return path;

				var isSameContext = SequenceHelper.IsSameContext(path, this);

				if (isSameContext)
				{
					if (flags.IsRoot() || flags.IsAggregationRoot() || flags.IsSubquery() || flags.IsExtractProjection())
						return path;
				}

				if (isSameContext)
				{
					if (flags.IsExpand())
					{
						if (flags.IsMemberRoot())
							return path;

						return MakeSubQueryExpression(path);
					}
				}

				if (isSameContext && flags.IsKeys() && GetInterfaceGroupingType().IsSameOrParentOf(path.Type))
				{
					var result = Builder.BuildExpression(this, _keyRef);
					return result;
				}

				if (isSameContext && (flags.IsExpression()/* || flags.IsExtractProjection()*/)/* && GetInterfaceGroupingType().IsSameOrParentOf(path.Type)*/)
				{
					if (!IsGroupingGuardDisabled)
					{
						var ex = new LinqToDBException(ErrorHelper.Error_GroupGuard)
						{
							HelpLink = "https://github.com/linq2db/linq2db/issues/365",
						};

						throw ex;
					}

					var groupingType = GetGroupingType();

					var groupingPath = ((ContextRefExpression)path).WithType(groupingType);

					var assignments = new List<SqlGenericConstructorExpression.Assignment>(2);

					assignments.Add(new SqlGenericConstructorExpression.Assignment(
						groupingType.GetProperty(nameof(Grouping<,>.Key))!,
						Expression.Property(groupingPath, nameof(IGrouping<,>.Key)), true, false));

					var eagerLoadingExpression = MakeSubQueryExpression(new ContextRefExpression(groupingType, this));

					assignments.Add(new SqlGenericConstructorExpression.Assignment(
						groupingType.GetProperty(nameof(Grouping<,>.Items))!,
						eagerLoadingExpression, true, false));

					return new SqlGenericConstructorExpression(
						SqlGenericConstructorExpression.CreateType.Auto,
						groupingType, null, assignments.AsReadOnly(), MappingSchema, path);
				}

				if (path is MemberExpression me)
				{
					if (flags.IsExtractProjection())
					{
						return path;
					}

					var currentMemberExpr = me;
					var found             = false;
					while (true)
					{
						if (currentMemberExpr.Expression is ContextRefExpression && string.Equals(currentMemberExpr.Member.Name, "Key", StringComparison.Ordinal))
						{
							found = true;
							break;
						}

						if (currentMemberExpr.Expression is MemberExpression memberExpr)
						{
							currentMemberExpr = memberExpr;
						}
						else
							break;
					}

					if (found)
					{
						var keyRef  = new ContextRefExpression(currentMemberExpr.Type, _key);
						var keyPath = me.Replace(currentMemberExpr, keyRef);

						var result = Builder.BuildExpression(_key, keyPath, BuildFlags.ForKeys);

						if (ExpressionEqualityComparer.Instance.Equals(result, keyPath) && flags.IsSqlOrExpression())
							return SqlErrorExpression.EnsureError(path);

						if (result is SqlPlaceholderExpression placeholder)
						{
							// we need to update path to the same as in placeholder
							result = placeholder.WithPath(path);
						}

						return result;
					}
				}

				if (path is MemberExpression { Expression: { } expr } /* || !flags.IsSql()*/)
				{
					var root = Builder.BuildAggregationRootExpression(expr);
					if (typeof(IGrouping<,>).IsSameOrParentOf(root.Type))
					{
						return path;
					}
				}

				if (isSameContext && flags.IsSql() && !flags.IsKeys() && path.Type != Element.ElementType)
				{
					return path;
				}

				var newPath = SequenceHelper.CorrectExpression(path, this, Element);

				return newPath;
			}

			public Type GetGroupingType()
			{
				var groupingType = typeof(Grouping<,>).MakeGenericType(
					_key.Body.Type, Element.Body.Type);
				return groupingType;
			}

			public Type GetInterfaceGroupingType()
			{
				var groupingType = typeof(IGrouping<,>).MakeGenericType(
					_key.Body.Type, Element.Body.Type);
				return groupingType;
			}

			public override IBuildContext Clone(CloningContext context)
			{
				var clone = new GroupByContext(
					context.CloneContext(SubQuery),
					context.CloneElement(SelectQuery),
					context.CloneExpression(_sequenceExpr),
					_groupingType,
					context.CloneContext(_key),
					context.CloneExpression(_keyRef),
					CurrentPlaceholders.ToDictionary(pair => context.CloneExpression(pair.Key), pair => context.CloneExpression(pair.Value), ExpressionEqualityComparer.Instance),
					context.CloneContext(Element),
					IsGroupingGuardDisabled,
					false
				);

				return clone;
			}

			static Expression MakeSubQueryExpression(MappingSchema mappingSchema, Expression sequence,
				ParameterExpression                                param,         Expression expr1, Expression expr2)
			{
				var filterLambda = Expression.Lambda(ExpressionBuilder.Equal(mappingSchema, expr1, expr2), param);
				return TypeHelper.MakeMethodCall(Methods.Enumerable.Where, sequence, filterLambda);
			}

			public Expression MakeSubQueryExpression(Expression buildExpression)
			{
				var expr = MakeSubQueryExpression(
					MappingSchema,
					_sequenceExpr,
					_key.Lambda.Parameters[0],
					ExpressionHelper.PropertyOrField(buildExpression, "Key"),
					_key.Lambda.Body);

				// do not repeat simple projection
				if (Element.Body != Element.Lambda.Parameters[0])
				{
					expr = TypeHelper.MakeMethodCall(Methods.Enumerable.Select, expr, Element.Lambda);
				}

				return expr;
			}

			public override IBuildContext? GetContext(Expression expression, BuildInfo buildInfo)
			{
				if (!buildInfo.IsSubQuery)
					return this;

				if (buildInfo.IsAggregation && !buildInfo.CreateSubQuery)
					return this;

				if (!SequenceHelper.IsSameContext(expression, this))
					return null;

				var expr = MakeSubQueryExpression(((ContextRefExpression)buildInfo.Expression).WithType(GetInterfaceGroupingType()));

				var parentContext = buildInfo.Parent ?? this;

				expr = Builder.UpdateNesting(parentContext, expr);

				var buildResult = Builder.TryBuildSequence(new BuildInfo(buildInfo, expr) { IsAggregation = false, CreateSubQuery = false});

				return buildResult.BuildContext;
			}

			internal sealed class Grouping<TKey,TElement> : IGrouping<TKey,TElement>
			{
				public TKey                   Key   { get; set; } = default!;
				public IEnumerable<TElement>? Items { get; set; } = default!;

				public IEnumerator<TElement> GetEnumerator()
				{
					if (Items == null)
						return Enumerable.Empty<TElement>().GetEnumerator();

					return Items.GetEnumerator();
				}

				IEnumerator IEnumerable.GetEnumerator()
				{
					return GetEnumerator();
				}
			}

			sealed class GroupingEnumerable<TKey, TElement> : IResultEnumerable<IGrouping<TKey, TElement>>
			{
				readonly IResultEnumerable<TElement> _elements;
				readonly Func<TElement, TKey>        _groupingKey;

				public GroupingEnumerable(IResultEnumerable<TElement> elements, Func<TElement, TKey> groupingKey)
				{
					_elements    = elements;
					_groupingKey = groupingKey;
				}

				public IEnumerator<IGrouping<TKey, TElement>> GetEnumerator()
				{
					return _elements.GroupBy(_groupingKey).GetEnumerator();
				}

				IEnumerator IEnumerable.GetEnumerator()
				{
					return GetEnumerator();
				}

				public IAsyncEnumerator<IGrouping<TKey, TElement>> GetAsyncEnumerator(CancellationToken cancellationToken = default)
				{
					return new GroupingAsyncEnumerator(_elements, _groupingKey, cancellationToken);
				}

				sealed class GroupingAsyncEnumerator : IAsyncEnumerator<IGrouping<TKey, TElement>>
				{
					readonly IResultEnumerable<TElement> _elements;
					readonly Func<TElement, TKey>        _groupingKey;
					readonly CancellationToken           _cancellationToken;

					IEnumerator<IGrouping<TKey, TElement>>? _grouped;
					IGrouping<TKey, TElement>?              _current;

					public GroupingAsyncEnumerator(IResultEnumerable<TElement> elements, Func<TElement, TKey> groupingKey, CancellationToken cancellationToken)
					{
						_elements          = elements;
						_groupingKey       = groupingKey;
						_cancellationToken = cancellationToken;
					}

					public ValueTask DisposeAsync()
					{
						_grouped?.Dispose();
						return new ValueTask();
					}

					public IGrouping<TKey, TElement> Current
					{
						get
						{
							if (_grouped == null)
								throw new InvalidOperationException(ErrorHelper.Error_EnumerationNotStarted);

							if (_current == null)
								throw new InvalidOperationException("Enumeration returned no result.");

							return _current;
						}
					}

					public async ValueTask<bool> MoveNextAsync()
					{
						_grouped ??= (await _elements.ToListAsync(_cancellationToken).ConfigureAwait(false))
							.GroupBy(_groupingKey)
							.GetEnumerator();

						if (_grouped.MoveNext())
						{
							_current = _grouped.Current;
							return true;
						}

						_current = null;
						return false;
					}
				}
			}
		}

		#endregion
	}
}
