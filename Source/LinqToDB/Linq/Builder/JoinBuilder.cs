﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using SqlQuery;

	class JoinBuilder : MethodCallBuilder
	{
		private static readonly string[] MethodNames = { "Join", "GroupJoin" };

		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			if (methodCall.Method.DeclaringType == typeof(LinqExtensions) || !methodCall.IsQueryable(MethodNames))
				return false;

			// other overload for Join
			if (!(methodCall.Arguments[2].Unwrap() is LambdaExpression lambda))
				return false;

			var body = lambda.Body.Unwrap();

			if (body.NodeType == ExpressionType.MemberInit)
			{
				var mi = (MemberInitExpression)body;
				bool throwExpr;

				if (mi.NewExpression.Arguments.Count > 0 || mi.Bindings.Count == 0)
					throwExpr = true;
				else
					throwExpr = mi.Bindings.Any(b => b.BindingType != MemberBindingType.Assignment);

				if (throwExpr)
					throw new NotSupportedException($"Explicit construction of entity type '{body.Type}' in join is not allowed.");
			}

			return true;
		}

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var isGroup      = methodCall.Method.Name == "GroupJoin";
			var outerContext = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0], buildInfo.SelectQuery));
			var innerContext = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[1], new SelectQuery()));

			var context  = new SubQueryContext(outerContext);
			innerContext = isGroup ? new GroupJoinSubQueryContext(innerContext) : new SubQueryContext(innerContext);

			var join = isGroup ? innerContext.SelectQuery.WeakLeftJoin() : innerContext.SelectQuery.InnerJoin();
			var sql  = context.SelectQuery;

			sql.From.Tables[0].Joins.Add(join.JoinedTable);

			var selector = (LambdaExpression)methodCall.Arguments[4].Unwrap();

			context.     SetAlias(selector.Parameters[0].Name);
			innerContext.SetAlias(selector.Parameters[1].Name);

			var outerKeyLambda = ((LambdaExpression)methodCall.Arguments[2].Unwrap());
			var innerKeyLambda = ((LambdaExpression)methodCall.Arguments[3].Unwrap());

			var outerKeySelector = outerKeyLambda.Body.Unwrap();
			var innerKeySelector = innerKeyLambda.Body.Unwrap();

			var outerParent = context.     Parent;
			var innerParent = innerContext.Parent;

			var outerKeyContext = new ExpressionContext(buildInfo.Parent, context,      outerKeyLambda);
			var innerKeyContext = new InnerKeyContext  (buildInfo.Parent, innerContext, innerKeyLambda);

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

					BuildJoin(builder, join.JoinedTable.Condition, outerKeyContext, arg1, innerKeyContext, arg2);
				}
			}
			else if (outerKeySelector.NodeType == ExpressionType.MemberInit)
			{
				var mi1 = (MemberInitExpression)outerKeySelector;
				var mi2 = (MemberInitExpression)innerKeySelector;

				for (var i = 0; i < mi1.Bindings.Count; i++)
				{
					if (mi1.Bindings[i].Member != mi2.Bindings[i].Member)
						throw new LinqException($"List of member inits does not match for entity type '{outerKeySelector.Type}'.");

					var arg1 = ((MemberAssignment)mi1.Bindings[i]).Expression;
					var arg2 = ((MemberAssignment)mi2.Bindings[i]).Expression;

					BuildJoin(builder, join.JoinedTable.Condition, outerKeyContext, arg1, innerKeyContext, arg2);
				}
			}
			else
			{
				BuildJoin(builder, join.JoinedTable.Condition, outerKeyContext, outerKeySelector, innerKeyContext, innerKeySelector);
			}

			builder.ReplaceParent(outerKeyContext, outerParent);
			builder.ReplaceParent(innerKeyContext, innerParent);

			if (isGroup)
			{
				var inner = (GroupJoinSubQueryContext)innerContext;

				inner.Join               = join.JoinedTable;
				inner.GetSubQueryContext = () =>
					GetSubQueryContext(builder, methodCall, buildInfo, sql,
						innerKeyLambda, outerKeySelector, innerKeySelector, outerKeyContext);

				return new GroupJoinContext(
					buildInfo.Parent, selector, context, inner, methodCall.Arguments[1], outerKeyLambda, innerKeyLambda);
			}

			return new JoinContext(buildInfo.Parent, selector, context, innerContext)
#if DEBUG
			{
				Debug_MethodCall = methodCall
			}
#endif
				;
		}

		IBuildContext GetSubQueryContext(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo,
			SelectQuery sql,
			LambdaExpression innerKeyLambda,
			Expression outerKeySelector,
			Expression innerKeySelector,
			IBuildContext outerKeyContext)
		{
			var subQueryContext = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[1], new SelectQuery()));

			subQueryContext = new SubQueryContext(subQueryContext);

			var subQueryParent     = subQueryContext.Parent;
			var subQueryKeyContext = new ExpressionContext(buildInfo.Parent, subQueryContext, innerKeyLambda);

			// Process SubQuery.
			//
			var subQuerySql = ((SubQueryContext)subQueryContext).SelectQuery;

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

					BuildSubQueryJoin(builder, outerKeyContext, arg1, arg2, subQueryKeyContext, subQuerySql);
				}
			}
			else if (outerKeySelector.NodeType == ExpressionType.MemberInit)
			{
				var mi1 = (MemberInitExpression)outerKeySelector;
				var mi2 = (MemberInitExpression)innerKeySelector;

				for (var i = 0; i < mi1.Bindings.Count; i++)
				{
					if (mi1.Bindings[i].Member != mi2.Bindings[i].Member)
						throw new LinqException($"List of member inits does not match for entity type '{outerKeySelector.Type}'.");

					var arg1 = ((MemberAssignment)mi1.Bindings[i]).Expression;
					var arg2 = ((MemberAssignment)mi2.Bindings[i]).Expression;

					BuildSubQueryJoin(builder, outerKeyContext, arg1, arg2, subQueryKeyContext, subQuerySql);
				}
			}
			else
			{
				BuildSubQueryJoin(builder, outerKeyContext, outerKeySelector, innerKeySelector, subQueryKeyContext, subQuerySql);
			}

			builder.ReplaceParent(subQueryKeyContext, subQueryParent);

			subQuerySql.ParentSelect = sql;
			subQuerySql.Select.Columns.Clear();

			return subQueryContext;
		}

		protected override SequenceConvertInfo? Convert(
			ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo, ParameterExpression? param)
		{
			return null;
		}

		internal static void BuildJoin(
			ExpressionBuilder builder,
			SqlSearchCondition condition,
			IBuildContext outerKeyContext, Expression outerKeySelector,
			IBuildContext innerKeyContext, Expression innerKeySelector)
		{
			var predicate = builder.ConvertObjectComparison(
				ExpressionType.Equal,
				outerKeyContext, outerKeySelector,
				innerKeyContext, innerKeySelector);

			if (predicate == null)
			{
				predicate = new SqlPredicate.ExprExpr(
					builder.ConvertToSql(outerKeyContext, outerKeySelector),
					SqlPredicate.Operator.Equal,
					builder.ConvertToSql(innerKeyContext, innerKeySelector), 
					Common.Configuration.Linq.CompareNullsAsValues ? true : null);
			}

			condition.Conditions.Add(new SqlCondition(false, predicate));
		}

		static void BuildSubQueryJoin(
			ExpressionBuilder           builder,
			IBuildContext outerKeyContext, Expression  outerKeySelector,
			Expression    innerKeySelector,
			IBuildContext subQueryKeyContext, SelectQuery subQuerySelect)
		{
			var predicate = builder.ConvertObjectComparison(
				ExpressionType.Equal,
				outerKeyContext, outerKeySelector,
				subQueryKeyContext, innerKeySelector);

			if (predicate == null)
			{
				predicate = new SqlPredicate.ExprExpr(
					builder.ConvertToSql(outerKeyContext, outerKeySelector),
					SqlPredicate.Operator.Equal,
					builder.ConvertToSql(subQueryKeyContext, innerKeySelector),
					Common.Configuration.Linq.CompareNullsAsValues ? true : null);
			}

			subQuerySelect.Where.SearchCondition.Conditions.Add(new SqlCondition(false, predicate));
		}

		internal class InnerKeyContext : ExpressionContext
		{
			public InnerKeyContext(IBuildContext? parent, IBuildContext sequence, LambdaExpression lambda)
				: base(parent, sequence, lambda)
			{
			}

			public override SqlInfo[] ConvertToSql(Expression? expression, int level, ConvertFlags flags)
			{
				return base
					.ConvertToSql(expression, level, flags)
					.Select(idx =>
					{
						var n = SelectQuery.Select.Add(idx.Sql);

						return new SqlInfo(idx.MemberChain, SelectQuery.Select.Columns[n], n);
					})
					.ToArray();
			}
		}

		internal class JoinContext : SelectContext
		{
			public JoinContext(IBuildContext? parent, LambdaExpression lambda, IBuildContext outerContext, IBuildContext innerContext)
				: base(parent, lambda, outerContext, innerContext)
			{
			}

			public override void CompleteColumns()
			{
			}
		}

		internal class GroupJoinContext : JoinContext
		{
			public GroupJoinContext(
				IBuildContext?           parent,
				LambdaExpression         lambda,
				IBuildContext            outerContext,
				GroupJoinSubQueryContext innerContext,
				Expression               innerExpression,
				LambdaExpression         outerKeyLambda,
				LambdaExpression         innerKeyLambda)
				: base(parent, lambda, outerContext, innerContext)
			{
				_innerExpression = innerExpression;
				OuterKeyLambda  = outerKeyLambda;
				InnerKeyLambda  = innerKeyLambda;

				innerContext.GroupJoin = this;
			}

			readonly Expression       _innerExpression;
			public   LambdaExpression  OuterKeyLambda { get; }
			public   LambdaExpression  InnerKeyLambda { get; }
			private  Expression?       _groupExpression;

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
					var outerParam = Expression.Parameter(context.OuterKeyLambda.Body.Type, "o");
					var outerKey   = context.OuterKeyLambda.GetBody(context.Lambda.Parameters[0]);

					outerKey = context.Builder.BuildExpression(context, outerKey, false);

					// Convert inner condition.
					//
					var parameters = context.Builder.CurrentSqlParameters
						.Select((p,i) => new { p, i })
						.ToDictionary(_ => _.p.Expression, _ => _.i);
					var paramArray = Expression.Parameter(typeof(object[]), "ps");

					var innerKey = context.InnerKeyLambda.Body.Transform(
						(parameters, paramArray),
						static (context, e) =>
						{
							if (context.parameters.TryGetValue(e, out var idx))
							{
								return Expression.Convert(
									Expression.ArrayIndex(context.paramArray, Expression.Constant(idx)),
									e.Type);
							}

							return e;
						});

					// Item reader.
					//
					var expr = Expression.Call(
						null,
						MemberHelper.MethodOf(() => Queryable.Where(null, (Expression<Func<TElement,bool>>)null!)),
						context._innerExpression,
						Expression.Lambda<Func<TElement,bool>>(
							ExpressionBuilder.Equal(context.Builder.MappingSchema, innerKey, outerParam),
							new[] { context.InnerKeyLambda.Parameters[0] }));

					var lambda = Expression.Lambda<Func<IDataContext,TKey,object?[]?,IQueryable<TElement>>>(
						Expression.Convert(expr, typeof(IQueryable<TElement>)),
						Expression.Parameter(typeof(IDataContext), "ctx"),
						outerParam,
						paramArray);

					var itemReader = CompiledQuery.Compile(lambda);

					return Expression.Call(
						null,
						MemberHelper.MethodOf(() => GetGrouping(null!, null!, default!, null!)),
						new[]
						{
							ExpressionBuilder.QueryRunnerParam,
							Expression.Constant(context.Builder.CurrentSqlParameters),
							outerKey,
							Expression.Constant(itemReader),
						});
				}

				static IEnumerable<TElement> GetGrouping(
					IQueryRunner             runner,
					List<ParameterAccessor>  parameterAccessor,
					TKey                     key,
					Func<IDataContext,TKey,object?[]?,IQueryable<TElement>> itemReader)
				{
					return new GroupByBuilder.GroupByContext.Grouping<TKey,TElement>(key, runner, parameterAccessor, itemReader);
				}
			}

			interface IGroupJoinCallHelper
			{
				Expression GetGroupJoinCall(GroupJoinContext context);
			}

			class GroupJoinCallHelper<T> : IGroupJoinCallHelper
			{
				public Expression GetGroupJoinCall(GroupJoinContext context)
				{
					var expr = Expression.Call(
						null,
						MemberHelper.MethodOf(() => Queryable.Where(null, (Expression<Func<T,bool>>)null!)),
						context._innerExpression,
						Expression.Lambda<Func<T,bool>>(
							ExpressionBuilder.Equal(
								context.Builder.MappingSchema,
								context.InnerKeyLambda.Body.Unwrap(),
								context.OuterKeyLambda.GetBody(context.Lambda.Parameters[0])),
							new[] { context.InnerKeyLambda.Parameters[0] }));

					return expr;
				}
			}

			public override Expression BuildExpression(Expression? expression, int level, bool enforceServerSide)
			{
				if (ReferenceEquals(expression, Lambda.Parameters[1]))
				{
					if (_groupExpression == null)
					{
						var loadingExpression = Builder.BuildMultipleQuery(this, expression, true);

						 _groupExpression = loadingExpression;
						return _groupExpression;
					}

					return _groupExpression;
				}

				if (expression != null && expression.NodeType == ExpressionType.Call)
				{
					Expression? replaceExpression = null;

					if (level == 0)
					{
						if (expression.Find(Lambda.Parameters[1]) != null)
							replaceExpression = Lambda.Parameters[1];
					}
					else
					{
						var levelExpression = expression.GetLevelExpression(Builder.MappingSchema, level);

						if (levelExpression.NodeType == ExpressionType.MemberAccess)
						{
							var memberExpression = GetMemberExpression(
								((MemberExpression)levelExpression).Member,
								ReferenceEquals(levelExpression, expression),
								levelExpression.Type,
								expression);

							if (memberExpression.Find(Lambda.Parameters[1]) != null)
								replaceExpression = levelExpression;
						}
					}

					if (replaceExpression != null)
					{
						var call   = (MethodCallExpression)expression;
						var gtype  = typeof(GroupJoinCallHelper<>).MakeGenericType(InnerKeyLambda.Parameters[0].Type);
						var helper = (IGroupJoinCallHelper)Activator.CreateInstance(gtype)!;
						var expr   = helper.GetGroupJoinCall(this);

						expr = call.Replace(replaceExpression, expr);

						return Builder.BuildExpression(this, expr, enforceServerSide);
					}

				}

				return base.BuildExpression(expression, level, enforceServerSide);
			}
		}

		internal class GroupJoinSubQueryContext : SubQueryContext
		{
			public SqlJoinedTable?      Join;
			public SelectQuery?         CounterSelect;
			public GroupJoinContext?    GroupJoin;
			public Func<IBuildContext>? GetSubQueryContext;

			public GroupJoinSubQueryContext(IBuildContext subQuery)
				: base(subQuery)
			{
			}

			public override IBuildContext? GetContext(Expression? expression, int level, BuildInfo buildInfo)
			{
				if (expression == null)
				{
					if (buildInfo.CreateSubQuery)
					{
						Join!.IsWeak   = true;
						var queryBuild = GetSubQueryContext!();
						//queryBuild.Parent = Context.Parent;
						return queryBuild;
					}

					return this;
				}

				return base.GetContext(expression, level, buildInfo);
			}

			public override IsExpressionResult IsExpression(Expression? expression, int level, RequestFor requestFlag)
			{
				if (requestFlag == RequestFor.GroupJoin && expression == null)
					return IsExpressionResult.True;

				return base.IsExpression(expression, level, requestFlag);
			}
		}
	}
}
