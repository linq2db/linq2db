using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB.Expressions;
using LinqToDB.SqlQuery;

namespace LinqToDB.Linq.Builder
{
	[BuildsMethodCall(
		nameof(LinqExtensions.Delete),
		nameof(LinqExtensions.DeleteWithOutput),
		nameof(LinqExtensions.DeleteWithOutputInto))]
	sealed class DeleteBuilder : MethodCallBuilder
	{
		public static bool CanBuildMethod(MethodCallExpression call, BuildInfo info, ExpressionBuilder builder)
			=> call.IsQueryable();

		protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
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
					condition : (LambdaExpression)methodCall.Arguments[1].Unwrap(), checkForSubQuery : false,
					enforceHaving : false, out var error);

				if (sequence == null)
					return BuildSequenceResult.Error(error ?? methodCall);
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

				deletedContext = new AnchorContext(null,
					new TableBuilder.TableContext(sequence.TranslationModifier, builder, sequence.MappingSchema, outputSelectQuery, deletedTable, false),
					SqlAnchor.AnchorKindEnum.Deleted);

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

					UpdateBuilder.InitializeSetExpressions(builder, destinationContext, sequence, outputExpressions, deleteStatement.Output.OutputItems, createColumns : false);

					deleteStatement.Output.OutputTable = destinationContext.SqlTable;
				}
			}

			return BuildSequenceResult.FromContext(new DeleteContext(sequence, deleteType, outputExpression, deleteStatement, deletedContext));
		}

		sealed class DeleteContext : PassThroughContext
		{

			public enum DeleteTypeEnum
			{
				Delete,
				DeleteOutput,
				DeleteOutputInto,
			}

			public IBuildContext QuerySequence => Context;

			public DeleteTypeEnum     DeleteType       { get; }
			public IBuildContext?     DeletedContext   { get; }
			public LambdaExpression?  OutputExpression { get; }
			public SqlDeleteStatement DeleteStatement  { get; }

			public DeleteContext(IBuildContext querySequence, DeleteTypeEnum deleteType,
				LambdaExpression? outputExpression, SqlDeleteStatement deleteStatement, IBuildContext? deletedContext)
				: base(querySequence, querySequence.SelectQuery)
			{
				DeleteType       = deleteType;
				OutputExpression = outputExpression;
				DeleteStatement  = deleteStatement;
				DeletedContext   = deletedContext;
			}

			public override IBuildContext Clone(CloningContext context)
			{
				return new DeleteContext(
					context.CloneContext(QuerySequence),
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
						var mapper = Builder.BuildMapper<T>(SelectQuery, expr);
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

						var sqlExpr = Builder.BuildSqlExpression(selectContext, outputRef);
						sqlExpr = SequenceHelper.CorrectSelectQuery(sqlExpr, outputSelectQuery);

						if (sqlExpr is SqlPlaceholderExpression)
							outputExpressions.Add(new UpdateBuilder.SetExpressionEnvelope(sqlExpr, sqlExpr, false));
						else
							UpdateBuilder.ParseSetter(Builder, outputRef, sqlExpr, outputExpressions);

						var setItems = new List<SqlSetExpression>();
						UpdateBuilder.InitializeSetExpressions(Builder, selectContext, selectContext, outputExpressions, setItems, createColumns : false);

						DeleteStatement.Output!.OutputColumns = setItems.Select(c => c.Column).ToList();

						return sqlExpr;
					}

					return Expression.Default(path.Type);
				}

				return base.MakeExpression(path, flags);
			}

			public override SqlStatement GetResultStatement()
			{
				return DeleteStatement;
			}
		}

	}
}
