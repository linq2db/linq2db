using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using LinqToDB.Extensions;
using LinqToDB.SqlQuery;

namespace LinqToDB.Linq.Builder
{
	class EnumerableBuilder : ISequenceBuilder
	{
		public int BuildCounter { get; set; }

		public bool CanBuild(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			var expr = buildInfo.Expression;

			if (buildInfo.IsSubQuery)
				return false;

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

			var enumerableContext = new EnumerableContext(builder, buildInfo, new SelectQuery(), collectionType.GetGenericArguments()[0],
				builder.ConvertToSql(buildInfo.Parent, buildInfo.Expression));

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
