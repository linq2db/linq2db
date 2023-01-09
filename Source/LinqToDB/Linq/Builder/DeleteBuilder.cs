using System;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using SqlQuery;

	sealed class DeleteBuilder : MethodCallBuilder
	{
		static readonly string[] MethodNames =
		{
			nameof(LinqExtensions.Delete),
			nameof(LinqExtensions.DeleteWithOutput),
			nameof(LinqExtensions.DeleteWithOutputInto)
		};

		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return methodCall.IsQueryable(MethodNames);
		}

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var deleteType = methodCall.Method.Name switch
			{
				nameof(LinqExtensions.DeleteWithOutput)     => DeleteContext.DeleteTypeEnum.DeleteOutput,
				nameof(LinqExtensions.DeleteWithOutputInto) => DeleteContext.DeleteTypeEnum.DeleteOutputInto,
				_                                           => DeleteContext.DeleteTypeEnum.Delete,
			};

			var sequenceArgument = methodCall.Arguments[0];
			var sequence         = builder.BuildSequence(new BuildInfo(buildInfo, sequenceArgument));

			if (methodCall.Arguments.Count == 2 && deleteType == DeleteContext.DeleteTypeEnum.Delete)
			{
				sequence = builder.BuildWhere(buildInfo.Parent, sequence,
					condition: (LambdaExpression)methodCall.Arguments[1].Unwrap(), checkForSubQuery: false,
					enforceHaving: false, isTest: buildInfo.AggregationTest,
					disableCache: false);
			}

			var deleteStatement = new SqlDeleteStatement(sequence.SelectQuery);

			var tableContext = SequenceHelper.GetTableContext(sequence);
			if (tableContext == null)
				throw new InvalidOperationException("Cannot find target table for DELETE statement");

			deleteStatement.Table = tableContext.SqlTable;

			static LambdaExpression BuildDefaultOutputExpression(Type outputType)
			{
				var param = Expression.Parameter(outputType);
				return Expression.Lambda(param, param);
			}

			LambdaExpression? outputExpression = null;
			IBuildContext?    deletedContext   = null;

			if (deleteType != DeleteContext.DeleteTypeEnum.Delete)
			{
				outputExpression =
					(LambdaExpression?)methodCall.GetArgumentByName("outputExpression")?.Unwrap()
					?? BuildDefaultOutputExpression(methodCall.Method.GetGenericArguments().Last());

				deleteStatement.Output = new SqlOutputClause();

				var deletedTable = deleteStatement.Table;

				// create separate query for output
				var outputSelectQuery = new SelectQuery();

				deletedContext = new TableBuilder.TableContext(builder, outputSelectQuery, deletedTable);

				if (builder.DataContext.SqlProviderFlags.OutputDeleteUseSpecialTable)
				{
					deletedContext = new AnchorContext(null,
						new TableBuilder.TableContext(builder, outputSelectQuery, deletedTable),
						SqlAnchor.AnchorKindEnum.Deleted);

					deleteStatement.Output.DeletedTable = deletedTable;
				}

				if (deleteType == DeleteContext.DeleteTypeEnum.DeleteOutputInto)
				{
					var outputTable = methodCall.GetArgumentByName("outputTable")!;

					var destinationSequence = builder.BuildSequence(new BuildInfo(buildInfo, outputTable, new SelectQuery()));
					var destinationContext = SequenceHelper.GetTableContext(destinationSequence);
					if (destinationContext == null)
						throw new InvalidOperationException();

					var destinationRef = new ContextRefExpression(destinationContext.ObjectType, destinationContext);

					var outputBody = SequenceHelper.PrepareBody(outputExpression, deletedContext);

					var outputExpressions = new List<UpdateBuilder.SetExpressionEnvelope>();
					UpdateBuilder.ParseSetter(builder, destinationRef, outputBody, outputExpressions);

					UpdateBuilder.InitializeSetExpressions(builder, destinationContext, sequence, outputExpressions, deleteStatement.Output.OutputItems, false);

					deleteStatement.Output.OutputTable = destinationContext.SqlTable;
				}
			}

			return new DeleteContext(buildInfo.Parent, sequence, deleteType, outputExpression, deleteStatement, deletedContext);
		}

		sealed class DeleteContext : SequenceContextBase
		{

			public enum DeleteTypeEnum
			{
				Delete,
				DeleteOutput,
				DeleteOutputInto,
			}

			public IBuildContext QuerySequence { get => Sequences[0]; set => Sequences[0] = value; }

			public DeleteTypeEnum     DeleteType       { get; }
			public IBuildContext?     DeletedContext   { get; }
			public LambdaExpression?  OutputExpression { get; }
			public SqlDeleteStatement DeleteStatement  { get; }

			public DeleteContext(IBuildContext? parent, IBuildContext sequence, DeleteTypeEnum deleteType,
				LambdaExpression? outputExpression, SqlDeleteStatement deleteStatement, IBuildContext? deletedContext)
				: base(parent, sequence, null)
			{
				DeleteType       = deleteType;
				OutputExpression = outputExpression;
				DeleteStatement  = deleteStatement;
				DeletedContext   = deletedContext;
			}

			public override IBuildContext Clone(CloningContext context)
			{
				return new DeleteContext(null, 
					context.CloneContext(Sequence), 
					DeleteType,
					context.CloneExpression(OutputExpression), 
					context.CloneElement(DeleteStatement),
					context.CloneContext(DeletedContext));
			}

			public override void SetRunQuery<T>(Query<T> query, Expression expr)
			{
				switch (DeleteType)
				{
					case DeleteTypeEnum.Delete:
					{
						QueryRunner.SetNonQueryQuery(query);
						break;
					}
					case DeleteTypeEnum.DeleteOutput:
					{
						var mapper = Builder.BuildMapper<T>(expr);
						QueryRunner.SetRunQuery(query, mapper);
						break;
					}					
					case DeleteTypeEnum.DeleteOutputInto:
					{
						QueryRunner.SetNonQueryQuery(query);
						break;
					}	
					default:
						throw new InvalidOperationException($"Unexpected delete type: {DeleteType}");
				}
			}

			public override void BuildQuery<T>(Query<T> query, ParameterExpression queryParameter)
			{
				QueryRunner.SetNonQueryQuery(query);
			}

			public override Expression MakeExpression(Expression path, ProjectFlags flags)
			{
				if (SequenceHelper.IsSameContext(path, this) && flags.HasFlag(ProjectFlags.Expression))
				{
					if (DeleteType == DeleteTypeEnum.DeleteOutput)
					{
						if (DeletedContext == null || OutputExpression == null)
							throw new InvalidOperationException();

						DeleteStatement.Output ??= new SqlOutputClause();

						var outputSelectQuery = new SelectQuery();

						var outputBody = SequenceHelper.PrepareBody(OutputExpression, DeletedContext);

						var selectContext     = new SelectContext(Parent, outputBody, QuerySequence, false);
						var outputRef         = new ContextRefExpression(path.Type, selectContext);
						var outputExpressions = new List<UpdateBuilder.SetExpressionEnvelope>();

						var sqlExpr = Builder.ConvertToSqlExpr(selectContext, outputRef);
						sqlExpr = SequenceHelper.CorrectSelectQuery(sqlExpr, outputSelectQuery);

						if (sqlExpr is SqlPlaceholderExpression)
							outputExpressions.Add(new UpdateBuilder.SetExpressionEnvelope(sqlExpr, sqlExpr));
						else
							UpdateBuilder.ParseSetter(Builder, outputRef, sqlExpr, outputExpressions);

						var setItems = new List<SqlSetExpression>();
						UpdateBuilder.InitializeSetExpressions(Builder, selectContext, selectContext, outputExpressions, setItems, false);

						DeleteStatement.Output!.OutputColumns = setItems.Select(c => c.Column).ToList();

						return sqlExpr;
					}

					return Expression.Default(path.Type);
				}

				return base.MakeExpression(path, flags);
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
				throw new NotImplementedException();
			}

			public override SqlStatement GetResultStatement()
			{
				return DeleteStatement;
			}
		}

	}
}
