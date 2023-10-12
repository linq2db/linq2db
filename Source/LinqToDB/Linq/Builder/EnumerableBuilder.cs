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

	sealed class EnumerableBuilder : ISequenceBuilder
	{
		static readonly MethodInfo[] _containsMethodInfos = { Methods.Enumerable.Contains, Methods.Queryable.Contains };

		public int          BuildCounter { get; set; }

		public bool CanBuild(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			var expr = buildInfo.Expression;

			if (expr.NodeType == ExpressionType.NewArrayInit)
			{
				return true;
			}

			if (expr.NodeType == ExpressionType.Call)
			{
				var mc = (MethodCallExpression)expr;
				if (mc.IsSameGenericMethod(_containsMethodInfos))
					return false;
			}

			if (!typeof(IEnumerable<>).IsSameOrParentOf(expr.Type))
				return false;

			var collectionType = typeof(IEnumerable<>).GetGenericType(expr.Type);
			if (collectionType == null)
				return false;

			switch (expr.NodeType)
			{
				case ExpressionType.MemberAccess:
					return CanBuildMemberChain(((MemberExpression)expr).Expression);
				case ExpressionType.Constant:
					if (((ConstantExpression)expr).Value is not IEnumerable)
						return false;
					break;
				default:
					return false;
			}

			return true;

			static bool CanBuildMemberChain(Expression? expr)
			{
				if (expr == null)
					return true;

				if (expr.NodeType == ExpressionType.MemberAccess)
					return CanBuildMemberChain(((MemberExpression)expr).Expression);

				return expr.NodeType == ExpressionType.Constant;
			}

		}

		public IBuildContext BuildSequence(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			var collectionType = typeof(IEnumerable<>).GetGenericType(buildInfo.Expression.Type) ??
			                     throw new InvalidOperationException();

			var enumerableContext = new EnumerableContext(builder, buildInfo, buildInfo.SelectQuery, collectionType.GetGenericArguments()[0]);

			return enumerableContext;
		}

		public bool IsSequence(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			return true;
		}
	}
}
