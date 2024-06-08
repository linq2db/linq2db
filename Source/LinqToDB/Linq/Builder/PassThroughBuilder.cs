using System;
using System.Reflection;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using Reflection;

	sealed class PassThroughBuilder : MethodCallBuilder
	{
		static readonly MethodInfo[] _supportedMethods = { Methods.Queryable.AsQueryable, Methods.LinqToDB.AsQueryable, Methods.LinqToDB.SqlExt.Alias };

		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return methodCall.IsSameGenericMethod(_supportedMethods);
		}

		protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return builder.TryBuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));
		}
	}
}
