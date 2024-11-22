using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using SqlQuery;

	partial class TableBuilder
	{
		static BuildSequenceResult BuildCteContext(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			var methodCall = (MethodCallExpression)buildInfo.Expression;

			var cteContext  = builder.FindRegisteredCteContext(methodCall);
			var elementType = methodCall.Method.GetGenericArguments()[0];

			if (cteContext == null)
			{
				string? tableName = null;

				var cteBody = methodCall.Arguments[0].Unwrap();
				if (methodCall.Arguments.Count > 1)
				{
					tableName = methodCall.Arguments[1].EvaluateExpression<string>();
				}

				var cteClause = new CteClause(null, elementType, true, tableName);
				cteContext               = new CteContext(builder, null, cteClause, null!);
				cteContext.CteExpression = cteBody;

				builder.RegisterCteContext(cteContext, methodCall);
			}

			var cteTableContext = new CteTableContext(builder, buildInfo.Parent, elementType, buildInfo.SelectQuery, cteContext);

			return BuildSequenceResult.FromContext(cteTableContext);
		}

		static BuildSequenceResult BuildRecursiveCteContextTable(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			var methodCall = ((MethodCallExpression)buildInfo.Expression);

			var cteContext  = builder.FindRegisteredCteContext(methodCall);
			var elementType = methodCall.Method.GetGenericArguments()[0];

			if (cteContext == null)
			{
				var parameters      = methodCall.Method.GetParameters();
				var isSecondVariant = parameters[1].ParameterType == typeof(string);

				var lambda    = methodCall.Arguments[isSecondVariant ? 2 : 1].UnwrapLambda();
				var tableName = builder.EvaluateExpression<string>(methodCall.Arguments[isSecondVariant ? 1 : 2]);

				var cteClause  = new CteClause(null, elementType, true, tableName);
				cteContext = new CteContext(builder, null, cteClause, null!);

				var cteBody = lambda.Body.Transform(e =>
				{
					if (e == lambda.Parameters[0])
					{
						var cteTableContext    = new CteTableContext(builder, null, elementType, new SelectQuery(), cteContext);
						var cteTableContextRef = new ContextRefExpression(e.Type, cteTableContext);
						return cteTableContextRef;
					}

					return e;
				});

				cteContext.CteExpression = cteBody;
				builder.RegisterCteContext(cteContext, methodCall);
			}

			var cteTableContext = new CteTableContext(builder, buildInfo.Parent, elementType, buildInfo.SelectQuery, cteContext);

			return BuildSequenceResult.FromContext(cteTableContext);
		}
	}
}
