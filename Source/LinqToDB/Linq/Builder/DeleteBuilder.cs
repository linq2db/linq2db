using System;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using SqlQuery;

	class DeleteBuilder : MethodCallBuilder
	{
		private static readonly string[] MethodNames =
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
				nameof(LinqExtensions.DeleteWithOutput)     => DeleteContext.DeleteType.DeleteOutput,
				nameof(LinqExtensions.DeleteWithOutputInto) => DeleteContext.DeleteType.DeleteOutputInto,
				_                                           => DeleteContext.DeleteType.Delete,
			};

			var sequenceArgument = methodCall.Arguments[0];
			var sequence         = builder.BuildSequence(new BuildInfo(buildInfo, sequenceArgument));

			if (methodCall.Arguments.Count == 2 && deleteType == DeleteContext.DeleteType.Delete)
				sequence = builder.BuildWhere(buildInfo.Parent, sequence, (LambdaExpression)methodCall.Arguments[1].Unwrap(), false, false, buildInfo.AggregationTest);

			var deleteStatement = new SqlDeleteStatement(sequence.SelectQuery);

			sequence.Statement = deleteStatement;

			var root = builder.MakeExpression(sequence, new ContextRefExpression(sequenceArgument.Type, sequence),
				ProjectFlags.Root);

			if (root is ContextRefExpression rootRef && rootRef.BuildContext is TableBuilder.TableContext tableContext)
				deleteStatement.Table = tableContext.SqlTable;

			static LambdaExpression BuildDefaultOutputExpression(Type outputType)
			{
				var param = Expression.Parameter(outputType);
				return Expression.Lambda(param, param);
			}

			IBuildContext? outputContext = null;
			LambdaExpression? outputExpression = null;

			if (deleteType != DeleteContext.DeleteType.Delete)
			{
				outputExpression =
					(LambdaExpression?)methodCall.GetArgumentByName("outputExpression")?.Unwrap()
					?? BuildDefaultOutputExpression(methodCall.Method.GetGenericArguments().Last());

				deleteStatement.Output = new SqlOutputClause();

				var deletedTable = builder.DataContext.SqlProviderFlags.OutputDeleteUseSpecialTable ? SqlTable.Deleted(methodCall.Method.GetGenericArguments()[0]) : deleteStatement.GetDeleteTable();

				if (deletedTable == null)
					ThrowHelper.ThrowInvalidOperationException("Cannot find target table for DELETE statement");

				outputContext = new TableBuilder.TableContext(builder, new SelectQuery(), deletedTable);

				if (builder.DataContext.SqlProviderFlags.OutputDeleteUseSpecialTable)
					deleteStatement.Output.DeletedTable = deletedTable;

				if (deleteType == DeleteContext.DeleteType.DeleteOutputInto)
				{
					var outputTable = methodCall.GetArgumentByName("outputTable")!;
					var destination = builder.BuildSequence(new BuildInfo(buildInfo, outputTable, new SelectQuery()));

					UpdateBuilder.BuildSetter(
						builder,
						buildInfo,
						outputExpression,
						destination,
						deleteStatement.Output.OutputItems,
						outputContext);

					deleteStatement.Output.OutputTable = ((TableBuilder.TableContext)destination).SqlTable;
				}
			}

			if (deleteType == DeleteContext.DeleteType.DeleteOutput)
				return new DeleteWithOutputContext(buildInfo.Parent, sequence, outputContext!, outputExpression!);

			return new DeleteContext(buildInfo.Parent, sequence);
		}

		class DeleteContext : SequenceContextBase
		{
			public enum DeleteType
			{
				Delete,
				DeleteOutput,
				DeleteOutputInto,
			}

			public DeleteContext(IBuildContext? parent, IBuildContext sequence)
				: base(parent, sequence, null)
			{
			}

			public override IBuildContext Clone(CloningContext context)
			{
				return new DeleteContext(null, context.CloneContext(Sequence));
			}

			public override void SetRunQuery<T>(Query<T> query, Expression expr)
			{
				QueryRunner.SetNonQueryQuery(query);
			}

			public override void BuildQuery<T>(Query<T> query, ParameterExpression queryParameter)
			{
				QueryRunner.SetNonQueryQuery(query);
			}

			public override Expression BuildExpression(Expression? expression, int level, bool enforceServerSide)
			{
				return ThrowHelper.ThrowNotImplementedException<Expression>();
			}

			public override SqlInfo[] ConvertToSql(Expression? expression, int level, ConvertFlags flags)
			{
				return ThrowHelper.ThrowNotImplementedException<SqlInfo[]>();
			}

			public override SqlInfo[] ConvertToIndex(Expression? expression, int level, ConvertFlags flags)
			{
				return ThrowHelper.ThrowNotImplementedException<SqlInfo[]>();
			}

			public override IsExpressionResult IsExpression(Expression? expression, int level, RequestFor requestFlag)
			{
				return ThrowHelper.ThrowNotImplementedException<IsExpressionResult>();
			}

			public override IBuildContext GetContext(Expression? expression, int level, BuildInfo buildInfo)
			{
				return ThrowHelper.ThrowNotImplementedException<IBuildContext>();
			}
		}

		class DeleteWithOutputContext : SelectContext
		{
			public DeleteWithOutputContext(IBuildContext? parent, IBuildContext sequence, IBuildContext outputContext, LambdaExpression outputExpression)
				: base(parent, outputExpression, false, outputContext)
			{
				Statement = sequence.Statement;
			}

			public override void BuildQuery<T>(Query<T> query, ParameterExpression queryParameter)
			{
				var expr = BuildExpression(null, 0, false);
				var mapper = Builder.BuildMapper<T>(expr);

				var deleteStatement = (SqlDeleteStatement)Statement!;
				var outputQuery = Sequence[0].SelectQuery;

				deleteStatement.Output!.OutputColumns = outputQuery.Select.Columns.Select(c => c.Expression).ToList();

				QueryRunner.SetRunQuery(query, mapper);
			}
		}
	}
}
