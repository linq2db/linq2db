using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;

using JetBrains.Annotations;

using LinqToDB.Expressions;
using LinqToDB.Extensions;
using LinqToDB.Reflection;

namespace LinqToDB.Tools.Comparers
{
	/// <summary>
	/// Builds comparer functions and comparers.
	/// </summary>
	[PublicAPI]
	public static class ComparerBuilder
	{
		/// <summary>
		/// Returns GetEqualsFunc function for type T to compare.
		/// </summary>
		/// <returns>GetEqualsFunc function.</returns>
		/// <typeparam name="T">The type of objects to compare.</typeparam>
		[NotNull, Pure]
		public static Func<T,T,bool> GetEqualsFunc<T>()
			=> GetEqualsFunc<T>(TypeAccessor.GetAccessor<T>().Members);

		/// <summary>
		/// Returns GetEqualsFunc function for provided members for type T to compare.
		/// </summary>
		/// <param name="members">Members to compare.</param>
		/// <returns>GetEqualsFunc function.</returns>
		/// <typeparam name="T">The type of objects to compare.</typeparam>
		[NotNull, Pure]
		public static Func<T,T,bool> GetEqualsFunc<T>([NotNull, InstantHandle] IEnumerable<MemberAccessor> members)
			=> CreateEqualsFunc<T>(members.Select(m => m.GetterExpression));

		/// <summary>
		/// Returns GetEqualsFunc function for provided members for type T to compare.
		/// </summary>
		/// <param name="members">Members to compare.</param>
		/// <returns>GetEqualsFunc function.</returns>
		/// <typeparam name="T">The type of objects to compare.</typeparam>
		[NotNull, Pure]
		public static Func<T,T,bool> GetEqualsFunc<T>([NotNull] params Expression<Func<T,object>>[] members)
			=> CreateEqualsFunc<T>(members);

		/// <summary>
		/// Returns GetHashCode function for type T to compare.
		/// </summary>
		/// <returns>GetHashCode function.</returns>
		/// <typeparam name="T">The type of objects to compare.</typeparam>
		[NotNull, Pure]
		public static Func<T,int> GetGetHashCodeFunc<T>()
			=> GetGetHashCodeFunc<T>(TypeAccessor.GetAccessor<T>().Members);

		static readonly int _randomSeed = new Random(unchecked((int)DateTime.Now.Ticks)).Next();

		/// <summary>
		/// Returns GetHashCode function for provided members for type T to compare.
		/// </summary>
		/// <param name="members">Members to compare.</param>
		/// <returns>GetHashCode function.</returns>
		/// <typeparam name="T">The type of objects to compare.</typeparam>
		[NotNull, Pure]
		public static Func<T,int> GetGetHashCodeFunc<T>([NotNull, InstantHandle] IEnumerable<MemberAccessor> members)
			=> CreateGetHashCodeFunc<T>(members.Select(m => m.GetterExpression));

		/// <summary>
		/// Returns GetHashCode function for provided members for type T to compare.
		/// </summary>
		/// <param name="members">Members to compare.</param>
		/// <returns>GetHashCode function.</returns>
		/// <typeparam name="T">The type of objects to compare.</typeparam>
		[NotNull, Pure]
		public static Func<T,int> GetGetHashCodeFunc<T>([NotNull] params Expression<Func<T, object>>[] members)
			=> CreateGetHashCodeFunc<T>(members);

		class Comparer<T> : EqualityComparer<T>
		{
			public Comparer(Func<T,T,bool> equals, Func<T,int> getHashCode)
			{
				_equals      = equals;
				_getHashCode = getHashCode;
			}

			readonly Func<T,T,bool> _equals;
			readonly Func<T,int>    _getHashCode;

			public override bool Equals     (T x, T y) => x != null ? y != null && _equals(x, y) : y == null;

			public override int  GetHashCode(T obj)    => obj == null ? 0 : _getHashCode(obj);

			internal static Comparer<T> DefaultInstance;
		}


		/// <summary>
		/// Returns implementations of the <see cref="T:System.Collections.Generic.IEqualityComparer`1" /> generic interface
		/// based on object public members equality.
		/// </summary>
		/// <returns>Instance of <see cref="T:System.Collections.Generic.IEqualityComparer`1" />.</returns>
		/// <typeparam name="T">The type of objects to compare.</typeparam>
		[NotNull, Pure]
		public static IEqualityComparer<T> GetEqualityComparer<T>()
			=> Comparer<T>.DefaultInstance ?? (Comparer<T>.DefaultInstance = new Comparer<T>(GetEqualsFunc<T>(), GetGetHashCodeFunc<T>()));

		/// <summary>
		/// Returns implementations of the <see cref="T:System.Collections.Generic.IEqualityComparer`1" /> generic interface
		/// based on provided object public members equality.
		/// </summary>
		/// <param name="membersToCompare">Members to compare.</param>
		/// <returns>Instance of <see cref="T:System.Collections.Generic.IEqualityComparer`1" />.</returns>
		/// <typeparam name="T">The type of objects to compare.</typeparam>
		[NotNull, Pure]
		public static IEqualityComparer<T> GetEqualityComparer<T>([NotNull] params Expression<Func<T,object>>[] membersToCompare)
		{
			if (membersToCompare == null) throw new ArgumentNullException(nameof(membersToCompare));
			return new Comparer<T>(CreateEqualsFunc<T>(membersToCompare), CreateGetHashCodeFunc<T>(membersToCompare));
		}

		/// <summary>
		/// Returns implementations of the <see cref="T:System.Collections.Generic.IEqualityComparer`1" /> generic interface
		/// based on provided object public members equality.
		/// </summary>
		/// <param name="membersToCompare">A function that returns members to compare.</param>
		/// <returns>Instance of <see cref="T:System.Collections.Generic.IEqualityComparer`1" />.</returns>
		/// <typeparam name="T">The type of objects to compare.</typeparam>
		[NotNull, Pure]
		public static IEqualityComparer<T> GetEqualityComparer<T>(
			[NotNull, InstantHandle] Func<TypeAccessor<T>,IEnumerable<MemberAccessor>> membersToCompare)
		{
			if (membersToCompare == null) throw new ArgumentNullException(nameof(membersToCompare));

			var members = membersToCompare(TypeAccessor.GetAccessor<T>()).ToList();
			return new Comparer<T>(GetEqualsFunc<T>(members), GetGetHashCodeFunc<T>(members));
		}

		/// <summary>
		/// Returns implementations of the <see cref="T:System.Collections.Generic.IEqualityComparer`1" /> generic interface
		/// based on provided object public members equality.
		/// </summary>
		/// <param name="memberPredicate">A function to filter members to compare.</param>
		/// <returns>Instance of <see cref="T:System.Collections.Generic.IEqualityComparer`1" />.</returns>
		/// <typeparam name="T">The type of objects to compare.</typeparam>
		[NotNull, Pure]
		public static IEqualityComparer<T> GetEqualityComparer<T>(
			[NotNull, InstantHandle] Func<MemberAccessor,bool> memberPredicate)
		{
			if (memberPredicate == null) throw new ArgumentNullException(nameof(memberPredicate));

			var members = TypeAccessor.GetAccessor<T>().Members.Where(memberPredicate).ToList();
			return new Comparer<T>(GetEqualsFunc<T>(members), GetGetHashCodeFunc<T>(members));
		}

		[NotNull, Pure]
		static Func<T,T,bool> CreateEqualsFunc<T>([NotNull] IEnumerable<LambdaExpression> membersToCompare)
		{
			var x = Expression.Parameter(typeof(T), "x");
			var y = Expression.Parameter(typeof(T), "y");

			var expressions = membersToCompare.Select(me =>
			{
				var arg0 = RemoveCastToObject(me.GetBody(x));
				var arg1 = RemoveCastToObject(me.GetBody(y));
				var eq   = GetEqualityComparer(arg1.Type);
				var pi   = eq.GetPropertyEx("Default");
				var mi   = eq.GetMethodsEx().Single(m => m.IsPublic && m.Name == "Equals" && m.GetParameters().Length == 2);

				Debug.Assert(pi != null, "pi != null");
				Expression expr = Expression.Call(Expression.Property(null, pi), mi, arg0, arg1);

				return expr;
			});

			var expression = expressions
				.DefaultIfEmpty(Expression.Constant(true))
				.Aggregate(Expression.AndAlso);

			return Expression.Lambda<Func<T,T,bool>>(expression, x, y).Compile();
		}

		static Type GetEqualityComparer(Type type)
		{
			if (type.IsArray)
				return typeof(ArrayEqualityComparer<>).MakeGenericType(type.GetElementType());

			if (type == typeof(BitArray))
				return typeof(BitArrayEqualityComparer);

			if (type != typeof(string) && typeof(IEnumerable).IsAssignableFromEx(type))
				return typeof(EnumerableEqualityComparer);

			return typeof(EqualityComparer<>).MakeGenericType(type);
		}

		[NotNull, Pure]
		static Func<T,int> CreateGetHashCodeFunc<T>([NotNull] IEnumerable<LambdaExpression> membersToCompare)
		{
			var parameter  = Expression.Parameter(typeof(T), "parameter");
			var expression = membersToCompare.Aggregate(
				(Expression)Expression.Constant(_randomSeed),
				(e, me) =>
				{
					var ma = RemoveCastToObject(me.GetBody(parameter));
					var eq = GetEqualityComparer(ma.Type);
					var pi = eq.GetPropertyEx("Default");
					var mi = eq.GetMethodsEx().Single(m => m.IsPublic && m.Name == "GetHashCode" && m.GetParameters().Length == 1);

					Debug.Assert(pi != null, "pi != null");
					return Expression.Add(
						Expression.Multiply(e, Expression.Constant(-1521134295)),
						Expression.Call(Expression.Property(null, pi), mi, ma));
				});

			return Expression.Lambda<Func<T, int>>(expression, parameter).Compile();
		}

		[NotNull, Pure]
		static Expression RemoveCastToObject([NotNull] Expression expression)
		{
			if (expression.Type != typeof(object) || expression.NodeType != ExpressionType.Convert)
				return expression;

			return ((UnaryExpression)expression).Operand;
		}
	}
}
