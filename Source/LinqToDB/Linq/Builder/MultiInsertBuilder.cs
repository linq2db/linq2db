using System;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Linq.Builder
{
	using SqlQuery;
	using LinqToDB.Expressions;

	using Methods = Reflection.Methods.LinqToDB.MultiInsert;

	sealed class MultiInsertBuilder : MethodCallBuilder
	{
		#region MultiInsertBuilder

		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
			=> methodCall.Method.DeclaringType == typeof(MultiInsertExtensions);

		static readonly Dictionary<MethodInfo, Func<ExpressionBuilder, MethodCallExpression, BuildInfo, IBuildContext>> _methodBuilders = new()
		{
			{ Methods.Begin,       BuildMultiInsert },
			{ Methods.Into,        BuildInto        },
			{ Methods.When,        BuildWhen        },
			{ Methods.Else,        BuildElse        },
			{ Methods.Insert,      BuildInsert      },
			{ Methods.InsertAll,   BuildInsertAll   },
			{ Methods.InsertFirst, BuildInsertFirst },
		};

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var genericMethod = methodCall.Method.GetGenericMethodDefinition();
			return _methodBuilders.TryGetValue(genericMethod, out var build)
				? build(builder, methodCall, buildInfo)
				: throw new InvalidOperationException("Unknown method " + methodCall.Method.Name);
		}

		static IBuildContext BuildMultiInsert(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			throw new NotImplementedException();

			// MultiInsert(IQueryable)
			//
			var sourceContext = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

			var sourceContextRef = new ContextRefExpression(methodCall.Method.GetGenericArguments()[0], sourceContext);
			
			var source    = new TableLikeQueryContext(sourceContextRef, sourceContextRef);
			var statement = new SqlMultiInsertStatement(source.Source);

			sourceContext.Statement = statement;
			return source;
		}

		static IBuildContext BuildTargetTable(
			ExpressionBuilder builder,
			BuildInfo         buildInfo,
			bool              isConditional,
			Expression        query,
			LambdaExpression? condition,
			Expression        table,
			LambdaExpression  setterLambda)
		{
			var source        = (TableLikeQueryContext)builder.BuildSequence(new BuildInfo(buildInfo, query));
			var statement     = (SqlMultiInsertStatement)source.InnerQueryContext.Statement!;
			var into          = builder.BuildSequence(new BuildInfo(buildInfo, table, new SelectQuery()));

			var intoTable = SequenceHelper.GetTableContext(into) ?? throw new LinqToDBException($"Cannot get table context from {source.GetType()}");

			var targetContext = new MultiInsertContext(statement, source, into);
			var when          = condition != null ? new SqlSearchCondition() : null;
			var insert        = new SqlInsertClause
			{
				Into          = intoTable.SqlTable
			};

			statement.Add(when, insert);

			if (condition != null)
			{
				builder.BuildSearchCondition(
					new ExpressionContext(null, new[] { source }, condition),
					builder.ConvertExpression(condition.Body.Unwrap()), ProjectFlags.SQL,
					when!.Conditions);
			}

			targetContext.AddSourceParameter(setterLambda.Parameters[0]);

			var setterExpression = SequenceHelper.PrepareBody(setterLambda, source);
			
			var targetRef        = new ContextRefExpression(setterExpression.Type, targetContext);

			var setterExpressions = new List<UpdateBuilder.SetExpressionEnvelope>();
			UpdateBuilder.ParseSetter(builder, targetRef, setterExpression, setterExpressions);
			UpdateBuilder.InitializeSetExpressions(builder, into, source, setterExpressions, insert.Items, false);

			return source;
		}

		static IBuildContext BuildInto(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
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

		static IBuildContext BuildWhen(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
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

		static IBuildContext BuildElse(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
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

		static IBuildContext BuildInsert(ExpressionBuilder builder, BuildInfo buildInfo, MultiInsertType type, Expression query)
		{
			var source           = (TableLikeQueryContext)builder.BuildSequence(new BuildInfo(buildInfo, query));
			var statement        = (SqlMultiInsertStatement)source.InnerQueryContext.Statement!;

			statement.InsertType = type;

			return new MultiInsertContext(source.InnerQueryContext);
		}

		static IBuildContext BuildInsert(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			// Insert(IQueryable)
			return BuildInsert(
				builder,
				buildInfo,
				MultiInsertType.Unconditional,
				methodCall.Arguments[0]);
		}

		static IBuildContext BuildInsertAll(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			// InsertAll(IQueryable)
			return BuildInsert(
				builder,
				buildInfo,
				MultiInsertType.All,
				methodCall.Arguments[0]);
		}

		static IBuildContext BuildInsertFirst(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			// InsertFirst(IQueryable)
			return BuildInsert(
				builder,
				buildInfo,
				MultiInsertType.First,
				methodCall.Arguments[0]);
		}

		#endregion

		#region MultiInsertContext

		sealed class MultiInsertContext : SequenceContextBase
		{
			readonly ISet<Expression> _sourceParameters = new HashSet<Expression>();

			public void AddSourceParameter(Expression param) => _sourceParameters.Add(param);

			IBuildContext  _source;
			IBuildContext? _target;

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

			public override IBuildContext Clone(CloningContext context)
			{
				throw new NotImplementedException();
			}

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
