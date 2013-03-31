using System;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using SqlBuilder;

	class IntersectBuilder : MethodCallBuilder
	{
		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return methodCall.Arguments.Count == 2 && methodCall.IsQueryable("Except", "Intersect");
		}

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var sequence = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));
			var query    = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[1], new SqlQuery()));
			var except   = query.SqlQuery;

			sequence = new SubQueryContext(sequence);

			var sql = sequence.SqlQuery;

			except.ParentSql = sql;

			if (methodCall.Method.Name == "Except")
				sql.Where.Not.Exists(except);
			else
				sql.Where.Exists(except);

			var keys1 = sequence.ConvertToSql(null, 0, ConvertFlags.Key);
			var keys2 = query.   ConvertToSql(null, 0, ConvertFlags.Key);

			if (keys1.Length != keys2.Length)
				throw new InvalidOperationException();

			for (var i = 0; i < keys1.Length; i++)
			{
				except.Where
					.Expr(keys1[i].Sql)
					.Equal
					.Expr(keys2[i].Sql);
			}

			return sequence;
		}

		protected override SequenceConvertInfo Convert(
			ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo, ParameterExpression param)
		{
			return null;
		}
	}
}
