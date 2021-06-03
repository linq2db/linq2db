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

			var sequence = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

			if (methodCall.Arguments.Count == 2 && deleteType == DeleteContext.DeleteType.Delete)
				sequence = builder.BuildWhere(buildInfo.Parent, sequence, (LambdaExpression)methodCall.Arguments[1].Unwrap(), false);

			var deleteStatement = new SqlDeleteStatement(sequence.SelectQuery);

			sequence.Statement = deleteStatement;

			// Check association.
			//

			if (sequence is SelectContext ctx && ctx.IsScalar)
			{
				var res = ctx.IsExpression(null, 0, RequestFor.Association);

				if (res.Result)
				{
					var isTableResult = res.Context!.IsExpression(null, 0, RequestFor.Table);
					if (!isTableResult.Result)
						throw new LinqException("Can not retrieve Table context from association.");

					var atc = (TableBuilder.TableContext)isTableResult.Context!;
					deleteStatement.Table = atc.SqlTable;
				}
				else
				{
					res = ctx.IsExpression(null, 0, RequestFor.Table);

					if (res.Result && res.Context is TableBuilder.TableContext context)
					{
						var tc = context;

						if (deleteStatement.SelectQuery.From.Tables.Count == 0 || deleteStatement.SelectQuery.From.Tables[0].Source != tc.SelectQuery)
							deleteStatement.Table = tc.SqlTable;
					}
				}
			}

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

				var deletedTable = SqlTable.Deleted(methodCall.Method.GetGenericArguments()[0]);

				outputContext = new TableBuilder.TableContext(builder, new SelectQuery(), deletedTable);

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

		protected override SequenceConvertInfo? Convert(
			ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo, ParameterExpression? param)
		{
			return null;
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

			public override void BuildQuery<T>(Query<T> query, ParameterExpression queryParameter)
			{
				QueryRunner.SetNonQueryQuery(query);
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
		}

		class DeleteWithOutputContext : SelectContext
		{
			public DeleteWithOutputContext(IBuildContext? parent, IBuildContext sequence, IBuildContext outputContext, LambdaExpression outputExpression)
				: base(parent, outputExpression, outputContext)
			{
				Statement = sequence.Statement;
			}

			public override void BuildQuery<T>(Query<T> query, ParameterExpression queryParameter)
			{
				var expr = BuildExpression(null, 0, false);
				var mapper = Builder.BuildMapper<T>(expr);

				var deleteStatement = (SqlDeleteStatement)Statement!;
				var outputQuery = Sequence[0].SelectQuery;

				deleteStatement.Output!.OutputQuery = outputQuery;

				QueryRunner.SetRunQuery(query, mapper);
			}
		}
	}
}
