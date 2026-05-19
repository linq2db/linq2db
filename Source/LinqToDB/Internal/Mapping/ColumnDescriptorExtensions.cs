using System;
using System.Linq.Expressions;

using LinqToDB.Expressions;
using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Extensions;
using LinqToDB.Mapping;

namespace LinqToDB.Internal.Mapping
{
	public static class ColumnDescriptorExtensions
	{
		/// <summary>
		/// Builds a plain chained <see cref="MemberExpression"/> from <paramref name="instance"/>
		/// to the CLR member this column is mapped to, following the dot-path in
		/// <see cref="ColumnDescriptor.MemberName"/> when the mapping is nested
		/// (e.g. <c>o =&gt; o.Sub.Field</c>).
		/// </summary>
		/// <remarks>
		/// Unlike <see cref="MemberAccessor.GetGetterExpression"/>, the result has no
		/// null-check block wrapping intermediate reference-type members, so it is
		/// suitable for SQL conversion. When <paramref name="instance"/>'s CLR type
		/// doesn't match the column's entity root (e.g. <c>Merge().Using&lt;TSource&gt;()</c>
		/// with TSource ≠ TTarget), the column's leaf member is looked up on the
		/// instance type by name.
		/// </remarks>
		/// <exception cref="InvalidOperationException">
		/// Thrown when no matching member exists on <paramref name="instance"/>'s type.
		/// </exception>
		public static Expression GetMemberAccessExpression(this ColumnDescriptor descriptor, Expression instance)
		{
			var memberName = descriptor.MemberName;

			if (memberName.IndexOf('.') >= 0
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
