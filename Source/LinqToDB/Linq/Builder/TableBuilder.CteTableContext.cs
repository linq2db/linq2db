using System;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using SqlQuery;

	partial class TableBuilder
	{
		static IBuildContext BuildCteContext(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			var methodCall = (MethodCallExpression)buildInfo.Expression;

			Expression  bodyExpr;
			IQueryable? query = null;
			string?     name  = null;
			bool        isRecursive = false;

			switch (methodCall.Arguments.Count)
			{
				case 1 :
					bodyExpr = methodCall.Arguments[0].Unwrap();
					break;
				case 2 :
					bodyExpr = methodCall.Arguments[0].Unwrap();
					name     = methodCall.Arguments[1].EvaluateExpression() as string;
					break;
				case 3 :
					query    = methodCall.Arguments[0].EvaluateExpression() as IQueryable;
					bodyExpr = methodCall.Arguments[1].Unwrap();
					name     = methodCall.Arguments[2].EvaluateExpression() as string;
					isRecursive = true;
					break;
				default:
					throw new InvalidOperationException();
			}

			bodyExpr = builder.ConvertExpression(bodyExpr);
			var cteContext = builder.RegisterCte(query, bodyExpr, () => new CteClause(null, bodyExpr.Type.GetGenericArguments()[0], isRecursive, name));

			var objectType      = methodCall.Method.GetGenericArguments()[0];
			var cteTableContext = new CteTableContext(builder, buildInfo.Parent, objectType, buildInfo.SelectQuery, cteContext, buildInfo.IsTest);

			// populate all fields
			if (isRecursive)
				_ = builder.MakeExpression(cteContext, new ContextRefExpression(methodCall.Method.GetGenericArguments()[0], cteContext), ProjectFlags.SQL);

			return cteTableContext;
		}

		static CteTableContext BuildCteContextTable(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			var queryable = (IQueryable)buildInfo.Expression.EvaluateExpression()!;
			var cteContext = builder.RegisterCte(queryable, null, () => new CteClause(null, queryable.ElementType, false, ""));

			var cteTableContext = new CteTableContext(builder, buildInfo.Parent, queryable.ElementType, buildInfo.SelectQuery, cteContext, buildInfo.IsTest);

			return cteTableContext;
		}
	}
}
