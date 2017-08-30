using System;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using SqlQuery;

	class InsertOrUpdateBuilder : MethodCallBuilder
	{
		#region InsertOrUpdateBuilder

		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return methodCall.IsQueryable("InsertOrUpdate");
		}

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var sequence = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

			UpdateBuilder.BuildSetter(
				builder,
				buildInfo,
				(LambdaExpression)methodCall.Arguments[1].Unwrap(),
				sequence,
				sequence.SelectQuery.Insert.Items,
				sequence);

			UpdateBuilder.BuildSetter(
				builder,
				buildInfo,
				(LambdaExpression)methodCall.Arguments[2].Unwrap(),
				sequence,
				sequence.SelectQuery.Update.Items,
				sequence);

			sequence.SelectQuery.Insert.Into  = ((TableBuilder.TableContext)sequence).SqlTable;
			sequence.SelectQuery.Update.Table = ((TableBuilder.TableContext)sequence).SqlTable;
			sequence.SelectQuery.From.Tables.Clear();
			sequence.SelectQuery.From.Table(sequence.SelectQuery.Update.Table);

			if (methodCall.Arguments.Count == 3)
			{
				var table = sequence.SelectQuery.Insert.Into;
				var keys  = table.GetKeys(false);

				if (keys.Count == 0)
					throw new LinqException("InsertOrUpdate method requires the '{0}' table to have a primary key.", table.Name);

				var q =
				(
					from k in keys
					join i in sequence.SelectQuery.Insert.Items on k equals i.Column
					select new { k, i }
				).ToList();

				var missedKey = keys.Except(q.Select(i => i.k)).FirstOrDefault();

				if (missedKey != null)
					throw new LinqException("InsertOrUpdate method requires the '{0}.{1}' field to be included in the insert setter.",
						table.Name,
						((SqlField)missedKey).Name);

				sequence.SelectQuery.Update.Keys.AddRange(q.Select(i => i.i));
			}
			else
			{
				UpdateBuilder.BuildSetter(
					builder,
					buildInfo,
					(LambdaExpression)methodCall.Arguments[3].Unwrap(),
					sequence,
					sequence.SelectQuery.Update.Keys,
					sequence);
			}

			sequence.SelectQuery.QueryType = QueryType.InsertOrUpdate;

			return new InsertOrUpdateContext(buildInfo.Parent, sequence);
		}

		protected override SequenceConvertInfo Convert(
			ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo, ParameterExpression param)
		{
			return null;
		}

		#endregion

		#region UpdateContext

		class InsertOrUpdateContext : SequenceContextBase
		{
			public InsertOrUpdateContext(IBuildContext parent, IBuildContext sequence)
				: base(parent, sequence, null)
			{
			}

			public override void BuildQuery<T>(Query<T> query, ParameterExpression queryParameter)
			{
				if (Builder.DataContext.SqlProviderFlags.IsInsertOrUpdateSupported)
					QueryRunner.SetNonQueryQuery(query);
				else
					QueryRunner.MakeAlternativeInsertOrUpdate(query, SelectQuery);
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

		#endregion
	}
}
