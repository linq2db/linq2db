using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using LinqToDB.Extensions;

namespace LinqToDB.Linq.Builder
{
	class EnumerableBuilder : ISequenceBuilder
	{
		public int BuildCounter { get; set; }

		public bool CanBuild(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			var expr = buildInfo.Expression;

			if (expr.NodeType == ExpressionType.NewArrayInit)
			{
				return true;
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
					if (ma.Expression.NodeType != ExpressionType.Constant)
						return false;
					break;
				}
				case ExpressionType.Constant:
					var ce = (ConstantExpression)expr;
					if (ce.Value == null)
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
