using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Linq.Parser
{
	public static class ParsingHelper
	{
		public static bool IsSameMethod(this MethodCallExpression mc, MethodInfo ethalon)
		{
			return mc.Method.IsSameMethod(ethalon);
		}

		public static MethodInfo EnsureDefinition(this MethodInfo mi)
		{
			if (mi.IsGenericMethod && mi.IsGenericMethod)
			{
				return mi.GetGenericMethodDefinition();
			}

			return mi;
		}

		public static bool IsOneOfMethods(this MethodCallExpression mc, params MethodInfo[] methods)
		{
			var mi = mc.Method;
			if (mi.IsGenericMethod && mi.IsGenericMethod)
			{
				mi = mi.GetGenericMethodDefinition();
			}

			foreach (var info in methods)
			{
				if (info == mi)
					return true;
			}

			return false;
		}

		public static bool IsSameMethod(this MethodInfo mi, MethodInfo ethalon)
		{
			if (mi == ethalon)
				return true;

			if (ethalon.IsGenericMethod && mi.IsGenericMethod)
			{
				return mi.GetGenericMethodDefinition() == ethalon;
			}

			return false;
		}

		public static ParameterExpression GetParameter(Expression expression)
		{
			var current = expression;
			while (current != null)
			{
				switch (current.NodeType)
				{
					case ExpressionType.Call:
						{
							current = ((MethodCallExpression)current).Object;
							break;
						} 

					case ExpressionType.MemberAccess:
						{
							current = ((MemberExpression)current).Expression;
							break;
						} 

					case ExpressionType.Parameter :
						return (ParameterExpression)current;

					default:
						return null;
				}
			}

			return null;
		}

	}
}
