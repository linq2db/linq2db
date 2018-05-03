using System;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;

	abstract class MethodCallBuilder : ISequenceBuilder
	{
		public int BuildCounter { get; set; }

		public bool CanBuild(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			if (buildInfo.Expression.NodeType == ExpressionType.Call)
				return CanBuildMethodCall(builder, (MethodCallExpression)buildInfo.Expression, buildInfo);
			return false;
		}

		public IBuildContext BuildSequence(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			return BuildMethodCall(builder, (MethodCallExpression)buildInfo.Expression, buildInfo);
		}

		public SequenceConvertInfo Convert(ExpressionBuilder builder, BuildInfo buildInfo, ParameterExpression param)
		{
			return Convert(builder, (MethodCallExpression)buildInfo.Expression, buildInfo, param);
		}

		public bool IsSequence(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			return builder.IsSequence(new BuildInfo(buildInfo, ((MethodCallExpression)buildInfo.Expression).Arguments[0]));
		}

		protected abstract bool                CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo);
		protected abstract IBuildContext       BuildMethodCall   (ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo);
		protected abstract SequenceConvertInfo Convert           (ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo, ParameterExpression param);

		protected static Expression ConvertMethod(
			MethodCallExpression methodCall,
			int                  sourceTypeNumber,
			SequenceConvertInfo  info,
			ParameterExpression  param,
			Expression           expression)
		{
			if (string.ReferenceEquals(expression, methodCall) && param != null && param.Type != info.Parameter.Type)
			{
				var types = methodCall.Method.GetGenericArguments();
				var mgen  = methodCall.Method.GetGenericMethodDefinition();

				types[sourceTypeNumber] = info.Parameter.Type;

				var args = methodCall.Arguments.ToArray();

				args[0] = info.Expression;

				for (var i = 1; i < args.Length; i++)
				{
					var arg = args[i].Unwrap();

					if (arg.NodeType == ExpressionType.Lambda)
					{
						var l = (LambdaExpression)arg;

						if (l.Parameters.Any(a => string.ReferenceEquals(a, param)))
						{
							args[i] = Expression.Lambda(
								l.Body.Transform(ex => ConvertMethod(methodCall, sourceTypeNumber, info, param, ex)),
								info.Parameter);

							return Expression.Call(methodCall.Object, mgen.MakeGenericMethod(types), args);
						}
					}
				}
			}

			if (expression == methodCall.Arguments[0])
				return info.Expression;

			switch (expression.NodeType)
			{
				case ExpressionType.Parameter :

					if (info.ExpressionsToReplace != null)
						foreach (var item in info.ExpressionsToReplace)
							if (expression == item.Path || expression == param && item.Path.NodeType == ExpressionType.Parameter)
								return item.Expr;
					break;

				case ExpressionType.MemberAccess :

					if (info.ExpressionsToReplace != null)
					{
						foreach (var item in info.ExpressionsToReplace)
						{
							var ex1 = expression;
							var ex2 = item.Path;

							while (ex1.NodeType == ex2.NodeType)
							{
								if (ex1.NodeType == ExpressionType.Parameter)
									return ex1 == ex2 || info.Parameter == ex2? item.Expr : expression;

								if (ex2.NodeType != ExpressionType.MemberAccess)
									break;

								var ma1 = (MemberExpression)ex1;
								var ma2 = (MemberExpression)ex2;

								if (ma1.Member != ma2.Member)
									break;

								ex1 = ma1.Expression;
								ex2 = ma2.Expression;
							}
						}
					}

					break;
			}

			return expression;
		}
	}
}
