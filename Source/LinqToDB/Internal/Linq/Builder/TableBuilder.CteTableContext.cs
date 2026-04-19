using System.Linq.Expressions;

using LinqToDB.Expressions;
using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.SqlQuery;

namespace LinqToDB.Internal.Linq.Builder
{
	partial class TableBuilder
	{
		static BuildSequenceResult BuildCteContext(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			var methodCall = (MethodCallExpression)buildInfo.Expression;

			var cteContext  = builder.FindRegisteredCteContext(methodCall);
			var elementType = methodCall.Method.GetGenericArguments()[0];

			if (cteContext == null)
			{
				string?                  tableName = null;
				CteAnnotationsContainer? config    = null;

				var cteBody = methodCall.Arguments[0].Unwrap();

				// Two shapes reach the expression tree:
				//   AsCte(source)                           — 1 arg
				//   AsCte(source, string? name)             — 2 args, arg1 string
				//   AsCteInternal(source, CteAnnotations…)  — 2 args, arg1 container (emitted by the
				//                                             Action<ICteBuilder> overload; the container
				//                                             carries name + annotations with value-based
				//                                             equality so the query cache differentiates
				//                                             distinct configurations).
				if (methodCall.Arguments.Count > 1)
				{
					var arg1 = methodCall.Arguments[1];

					if (arg1.Type == typeof(string))
					{
						tableName = arg1.EvaluateExpression<string>();
					}
					else if (arg1.Type == typeof(CteAnnotationsContainer))
					{
						config    = arg1.EvaluateExpression<CteAnnotationsContainer>();
						tableName = config?.Name;
					}
				}

				var cteClause = new CteClause(null, elementType, true, tableName);

				if (config != null)
				{
					foreach (var ann in config.Annotations.GetAnnotations())
					{
						cteClause.Annotations.SetAnnotation(ann.Name, ann.Value);
					}
				}

				cteContext               = new CteContext(builder.GetTranslationModifier(), builder, null, cteClause, null!);
				cteContext.CteExpression = cteBody;

				builder.RegisterCteContext(cteContext, methodCall);
			}

			var cteTableContext = new CteTableContext(cteContext.TranslationModifier, builder, buildInfo.Parent, elementType, buildInfo.SelectQuery, cteContext);

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
				cteContext = new CteContext(builder.GetTranslationModifier(), builder, null, cteClause, null!);

				var cteBody = lambda.Body.Transform(e =>
				{
					if (e == lambda.Parameters[0])
					{
						var cteTableContext    = new CteTableContext(cteContext.TranslationModifier, builder, null, elementType, new SelectQuery(), cteContext);
						var cteTableContextRef = new ContextRefExpression(e.Type, cteTableContext);
						return cteTableContextRef;
					}

					return e;
				});

				cteContext.CteExpression = cteBody;
				builder.RegisterCteContext(cteContext, methodCall);
			}

			var cteTableContext = new CteTableContext(cteContext.TranslationModifier, builder, buildInfo.Parent, elementType, buildInfo.SelectQuery, cteContext);

			return BuildSequenceResult.FromContext(cteTableContext);
		}
	}
}
