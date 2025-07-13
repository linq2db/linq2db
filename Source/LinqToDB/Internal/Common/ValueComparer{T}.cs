using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using LinqToDB.Common;
using LinqToDB.Internal.Extensions;

namespace LinqToDB.Internal.Common
{
	/// <summary>
	/// This is internal API and is not intended for use by Linq To DB applications.
	/// It may change or be removed without further notice.
	/// </summary>
	public class ValueComparer<T> : ValueComparer, IEqualityComparer<T>
	{
		private Func<T?, T?, bool>? _equals;
		private Func<T?, int>?      _hashCode;

		/// <summary>
		///     Creates a new <see cref="ValueComparer{T}" /> with a default comparison
		///     expression and a shallow copy for the snapshot.
		/// </summary>
		/// <param name="favorStructuralComparisons">
		///     If <see langword="true" />, then EF will use <see cref="IStructuralEquatable" /> if the type
		///     implements it. This is usually used when byte arrays act as keys.
		/// </param>
		public ValueComparer(bool favorStructuralComparisons)
			: this(
				CreateDefaultEqualsExpression(),
				CreateDefaultHashCodeExpression(favorStructuralComparisons))
		{
		}

		/// <summary>
		///     Creates a new <see cref="ValueComparer{T}" /> with the given comparison expression.
		///     A shallow copy will be used for the snapshot.
		/// </summary>
		/// <param name="equalsExpression"> The comparison expression. </param>
		/// <param name="hashCodeExpression"> The associated hash code generator. </param>
		public ValueComparer(
			Expression<Func<T, T, bool>> equalsExpression,
			Expression<Func<T, int>> hashCodeExpression)
			: base(equalsExpression, hashCodeExpression)
		{
		}

		/// <summary>
		///     Creates an expression for equality.
		/// </summary>
		/// <returns> The equality expression. </returns>
		protected static Expression<Func<T, T, bool>> CreateDefaultEqualsExpression()
		{
			var type = typeof(T);
			var param1 = Expression.Parameter(type, "v1");
			var param2 = Expression.Parameter(type, "v2");

			if (typeof(IStructuralEquatable).IsAssignableFrom(type))
			{
				return Expression.Lambda<Func<T, T, bool>>(
					Expression.Call(
						Expression.Constant(StructuralComparisons.StructuralEqualityComparer, typeof(IEqualityComparer)),
						EqualityComparerEqualsMethod,
						Expression.Convert(param1, typeof(object)),
						Expression.Convert(param2, typeof(object))
					),
					param1, param2);
			}

			var unwrappedType = type.UnwrapNullableType();
			if (unwrappedType.IsInteger()
				|| unwrappedType == typeof(string)
				|| unwrappedType == typeof(Guid)
				|| unwrappedType == typeof(bool)
				|| unwrappedType == typeof(decimal)
				|| unwrappedType == typeof(object)
			)
			{
				return Expression.Lambda<Func<T, T, bool>>(
					Expression.Equal(param1, param2),
					param1, param2);
			}

			var typedEquals = type.GetRuntimeMethods().FirstOrDefault(
				m => m.ReturnType == typeof(bool)
					&& !m.IsStatic
					&& nameof(object.Equals).Equals(m.Name, StringComparison.Ordinal)
					&& m.GetParameters().Length == 1
					&& m.GetParameters()[0].ParameterType == typeof(T));

			while (typedEquals == null
				&& type != null)
			{
				var declaredMethods = type.GetTypeInfo().DeclaredMethods;
				typedEquals = declaredMethods.FirstOrDefault(
					m => m.IsStatic
						&& m.ReturnType == typeof(bool)
						&& "op_Equality".Equals(m.Name, StringComparison.Ordinal)
						&& m.GetParameters().Length == 2
						&& m.GetParameters()[0].ParameterType == typeof(T)
						&& m.GetParameters()[1].ParameterType == typeof(T));

				type = type.BaseType;
			}

			return Expression.Lambda<Func<T, T, bool>>(
				typedEquals == null
					? Expression.Call(
						ObjectEqualsMethod,
						Expression.Convert(param1, typeof(object)),
						Expression.Convert(param2, typeof(object)))
					: typedEquals.IsStatic
						? Expression.Call(typedEquals, param1, param2)
						: Expression.Call(param1, typedEquals, param2),
				param1, param2);
		}

		/// <summary>
		///     Creates an expression for generating a hash code.
		/// </summary>
		/// <param name="favorStructuralComparisons">
		///     If <see langword="true" />, then <see cref="IStructuralEquatable" /> is used if the type implements it.
		/// </param>
		/// <returns> The hash code expression. </returns>
		protected static Expression<Func<T, int>> CreateDefaultHashCodeExpression(bool favorStructuralComparisons)
		{
			var type          = typeof(T);
			var unwrappedType = type.UnwrapNullableType();
			var param         = Expression.Parameter(type, "v");

			if (favorStructuralComparisons
			    && typeof(IStructuralEquatable).IsAssignableFrom(type))
			{
				return Expression.Lambda<Func<T, int>>(
					Expression.Call(
						Expression.Constant(StructuralComparisons.StructuralEqualityComparer, typeof(IEqualityComparer)),
						EqualityComparerHashCodeMethod,
						Expression.Convert(param, typeof(object))
					),
					param);
			}

			var expression
				= type == typeof(int)
					? param
					: unwrappedType    == typeof(int)
					  || unwrappedType == typeof(short)
					  || unwrappedType == typeof(byte)
					  || unwrappedType == typeof(uint)
					  || unwrappedType == typeof(ushort)
					  || unwrappedType == typeof(sbyte)
					  || unwrappedType == typeof(char)
						? (Expression)Expression.Convert(param, typeof(int))
						: Expression.Call(
							Expression.Convert(param, typeof(object)), ObjectGetHashCodeMethod);

			if (type != unwrappedType || !type.IsValueType)
			{
				expression = Expression.Condition(Expression.NotEqual(param, Expression.Constant(null, param.Type)),
					expression, Expressions.ExpressionInstances.Constant0);
			}

			return Expression.Lambda<Func<T, int>>(expression, param);
		}

		/// <summary>
		///     Compares the two instances to determine if they are equal.
		/// </summary>
		/// <param name="x"> The first instance. </param>
		/// <param name="y"> The second instance. </param>
		/// <returns> <see langword="true" /> if they are equal; <see langword="false" /> otherwise. </returns>
		public override bool Equals(object? x, object? y)
		{
			var v1Null = x == null;
			var v2Null = y == null;

			return v1Null || v2Null ? v1Null && v2Null : Equals((T)x!, (T)y!);
		}

		/// <summary>
		///     Returns the hash code for the given instance.
		/// </summary>
		/// <param name="obj"> The instance. </param>
		/// <returns> The hash code. </returns>
		public override int GetHashCode(object? obj)
			=> obj == null ? 0 : GetHashCode((T)obj);

		/// <summary>
		///     Compares the two instances to determine if they are equal.
		/// </summary>
		/// <param name="x"> The first instance. </param>
		/// <param name="y"> The second instance. </param>
		/// <returns> <see langword="true" /> if they are equal; <see langword="false" /> otherwise. </returns>
		public virtual bool Equals(T? x, T? y)
			=> NonCapturingLazyInitializer.EnsureInitialized(
				ref _equals, this, c => c.EqualsExpression.CompileExpression())(x, y);

		/// <summary>
		///     Returns the hash code for the given instance.
		/// </summary>
		/// <param name="obj"> The instance. </param>
		/// <returns> The hash code. </returns>
		public virtual int GetHashCode(T? obj)
			=> NonCapturingLazyInitializer.EnsureInitialized(
				ref _hashCode, this, c => c.HashCodeExpression.CompileExpression())(obj);

		/// <summary>
		///     The type.
		/// </summary>
		public override Type Type
			=> typeof(T);

		/// <summary>
		///     The comparison expression.
		/// </summary>
		public new virtual Expression<Func<T?, T?, bool>> EqualsExpression
			=> (Expression<Func<T?, T?, bool>>)base.EqualsExpression;

		/// <summary>
		///     The hash code expression.
		/// </summary>
		public new virtual Expression<Func<T?, int>> HashCodeExpression
			=> (Expression<Func<T?, int>>)base.HashCodeExpression;

	}
}
