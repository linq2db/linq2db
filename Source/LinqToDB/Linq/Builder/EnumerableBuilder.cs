using System;
using System.Collections;
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
			if (buildInfo.IsSubQuery)
				return false;

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

			if (!builder.CanBeCompiled(expr, buildInfo.CreateSubQuery))
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

		public Expression Expand(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			return buildInfo.Expression;
		}
	}
}
