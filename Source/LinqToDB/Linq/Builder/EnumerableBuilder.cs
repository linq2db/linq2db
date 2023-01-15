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
	[BuildsExpression(ExpressionType.Constant, ExpressionType.MemberAccess, ExpressionType.NewArrayInit)]
	sealed class EnumerableBuilder : ISequenceBuilder
	{
		public static bool CanBuild(Expression expr, BuildInfo info, ExpressionBuilder builder)
		{
			if (info.IsSubQuery)
				return false;

			if (expr.NodeType == ExpressionType.NewArrayInit)
				return true;

			if (!typeof(IEnumerable<>).IsSameOrParentOf(expr.Type))
				return false;

			var collectionType = typeof(IEnumerable<>).GetGenericType(expr.Type);
			if (collectionType == null)
				return false;

			if (!builder.CanBeCompiled(expr, info.CreateSubQuery))
				return false;

			switch (expr.NodeType)
			{
				case ExpressionType.MemberAccess:
					return ((MemberExpression)expr).Expression is null or { NodeType: ExpressionType.Constant };

				case ExpressionType.Constant:
					return ((ConstantExpression)expr).Value is IEnumerable;

				default:
					return false;
			}
		}

		public IBuildContext BuildSequence(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			var collectionType = typeof(IEnumerable<>).GetGenericType(buildInfo.Expression.Type) ??
			                     throw new InvalidOperationException();

			return new EnumerableContext(
				builder, 
				buildInfo, 
				buildInfo.SelectQuery, 
				collectionType.GetGenericArguments()[0]);
		}

		public SequenceConvertInfo? Convert(ExpressionBuilder builder, BuildInfo buildInfo, ParameterExpression? param)
			=> null;

		public bool IsSequence(ExpressionBuilder builder, BuildInfo buildInfo)
			=> true;
	}
}
