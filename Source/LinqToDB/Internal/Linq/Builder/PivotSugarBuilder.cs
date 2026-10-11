using System;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB.Expressions;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.Expressions;

namespace LinqToDB.Internal.Linq.Builder
{
	// The public Pivot / Unpivot / SelectDynamic overloads rewrite themselves into the markers the other builders
	// handle, but only when they run. One reached unevaluated inside a lambda is run here over a stand-in queryable
	// carrying its source expression, so a correlated source is never evaluated.
	[BuildsMethodCall(nameof(LinqExtensions.Unpivot), nameof(LinqExtensions.Pivot), nameof(LinqExtensions.SelectDynamic))]
	sealed class PivotSugarBuilder : MethodCallBuilder
	{
		public static bool CanBuildMethod(MethodCallExpression call)
			=> call.IsQueryable
				&& call.Method.DeclaringType == typeof(LinqExtensions)
				&& !(string.Equals(call.Method.Name, nameof(LinqExtensions.Unpivot), StringComparison.Ordinal) && call.Arguments.Count == 5);

		protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var parameters = methodCall.Method.GetParameters();
			var args       = new object?[methodCall.Arguments.Count];

			args[0] = ExpressionQueryImpl.CreateQuery(methodCall.Method.GetGenericArguments()[0], builder.DataContext, methodCall.Arguments[0]);

			for (var i = 1; i < args.Length; i++)
				args[i] = GetArgument(methodCall.Arguments[i], parameters[i].ParameterType);

			var desugared = (IQueryable)methodCall.Method.InvokeExt(null, args)!;

			return BuildSequenceResult.FromContext(builder.BuildSequence(new BuildInfo(buildInfo, desugared.Expression)));
		}

		// A quoted lambda is passed through rather than compiled, so it may reference an enclosing lambda's parameter.
		static object? GetArgument(Expression argument, Type parameterType)
		{
			if (argument.NodeType == ExpressionType.Quote)
				return ((UnaryExpression)argument).Operand;

			if (argument is NewArrayExpression { NodeType: ExpressionType.NewArrayInit } array && array.Expressions.All(static e => e.NodeType == ExpressionType.Quote))
			{
				var result = Array.CreateInstance(parameterType.GetElementType()!, array.Expressions.Count);

				for (var i = 0; i < array.Expressions.Count; i++)
					result.SetValue(((UnaryExpression)array.Expressions[i]).Operand, i);

				return result;
			}

			return argument.EvaluateExpression();
		}
	}
}
