using System;
using System.Linq.Expressions;

using LinqToDB.Expressions;
using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Extensions;
using LinqToDB.Mapping;
using LinqToDB.Reflection;

namespace LinqToDB.Internal.Mapping
{
	public static class ColumnDescriptorExtensions
	{
		/// <summary>
		/// Builds a member-access expression from <paramref name="instance"/> to the CLR
		/// member this column is mapped to, following the dot-path in
		/// <see cref="ColumnDescriptor.MemberName"/> when the mapping is nested
		/// (e.g. <c>o =&gt; o.Sub.Field</c>).
		/// </summary>
		/// <remarks>
		/// Unlike <see cref="MemberAccessor.GetGetterExpression(Expression)"/>, the result has no
		/// null-check block wrapping intermediate reference-type members, so it is
		/// suitable for SQL conversion. When <paramref name="instance"/>'s CLR type
		/// doesn't match the column's entity root (e.g. <c>Merge().Using&lt;TSource&gt;()</c>
		/// with TSource ≠ TTarget), the column's leaf member is looked up on the
		/// instance type by name; for dynamic columns the call is forwarded through
		/// <see cref="ExpressionExtensions.GetMemberGetter"/>, which may return a
		/// <see cref="MethodCallExpression"/>.
		/// </remarks>
		/// <exception cref="InvalidOperationException">
		/// Thrown when no matching member exists on <paramref name="instance"/>'s type.
		/// </exception>
		public static Expression GetMemberAccessExpression(this ColumnDescriptor descriptor, Expression instance)
		{
			var memberName = descriptor.MemberName;

			if (memberName.Contains('.', StringComparison.Ordinal)
				&& instance.Type == descriptor.MemberAccessor.TypeAccessor.Type)
			{
				var expr = instance;
				foreach (var part in memberName.Split('.'))
					expr = ExpressionHelper.PropertyOrField(expr, part);
				return expr;
			}

			var leaf = instance.Type.GetMemberEx(descriptor.MemberInfo);
			if (leaf is null)
				throw new InvalidOperationException(
					$"Member '{descriptor.MemberInfo}' not found in type '{instance.Type}'.");

			return ExpressionExtensions.GetMemberGetter(leaf, instance);
		}
	}
}
