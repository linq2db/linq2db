using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Reflection;

namespace LinqToDB.Internal.Linq.Builder
{
	[BuildsMethodCall(nameof(Queryable.AsQueryable), nameof(Enumerable.AsEnumerable), nameof(Sql.Alias))]
	sealed class PassThroughBuilder : MethodCallBuilder
	{
		static readonly MethodInfo[] _supportedMethods = [
			Methods.Queryable.AsQueryable,
			Methods.Enumerable.AsEnumerable,
			Methods.LinqToDB.AsQueryable,
			Methods.LinqToDB.SqlExt.Alias,
		];

		public static bool CanBuildMethod(MethodCallExpression call)
			=> call.IsSameGenericMethod(_supportedMethods);

		protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return builder.TryBuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));
		}
	}
}
