using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using Extensions;
	using SqlBuilder;

	class JoinBuilder : MethodCallBuilder
	{
		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			if (!methodCall.IsQueryable("Join", "GroupJoin") || methodCall.Arguments.Count != 5)
				return false;

			var body = ((LambdaExpression)methodCall.Arguments[2].Unwrap()).Body.Unwrap();

			if (body.NodeType == ExpressionType	.MemberInit)
			{
				var mi = (MemberInitExpression)body;
				bool throwExpr;

				if (mi.NewExpression.Arguments.Count > 0 || mi.Bindings.Count == 0)
					throwExpr = true;
				else
					throwExpr = mi.Bindings.Any(b => b.BindingType != MemberBindingType.Assignment);

				if (throwExpr)
					throw new NotSupportedException(string.Format("Explicit construction of entity type '{0}' in join is not allowed.", body.Type));
			}

			return true;
		}

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var isGroup      = methodCall.Method.Name == "GroupJoin";
			var outerContext = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0], buildInfo.SqlQuery));
			var innerContext = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[1], new SqlQuery()));
			var countContext = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[1], new SqlQuery()));

			var context  = new SubQueryContext(outerContext);
			innerContext = isGroup ? new GroupJoinSubQueryContext(innerContext, methodCall) : new SubQueryContext(innerContext);
			countContext = new SubQueryContext(countContext);

			var join = isGroup ? innerContext.SqlQuery.WeakLeftJoin() : innerContext.SqlQuery.InnerJoin();
			var sql  = context.SqlQuery;

			sql.From.Tables[0].Joins.Add(join.JoinedTable);

			var selector = (LambdaExpression)methodCall.Arguments[4].Unwrap();

			context.SetAlias(selector.Parameters[0].Name);
			innerContext.SetAlias(selector.Parameters[1].Name);

			var outerKeyLambda = ((LambdaExpression)methodCall.Arguments[2].Unwrap());
			var innerKeyLambda = ((LambdaExpression)methodCall.Arguments[3].Unwrap());

			var outerKeySelector = outerKeyLambda.Body.Unwrap();
			var innerKeySelector = innerKeyLambda.Body.Unwrap();

			var outerParent = context.     Parent;
			var innerParent = innerContext.Parent;
			var countParent = countContext.Parent;

			var outerKeyContext = new ExpressionContext(buildInfo.Parent, context,      outerKeyLambda);
			var innerKeyContext = new InnerKeyContext  (buildInfo.Parent, innerContext, innerKeyLambda);
			var countKeyContext = new ExpressionContext(buildInfo.Parent, countContext, innerKeyLambda);

			// Process counter.
			//
			var counterSql = ((SubQueryContext)countContext).SqlQuery;

			// Make join and where for the counter.
			//
			if (outerKeySelector.NodeType == ExpressionType.New)
			{
				var new1 = (NewExpression)outerKeySelector;
				var new2 = (NewExpression)innerKeySelector;

				for (var i = 0; i < new1.Arguments.Count; i++)
				{
					var arg1 = new1.Arguments[i];
					var arg2 = new2.Arguments[i];

					BuildJoin(builder, join, outerKeyContext, arg1, innerKeyContext, arg2, countKeyContext, counterSql);
				}
			}
			else if (outerKeySelector.NodeType == ExpressionType.MemberInit)
			{
				var mi1 = (MemberInitExpression)outerKeySelector;
				var mi2 = (MemberInitExpression)innerKeySelector;

				for (var i = 0; i < mi1.Bindings.Count; i++)
				{
					if (mi1.Bindings[i].Member != mi2.Bindings[i].Member)
						throw new LinqException(string.Format("List of member inits does not match for entity type '{0}'.", outerKeySelector.Type));

					var arg1 = ((MemberAssignment)mi1.Bindings[i]).Expression;
					var arg2 = ((MemberAssignment)mi2.Bindings[i]).Expression;

					BuildJoin(builder, join, outerKeyContext, arg1, innerKeyContext, arg2, countKeyContext, counterSql);
				}
			}
			else
			{
				BuildJoin(builder, join, outerKeyContext, outerKeySelector, innerKeyContext, innerKeySelector, countKeyContext, counterSql);
			}

			builder.ReplaceParent(outerKeyContext, outerParent);
			builder.ReplaceParent(innerKeyContext, innerParent);
			builder.ReplaceParent(countKeyContext, countParent);

			if (isGroup)
			{
				counterSql.ParentSql = sql;
				counterSql.Select.Columns.Clear();

				var inner = (GroupJoinSubQueryContext)innerContext;

				inner.Join       = join.JoinedTable;
				inner.CounterSql = counterSql;
				return new GroupJoinContext(
					buildInfo.Parent, selector, context, inner, methodCall.Arguments[1], outerKeyLambda, innerKeyLambda);
			}

			return new JoinContext(buildInfo.Parent, selector, context, innerContext)
#if DEBUG
			{
				MethodCall = methodCall
			}
#endif
				;
		}

		protected override SequenceConvertInfo Convert(
			ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo, ParameterExpression param)
		{
			return null;
		}

		static void BuildJoin(
			ExpressionBuilder        builder,
			SqlQuery.FromClause.Join join,
			IBuildContext outerKeyContext, Expression outerKeySelector,
			IBuildContext innerKeyContext, Expression innerKeySelector,
			IBuildContext countKeyContext, SqlQuery countSql)
		{
			var predicate = builder.ConvertObjectComparison(
				ExpressionType.Equal,
				outerKeyContext, outerKeySelector,
				innerKeyContext, innerKeySelector);

			if (predicate != null)
				join.JoinedTable.Condition.Conditions.Add(new SqlQuery.Condition(false, predicate));
			else
				join
					.Expr(builder.ConvertToSql(outerKeyContext, outerKeySelector)).Equal
					.Expr(builder.ConvertToSql(innerKeyContext, innerKeySelector));

			predicate = builder.ConvertObjectComparison(
				ExpressionType.Equal,
				outerKeyContext, outerKeySelector,
				countKeyContext, innerKeySelector);

			if (predicate != null)
				countSql.Where.SearchCondition.Conditions.Add(new SqlQuery.Condition(false, predicate));
			else
				countSql.Where
					.Expr(builder.ConvertToSql(outerKeyContext, outerKeySelector)).Equal
					.Expr(builder.ConvertToSql(countKeyContext, innerKeySelector));
		}

		class InnerKeyContext : ExpressionContext
		{
			public InnerKeyContext(IBuildContext parent, IBuildContext sequence, LambdaExpression lambda)
				: base(parent, sequence, lambda)
			{
			}

			public override SqlInfo[] ConvertToSql(Expression expression, int level, ConvertFlags flags)
			{
				return base
					.ConvertToSql(expression, level, flags)
					.Select(idx =>
					{
						var n = SqlQuery.Select.Add(idx.Sql);

						return new SqlInfo
						{
							Sql    = SqlQuery.Select.Columns[n],
							Member = idx.Member,
							Index  = n
						};
					})
					.ToArray();
			}
		}

		internal class JoinContext : SelectContext
		{
			public JoinContext(IBuildContext parent, LambdaExpression lambda, IBuildContext outerContext, IBuildContext innerContext)
				: base(parent, lambda, outerContext, innerContext)
			{
			}
		}

		internal class GroupJoinContext : JoinContext
		{
			public GroupJoinContext(
				IBuildContext            parent,
				LambdaExpression         lambda,
				IBuildContext            outerContext,
				GroupJoinSubQueryContext innerContext,
				Expression               innerExpression,
				LambdaExpression         outerKeyLambda,
				LambdaExpression         innerKeyLambda)
				: base(parent, lambda, outerContext, innerContext)
			{
				_innerExpression = innerExpression;
				_outerKeyLambda  = outerKeyLambda;
				_innerKeyLambda  = innerKeyLambda;

				innerContext.GroupJoin = this;
			}

			readonly Expression       _innerExpression;
			readonly LambdaExpression _outerKeyLambda;
			readonly LambdaExpression _innerKeyLambda;
			private  Expression       _groupExpression;

			interface IGroupJoinHelper
			{
				Expression GetGroupJoin(GroupJoinContext context);
			}

			class GroupJoinHelper<TKey,TElement> : IGroupJoinHelper
			{
				public Expression GetGroupJoin(GroupJoinContext context)
				{
					// Convert outer condition.
					//
					var outerParam = Expression.Parameter(context._outerKeyLambda.Body.Type, "o");
					var outerKey   = context._outerKeyLambda.Body.Transform(
						e => e == context._outerKeyLambda.Parameters[0] ? context.Lambda.Parameters[0] : e);

					outerKey = context.Builder.BuildExpression(context, outerKey);

					// Convert inner condition.
					//
					var parameters = context.Builder.CurrentSqlParameters
						.Select((p,i) => new { p, i })
						.ToDictionary(_ => _.p.Expression, _ => _.i);
					var paramArray = Expression.Parameter(typeof(object[]), "ps");

					var innerKey = context._innerKeyLambda.Body.Transform(e =>
					{
						int idx;

						if (parameters.TryGetValue(e, out idx))
						{
							return
								Expression.Convert(
									Expression.ArrayIndex(paramArray, Expression.Constant(idx)),
									e.Type);
						}

						return e;
					});

					// Item reader.
					//
// ReSharper disable AssignNullToNotNullAttribute

					var expr = Expression.Call(
						null,
						ReflectionHelper.Expressor<object>.MethodExpressor(_ => Queryable.Where(null, (Expression<Func<TElement,bool>>)null)),
						context._innerExpression,
						Expression.Lambda<Func<TElement,bool>>(
							Expression.Equal(innerKey, outerParam),
							new[] { context._innerKeyLambda.Parameters[0] }));

// ReSharper restore AssignNullToNotNullAttribute

					var lambda = Expression.Lambda<Func<IDataContext,TKey,object[],IQueryable<TElement>>>(
						Expression.Convert(expr, typeof(IQueryable<TElement>)),
						Expression.Parameter(typeof(IDataContext), "ctx"),
						outerParam,
						paramArray);

					var itemReader = CompiledQuery.Compile(lambda);

					return Expression.Call(
						null,
						ReflectionHelper.Expressor<object>.MethodExpressor(_p => GetGrouping(null, null, default(TKey), null)),
						new[]
						{
							ExpressionBuilder.ContextParam,
							Expression.Constant(context.Builder.CurrentSqlParameters),
							outerKey,
							Expression.Constant(itemReader),
						});
				}

				static IEnumerable<TElement> GetGrouping(
					QueryContext             context,
					List<ParameterAccessor>  parameterAccessor,
					TKey                     key,
					Func<IDataContext,TKey,object[],IQueryable<TElement>> itemReader)
				{
					return new GroupByBuilder.GroupByContext.Grouping<TKey,TElement>(key, context, parameterAccessor, itemReader);
				}
			}

			public override Expression BuildExpression(Expression expression, int level)
			{
				if (ReferenceEquals(expression, Lambda.Parameters[1]))
				{
					if (_groupExpression == null)
					{
						var gtype  = typeof(GroupJoinHelper<,>).MakeGenericType(
							_innerKeyLambda.Body.Type,
							_innerKeyLambda.Parameters[0].Type);

						var helper = (IGroupJoinHelper)Activator.CreateInstance(gtype);

						_groupExpression = helper.GetGroupJoin(this);
					}

					return _groupExpression;
				}

				return base.BuildExpression(expression, level);
			}
		}

		internal class GroupJoinSubQueryContext : SubQueryContext
		{
			readonly MethodCallExpression _methodCall;

			public SqlQuery.JoinedTable Join;
			public SqlQuery             CounterSql;
			public GroupJoinContext     GroupJoin;

			public GroupJoinSubQueryContext(IBuildContext subQuery, MethodCallExpression methodCall)
				: base(subQuery)
			{
				_methodCall = methodCall;
			}

			public override IBuildContext GetContext(Expression expression, int level, BuildInfo buildInfo)
			{
				if (expression == null)
					return this;

				return base.GetContext(expression, level, buildInfo);
			}

			Expression _counterExpression;
			SqlInfo[]  _counterInfo;

			public override SqlInfo[] ConvertToIndex(Expression expression, int level, ConvertFlags flags)
			{
				if (expression != null && ReferenceEquals(expression, _counterExpression))
					return _counterInfo ?? (_counterInfo = new[]
					{
						new SqlInfo
						{
							Query = CounterSql.ParentSql,
							Index = CounterSql.ParentSql.Select.Add(CounterSql),
							Sql   = CounterSql
						}
					});

				return base.ConvertToIndex(expression, level, flags);
			}

			public override IsExpressionResult IsExpression(Expression expression, int level, RequestFor testFlag)
			{
				if (testFlag == RequestFor.GroupJoin && expression == null)
					return IsExpressionResult.True;

				return base.IsExpression(expression, level, testFlag);
			}

			public SqlQuery GetCounter(Expression expr)
			{
				Join.IsWeak = true;

				_counterExpression = expr;

				return CounterSql;
			}
		}
	}
}
