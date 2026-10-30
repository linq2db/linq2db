using System.Globalization;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB.Expressions;
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
			{
				var argument = methodCall.Arguments[i];

				if (!IsQueryIndependent(builder, argument))
					throw new LinqToDBException(string.Format(CultureInfo.InvariantCulture, ErrorHelper.SelectDynamic.Error_ArgumentDependsOnQuery, methodCall.Method.GetParameters()[i].Name));

				args[i] = BuildTimeArgumentReader.ReadArgument(builder, argument);
			}

			var desugared = (IQueryable)methodCall.Method.InvokeExt(null, args)!;

			return BuildSequenceResult.FromContext(builder.BuildSequence(new BuildInfo(buildInfo, builder.ConvertExpressionTree(desugared.Expression))));
		}

		// A quoted lambda becomes SQL, so it may read the row. Everything else runs once, while the query is built.
		static bool IsQueryIndependent(ExpressionBuilder builder, Expression argument)
			=> argument switch
			{
				UnaryExpression { NodeType: ExpressionType.Quote } => true,
				LambdaExpression lambda => builder.CanBeEvaluatedOnClient(
					lambda.Parameters.Aggregate(lambda.Body, static (body, p) => body.Replace(p, Expression.Default(p.Type)))),
				_ => builder.CanBeEvaluatedOnClient(argument),
			};
	}
}
