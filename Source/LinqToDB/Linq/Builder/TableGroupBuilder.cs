using System.Collections.Generic;
using System.Linq.Expressions;
using LinqToDB.Expressions;
using LinqToDB.SqlQuery;

namespace LinqToDB.Linq.Builder
{
	class TableGroupBuilder : MethodCallBuilder
	{
		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return methodCall.IsSameGenericMethod(LinqExtensions.TableGroupMethodInfo);
		}

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var context = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));
			if (context is TableBuilder.TableContext tableContext)
			{
				var sqlTable = tableContext.SqlTable;
				if (sqlTable.Groups == null)
				{
					sqlTable.Groups = new HashSet<string>();
				}

				var groups = (string)methodCall.Arguments[1].EvaluateExpression();
				foreach (var group in groups.Split(',', ';'))
				{
					sqlTable.Groups.Add(group);
				}
			}

			return context;
		}

		protected override SequenceConvertInfo Convert(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo,
			ParameterExpression param)
		{
			return null;
		}
	}
}
