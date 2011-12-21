using System;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.Data.Linq.Builder
{
	using Extensions;
	using SqlBuilder;

	class FirstSingleBuilder : MethodCallBuilder
	{
		protected override bool CanBuildMethodCall(ExpressionBuilder _builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return 
				methodCall.IsQueryable("First", "FirstOrDefault", "Single", "SingleOrDefault") &&
				methodCall.Arguments.Count == 1;
		}

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var sequence = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));
			var take     = 0;

			if (!buildInfo.IsSubQuery || builder.SqlProvider.IsSubQueryTakeSupported)
				switch (methodCall.Method.Name)
				{
					case "First"           :
					case "FirstOrDefault"  :
						take = 1;
						break;

					case "Single"          :
					case "SingleOrDefault" :
						if (!buildInfo.IsSubQuery)
							take = 2;
						break;
				}

			if (take != 0)
				builder.BuildTake(sequence, new SqlValue(take));

			return new FirstSingleContext(buildInfo.Parent, sequence, methodCall);
		}

		protected override SequenceConvertInfo Convert(
			ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo, ParameterExpression param)
		{
			return null;
		}

		public class FirstSingleContext : SequenceContextBase
		{
			public FirstSingleContext(IBuildContext parent, IBuildContext sequence, MethodCallExpression methodCall)
				: base(parent, sequence, null)
			{
				_methodCall = methodCall;
			}

			readonly MethodCallExpression _methodCall;

			public override void BuildQuery<T>(Query<T> query, ParameterExpression queryParameter)
			{
				Sequence.BuildQuery(query, queryParameter);

				switch (_methodCall.Method.Name)
				{
					case "First"           : query.GetElement = (ctx, db, expr, ps) => query.GetIEnumerable(ctx, db, expr, ps).First();           break;
					case "FirstOrDefault"  : query.GetElement = (ctx, db, expr, ps) => query.GetIEnumerable(ctx, db, expr, ps).FirstOrDefault();  break;
					case "Single"          : query.GetElement = (ctx, db, expr, ps) => query.GetIEnumerable(ctx, db, expr, ps).Single();          break;
					case "SingleOrDefault" : query.GetElement = (ctx, db, expr, ps) => query.GetIEnumerable(ctx, db, expr, ps).SingleOrDefault(); break;
				}
			}

			static object SequenceException()
			{
				return new object[0].First();
			}

			public override Expression BuildExpression(Expression expression, int level)
			{
				if (expression == null)
				{
					if (Builder.SqlProvider.IsApplyJoinSupported)
					{
						var join = SqlQuery.OuterApply(SqlQuery);

						Parent.SqlQuery.From.Tables[0].Joins.Add(join.JoinedTable);

						var expr = Sequence.BuildExpression(expression, level);
						var idx  = SqlQuery.Select.Add(new SqlValue(1));

						idx = ConvertToParentIndex(idx, this);

						var defaultValue = _methodCall.Method.Name.EndsWith("OrDefault") ?
							Expression.Constant(expr.Type.GetDefaultValue(), expr.Type) as Expression :
							Expression.Convert(
								Expression.Call(
									null,
									ReflectionHelper.Expressor<object>.MethodExpressor(_ => SequenceException())),
								expr.Type);

						expr = Expression.Condition(
							Expression.Call(
								ExpressionBuilder.DataReaderParam,
								ReflectionHelper.DataReader.IsDBNull,
								Expression.Constant(idx)),
							defaultValue,
							expr);

						return expr;
					}

					if (Sequence.IsExpression(null, level, RequestFor.Object).Result)
						return Builder.BuildMultipleQuery(Parent, _methodCall);

					return Builder.BuildSql(_methodCall.Type, Parent.SqlQuery.Select.Add(SqlQuery));
				}

				throw new NotImplementedException();
			}

			public override SqlInfo[] ConvertToSql(Expression expression, int level, ConvertFlags flags)
			{
				return Sequence.ConvertToSql(expression, level + 1, flags);
			}

			public override SqlInfo[] ConvertToIndex(Expression expression, int level, ConvertFlags flags)
			{
				return Sequence.ConvertToIndex(expression, level, flags);
			}

			public override IsExpressionResult IsExpression(Expression expression, int level, RequestFor requestFlag)
			{
				return Sequence.IsExpression(expression, level, requestFlag);
			}

			public override IBuildContext GetContext(Expression expression, int level, BuildInfo buildInfo)
			{
				throw new NotImplementedException();
			}
		}
	}
}
