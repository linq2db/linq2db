using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using JetBrains.Annotations;

using LinqToDB.Common;
using LinqToDB.Common.Internal;
using LinqToDB.Extensions;
using LinqToDB.Internal.Expressions;
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
		[Pure]
		public static Func<T,T,bool> GetEqualsFunc<T>()
			=> GetEqualsFunc<T>(TypeAccessor.GetAccessor<T>().Members.Where(static m => !m.MemberInfo.HasAttribute<IgnoreComparisonAttribute>()));

		/// <summary>
		/// Returns GetEqualsFunc function for provided members for type T to compare.
		/// </summary>
		/// <param name="members">Members to compare.</param>
		/// <returns>GetEqualsFunc function.</returns>
		/// <typeparam name="T">The type of objects to compare.</typeparam>
		[Pure]
		public static Func<T,T,bool> GetEqualsFunc<T>([InstantHandle] IEnumerable<MemberAccessor> members)
			=> CreateEqualsFunc<T>(members.Select(m => (Func<Expression, Expression>)m.GetGetterExpression));

		/// <summary>
		/// Returns GetEqualsFunc function for provided members for type T to compare.
		/// </summary>
		/// <param name="members">Members to compare.</param>
		/// <returns>GetEqualsFunc function.</returns>
		/// <typeparam name="T">The type of objects to compare.</typeparam>
		[Pure]
		public static Func<T,T,bool> GetEqualsFunc<T>(params Expression<Func<T,object?>>[] members)
			=> CreateEqualsFunc<T>(members.Select(e => (Func<Expression, Expression>)e.GetBody));

		/// <summary>
		/// Returns GetHashCode function for type T to compare.
		/// </summary>
		/// <returns>GetHashCode function.</returns>
		/// <typeparam name="T">The type of objects to compare.</typeparam>
		[Pure]
		public static Func<T,int> GetGetHashCodeFunc<T>()
			=> GetGetHashCodeFunc<T>(TypeAccessor.GetAccessor<T>().Members.Where(static m => !m.MemberInfo.HasAttribute<IgnoreComparisonAttribute>()));

		static readonly int _randomSeed = new Random(unchecked((int)DateTime.Now.Ticks)).Next();

		/// <summary>
		/// Returns GetHashCode function for provided members for type T to compare.
		/// </summary>
		/// <param name="members">Members to compare.</param>
		/// <returns>GetHashCode function.</returns>
		/// <typeparam name="T">The type of objects to compare.</typeparam>
		[Pure]
		public static Func<T,int> GetGetHashCodeFunc<T>([InstantHandle] IEnumerable<MemberAccessor> members)
			=> CreateGetHashCodeFunc<T>(members.Select(m => (Func<Expression, Expression>)m.GetGetterExpression));

		/// <summary>
		/// Returns GetHashCode function for provided members for type T to compare.
		/// </summary>
		/// <param name="members">Members to compare.</param>
		/// <returns>GetHashCode function.</returns>
		/// <typeparam name="T">The type of objects to compare.</typeparam>
		[Pure]
		public static Func<T,int> GetGetHashCodeFunc<T>(params Expression<Func<T, object?>>[] members)
			=> CreateGetHashCodeFunc<T>(members.Select(e => (Func<Expression, Expression>)e.GetBody));

		sealed class Comparer<T> : EqualityComparer<T>
		{
			public Comparer(Func<T,T,bool> equals, Func<T,int> getHashCode)
			{
				_equals      = equals;
				_getHashCode = getHashCode;
			}

			readonly Func<T,T,bool> _equals;
			readonly Func<T,int>    _getHashCode;

			public override bool Equals     (T? x, T? y) => x != null ? y != null && _equals(x, y) : y == null;

			public override int  GetHashCode(T obj)      => obj == null ? 0 : _getHashCode(obj);

			internal static Comparer<T>? DefaultInstance;
		}

		/// <summary>
		/// Returns implementations of the <see cref="IEqualityComparer{T}" /> generic interface
		/// based on object public members equality.
		/// </summary>
		/// <returns>Instance of <see cref="IEqualityComparer{T}" />.</returns>
		/// <typeparam name="T">The type of objects to compare.</typeparam>
		[Pure]
		public static IEqualityComparer<T> GetEqualityComparer<T>()
			=> Comparer<T>.DefaultInstance ?? (Comparer<T>.DefaultInstance = new Comparer<T>(GetEqualsFunc<T>(), GetGetHashCodeFunc<T>()));

		private static MethodInfo _getEqualityComparerMethodInfo =
			MemberHelper.MethodOf(() => GetEqualityComparer<object>()).GetGenericMethodDefinition();

		public static IEqualityComparer GetEqualityComparer(Type type)
		{
			var method = _getEqualityComparerMethodInfo.MakeGenericMethod(type);
			return method.InvokeExt<IEqualityComparer>(null, null);
		}

		/// <summary>
		/// Returns implementations of the <see cref="IEqualityComparer{T}" /> generic interface
		/// based on provided object public members equality.
		/// </summary>
		/// <param name="membersToCompare">Members to compare.</param>
		/// <returns>Instance of <see cref="IEqualityComparer{T}" />.</returns>
		/// <typeparam name="T">The type of objects to compare.</typeparam>
		[Pure]
		public static IEqualityComparer<T> GetEqualityComparer<T>(params Expression<Func<T,object?>>[] membersToCompare)
		{
			if (membersToCompare == null) throw new ArgumentNullException(nameof(membersToCompare));
			return new Comparer<T>(CreateEqualsFunc<T>(membersToCompare.Select(e => (Func<Expression, Expression>)e.GetBody)), CreateGetHashCodeFunc<T>(membersToCompare.Select(e => (Func<Expression, Expression>)e.GetBody)));
		}

		/// <summary>
		/// Returns implementations of the <see cref="IEqualityComparer{T}" /> generic interface
		/// based on object public members equality.
		/// </summary>
		/// <returns>Instance of <see cref="IEqualityComparer{T}" />.</returns>
		/// <typeparam name="T">The type of objects to compare.</typeparam>
		[Pure]
		public static IEqualityComparer<T> GetEqualityComparer<T>(IEnumerable<T> ignored) =>
			GetEqualityComparer<T>();

		/// <summary>
		/// Returns implementations of the <see cref="IEqualityComparer{T}" /> generic interface
		/// based on provided object public members equality.
		/// </summary>
		/// <param name="membersToCompare">A function that returns members to compare.</param>
		/// <returns>Instance of <see cref="IEqualityComparer{T}" />.</returns>
		/// <typeparam name="T">The type of objects to compare.</typeparam>
		[Pure]
		public static IEqualityComparer<T> GetEqualityComparer<T>(
			[InstantHandle] Func<TypeAccessor<T>,IEnumerable<MemberAccessor>> membersToCompare)
		{
			if (membersToCompare == null) throw new ArgumentNullException(nameof(membersToCompare));

			var members = membersToCompare(TypeAccessor.GetAccessor<T>()).ToList();
			return new Comparer<T>(GetEqualsFunc<T>(members), GetGetHashCodeFunc<T>(members));
		}

		/// <summary>
		/// Returns implementations of the <see cref="IEqualityComparer{T}" /> generic interface
		/// based on provided object public members equality.
		/// </summary>
		/// <param name="memberPredicate">A function to filter members to compare.</param>
		/// <returns>Instance of <see cref="IEqualityComparer{T}" />.</returns>
		/// <typeparam name="T">The type of objects to compare.</typeparam>
		[Pure]
		public static IEqualityComparer<T> GetEqualityComparer<T>(
			[InstantHandle] Func<MemberAccessor,bool> memberPredicate)
		{
			if (memberPredicate == null) throw new ArgumentNullException(nameof(memberPredicate));

			var members = TypeAccessor.GetAccessor<T>().Members.Where(memberPredicate).ToList();
			return new Comparer<T>(GetEqualsFunc<T>(members), GetGetHashCodeFunc<T>(members));
		}

		[Pure]
		static Func<T,T,bool> CreateEqualsFunc<T>(IEnumerable<Func<Expression, Expression>> membersToCompare)
		{
			var x = Expression.Parameter(typeof(T), "x");
			var y = Expression.Parameter(typeof(T), "y");

			var expressions = membersToCompare.Select(me =>
			{
				var arg0 = RemoveCastToObject(me(x));
				var arg1 = RemoveCastToObject(me(y));
				var eq   = GetEqualityComparerExpression(arg1.Type);
				var mi   = eq.Type.GetMethods().Single(m => m.IsPublic && m.Name == "Equals" && m.GetParameters().Length == 2);

				Expression expr = Expression.Call(eq, mi, arg0, arg1);

				return expr;
			});

			var expression = expressions
				.DefaultIfEmpty(ExpressionInstances.True)
				.Aggregate(Expression.AndAlso);

			return Expression.Lambda<Func<T,T,bool>>(expression, x, y).CompileExpression();
		}

		static Expression GetEqualityComparerExpression(Type type)
		{
			Type comparerType;

			if (type == typeof(BitArray))
				comparerType = typeof(BitArrayEqualityComparer);
			else if (type != typeof(string) && typeof(IEnumerable).IsAssignableFrom(type))
				comparerType = typeof(IEnumerable<>).IsSameOrParentOf(type)
					? typeof(EnumerableEqualityComparer<>).MakeGenericType(type.IsArray
						? type.GetElementType()!
						: type.GetGenericArguments()[0])
					: typeof(EnumerableEqualityComparer);
			else if (type.IsClass &&  (type.Name.StartsWith("<>") || !type.GetMethods().Any(m => m.Name == "Equals" && m.DeclaringType == type)))
				return Expression.Call(_getEqualityComparerMethodInfo.MakeGenericMethod(type));
			else 
				comparerType = typeof(EqualityComparer<>).MakeGenericType(type);

			var constructors = comparerType.GetConstructors();

			if (comparerType.IsGenericType && !comparerType.IsGenericTypeDefinition)
			{
				var withComparerConstructor = constructors.FirstOrDefault(c => c.GetParameters().Length == 1);
				if (withComparerConstructor != null)
				{
					return Expression.New(withComparerConstructor,
						GetEqualityComparerExpression(comparerType.GetGenericArguments()[0]));
				}
			}

			return Expression.MakeMemberAccess(null, comparerType.GetProperty("Default")!);
		}

		[Pure]
		static Func<T,int> CreateGetHashCodeFunc<T>(IEnumerable<Func<Expression, Expression>> membersToCompare)
		{
			var parameter  = Expression.Parameter(typeof(T), "parameter");
			var expression = membersToCompare.Aggregate(
				(Expression)Expression.Constant(_randomSeed),
				(e, me) =>
				{
					var ma = RemoveCastToObject(me(parameter));
					var eq = GetEqualityComparerExpression(ma.Type);
					var mi = eq.Type.GetMethods().Single(m => m.IsPublic && m.Name == "GetHashCode" && m.GetParameters().Length == 1);

					return Expression.Add(
						Expression.Multiply(e, ExpressionInstances.HashMultiplier),
						Expression.Call(eq, mi, ma));
				});

			return Expression.Lambda<Func<T, int>>(expression, parameter).CompileExpression();
		}

		[Pure]
		static Expression RemoveCastToObject(Expression expression)
		{
			if (expression.Type != typeof(object) || expression.NodeType != ExpressionType.Convert)
				return expression;

			return ((UnaryExpression)expression).Operand;
		}
	}
}
