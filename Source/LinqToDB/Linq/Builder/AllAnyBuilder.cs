﻿using System;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using SqlQuery;

	class AllAnyBuilder : MethodCallBuilder
	{
		private static readonly string[] MethodNames      = { "All"     , "Any"      };
		private static readonly string[] MethodNamesAsync = { "AllAsync", "AnyAsync" };

		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return
				methodCall.IsQueryable     (MethodNames     ) ||
				methodCall.IsAsyncExtension(MethodNamesAsync);
		}

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var sequence = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]) { CopyTable = true, CreateSubQuery = true });

			var isAsync = methodCall.Method.DeclaringType == typeof(AsyncExtensions);

			if (methodCall.Arguments.Count == (isAsync ? 3 : 2))
			{
				if (sequence.SelectQuery.Select.TakeValue != null ||
				    sequence.SelectQuery.Select.SkipValue != null)
				{
					sequence = new SubQueryContext(sequence);
				}

				var condition = (LambdaExpression)methodCall.Arguments[1].Unwrap();

				if (methodCall.Method.Name.StartsWith("All"))
					condition = Expression.Lambda(Expression.Not(condition.Body), condition.Name, condition.Parameters);

				sequence = builder.BuildWhere(buildInfo.Parent, sequence, condition, true);
				sequence.SetAlias(condition.Parameters[0].Name);
			}

			return new AllAnyContext(buildInfo.Parent, methodCall, sequence);
		}

		protected override SequenceConvertInfo? Convert(
			ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo, ParameterExpression? param)
		{
			var isAsync = methodCall.Method.DeclaringType == typeof(AsyncExtensions);

			if (methodCall.Arguments.Count == (isAsync ? 3 : 2))
			{
				var predicate = (LambdaExpression)methodCall.Arguments[1].Unwrap();
				var info      = builder.ConvertSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]), predicate.Parameters[0], true);

				if (info != null)
				{
					info.Expression = methodCall.Transform(
						(methodCall, info, predicate),
						static (context, ex) => ConvertMethod(context.methodCall, 0, context.info, context.predicate.Parameters[0], ex));
					info.Parameter  = param;

					return info;
				}
			}
			else
			{
				var info = builder.ConvertSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]), null, true);

				if (info != null)
				{
					info.Expression = methodCall.Transform(
						(methodCall, info),
						static (context, ex) => ConvertMethod(context.methodCall, 0, context.info, null, ex));
					info.Parameter  = param;

					return info;
				}
			}

			return null;
		}

		class AllAnyContext : SequenceContextBase
		{
			readonly MethodCallExpression _methodCall;

			public AllAnyContext(IBuildContext? parent, MethodCallExpression methodCall, IBuildContext sequence)
				: base(parent, sequence, null)
			{
				_methodCall = methodCall;
			}

			public override void BuildQuery<T>(Query<T> query, ParameterExpression queryParameter)
			{
				var sql = GetSubQuery(null);

				var sq = new SqlSelectStatement();
				sq.SelectQuery.Select.Add(sql);

				query.Queries[0].Statement = sq;

				var expr   = Builder.BuildSql(typeof(bool), 0, sql);
				var mapper = Builder.BuildMapper<object>(expr);

				CompleteColumns();
				QueryRunner.SetRunQuery(query, mapper);
			}

			public override Expression BuildExpression(Expression? expression, int level, bool enforceServerSide)
			{
				var info  = ConvertToIndex(expression, level, ConvertFlags.Field)[0];
				var index = info.Index;
				if (Parent != null)
					index = ConvertToParentIndex(index, Parent);
				return Builder.BuildSql(typeof(bool), index, info.Sql);
			}

			public override SqlInfo[] ConvertToSql(Expression? expression, int level, ConvertFlags flags)
			{
				if (expression == null)
				{
					var sql   = GetSubQuery(null);
					var query = SelectQuery;

					if (Parent != null)
						query = Parent.SelectQuery;

					return new[] { new SqlInfo(sql, query) };
				}

				throw new NotImplementedException();
			}

			public override SqlInfo[] ConvertToIndex(Expression? expression, int level, ConvertFlags flags)
			{
				var sql = ConvertToSql(expression, level, flags);

				if (sql[0].Index < 0)
					sql[0] = sql[0].WithIndex(sql[0].Query!.Select.Add(sql[0].Sql));

				return sql;
			}

			public override IsExpressionResult IsExpression(Expression? expression, int level, RequestFor requestFlag)
			{
				if (expression == null)
				{
					switch (requestFlag)
					{
						case RequestFor.Expression :
						case RequestFor.Field      : return IsExpressionResult.False;
					}
				}

				return IsExpressionResult.False;
			}

			public override IBuildContext GetContext(Expression? expression, int level, BuildInfo buildInfo)
			{
				throw new NotImplementedException();
			}

			ISqlExpression? _subQuerySql;

			public override ISqlExpression GetSubQuery(IBuildContext? context)
			{
				if (_subQuerySql == null)
				{
					var cond = new SqlCondition(
						_methodCall.Method.Name.StartsWith("All"),
						new SqlPredicate.FuncLike(SqlFunction.CreateExists(SelectQuery)));

					Sequence.CompleteColumns();

					_subQuerySql = new SqlSearchCondition(cond);
				}

				return _subQuerySql;
			}
		}
	}
}
