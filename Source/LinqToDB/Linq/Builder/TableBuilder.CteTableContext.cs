using System;
using System.Linq;
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

			string? name     = null;

			var bodyExpr = methodCall.Arguments[0].Unwrap();
			if (methodCall.Arguments.Count > 1)
			{
				name = methodCall.Arguments[1].EvaluateExpression<string>();
			}

			// ensure prepared for SQL
			bodyExpr = builder.ConvertExpression(bodyExpr);

			var cteContext = builder.RegisterCte(null, bodyExpr, () => new CteClause(null, bodyExpr.Type.GetGenericArguments()[0], false, name));

			var elementType = methodCall.Method.GetGenericArguments()[0];
			var cteTableContext = new CteTableContext(builder, buildInfo.Parent, elementType, buildInfo.SelectQuery, cteContext, buildInfo.IsTest);

			return BuildSequenceResult.FromContext(cteTableContext);
		}

		static BuildSequenceResult BuildRecursiveCteContextTable(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			var methodCallExpression = ((MethodCallExpression)buildInfo.Expression);
			var elementType          = methodCallExpression.Method.GetGenericArguments()[0];

			var parameters      = methodCallExpression.Method.GetParameters();
			var isSecondVariant = parameters[1].ParameterType == typeof(string);

			var lambda    = methodCallExpression.Arguments[isSecondVariant ? 2 : 1].UnwrapLambda();
			var tableName = builder.EvaluateExpression<string>(methodCallExpression.Arguments[isSecondVariant ? 1 : 2]);

			var cteClause = new CteClause(null, elementType, true, tableName);
			var cteContext = new CteContext(builder, null, cteClause, null!);

			var cteBody = lambda.Body.Transform(e =>
			{
				if (e == lambda.Parameters[0])
				{
					var cteTableContext = new CteTableContext(builder, null, elementType, new SelectQuery(), cteContext, buildInfo.IsTest);
					var cteTableContextRef = new ContextRefExpression(e.Type, cteTableContext);
					return cteTableContextRef;
				}

				return e;
			});

			cteContext.CteExpression = cteBody;

			var cteTableContext = new CteTableContext(builder, buildInfo.Parent, elementType, buildInfo.SelectQuery, cteContext, buildInfo.IsTest);

			return BuildSequenceResult.FromContext(cteTableContext);
		}
	}
}
