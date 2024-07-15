using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Linq.Builder
{
	using Extensions;
	using Reflection;
	using LinqToDB.Expressions;

	[BuildsExpression(ExpressionType.Constant, ExpressionType.Call, ExpressionType.MemberAccess, ExpressionType.NewArrayInit)]
	sealed class EnumerableBuilder : ISequenceBuilder
	{
		public static bool CanBuild(Expression expr, BuildInfo info, ExpressionBuilder builder)
		{
			if (expr.NodeType == ExpressionType.NewArrayInit)
				return true;

			if (!typeof(IEnumerable<>).IsSameOrParentOf(expr.Type))
				return false;

			if (typeof(IEnumerable<>).GetGenericType(expr.Type) is null)
				return false;

			return expr.NodeType switch
			{
				ExpressionType.MemberAccess => CanBuildMemberChain(((MemberExpression)expr).Expression),
				ExpressionType.Constant     => ((ConstantExpression)expr).Value is IEnumerable,
				ExpressionType.Call         => builder.CanBeCompiled(expr, false),
				_ => false,
			};

			static bool CanBuildMemberChain(Expression? expr)
			{
				while (expr is { NodeType: ExpressionType.MemberAccess })
					expr = ((MemberExpression)expr).Expression;
				
				return expr is null or { NodeType: ExpressionType.Constant };
			}
		}

		public BuildSequenceResult BuildSequence(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			var collectionType = typeof(IEnumerable<>).GetGenericType(buildInfo.Expression.Type) ??
			                     throw new InvalidOperationException();

			var enumerableContext = new EnumerableContext(builder, buildInfo, buildInfo.SelectQuery, collectionType.GetGenericArguments()[0]);

			return BuildSequenceResult.FromContext(enumerableContext);
		}

		public bool IsSequence(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			return true;
		}
	}
}
