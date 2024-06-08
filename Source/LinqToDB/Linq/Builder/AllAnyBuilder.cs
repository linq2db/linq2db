﻿using System;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using SqlQuery;

	internal sealed class AllAnyBuilder : MethodCallBuilder
	{
		static readonly string[] MethodNames      = { "All"     , "Any"      };
		static readonly string[] MethodNamesAsync = { "AllAsync", "AnyAsync" };

		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return
				methodCall.IsQueryable     (MethodNames     ) ||
				methodCall.IsAsyncExtension(MethodNamesAsync);
		}

		protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var sequenceBuildInfo = new BuildInfo(buildInfo, methodCall.Arguments[0])
			{
				/*CopyTable = true,*/
				CreateSubQuery = true,
				SourceCardinality = SourceCardinality.Unknown,
				SelectQuery = new SelectQuery()
			};

			var buildResult = builder.TryBuildSequence(sequenceBuildInfo);
			if (buildResult.BuildContext == null)
				return buildResult;

			var sequence = buildResult.BuildContext;

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

				sequence = builder.BuildWhere(buildInfo.Parent, sequence,
					condition : condition, checkForSubQuery : true, enforceHaving : false,
					isTest : buildInfo.IsTest);

				if (sequence == null)
					return BuildSequenceResult.Error(methodCall);

				sequence.SetAlias(condition.Parameters[0].Name);
			}

			// finalizing context
			_ = builder.MakeExpression(sequence, new ContextRefExpression(methodCall.Method.GetGenericArguments()[0], sequence),
				ProjectFlags.ExtractProjection);

			return BuildSequenceResult.FromContext(new AllAnyContext(buildInfo.Parent, buildInfo.SelectQuery, methodCall, sequence));
		}

		sealed class AllAnyContext : SequenceContextBase
		{
			readonly MethodCallExpression _methodCall;

			public AllAnyContext(IBuildContext? parent,     SelectQuery   selectQuery,
				MethodCallExpression            methodCall, IBuildContext sequence)
				: base(parent, sequence, null)
			{
				SelectQuery = selectQuery;
				_methodCall = methodCall;
			}

			SqlPlaceholderExpression? _innerSql;

			public override Expression MakeExpression(Expression path, ProjectFlags flags)
			{
				if (!SequenceHelper.IsSameContext(path, this))
					throw new InvalidOperationException();

				if (_innerSql != null)
					return _innerSql;

				var predicate = new SqlPredicate.FuncLike(SqlFunction.CreateExists(Sequence.SelectQuery)).MakeNot(_methodCall.Method.Name.StartsWith("All"));
				
				var innerSql = ExpressionBuilder.CreatePlaceholder(Parent?.SelectQuery ?? SelectQuery, new SqlSearchCondition(false, predicate), path, convertType: typeof(bool));

				if (flags.IsTest())
				{
					_innerSql = innerSql;
				}

				return innerSql;
			}

			public override SqlStatement GetResultStatement()
			{
				return new SqlSelectStatement(SelectQuery);
			}

			public override void SetRunQuery<T>(Query<T> query, Expression expr)
			{
				var mapper = Builder.BuildMapper<object>(SelectQuery, expr);

				QueryRunner.SetRunQuery(query, mapper);
			}

			public override IBuildContext Clone(CloningContext context)
			{
				return new AllAnyContext(null, context.CloneElement(SelectQuery), context.CloneExpression(_methodCall), context.CloneContext(Sequence));
			}
		}
	}
}
