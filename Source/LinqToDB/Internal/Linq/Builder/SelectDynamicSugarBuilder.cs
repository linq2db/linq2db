using System.Linq;
using System.Linq.Expressions;

using LinqToDB.Internal.Common;
using LinqToDB.Internal.Expressions;

namespace LinqToDB.Internal.Linq.Builder
{
	// The public SelectDynamic rewrites itself into the marker SelectDynamicBuilder handles, but only when it runs. One
	// reached unevaluated inside a lambda is run here over a stand-in queryable carrying its source expression, so a
	// correlated source is never evaluated.
	[BuildsMethodCall(nameof(LinqExtensions.SelectDynamic))]
	sealed class SelectDynamicSugarBuilder : MethodCallBuilder
	{
		public static bool CanBuildMethod(MethodCallExpression call)
			=> call.IsQueryable
				&& call.Method.DeclaringType == typeof(LinqExtensions);

		protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var args = new object?[methodCall.Arguments.Count];

			args[0] = ExpressionQueryImpl.CreateQuery(methodCall.Method.GetGenericArguments()[0], builder.DataContext, methodCall.Arguments[0]);

			for (var i = 1; i < args.Length; i++)
				args[i] = BuildTimeArgumentReader.ReadArgument(builder, methodCall.Arguments[i]);

			var desugared = (IQueryable)methodCall.Method.InvokeExt(null, args)!;

			return BuildSequenceResult.FromContext(builder.BuildSequence(new BuildInfo(buildInfo, builder.ConvertExpressionTree(desugared.Expression))));
		}
	}
}
