using System;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using SqlQuery;

	class DeleteBuilder : MethodCallBuilder
	{
		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return methodCall.IsQueryable("Delete");
		}

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var sequence = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

			if (methodCall.Arguments.Count == 2)
				sequence = builder.BuildWhere(buildInfo.Parent, sequence, (LambdaExpression)methodCall.Arguments[1].Unwrap(), false);

			sequence.SelectQuery.QueryType = QueryType.Delete;

			// Check association.
			//
			var ctx = sequence as SelectContext;

			if (ctx != null && ctx.IsScalar)
			{
				var res = ctx.IsExpression(null, 0, RequestFor.Association);

				if (res.Result && res.Context is TableBuilder.AssociatedTableContext)
				{
					var atc = (TableBuilder.AssociatedTableContext)res.Context;
					sequence.SelectQuery.Delete.Table = atc.SqlTable;
				}
				else
				{
					res = ctx.IsExpression(null, 0, RequestFor.Table);

					if (res.Result && res.Context is TableBuilder.TableContext)
					{
						var tc = (TableBuilder.TableContext)res.Context;

						if (sequence.SelectQuery.From.Tables.Count == 0 || sequence.SelectQuery.From.Tables[0].Source != tc.SelectQuery)
							sequence.SelectQuery.Delete.Table = tc.SqlTable;
					}
				}
			}

			return new DeleteContext(buildInfo.Parent, sequence);
		}

		protected override SequenceConvertInfo Convert(
			ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo, ParameterExpression param)
		{
			return null;
		}

		class DeleteContext : SequenceContextBase
		{
			public DeleteContext(IBuildContext parent, IBuildContext sequence)
				: base(parent, sequence, null)
			{
			}

			public override void BuildQuery<T>(QueryOld<T> query, ParameterExpression queryParameter)
			{
				query.SetNonQueryQuery();
			}

			public override Expression BuildExpression(Expression expression, int level)
			{
				throw new NotImplementedException();
			}

			public override SqlInfo[] ConvertToSql(Expression expression, int level, ConvertFlags flags)
			{
				throw new NotImplementedException();
			}

			public override SqlInfo[] ConvertToIndex(Expression expression, int level, ConvertFlags flags)
			{
				throw new NotImplementedException();
			}

			public override IsExpressionResult IsExpression(Expression expression, int level, RequestFor requestFlag)
			{
				throw new NotImplementedException();
			}

			public override IBuildContext GetContext(Expression expression, int level, BuildInfo buildInfo)
			{
				throw new NotImplementedException();
			}
		}
	}
}
