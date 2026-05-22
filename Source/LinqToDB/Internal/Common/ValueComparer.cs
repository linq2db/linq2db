using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

using LinqToDB.Expressions;
using LinqToDB.Internal.Extensions;

namespace LinqToDB.Internal.Common
{
	/// <summary>
	/// This is internal API and is not intended for use by Linq To DB applications.
	/// It may change or be removed without further notice.
	/// </summary>
	public abstract class ValueComparer : IEqualityComparer, IEqualityComparer<object>
	{
		private protected static readonly MethodInfo _doubleEqualsMethodInfo
			= MemberHelper.MethodOf<double>(d => d.Equals(0));

		private protected static readonly MethodInfo _floatEqualsMethodInfo
			= MemberHelper.MethodOf<float>(d => d.Equals(0));

		internal static readonly MethodInfo EqualityComparerHashCodeMethod
			= MemberHelper.MethodOf<IEqualityComparer>(e => e.GetHashCode(0));

		internal static readonly MethodInfo EqualityComparerEqualsMethod
			= MemberHelper.MethodOf<IEqualityComparer>(c => c.Equals(0, 0));

		internal static readonly MethodInfo ObjectEqualsMethod
			= MemberHelper.MethodOf(() => object.Equals(0, 0));

		internal static readonly MethodInfo ObjectGetHashCodeMethod
			= MemberHelper.MethodOf<object>(e => e.GetHashCode());

		/// <summary>
		///     Creates a new <see cref="ValueComparer" /> with the given comparison.
		/// </summary>
		/// <param name="equalsExpression"> The comparison expression. </param>
		/// <param name="hashCodeExpression"> The associated hash code generator. </param>
		protected ValueComparer(
			LambdaExpression equalsExpression,
			LambdaExpression hashCodeExpression)
		{
			EqualsExpression = equalsExpression;
			HashCodeExpression = hashCodeExpression;
		}

		/// <summary>
		///     The type.
		/// </summary>
		public abstract Type Type { get; }

		/// <summary>
		///     Compares the two instances to determine if they are equal.
		/// </summary>
		/// <param name="x"> The first instance. </param>
		/// <param name="y"> The second instance. </param>
		/// <returns> <see langword="true" /> if they are equal; <see langword="false" /> otherwise. </returns>
		public new abstract bool Equals(object? x, object? y);

		/// <summary>
		///     Returns the hash code for the given instance.
		/// </summary>
		/// <param name="obj"> The instance. </param>
		/// <returns> The hash code. </returns>
		public abstract int GetHashCode(object? obj);

		/// <summary>
		///     The comparison expression.
		/// </summary>
		public virtual LambdaExpression EqualsExpression { get; }

		/// <summary>
		///     The hash code expression.
		/// </summary>
		public virtual LambdaExpression HashCodeExpression { get; }

		/// <summary>
		///     Takes <see cref="EqualsExpression" /> and replaces the two parameters with the given expressions,
		///     returning the transformed body.
		/// </summary>
		/// <param name="leftExpression"> The new left expression. </param>
		/// <param name="rightExpression"> The new right expression. </param>
		/// <returns> The body of the lambda with left and right parameters replaced.</returns>
		public virtual Expression ExtractEqualsBody(
			Expression leftExpression,
			Expression rightExpression)
		{
			return EqualsExpression.GetBody(leftExpression, rightExpression);
		}

		/// <summary>
		///     Creates a default <see cref="ValueComparer{T}" /> for the given type.
		/// </summary>
		/// <param name="type"> The type. </param>
		/// <param name="favorStructuralComparisons">
		///     If <see langword="true" />, then <see cref="IStructuralEquatable" /> will be used if the type
		///     implements it. This is usually used when byte arrays act as keys.
		/// </param>
		/// <returns> The <see cref="ValueComparer{T}" />. </returns>
		public static ValueComparer CreateDefault(Type type, bool favorStructuralComparisons)
		{
			var nonNullabletype = type.UnwrapNullableType();

			// The equality operator returns false for NaNs, but the Equals methods returns true
			if (nonNullabletype == typeof(double))
			{
				return new DefaultDoubleValueComparer(favorStructuralComparisons);
			}

			if (nonNullabletype == typeof(float))
			{
				return new DefaultFloatValueComparer(favorStructuralComparisons);
			}

			var comparerType = nonNullabletype.IsIntegerType
				|| nonNullabletype == typeof(decimal)
				|| nonNullabletype == typeof(bool)
				|| nonNullabletype == typeof(string)
				|| nonNullabletype == typeof(DateTime)
				|| nonNullabletype == typeof(Guid)
				|| nonNullabletype == typeof(DateTimeOffset)
				|| nonNullabletype == typeof(TimeSpan)
					? typeof(DefaultValueComparer<>)
					: typeof(ValueComparer<>);

			return ActivatorExt.CreateInstance<ValueComparer>(
				comparerType.MakeGenericType(type),
				new object[] { favorStructuralComparisons });
		}

		static readonly ConcurrentDictionary<(Type Type, bool FavorStructuralComparisons), ValueComparer> _defaultValueComparers = new();

		public static ValueComparer GetDefaultValueComparer(Type type, bool favorStructuralComparisons)
		{
			return _defaultValueComparers.GetOrAdd(
				(type, favorStructuralComparisons),
				static t => CreateDefault(t.Type, t.FavorStructuralComparisons)
			);
		}

		public static ValueComparer<T> GetDefaultValueComparer<T>(bool favorStructuralComparisons)
		{
			return (ValueComparer<T>)GetDefaultValueComparer(typeof(T), favorStructuralComparisons);
		}

		internal class DefaultValueComparer<T> : ValueComparer<T>
		{
			public DefaultValueComparer(bool favorStructuralComparisons)
				: base(favorStructuralComparisons)
			{
			}

			public override Expression ExtractEqualsBody(Expression leftExpression, Expression rightExpression)
				=> Expression.Equal(leftExpression, rightExpression);
		}

		internal sealed class DefaultDoubleValueComparer : DefaultValueComparer<double>
		{
			public DefaultDoubleValueComparer(bool favorStructuralComparisons)
				: base(favorStructuralComparisons)
			{
			}

			public override Expression ExtractEqualsBody(Expression leftExpression, Expression rightExpression)
				=> Expression.Call(leftExpression, _doubleEqualsMethodInfo, rightExpression);
		}

		internal sealed class DefaultFloatValueComparer : DefaultValueComparer<float>
		{
			public DefaultFloatValueComparer(bool favorStructuralComparisons)
				: base(favorStructuralComparisons)
			{
			}

			public override Expression ExtractEqualsBody(Expression leftExpression, Expression rightExpression)
				=> Expression.Call(leftExpression, _floatEqualsMethodInfo, rightExpression);
		}
	}
}
