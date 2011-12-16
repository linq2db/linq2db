using System;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB.Extensions;
using LinqToDB.SqlBuilder;

namespace LinqToDB.Data.Linq.Builder
{
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
				sequence.SqlQuery.Insert.Items,
				sequence);

			UpdateBuilder.BuildSetter(
				builder,
				buildInfo,
				(LambdaExpression)methodCall.Arguments[2].Unwrap(),
				sequence,
				sequence.SqlQuery.Update.Items,
				sequence);

			sequence.SqlQuery.Insert.Into  = ((TableBuilder.TableContext)sequence).SqlTable;
			sequence.SqlQuery.Update.Table = ((TableBuilder.TableContext)sequence).SqlTable;
			sequence.SqlQuery.From.Tables.Clear();
			sequence.SqlQuery.From.Table(sequence.SqlQuery.Update.Table);

			if (methodCall.Arguments.Count == 3)
			{
				var table = sequence.SqlQuery.Insert.Into;
				var keys  = table.GetKeys(false);

				if (keys.Count == 0)
					throw new LinqException("InsertOrUpdate method requires the '{0}' table to have a primary key.", table.Name);

				var q =
				(
					from k in keys
					join i in sequence.SqlQuery.Insert.Items on k equals i.Column
					select new { k, i }
				).ToList();

				var missedKey = keys.Except(q.Select(i => i.k)).FirstOrDefault();

				if (missedKey != null)
					throw new LinqException("InsertOrUpdate method requires the '{0}.{1}' field to be included in the insert setter.",
						table.Name,
						((SqlField)missedKey).Name);

				sequence.SqlQuery.Update.Keys.AddRange(q.Select(i => i.i));
			}
			else
			{
				UpdateBuilder.BuildSetter(
					builder,
					buildInfo,
					(LambdaExpression)methodCall.Arguments[3].Unwrap(),
					sequence,
					sequence.SqlQuery.Update.Keys,
					sequence);
			}

			sequence.SqlQuery.QueryType = QueryType.InsertOrUpdate;

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
				if (Builder.SqlProvider.IsInsertOrUpdateSupported)
					query.SetNonQueryQuery();
				else
					query.MakeAlternativeInsertOrUpdate(SqlQuery);
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
