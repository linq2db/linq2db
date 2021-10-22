using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using LinqToDB.Expressions;
using LinqToDB.Extensions;
using LinqToDB.Reflection;

namespace LinqToDB.Linq.Builder
{
	class EnumerableBuilder : ISequenceBuilder
	{
		public int BuildCounter { get; set; }

		private static MethodInfo[] _containsMethodInfos = { Methods.Enumerable.Contains, Methods.Queryable.Contains };

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

			if (!builder.CanBeCompiled(expr))
				return false;

			switch (expr.NodeType)
			{
				case ExpressionType.MemberAccess:
				{
					var ma = (MemberExpression)expr;
					if (ma.Expression == null)
						break;

					if (ma.Expression.NodeType != ExpressionType.Constant)
						return false;
					break;
				}
				case ExpressionType.Constant:
					if (((ConstantExpression)expr).Value is not IEnumerable)
						return false;
					break;
				default:
					return false;
			}

			return true;

		}

		public IBuildContext BuildSequence(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			var collectionType = typeof(IEnumerable<>).GetGenericType(buildInfo.Expression.Type) ??
			                     throw new InvalidOperationException();

			var enumerableContext = new EnumerableContext(builder, buildInfo, buildInfo.SelectQuery, collectionType.GetGenericArguments()[0]);

			return enumerableContext;
		}

		public SequenceConvertInfo? Convert(ExpressionBuilder builder, BuildInfo buildInfo, ParameterExpression? param)
		{
			return null;
		}

		public bool IsSequence(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			return true;
		}

	}
}
