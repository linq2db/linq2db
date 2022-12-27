using System;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using SqlQuery;

	sealed class ContainsBuilder : MethodCallBuilder
	{
		static readonly string[] MethodNames      = { "Contains"      };
		static readonly string[] MethodNamesAsync = { "ContainsAsync" };

		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return
				methodCall.IsQueryable     (MethodNames     ) && methodCall.Arguments.Count == 2 ||
				methodCall.IsAsyncExtension(MethodNamesAsync) && methodCall.Arguments.Count == 3;
		}

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var innerQuery = new SelectQuery();
			var sequence   = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0], innerQuery));

			var buildInStatement = false;

			sequence         = new SubQueryContext(sequence);
			buildInStatement = true;

			return new ContainsContext(buildInfo.Parent, methodCall, buildInfo.SelectQuery, sequence, buildInStatement);
		}

		public static bool IsConstant(MethodCallExpression methodCall)
		{
			if (!methodCall.IsQueryable("Contains"))
				return false;

			return methodCall.IsQueryable(false) == false;
		}

		sealed class ContainsContext : IBuildContext
		{
#if DEBUG
			public string SqlQueryText => SelectQuery.SqlText;
			public string Path         => this.GetPath();
			public int    ContextId    { get; }
#endif
			public Expression Expression => _methodCall;

			public SelectQuery SelectQuery
			{
				get => OuterQuery;
				set { }
			}

			public SqlStatement?  Statement { get; set; }
			public IBuildContext? Parent    { get; set; }

			public   SelectQuery          OuterQuery    { get; }
			public   IBuildContext        InnerSequence { get; }
			public   ExpressionBuilder    Builder       => InnerSequence.Builder;

			readonly MethodCallExpression _methodCall;
			readonly bool                 _buildInStatement;

			public ContainsContext(IBuildContext? parent, MethodCallExpression methodCall, SelectQuery outerQuery, IBuildContext innerSequence, bool buildInStatement)
			{
				Parent            = parent;
				OuterQuery        = outerQuery;
				_methodCall       = methodCall;
				_buildInStatement = buildInStatement;
				InnerSequence     = innerSequence;
			}


			public void BuildQuery<T>(Query<T> query, ParameterExpression queryParameter)
			{
				throw new NotImplementedException();
			}

			public Expression BuildExpression(Expression? expression, int level, bool enforceServerSide)
			{
				throw new NotImplementedException();
			}

			public SqlInfo[] ConvertToSql(Expression? expression, int level, ConvertFlags flags)
			{
				throw new NotImplementedException();
			}

			public SqlInfo[] ConvertToIndex(Expression? expression, int level, ConvertFlags flags)
			{
				throw new NotImplementedException();
			}

			public IsExpressionResult IsExpression(Expression? expression, int level, RequestFor requestFlag)
			{
				throw new NotImplementedException();
			}

			public IBuildContext GetContext(Expression? expression, int level, BuildInfo buildInfo)
			{
				return this;
			}

			public int  ConvertToParentIndex(int index, IBuildContext context)
			{
				throw new NotImplementedException();
			}

			public void SetAlias(string?         alias)
			{
				throw new NotImplementedException();
			}

			public ISqlExpression GetSubQuery(IBuildContext? context)
			{
				throw new NotImplementedException();
			}

			public SqlStatement GetResultStatement()
			{
				return new SqlSelectStatement(OuterQuery);
			}

			public void CompleteColumns()
			{
			}

			SqlPlaceholderExpression? _cachedPlaceholder;

			public Expression MakeExpression(Expression path, ProjectFlags flags)
			{
				if (_cachedPlaceholder != null)
					return _cachedPlaceholder;

				var placeholder = CreatePlaceholder(flags.SqlFlag());

				if (!flags.IsTest())
					_cachedPlaceholder = placeholder;

				return placeholder;
			}

			public IBuildContext Clone(CloningContext context)
			{
				var result = new ContainsContext(null, _methodCall, context.CloneElement(OuterQuery), context.CloneContext(InnerSequence), _buildInStatement);
				if (_cachedPlaceholder != null)
					result._cachedPlaceholder = context.CloneExpression(_cachedPlaceholder);
				return result;
			}

			SqlPlaceholderExpression CreatePlaceholder(ProjectFlags flags)
			{
				var args  = _methodCall.Method.GetGenericArguments();
				var param = Expression.Parameter(args[0], "param");
				var expr  = _methodCall.Arguments[1];

				var testPlaceholder = Builder.TryConvertToSqlPlaceholder(InnerSequence, expr, flags);

				var contextRef          = new ContextRefExpression(args[0], InnerSequence);
				var sequencePlaceholder = Builder.TryConvertToSqlPlaceholder(InnerSequence, contextRef, flags);

				SqlCondition cond;

				if (_buildInStatement && testPlaceholder != null && sequencePlaceholder != null)
				{
					if (!flags.IsTest())
						_ = Builder.ToColumns(InnerSequence, sequencePlaceholder);
					cond = new SqlCondition(false, new SqlPredicate.InSubQuery(testPlaceholder.Sql, false, InnerSequence.SelectQuery));
				}
				else
				{
					var condition = Expression.Lambda(ExpressionBuilder.Equal(Builder.MappingSchema, param, expr), param);
					var sequence  = Builder.BuildWhere(Parent, InnerSequence, condition, checkForSubQuery: true, enforceHaving: false, isTest: flags.IsTest());
					cond = new SqlCondition(false, new SqlPredicate.FuncLike(SqlFunction.CreateExists(sequence.SelectQuery)));
				}

				var subQuerySql = new SqlSearchCondition(cond);

				return ExpressionBuilder.CreatePlaceholder(OuterQuery, subQuerySql, _methodCall);
			}

			public void SetRunQuery<T>(Query<T> query, Expression expr)
			{
				var mapper = Builder.BuildMapper<object>(expr);
				QueryRunner.SetRunQuery(query, mapper);
			}
		}
	}
}
