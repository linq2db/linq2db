using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.Expressions.ExpressionVisitors
{
	internal class ContainsFilterConstantVisitor : ExpressionVisitor
	{
		public ContainsFilterConstantVisitor()
		{
		}

		protected override Expression VisitConstant(ConstantExpression node)
		{
			var sourceList = node.Value;
			var sourceListType = node.Value?.GetType();

			if (sourceList == null || sourceListType == null)
			{
				return base.VisitConstant(node);
			}

			if (!typeof(IEnumerable).IsAssignableFrom(sourceListType))
			{
				return base.VisitConstant(node);
			}

			// find out generic argument type
			var t = sourceListType.GenericTypeArguments != null && sourceListType.GenericTypeArguments.Length == 1
							? sourceListType.GenericTypeArguments[0]
							: sourceListType.GetInterfaces()
								.Where(t => t.IsGenericType
									&& t.GetGenericTypeDefinition() == typeof(IEnumerable<>))
								.Select(t => t.GetGenericArguments()[0]).FirstOrDefault();

			if (t == typeof(int))
			{
				var list = (sourceList as IEnumerable<int>)?.ToArray();

				if (list == null)
				{
					return base.VisitConstant(node);
				}

				return Expression.Constant(list);
			}
			else if (t == typeof(string))
			{
				var list = (sourceList as IEnumerable<string>)?.ToArray();

				if (list == null)
				{
					return base.VisitConstant(node);
				}

				return Expression.Constant(list);
			}
			else
			{
				return base.VisitConstant(node);
			}
		}
	}
}
