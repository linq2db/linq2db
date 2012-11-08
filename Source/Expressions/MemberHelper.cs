using System;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Expressions
{
	public class MemberHelper
	{
		public static MemberInfo GetMemeberInfo(LambdaExpression func)
		{
			var ex = func.Body;

			if (ex is UnaryExpression)
				ex = ((UnaryExpression)ex).Operand;

			return
				ex is MemberExpression     ? ((MemberExpression)    ex).Member :
				ex is MethodCallExpression ? ((MethodCallExpression)ex).Method :
				                 (MemberInfo)((NewExpression)       ex).Constructor;
		}

		public static MemberInfo MemberOf<T>(Expression<Func<T,object>> func)
		{
			return GetMemeberInfo(func);
		}

		public static FieldInfo FieldOf<T>(Expression<Func<T,object>> func)
		{
			return (FieldInfo)GetMemeberInfo(func);
		}

		public static PropertyInfo PropertyOf<T>(Expression<Func<T,object>> func)
		{
			return (PropertyInfo)GetMemeberInfo(func);
		}

		public static MethodInfo MethodOf<T>(Expression<Func<T,object>> func)
		{
			var mi = GetMemeberInfo(func);
			return mi is PropertyInfo ? ((PropertyInfo)mi).GetGetMethod() : (MethodInfo)mi;
		}
	}
}
