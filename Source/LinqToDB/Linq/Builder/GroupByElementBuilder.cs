using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using Extensions;
	
	class GroupByElementBuilder : ISequenceBuilder
	{
		public int BuildCounter { get; set; }

		GroupByBuilder.GroupByContext? GetGroupByContext(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			if (buildInfo.Expression.NodeType == ExpressionType.Parameter)
			{
				if (typeof(IGrouping<,>).IsSameOrParentOf(buildInfo.Expression.Type))
				{
					var context = builder.GetContext(buildInfo.Parent, buildInfo.Expression);
					if (context is SelectContext sc)
					{
						var current = sc.Sequence[0];
						while (current is SubQueryContext subQuery)
						{
							current = subQuery.SubQuery;
						}

						if (current is GroupByBuilder.GroupByContext groupByContext)
							return groupByContext;
					}
				}
			}

			return null;
		}


		public bool CanBuild(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			return GetGroupByContext(builder, buildInfo) != null;
		}

		public IBuildContext? BuildSequence(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			var groupByContext = GetGroupByContext(builder, buildInfo)!;

			var elementContext = groupByContext.GetContext(buildInfo.Expression, 0, buildInfo);

			return elementContext;
		}

		public SequenceConvertInfo? Convert(ExpressionBuilder builder, BuildInfo buildInfo, ParameterExpression? param)
		{
			return null;
		}

		public bool IsSequence(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			return true;
		}
	}
}
