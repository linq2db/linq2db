using System;
using System.Reflection;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using Reflection;

	[BuildsMethodCall("AsQueryable", nameof(Sql.Alias))]
	sealed class PassThroughBuilder : MethodCallBuilder
	{
		static readonly MethodInfo[] _supportedMethods = { Methods.Enumerable.AsQueryable, Methods.LinqToDB.AsQueryable, Methods.LinqToDB.SqlExt.Alias };

		public static bool CanBuildMethod(MethodCallExpression call, BuildInfo info, ExpressionBuilder builder)
			=> call.IsSameGenericMethod(_supportedMethods);

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression call, BuildInfo info)
			=> builder.BuildSequence(new BuildInfo(info, call.Arguments[0]));
	}
}
