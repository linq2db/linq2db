using System;
using System.Linq.Expressions;

using LinqToDB.Expressions;
using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.Reflection;
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
		/// instance type by name. Dynamic columns (<c>[DynamicColumnsStore]</c> /
		/// <c>Sql.Property&lt;T&gt;</c>) resolve to a <c>Sql.Property&lt;T&gt;(instance, name)</c> call
		/// regardless of <paramref name="instance"/>'s type — the raw dynamic-column getter throws
		/// "Dynamic column getter is not to be called" and isn't SQL-convertible, whereas the
		/// <c>Sql.Property</c> shape both converts to a column reference and structurally matches a
		/// user <c>x =&gt; Sql.Property&lt;T&gt;(x, name)</c> selector after canonicalisation.
		/// </remarks>
		/// <exception cref="InvalidOperationException">
		/// Thrown when no matching member exists on <paramref name="instance"/>'s type.
		/// </exception>
		public static Expression GetMemberAccessExpression(this ColumnDescriptor descriptor, Expression instance)
		{
			// Dynamic columns have no real CLR member to walk to. When the instance is the entity root,
			// emit a member access against the DynamicColumnInfo: that is exactly the shape the expose
			// pipeline rewrites a user `Sql.Property<T>(x, name)` selector into, so the result compares
			// structurally equal to a canonicalised user selector (needed for .Set/.Ignore override
			// matching), and the table builder resolves the dynamic member to its column. When the
			// instance type differs from the entity root (e.g. a MERGE source row of another type),
			// Expression.MakeMemberAccess would throw, so fall back to Sql.Property<T>(instance, name) —
			// the form GetMemberGetter produces — which carries no declaring-type constraint.
			if (descriptor.MemberInfo.IsDynamicColumnProperty)
				return instance.Type == descriptor.MemberAccessor.TypeAccessor.Type
					? Expression.MakeMemberAccess(instance, descriptor.MemberInfo)
					: Expression.Call(
						Methods.LinqToDB.SqlExt.Property.MakeGenericMethod(descriptor.MemberType),
						instance,
						Expression.Constant(descriptor.MemberName));

			// IsComplex is true only for nested member paths and dynamic columns. Gating on it (rather than
			// MemberName.Contains('.')) keeps explicit-interface members — whose CLR name itself contains
			// dots — on the leaf-lookup branch below. Dynamic columns are already handled above.
			if (descriptor.MemberAccessor.IsComplex
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
