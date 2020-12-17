using System;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Expressions
{
	public static class ExpressionHelper
	{
		public static Expression TrueConstant  = Expression.Constant(true);
		public static Expression FalseConstant = Expression.Constant(false);
		
		/// <summary>
		/// Compared to <see cref="Expression.Field(Expression, string)"/>, performs case-sensitive field search.
		/// </summary>
		public static MemberExpression Field(Expression obj, string name)
		{
			var fi = obj.Type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);

			if (fi == null)
				fi = obj.Type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
			
			if (fi == null)
				throw new InvalidOperationException($"Instance field with name {name} not found on type {obj.Type}");

			return Expression.Field(obj, fi);
		}

		/// <summary>
		/// Compared to <see cref="Expression.Field(Expression, Type, string)"/>, performs case-sensitive field search and search
		/// only for static fields.
		/// </summary>
		public static MemberExpression Field(Type type, string name)
		{
			var fi = type.GetField(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy);

			if (fi == null)
				fi = type.GetField(name, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);

			if (fi == null)
				throw new InvalidOperationException($"Static field with name {name} not found on type {type}");

			return Expression.Field(null, fi);
		}

		/// <summary>
		/// Compared to <see cref="Expression.Property(Expression, string)"/>, performs case-sensitive property search.
		/// </summary>
		public static MemberExpression Property(Expression obj, string name)
		{
			var pi = obj.Type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);

			if (pi == null)
				pi = obj.Type.GetProperty(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);

			if (pi == null)
				throw new InvalidOperationException($"Instance property with name {name} not found on type {obj.Type}");

			return Expression.Property(obj, pi);
		}

		/// <summary>
		/// Compared to <see cref="Expression.Property(Expression, Type, string)"/>, performs case-sensitive property search and search
		/// only for static properties.
		/// </summary>
		public static MemberExpression Property(Type type, string name)
		{
			var pi = type.GetProperty(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy);

			if (pi == null)
				pi = type.GetProperty(name, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);

			if (pi == null)
				throw new InvalidOperationException($"Static property with name {name} not found on type {type}");

			return Expression.Property(null, pi);
		}

		/// <summary>
		/// Compared to <see cref="Expression.PropertyOrField(Expression, string)"/>, performs case-sensitive member search.
		/// </summary>
		public static MemberExpression PropertyOrField(Expression obj, string name)
		{
			var pi = obj.Type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);

			if (pi != null)
				return Expression.Property(obj, pi);

			var fi = obj.Type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);
			if (fi != null)
				return Expression.Field(obj, fi);

			pi = obj.Type.GetProperty(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
			if (pi != null)
				return Expression.Property(obj, pi);

			fi = obj.Type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
			if (fi != null)
				return Expression.Field(obj, fi);

			throw new InvalidOperationException($"Instance property or field with name {name} not found on type {obj.Type}");
		}
	}
}
