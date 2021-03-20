using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using LinqToDB.Expressions;
using LinqToDB.SqlQuery;

namespace LinqToDB.Linq.Builder
{
	class MultiInsertBuilder : MethodCallBuilder
	{		
		#region MultiInsertBuilder

		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
			=> methodCall.Method.DeclaringType == typeof(MultiInsertExtensions);

		private static readonly Dictionary<MethodInfo, Func<ExpressionBuilder, MethodCallExpression, BuildInfo, IBuildContext>> methodBuilders = new() 
		{
			{ MultiInsertExtensions.MultiInsertMethodInfo,   BuildMultiInsert },
			{ MultiInsertExtensions.IntoMethodInfo,          BuildInto        },
			{ MultiInsertExtensions.WhenMethodInfo,          BuildWhen        },
			{ MultiInsertExtensions.ElseMethodInfo,          BuildElse        },
			{ MultiInsertExtensions.InsertMethodInfo,        BuildInsert      },
			{ MultiInsertExtensions.InsertAllMethodInfo,     BuildInsertAll   },
			{ MultiInsertExtensions.InsertFirstMethodInfo,   BuildInsertFirst },
		};

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{	
			var genericMethod = methodCall.Method.GetGenericMethodDefinition();
			return methodBuilders.TryGetValue(genericMethod, out var build)
				? build(builder, methodCall, buildInfo)
				: throw new InvalidOperationException("Unknown method " + methodCall.Method.Name);
		}

		private static IBuildContext BuildMultiInsert(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{ 
			// MultiInsert(IQueryable)			
			var sourceContext       = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));
			var source              = new TableLikeQueryContext(sourceContext);
			var statement           = new SqlMultiInsertStatement(source.Source);			
			sourceContext.Statement = statement;
			
			return source;
		}

		private static IBuildContext BuildTargetTable(
			ExpressionBuilder builder,
			BuildInfo         buildInfo,
			bool              isConditional,
			Expression        query,
			LambdaExpression? condition,
			Expression        table,
			LambdaExpression  setter)
		{
			var source        = (TableLikeQueryContext)builder.BuildSequence(new BuildInfo(buildInfo, query));
			var statement     = (SqlMultiInsertStatement)source.Context.Statement!;
			var into          = builder.BuildSequence(new BuildInfo(buildInfo, table, new SelectQuery()));
			var targetContext = new MultiInsertContext(statement, source, into);
			var when          = condition != null ? new SqlSearchCondition() : null;
			var insert        = new SqlInsertClause
			{
				Into          = ((TableBuilder.TableContext)into).SqlTable
			};

			statement.Add(when, insert);
			
			if (condition != null)
			{
				builder.BuildSearchCondition(
					new ExpressionContext(null, new[] { source }, condition),
					builder.ConvertExpression(condition.Body.Unwrap()),
					when!.Conditions);
			}

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

			return source;
		}

		private static IBuildContext BuildInto(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{ 
			// Into(IQueryable, ITable, Expression setter)
			return BuildTargetTable(
				builder, 
				buildInfo, 
				false,
				methodCall.Arguments[0],
				null,
				methodCall.Arguments[1],
				methodCall.Arguments[2].UnwrapLambda());
		}
		
		private static IBuildContext BuildWhen(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{ 
			// When(IQueryable, Expression condition, ITable, Expression setter)
			return BuildTargetTable(
				builder,
				buildInfo,
				true,
				methodCall.Arguments[0],
				methodCall.Arguments[1].UnwrapLambda(),
				methodCall.Arguments[2],
				methodCall.Arguments[3].UnwrapLambda());
		}
		
		private static IBuildContext BuildElse(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{ 
			// Else(IQueryable, ITable, Expression setter)
			return BuildTargetTable(
				builder,
				buildInfo,
				true,
				methodCall.Arguments[0],
				null,
				methodCall.Arguments[1],
				methodCall.Arguments[2].UnwrapLambda());
		}

		private static IBuildContext BuildInsert(ExpressionBuilder builder, BuildInfo buildInfo, MultiInsertType type, Expression query)
		{
			var source           = (TableLikeQueryContext)builder.BuildSequence(new BuildInfo(buildInfo, query));
			var statement        = (SqlMultiInsertStatement)source.Context.Statement!;
			
			statement.InsertType = type;
			
			return new MultiInsertContext(source.Context);
		}

		private static IBuildContext BuildInsert(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{ 
			// Insert(IQueryable)
			return BuildInsert(
				builder,
				buildInfo,
				MultiInsertType.Unconditional,
				methodCall.Arguments[0]);
		}

		private static IBuildContext BuildInsertAll(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{ 
			// InsertAll(IQueryable)
			return BuildInsert(
				builder,
				buildInfo,
				MultiInsertType.All,
				methodCall.Arguments[0]);
		}

		private static IBuildContext BuildInsertFirst(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{ 
			// InsertFirst(IQueryable)
			return BuildInsert(
				builder,
				buildInfo,
				MultiInsertType.First,
				methodCall.Arguments[0]);
		}		

		protected override SequenceConvertInfo? Convert(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo, ParameterExpression? param)
			=> null;

		#endregion

		#region MultiInsertContext

		class MultiInsertContext : SequenceContextBase
		{
			private readonly ISet<Expression> _sourceParameters = new HashSet<Expression>();

			public void AddSourceParameter(Expression param) => _sourceParameters.Add(param);

			private IBuildContext  _source;
			private IBuildContext? _target;

			public MultiInsertContext(IBuildContext source)
				: base(null, source, null)
			{ 
				_source = source;
			}

			public MultiInsertContext(SqlMultiInsertStatement insert, IBuildContext source, IBuildContext target)
				: base(null, new[] { target, source }, null)
			{ 
				_source   = source;
				_target   = target;
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
