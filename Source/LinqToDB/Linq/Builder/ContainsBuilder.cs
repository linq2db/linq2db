using System;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using SqlQuery;

	class ContainsBuilder : MethodCallBuilder
	{
		private static readonly string[] MethodNames      = { "Contains"      };
		private static readonly string[] MethodNamesAsync = { "ContainsAsync" };

		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return
				methodCall.IsQueryable     (MethodNames     ) && methodCall.Arguments.Count == 2 ||
				methodCall.IsAsyncExtension(MethodNamesAsync) && methodCall.Arguments.Count == 3;
		}

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var sequence         = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));
			var buildInStatement = false;

			sequence         = new SubQueryContext(sequence);
			buildInStatement = true;

			return new ContainsContext(buildInfo.Parent, methodCall, sequence, buildInStatement);
		}

		public static bool IsConstant(MethodCallExpression methodCall)
		{
			if (!methodCall.IsQueryable("Contains"))
				return false;

			return methodCall.IsQueryable(false) == false;
		}

		class ContainsContext : SequenceContextBase
		{
			readonly MethodCallExpression _methodCall;
			readonly bool                 _buildInStatement;

			public ContainsContext(IBuildContext? parent, MethodCallExpression methodCall, IBuildContext sequence, bool buildInStatement)
				: base(parent, sequence, null)
			{
				_methodCall       = methodCall;
				_buildInStatement = buildInStatement;
			}

			public override void BuildQuery<T>(Query<T> query, ParameterExpression queryParameter)
			{
				throw new NotImplementedException();

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
				throw new NotImplementedException();
			}

			public override SqlInfo[] ConvertToSql(Expression? expression, int level, ConvertFlags flags)
			{
				throw new NotImplementedException();
			}

			public override SqlInfo[] ConvertToIndex(Expression? expression, int level, ConvertFlags flags)
			{
				throw new NotImplementedException();
			}

			public override IsExpressionResult IsExpression(Expression? expression, int level, RequestFor requestFlag)
			{
				throw new NotImplementedException();
			}

			public override IBuildContext GetContext(Expression? expression, int level, BuildInfo buildInfo)
			{
				return this;
			}

			ISqlExpression? _subQuerySql;

			public override ISqlExpression GetSubQuery(IBuildContext? context)
			{
				if (_subQuerySql == null)
				{
					var args      = _methodCall.Method.GetGenericArguments();
					var param     = Expression.Parameter(args[0], "param");
					var expr      = _methodCall.Arguments[1];
					var condition = Expression.Lambda(ExpressionBuilder.Equal(Builder.MappingSchema, param, expr), param);

					IBuildContext ctx = new ExpressionContext(Parent, Sequence, condition);

					ctx = Builder.GetContext(ctx, expr) ?? ctx;

					Builder.ReplaceParent(ctx, this);

					SqlCondition cond;

					if ((Sequence.SelectQuery != SelectQuery || _buildInStatement) &&
						(ctx.IsExpression(expr, 0, RequestFor.Field).     Result ||
						 ctx.IsExpression(expr, 0, RequestFor.Expression).Result))
					{
						Sequence.ConvertToIndex(null, 0, ConvertFlags.All);
						var ex = Builder.ConvertToSql(ctx, _methodCall.Arguments[1]);
						cond = new SqlCondition(false, new SqlPredicate.InSubQuery(ex, false, SelectQuery));
					}
					else
					{
						var sequence = Builder.BuildWhere(Parent, Sequence, condition, true, false, false, forJoin: false);
						cond = new SqlCondition(false, new SqlPredicate.FuncLike(SqlFunction.CreateExists(sequence.SelectQuery)));
					}

					_subQuerySql = new SqlSearchCondition(cond);
				}

				return _subQuerySql;
			}

			private SqlPlaceholderExpression? _placeholder;

			public override Expression MakeExpression(Expression path, ProjectFlags flags)
			{
				if (!(flags.HasFlag(ProjectFlags.SQL) || flags.HasFlag(ProjectFlags.Expression)))
					return base.MakeExpression(path, flags);

				if (_placeholder != null)
					return _placeholder;

				_placeholder = CreatePlaceholder(ProjectFlags.SQL);

				return _placeholder;
			}

			public override IBuildContext Clone(CloningContext context)
			{
				var result = new ContainsContext(null, _methodCall, context.CloneContext(Sequence), _buildInStatement);
				if (_placeholder != null)
					result._placeholder = context.CloneExpression(_placeholder);
				return result;
			}

			private SqlPlaceholderExpression CreatePlaceholder(ProjectFlags flags)
			{
				var args  = _methodCall.Method.GetGenericArguments();
				var param = Expression.Parameter(args[0], "param");
				var expr  = _methodCall.Arguments[1];

				var subQueryCtx = (SubQueryContext)Sequence;

				var testPlaceholder = Builder.TryConvertToSqlPlaceholder(Parent, expr, flags) as SqlPlaceholderExpression;

				var contextRef = new ContextRefExpression(args[0], subQueryCtx.SubQuery);
				var sequencePlaceholder = Builder.TryConvertToSqlPlaceholder(subQueryCtx.SubQuery, contextRef, flags) as SqlPlaceholderExpression;

				SqlCondition cond;

				if ((Sequence.SelectQuery != SelectQuery || _buildInStatement) && testPlaceholder != null && sequencePlaceholder != null)
				{
					_ = Builder.ToColumns(Sequence, sequencePlaceholder);
					cond = new SqlCondition(false, new SqlPredicate.InSubQuery(testPlaceholder.Sql, false, SelectQuery));
				}
				else
				{
					var condition = Expression.Lambda(ExpressionBuilder.Equal(Builder.MappingSchema, param, expr), param);
					var sequence  = Builder.BuildWhere(Parent, Sequence, condition, checkForSubQuery: true, enforceHaving: false, isTest: false, forJoin: false);
					cond = new SqlCondition(false, new SqlPredicate.FuncLike(SqlFunction.CreateExists(sequence.SelectQuery)));
				}

				var subQuerySql = new SqlSearchCondition(cond);

				return ExpressionBuilder.CreatePlaceholder(Parent, subQuerySql, _methodCall);
			}
		}
	}
}
