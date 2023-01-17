﻿using System;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Expressions
{
	using LinqToDB.Extensions;
	using Mapping;
	using Reflection;

	public static class MemberHelper
	{
		[DebuggerDisplay("{Type.Name}.{MemberInfo.Name}")]
		public struct MemberInfoWithType
		{
			public MemberInfoWithType(Type? type, MemberInfo memberInfo)
			{
				Type       = type;
				MemberInfo = memberInfo;
			}

			public Type?      Type;
			public MemberInfo MemberInfo;

			public bool Equals(MemberInfoWithType other)
			{
				return Equals(Type, other.Type) && MemberInfo.Equals(other.MemberInfo);
			}

			public override bool Equals(object? obj)
			{
				return obj is MemberInfoWithType other && Equals(other);
			}

			public override int GetHashCode()
			{
				unchecked
				{
					return ((Type != null ? Type.GetHashCode() : 0) * 397) ^ MemberInfo.GetHashCode();
				}
			}
		}

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
		/// Gets the member information with type from given lambda expression. <seealso cref="GetMemberInfo(Expression)" />
		/// </summary>
		/// <param name="func">The lambda expression.</param>
		/// <returns></returns>
		/// <exception cref="ArgumentException">Only simple, non-navigational, member names are supported in this context (e.g.: x =&gt; Sql.Property(x, \"SomeProperty\")).</exception>
		public static MemberInfoWithType GetMemberInfoWithType(LambdaExpression func)
		{
			return GetMemberInfoWithType(func.Body);
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
			return GetMemberInfoWithType(expr).MemberInfo;
		}

		/// <summary>
		/// Gets the member information with type from given expression.
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
		public static MemberInfoWithType GetMemberInfoWithType(Expression expr)
		{
			while (expr.NodeType == ExpressionType.Convert || expr.NodeType == ExpressionType.ConvertChecked || expr.NodeType == ExpressionType.TypeAs)
				expr = ((UnaryExpression)expr).Operand;

			if (expr.NodeType == ExpressionType.New)
				return new MemberInfoWithType(expr.Type, ((NewExpression)expr).Constructor!);

			if (expr is MethodCallExpression methodCall && methodCall.Method.IsSqlPropertyMethodEx())
			{
				// validate expression and get member name
				var objectExpr = methodCall.Arguments[0].UnwrapConvert();
				var memberName = methodCall.Arguments[1].EvaluateExpression() as string
#pragma warning disable CA2208 // Instantiate argument exceptions correctly
					?? throw new ArgumentNullException("propertyName");
#pragma warning restore CA2208 // Instantiate argument exceptions correctly

				// check if member exists on type
				var existingMember = TypeAccessor.GetAccessor(objectExpr.Type).Members.SingleOrDefault(m =>
					m.Name == memberName &&
					(m.MemberInfo.MemberType == MemberTypes.Property || m.MemberInfo.MemberType == MemberTypes.Field));

				if (existingMember != null)
					return new MemberInfoWithType(objectExpr.Type, existingMember.MemberInfo);

				// create dynamic column info
				return new MemberInfoWithType(objectExpr.Type, new DynamicColumnInfo(objectExpr.Type, methodCall.Method.GetGenericArguments()[0], memberName));
			}

			if (expr.NodeType == ExpressionType.ArrayLength)
				return new MemberInfoWithType(((UnaryExpression)expr).Operand.Type, ((UnaryExpression)expr).Operand.Type.GetProperty(nameof(Array.Length))!);

			return
				expr is MemberExpression me
					? new MemberInfoWithType(me.Expression?.Type, me.Member)
					: expr is MethodCallExpression mce
						? new MemberInfoWithType(mce.Object?.Type ?? mce.Method.ReflectedType, mce.Method)
						: new MemberInfoWithType(expr.Type, (MemberInfo)((NewExpression)expr).Constructor!);
		}

		public static MemberInfo MemberOf<T>(Expression<Func<T,object?>> func)
		{
			return GetMemberInfo(func);
		}

		public static MemberInfo MemberOf<T, TMember>(Expression<Func<T,TMember>> func)
		{
			return GetMemberInfo(func);
		}

		public static FieldInfo FieldOf<T>(Expression<Func<T,object?>> func)
		{
			return (FieldInfo)GetMemberInfo(func);
		}

		public static PropertyInfo PropertyOf<T>(Expression<Func<T,object?>> func)
		{
			return (PropertyInfo)GetMemberInfo(func);
		}

		public static MethodInfo MethodOf<T>(Expression<Func<T,object?>> func)
		{
			var mi = GetMemberInfo(func);
			return mi is PropertyInfo info ? info.GetGetMethod()! : (MethodInfo)mi;
		}

		public static MethodInfo MethodOf<T1, T2>(Expression<Func<T1, T2,object?>> func)
		{
			var mi = GetMemberInfo(func);
			return mi is PropertyInfo info ? info.GetGetMethod()! : (MethodInfo)mi;
		}

		public static MethodInfo MethodOf<T1, T2, T3>(Expression<Func<T1, T2, T3, object?>> func)
		{
			var mi = GetMemberInfo(func);
			return mi is PropertyInfo info ? info.GetGetMethod()! : (MethodInfo)mi;
		}

		public static MethodInfo MethodOf<T1, T2, T3, T4>(Expression<Func<T1, T2, T3, T4, object?>> func)
		{
			var mi = GetMemberInfo(func);
			return mi is PropertyInfo info ? info.GetGetMethod()! : (MethodInfo)mi;
		}

		public static MethodInfo MethodOf(Expression<Func<object?>> func)
		{
			var mi = GetMemberInfo(func);
			return mi is PropertyInfo info ? info.GetGetMethod()! : (MethodInfo)mi;
		}

		public static MethodInfo MethodOf(Expression<Action> func)
		{
			var mi = GetMemberInfo(func);
			return mi is PropertyInfo info ? info.GetGetMethod()! : (MethodInfo)mi;
		}

		public static MethodInfo MethodOfGeneric<T>(Expression<Func<T,object?>> func)
		{
			var mi = MethodOf(func);
			if (mi.IsGenericMethod)
				mi = mi.GetGenericMethodDefinition();
			return mi;
		}

		public static MethodInfo MethodOfGeneric<T1, T2>(Expression<Func<T1, T2, object?>> func)
		{
			var mi = MethodOf(func);
			if (mi.IsGenericMethod)
				mi = mi.GetGenericMethodDefinition();
			return mi;
		}

		public static MethodInfo MethodOfGeneric<T1, T2, T3>(Expression<Func<T1, T2, T3, object?>> func)
		{
			var mi = MethodOf(func);
			if (mi.IsGenericMethod)
				mi = mi.GetGenericMethodDefinition();
			return mi;
		}

		public static MethodInfo MethodOfGeneric<T1, T2, T3, T4>(Expression<Func<T1, T2, T3, T4, object?>> func)
		{
			var mi = MethodOf(func);
			if (mi.IsGenericMethod)
				mi = mi.GetGenericMethodDefinition();
			return mi;
		}

		public static MethodInfo MethodOfGeneric(Expression<Func<object?>> func)
		{
			var mi = MethodOf(func);
			if (mi.IsGenericMethod)
				mi = mi.GetGenericMethodDefinition();
			return mi;
		}

		public static MethodInfo MethodOfGeneric(Expression<Action> func)
		{
			var mi = MethodOf(func);
			if (mi.IsGenericMethod)
				mi = mi.GetGenericMethodDefinition();
			return mi;
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
