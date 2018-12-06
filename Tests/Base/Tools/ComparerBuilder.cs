using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB.Expressions;
using LinqToDB.Extensions;
using LinqToDB.Linq;
using LinqToDB.Reflection;

namespace Tests.Tools
{
	/// <summary>
	/// Builds comparer functions and comparers.
	/// </summary>
	/// <typeparam name="T">The type of objects to compare.</typeparam>
	public static class ComparerBuilder<T>
	{
		/// <summary>
		/// Returns GetEqualsFunc function for type T to compare.
		/// </summary>
		/// <returns>GetEqualsFunc function.</returns>
		public static Func<T, T, bool> GetEqualsFunc(bool assertOnFail = false)
			=> GetEqualsFunc(TypeAccessor.GetAccessor<T>().Members, assertOnFail);

		/// <summary>
		/// Returns GetEqualsFunc function for provided members for type T to compare.
		/// </summary>
		/// <param name="members">Members to compare.</param>
		/// <returns>GetEqualsFunc function.</returns>
		public static Func<T, T, bool> GetEqualsFunc(IEnumerable<MemberAccessor> members, bool assertOnFail = false)
			=> CreateEqualsFunc(members.Select(m => m.GetterExpression), assertOnFail);

		/// <summary>
		/// Returns GetEqualsFunc function for provided members for type T to compare.
		/// </summary>
		/// <param name="members">Members to compare.</param>
		/// <returns>GetEqualsFunc function.</returns>
		public static Func<T, T, bool> GetEqualsFunc(params Expression<Func<T, object>>[] members)
			=> CreateEqualsFunc(members);

		/// <summary>
		/// Returns GetHashCode function for type T to compare.
		/// </summary>
		/// <returns>GetHashCode function.</returns>
		public static Func<T, int> GetGetHashCodeFunc()
			=> GetGetHashCodeFunc(TypeAccessor.GetAccessor<T>().Members);

		// ReSharper disable once StaticMemberInGenericType
		private static readonly int _randomSeed = new Random(unchecked((int)DateTime.Now.Ticks)).Next();

		/// <summary>
		/// Returns GetHashCode function for provided members for type T to compare.
		/// </summary>
		/// <param name="members">Members to compare.</param>
		/// <returns>GetHashCode function.</returns>
		public static Func<T, int> GetGetHashCodeFunc(IEnumerable<MemberAccessor> members)
			=> CreateGetHashCodeFunc(members.Select(m => m.GetterExpression));

		/// <summary>
		/// Returns GetHashCode function for provided members for type T to compare.
		/// </summary>
		/// <param name="members">Members to compare.</param>
		/// <returns>GetHashCode function.</returns>
		public static Func<T, int> GetGetHashCodeFunc(params Expression<Func<T, object>>[] members)
			=> CreateGetHashCodeFunc(members);

		private class Comparer : EqualityComparer<T>
		{
			public Comparer(Func<T, T, bool> equals, Func<T, int> getHashCode)
			{
				_equals = equals;
				_getHashCode = getHashCode;
			}

			private readonly Func<T, T, bool> _equals;
			private readonly Func<T, int> _getHashCode;

			public override bool Equals(T x, T y)
				=> x != null ? y != null && _equals(x, y) : y == null;

			public override int GetHashCode(T obj)
				=> obj == null ? 0 : _getHashCode(obj);
		}

		private static Comparer _equalityComparer;
		private static Comparer _assertingEqualityComparer;

		/// <summary>
		/// Returns implementations of the <see cref="T:System.Collections.Generic.IEqualityComparer`1" /> generic interface
		/// based on object public members equality.
		/// </summary>
		/// <returns>Instance of <see cref="T:System.Collections.Generic.IEqualityComparer`1" />.</returns>
		public static IEqualityComparer<T> GetEqualityComparer(bool assertOnFail = false)
		{
			if (assertOnFail)
				return _assertingEqualityComparer ?? (_assertingEqualityComparer = new Comparer(GetEqualsFunc(true),  GetGetHashCodeFunc()));
			else
				return _equalityComparer          ?? (_equalityComparer          = new Comparer(GetEqualsFunc(false), GetGetHashCodeFunc()));
		}

		/// <summary>
		/// Returns implementations of the <see cref="T:System.Collections.Generic.IEqualityComparer`1" /> generic interface
		/// based on provided object public members equality.
		/// </summary>
		/// <param name="membersToCompare">Members to compare.</param>
		/// <returns>Instance of <see cref="T:System.Collections.Generic.IEqualityComparer`1" />.</returns>
		public static IEqualityComparer<T> GetEqualityComparer(params Expression<Func<T, object>>[] membersToCompare)
		{
			return new Comparer(CreateEqualsFunc(membersToCompare), CreateGetHashCodeFunc(membersToCompare));
		}

		/// <summary>
		/// Returns implementations of the <see cref="T:System.Collections.Generic.IEqualityComparer`1" /> generic interface
		/// based on provided object public members equality.
		/// </summary>
		/// <param name="membersToCompare">A function that returns members to compare.</param>
		/// <returns>Instance of <see cref="T:System.Collections.Generic.IEqualityComparer`1" />.</returns>
		public static IEqualityComparer<T> GetEqualityComparer(Func<TypeAccessor<T>, IEnumerable<MemberAccessor>> membersToCompare)
		{
			var members = membersToCompare(TypeAccessor.GetAccessor<T>()).ToList();
			return new Comparer(GetEqualsFunc(members), GetGetHashCodeFunc(members));
		}

		private static Func<T, T, bool> CreateEqualsFunc(IEnumerable<LambdaExpression> membersToCompare, bool assertOnFail = false)
		{
			var x = Expression.Parameter(typeof(T), "x");
			var y = Expression.Parameter(typeof(T), "y");

			var expressions = membersToCompare.Select(me =>
			{
				var arg0 = RemoveCastToObject(me.GetBody(x));
				var arg1 = RemoveCastToObject(me.GetBody(y));
				var eq = GetEqualityComparer(arg1.Type);
				var pi = eq.GetPropertyEx("Default");
				var mi = eq.GetMethodsEx().Single(m => m.IsPublic && m.Name == "Equals" && m.GetParameters().Length == 2);

				Debug.Assert(pi != null, "pi != null");
				Expression expr = Expression.Call(Expression.Property(null, pi), mi, arg0, arg1);

				if (assertOnFail)
				{
					expr = Expression.Call(
						MethodHelper.GetMethodInfo(Assert, true, (object)null, (object)null),
						expr,
						Expression.Convert(arg0, typeof(object)),
						Expression.Convert(arg1, typeof(object)));
				}

				return expr;
			});

			var expression = expressions
				.DefaultIfEmpty(Expression.Constant(true))
				.Aggregate(Expression.AndAlso);

			return Expression
				.Lambda<Func<T, T, bool>>(expression, x, y)
				.Compile();
		}

		private static bool Assert(bool isEqual, object left, object right)
		{
			if (!isEqual)
				NUnit.Framework.Assert.Fail($"Equality check failed: {left} not equals {right}");

			return isEqual;
		}

		private static Type GetEqualityComparer(Type type)
		{
			if (type.IsArray)
				return typeof(ArrayEqualityComparer<>).MakeGenericType(type.GetElementType());

			if (type == typeof(BitArray))
				return typeof(BitArrayEqualityComparer);

			if (type != typeof(string) && typeof(IEnumerable).IsAssignableFromEx(type))
				return typeof(EnumerableEqualityComparer);

			return typeof(EqualityComparer<>).MakeGenericType(type);
		}

		private static Func<T, int> CreateGetHashCodeFunc(IEnumerable<LambdaExpression> membersToEval)
		{
			var parameter = Expression.Parameter(typeof(T), "parameter");
			var expression = membersToEval.Aggregate(
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

			return Expression
				.Lambda<Func<T, int>>(expression, parameter)
				.Compile();
		}

		private static Expression RemoveCastToObject(Expression expression)
		{
			if (expression.Type != typeof(object) || expression.NodeType != ExpressionType.Convert)
				return expression;

			return ((UnaryExpression)expression).Operand;
		}
	}
}
