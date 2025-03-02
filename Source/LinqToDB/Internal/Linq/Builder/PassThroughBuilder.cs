using System.Linq.Expressions;
using System.Reflection;

using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Reflection;
using LinqToDB.Reflection;

namespace LinqToDB.Internal.Linq.Builder
{
	[BuildsMethodCall("AsQueryable", nameof(Sql.Alias))]
	sealed class PassThroughBuilder : MethodCallBuilder
	{
		static readonly MethodInfo[] _supportedMethods = [
			Methods.Queryable.AsQueryable,
			Methods.LinqToDB.AsQueryable,
			Methods.LinqToDB.SqlExt.Alias
		];

		public static bool CanBuildMethod(MethodCallExpression call, BuildInfo info, ExpressionBuilder builder)
			=> call.IsSameGenericMethod(_supportedMethods);

		protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return builder.TryBuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));
		}
	}
}
