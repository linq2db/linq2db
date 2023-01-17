using System;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using SqlQuery;

	//TODO: remove
	sealed class CountBuilder : MethodCallBuilder
	{
		public  static readonly string[] MethodNames      = { "Count"     , "LongCount"      };
		private static readonly string[] MethodNamesAsync = { "CountAsync", "LongCountAsync" };

		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return methodCall.IsQueryable(MethodNames) || methodCall.IsAsyncExtension(MethodNamesAsync);
		}

		public override bool IsAggregationContext(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			return true;
		}

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var inGrouping = false;

			IBuildContext? sequence = null;

			if (buildInfo.IsSubQuery)
			{
				var testSequence = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0], new SelectQuery()) { AggregationTest = true });

				// It means that as root we have used fake context
				var testSelectQuery = testSequence.SelectQuery;
				if (testSelectQuery.From.Tables.Count == 0)
				{
					var valid = true;
					if (!testSelectQuery.Where.IsEmpty)
					{
						valid = false;
						//TODO: we can use filter for building count
					}

					if (valid)
					{
						sequence = builder.BuildSequence(
							new BuildInfo(buildInfo, methodCall.Arguments[0]) { CreateSubQuery = false, IsAggregation = true });
						inGrouping = true;
					}
				}
			}

			if (sequence == null)
			{
				sequence = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]) { CreateSubQuery = true, IsAggregation = false });
			}

			var returnType = methodCall.Method.ReturnType;

			if (methodCall.IsAsyncExtension())
				returnType = returnType.GetGenericArguments()[0];

			if (!buildInfo.IsSubQuery)
			{
				if (sequence.SelectQuery.Select.IsDistinct        ||
				    sequence.SelectQuery.Select.TakeValue != null ||
				    sequence.SelectQuery.Select.SkipValue != null)
				{
					sequence = new SubQueryContext(sequence);
				}
				else if (inGrouping)
				{
					//TODO: maybe remove
					if (!builder.DataContext.SqlProviderFlags.IsSybaseBuggyGroupBy)
						sequence.SelectQuery.Select.Add(new SqlValue(0));
					else
						foreach (var item in sequence.SelectQuery.GroupBy.Items)
							sequence.SelectQuery.Select.Add(item);

					sequence = new SubQueryContext(sequence);
				}

				if (sequence.SelectQuery.OrderBy.Items.Count > 0)
				{
					if (sequence.SelectQuery.Select.TakeValue == null && sequence.SelectQuery.Select.SkipValue == null)
						sequence.SelectQuery.OrderBy.Items.Clear();
					else
						sequence = new SubQueryContext(sequence);
				}
			}

			var parentContext = buildInfo.Parent;
			if (parentContext is SubQueryContext subQuery)
			{
				parentContext = subQuery.SubQuery;
			}

			var functionPlaceholder = ExpressionBuilder.CreatePlaceholder(sequence, SqlFunction.CreateCount(returnType, sequence.SelectQuery), buildInfo.Expression);
			var context = new CountContext(parentContext, sequence, returnType);

			if (buildInfo.IsSubQuery)
			{
				if (!inGrouping)
				{
					CreateWeakOuterJoin(buildInfo.Parent!, sequence.SelectQuery);
				}
			}

			context.Placeholder = functionPlaceholder;

			return context;
		}

		void CreateWeakOuterJoin(IBuildContext parent, SelectQuery selectQuery)
		{
			var join = selectQuery.OuterApply();
			join.JoinedTable.IsWeak = true;

			parent.SelectQuery.From.Tables[0].Joins.Add(join.JoinedTable);
		}


		protected override SequenceConvertInfo? Convert(
			ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo, ParameterExpression? param)
		{
			return null;
		}

		internal sealed class CountContext : SequenceContextBase
		{
			public CountContext(IBuildContext? parent, IBuildContext sequence, Type returnType)
				: base(parent, sequence, null)
			{
			}

			public int             FieldIndex;
			public ISqlExpression? Sql;
			public SqlPlaceholderExpression Placeholder = null!;

			public override IBuildContext? GetContext(Expression expression, BuildInfo buildInfo)
			{
				return Sequence.GetContext(expression, buildInfo);
			}

			public override Expression MakeExpression(Expression path, ProjectFlags flags)
			{
				return Placeholder;
			}

			public override IBuildContext Clone(CloningContext context)
			{
				throw new NotImplementedException();
			}
		}
	}
}
