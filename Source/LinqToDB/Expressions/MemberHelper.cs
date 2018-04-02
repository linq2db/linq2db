using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using LinqToDB.Mapping;
using LinqToDB.Reflection;

namespace LinqToDB.Expressions
{
	using LinqToDB.Extensions;

	public static class MemberHelper
	{
		/// <summary>
		/// Gets the member information from given lambda expression. <seealso cref="GetMemberInfo(Expression)" />
		/// </summary>
		/// <param name="func">The lambda expression.</param>
		/// <returns></returns>
		/// <exception cref="ArgumentException">Only simple, non-navigational, member names are supported in this context (e.g.: x =&gt; Sql.Property(x, \"SomeProperty\")).</exception>
		public static MemberInfo GetMemberInfo(LambdaExpression func)
		{
			return GetMemberInfo(func.Body);
		}

		/// <summary>
		/// Gets the member information from given expression.
		/// </summary>
		/// <remarks>
		/// Returns member information for given expressions, e.g.:
		/// <list type="bullet">
		/// <item><description>For: x =&gt; x.SomeProperty, returns MemberInfo of SomeProperty.</description></item>
		/// <item><description>For: x =&gt; x.SomeMethod(), returns MethodInfo of SomeMethod.</description></item>
		/// <item><description>For: x =&gt; new { X = x.Name }, return ConstructorInfo of anonymous type.</description></item>
		/// <item><description>For: x =&gt; Sql.Property&lt;int&gt;(x, "SomeProperty"), returns MemberInfo of "SomeProperty" if exists on type, otherwise returns DynamicColumnInfo for SomeProperty on given type.</description></item>
		/// </list>
		/// </remarks>
		/// <param name="expr">The expression.</param>
		/// <returns></returns>
		/// <exception cref="ArgumentException">Only simple, non-navigational, member names are supported in this context (e.g.: x =&gt; Sql.Property(x, \"SomeProperty\")).</exception>
		public static MemberInfo GetMemberInfo(Expression expr)
		{
			while (expr.NodeType == ExpressionType.Convert || expr.NodeType == ExpressionType.ConvertChecked)
				expr = ((UnaryExpression)expr).Operand;

			if (expr.NodeType == ExpressionType.New)
				return ((NewExpression)expr).Constructor;

			if (expr is MethodCallExpression methodCall && methodCall.Method.IsSqlPropertyMethodEx())
			{
				// validate expression and get member name
				var arg1 = methodCall.Arguments[0].NodeType == ExpressionType.Convert
					? ((UnaryExpression)methodCall.Arguments[0]).Operand
					: methodCall.Arguments[0];

				if (arg1.NodeType != ExpressionType.Constant && arg1.NodeType != ExpressionType.Parameter || methodCall.Arguments[1].NodeType != ExpressionType.Constant)
					throw new ArgumentException("Only simple, non-navigational, member names are supported in this context (e.g.: x => Sql.Property(x, \"SomeProperty\")).");

				var memberName = (string)((ConstantExpression)methodCall.Arguments[1]).Value;

				// check if member exists on type
				var existingMember = TypeAccessor.GetAccessor(arg1.Type).Members.SingleOrDefault(m =>
					m.Name == memberName &&
					(m.MemberInfo.MemberType == MemberTypes.Property || m.MemberInfo.MemberType == MemberTypes.Field));

				if (existingMember != null)
					return existingMember.MemberInfo;
				
#if !NETSTANDARD1_6
				// create dynamic column info
				return new DynamicColumnInfo(arg1.Type, methodCall.Method.GetGenericArguments()[0], memberName);
#else
				throw new NotSupportedException("Dynamic columns are not supported on .NET Standard 1.6.");
#endif
			}

			return
				expr is MemberExpression     ? ((MemberExpression)    expr).Member :
				expr is MethodCallExpression ? ((MethodCallExpression)expr).Method :
				                 (MemberInfo)((NewExpression)         expr).Constructor;
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
