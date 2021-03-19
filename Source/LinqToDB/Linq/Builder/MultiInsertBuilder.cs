using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using LinqToDB.Expressions;
using LinqToDB.SqlQuery;

namespace LinqToDB.Linq.Builder
{
	class MultiInsertBuilder : MethodCallBuilder
	{		
		#region MultiInsertBuilder

		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return methodCall.IsQueryable("InsertAllUnconditional", "InsertAll", "InsertFirst");
		}

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			// static int InsertAll<TSource>(IQueryable<TSource> source, List<(condition?, table, setter)> targets)
			// static int InsertFirst<TSource>(IQueryable<TSource> source, List<(condition?, table, setter)> targets)

			var insertType = methodCall.Method.Name switch
			{
				"InsertAllUnconditional" => MultiInsertType.Unconditional,
				"InsertAll" => MultiInsertType.All,
				"InsertFirst" => MultiInsertType.First,
				//	Can't happen because of CanBuildMethodCall
				_ => throw new InvalidOperationException("Unknown method " + methodCall.Method.Name),
			};

			var sourceContext = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));
			var source = new TableLikeQueryContext(sourceContext);

			var statement = new SqlMultiInsertStatement(insertType, source.Source);
			sourceContext.Statement = statement;

			var targets = (List<(LambdaExpression? condition, Expression table, Expression setter)>)((ConstantExpression)methodCall.Arguments[1]).Value;

			var dummySelect = new SelectQuery();

			foreach (var target in targets)
			{
				var into = builder.BuildSequence(new BuildInfo(buildInfo, target.table, dummySelect));
				var targetContext = new MultiInsertContext(statement, source, into);
				var setter = (LambdaExpression)target.setter;

				if (target.condition != null)
				{
					var when = statement.AddWhen();
					builder.BuildSearchCondition(
						new ExpressionContext(null, new[] { source }, target.condition),
						builder.ConvertExpression(target.condition.Body.Unwrap()),
						when.Conditions);
				}
				else if (insertType != MultiInsertType.Unconditional)
				{
					statement.AddElse();
				}

				var insert = statement.AddInsert();
				insert.Into = ((TableBuilder.TableContext)into).SqlTable;

				targetContext.AddSourceParameter(setter.Parameters[0]);

				UpdateBuilder.BuildSetterWithContext(
					builder,
					buildInfo,
					setter,
					into,
					(List<SqlSetExpression>)insert.Items,
					targetContext);

				if (insert.Items.Count == 0)
					insert.Items.AddRange(insert.DefaultItems);
			}
			
			return new MultiInsertContext(sourceContext);
		}

		protected override SequenceConvertInfo? Convert(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo, ParameterExpression? param)
			=> null;

		#endregion

		#region MultiInsertContext

		class MultiInsertContext : SequenceContextBase
		{
			private readonly ISet<Expression> _sourceParameters = new HashSet<Expression>();

			public void AddSourceParameter(Expression param) => _sourceParameters.Add(param);

			private IBuildContext _source;
			private IBuildContext? _target;

			public MultiInsertContext(IBuildContext source)
				: base(null, source, null)
			{ 
				_source = source;
			}

			public MultiInsertContext(SqlMultiInsertStatement insert, IBuildContext source, IBuildContext target)
				: base(null, new[] { target, source }, null)
			{ 
				_source = source;
				_target = target;
				Statement = insert;
			}

			public override void BuildQuery<T>(Query<T> query, ParameterExpression queryParameter)
				=> QueryRunner.SetNonQueryQuery(query);				

			public override Expression BuildExpression(Expression? expression, int level, bool enforceServerSide) 
				=> throw new NotImplementedException();

			public override SqlInfo[] ConvertToIndex(Expression? expression, int level, ConvertFlags flags)
				=> throw new NotImplementedException();

			public override SqlInfo[] ConvertToSql(Expression? expression, int level, ConvertFlags flags)
			{
				if (expression != null)
				{
					switch (flags)
					{
						case ConvertFlags.Field:
							{
								var root = Builder.GetRootObject(expression);

								if (root.NodeType == ExpressionType.Parameter)
								{
									if (_sourceParameters.Contains(root))
										return _source.ConvertToSql(expression, level, flags);

									return _target!.ConvertToSql(expression, level, flags);
								}

								if (root is ContextRefExpression contextRef)
								{
									return contextRef.BuildContext.ConvertToSql(expression, level, flags);
								}

								break;
							}
					}
				}

				throw new LinqException("'{0}' cannot be converted to SQL.", expression);
			}

			public override IsExpressionResult IsExpression(Expression? expression, int level, RequestFor requestFlag)
				=> _source.IsExpression(expression, level, requestFlag);

			public override IBuildContext GetContext(Expression? expression, int level, BuildInfo buildInfo)
				=> throw new NotImplementedException();
		}

		#endregion
	}
}
