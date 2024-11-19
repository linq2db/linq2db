using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Expressions
{
	public static class ExpressionHelper
	{
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

		/// <summary>
		/// Get the same <see cref="MemberInfo"/> as the <see cref="PropertyOrField(Expression, string)"/> method
		/// </summary>
		/// <param name="type"></param>
		/// <param name="name"></param>
		/// <returns></returns>
		/// <exception cref="InvalidOperationException"></exception>
		internal static MemberInfo GetPropertyOrFieldMemberInfo(Type type, string name)
		{
			var pi = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);

			if (pi != null)
				return pi;

			var fi = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);
			if (fi != null)
				return fi;

			pi = type.GetProperty(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
			if (pi != null)
				return pi;

			fi = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
			if (fi != null)
				return fi;

			throw new InvalidOperationException($"Instance property or field with name {name} not found on type {type}");
		}

		/// <summary>
		/// Compared to <see cref="Expression.PropertyOrField(Expression, string)"/>, performs case-sensitive member search.
		/// </summary>
		public static MemberExpression PropertyOrField(Type type, string name, bool allowInherited = true)
		{
			var flags = BindingFlags.Static | BindingFlags.Public;
			if (allowInherited)
				flags |= BindingFlags.FlattenHierarchy;

			var pi = type.GetProperty(name, flags);

			if (pi != null)
				return Expression.Property(null, pi);

			var fi = type.GetField(name, flags);
			if (fi != null)
				return Expression.Field(null, fi);

			flags = BindingFlags.Static | BindingFlags.NonPublic;
			if (allowInherited)
				flags |= BindingFlags.FlattenHierarchy;

			pi = type.GetProperty(name, flags);
			if (pi != null)
				return Expression.Property(null, pi);

			fi = type.GetField(name, flags);
			if (fi != null)
				return Expression.Field(null, fi);

			throw new InvalidOperationException($"Static property or field with name {name} not found on type {type}");
		}
	}
}
