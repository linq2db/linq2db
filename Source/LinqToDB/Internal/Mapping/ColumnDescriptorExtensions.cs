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
			// IsComplex is true only for nested member paths and dynamic columns. Gating on it (rather than
			// MemberName.Contains('.')) keeps explicit-interface members — whose CLR name itself contains
			// dots — on the leaf-lookup branch below; dynamic columns are excluded there too and resolved
			// via GetMemberGetter. Mirrors the canonical guard at TableBuilder.TableContext.cs.
			if (descriptor.MemberAccessor.IsComplex
				&& !descriptor.MemberInfo.IsDynamicColumnProperty
				&& instance.Type == descriptor.MemberAccessor.TypeAccessor.Type)
			{
				var expr = instance;
				foreach (var part in descriptor.MemberName.Split('.'))
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
