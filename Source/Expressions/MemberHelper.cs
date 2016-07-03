using System;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Expressions
{
	using LinqToDB.Extensions;

	public static class MemberHelper
	{
		public static MemberInfo GetMemberInfo(LambdaExpression func)
		{
			var ex = func.Body;

			if (ex is UnaryExpression)
				ex = ((UnaryExpression)ex).Operand;

			if (ex.NodeType == ExpressionType.New)
				return ((NewExpression)ex).Constructor;

			return
				ex is MemberExpression     ? ((MemberExpression)    ex).Member :
				ex is MethodCallExpression ? ((MethodCallExpression)ex).Method :
				                 (MemberInfo)((NewExpression)       ex).Constructor;
		}

		public static MemberInfo MemberOf<T>(Expression<Func<T,object>> func)
		{
			return GetMemberInfo(func);
		}

		public static FieldInfo FieldOf<T>(Expression<Func<T,object>> func)
		{
			return (FieldInfo)GetMemberInfo(func);
		}

		public static PropertyInfo PropertyOf<T>(Expression<Func<T,object>> func)
		{
			return (PropertyInfo)GetMemberInfo(func);
		}

		public static MethodInfo MethodOf<T>(Expression<Func<T,object>> func)
		{
			var mi = GetMemberInfo(func);
			return mi is PropertyInfo ? ((PropertyInfo)mi).GetGetMethodEx() : (MethodInfo)mi;
		}

		public static MethodInfo MethodOf(Expression<Func<object>> func)
		{
			var mi = GetMemberInfo(func);
			return mi is PropertyInfo ? ((PropertyInfo)mi).GetGetMethodEx() : (MethodInfo)mi;
		}

		public static ConstructorInfo ConstructorOf<T>(Expression<Func<T,object>> func)
		{
			return (ConstructorInfo)GetMemberInfo(func);
		}

		public static ConstructorInfo ConstructorOf(Expression<Func<object>> func)
		{
			return (ConstructorInfo)GetMemberInfo(func);
		}
	}
}
