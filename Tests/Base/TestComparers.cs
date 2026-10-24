using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using LinqToDB.Reflection;
using LinqToDB.Tools.Comparers;

namespace Tests
{
	/// <summary>
	/// Default comparers for test assertions that receive no explicit comparer.
	/// </summary>
	public static class TestComparers
	{
		/// <summary>
		/// Returns the default comparer for <typeparamref name="T"/>: value equality for a scalar
		/// (<see cref="string"/>, <see cref="decimal"/>, <see cref="Guid"/>, a type with no public instance members, or a <see cref="Nullable{T}"/> of one),
		/// ordered element-wise comparison for an <see cref="IEnumerable{T}"/>, and <see cref="ComparerBuilder"/>'s
		/// member comparison otherwise.
		/// </summary>
		public static IEqualityComparer<T> GetDefault<T>() => Cache<T>.Instance;

		/// <summary>
		/// Non-generic form of <see cref="GetDefault{T}()"/>; returns an <see cref="IEqualityComparer{T}"/> of <paramref name="type"/>.
		/// </summary>
		public static object GetDefault(Type type) =>
			_getDefaultMethod.MakeGenericMethod(type).Invoke(null, null)!;

		static readonly MethodInfo _getDefaultMethod = typeof(TestComparers).GetMethod(nameof(GetDefault), Type.EmptyTypes)!;

		static class Cache<T>
		{
			internal static readonly IEqualityComparer<T> Instance = (IEqualityComparer<T>)Create(typeof(T));
		}

#if !NETFRAMEWORK
		/// <summary>
		/// Value comparer for <see cref="decimal"/>: scale-insensitive, with a hash that agrees on .NET Framework too.
		/// </summary>
		public static readonly IEqualityComparer<decimal> DecimalByValue = EqualityComparer<decimal>.Default;
#else
		/// <summary>
		/// Value comparer for <see cref="decimal"/>: scale-insensitive, with a hash that agrees on .NET Framework too.
		/// </summary>
		public static readonly IEqualityComparer<decimal> DecimalByValue = new DecimalComparer();

		sealed class DecimalComparer : IEqualityComparer<decimal>
		{
			bool IEqualityComparer<decimal>.Equals(decimal x, decimal y) => x == y;

			int IEqualityComparer<decimal>.GetHashCode(decimal obj)
			{
				// workaround netfx bug in decimal.GetHashCode
				return (obj / 1.00000000000000000000000000000m).GetHashCode();
			}
		}
#endif

		static object Create(Type type)
		{
			var elementType = type == typeof(string) ? null : GetElementType(type);

			if (elementType != null)
				return Activator.CreateInstance(typeof(SequenceComparer<,>).MakeGenericType(type, elementType))!;

			if (IsScalar(type))
				return type == typeof(decimal) ? DecimalByValue : DefaultOf(type);

			var underlying = Nullable.GetUnderlyingType(type);

			if (underlying != null && IsScalar(underlying))
				return Activator.CreateInstance(typeof(NullableComparer<>).MakeGenericType(underlying), GetDefault(underlying))!;

			return ComparerBuilder.GetEqualityComparer(type);
		}

		static Type? GetElementType(Type type)
		{
			if (IsEnumerableOfT(type))
				return type.GetGenericArguments()[0];

			return type
				.GetInterfaces()
				.FirstOrDefault(IsEnumerableOfT)
				?.GetGenericArguments()[0];

			static bool IsEnumerableOfT(Type t) => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>);
		}

		// string, decimal and Guid have public members that don't express their value (Length; Scale; Version/Variant on .NET 9+)
		static bool IsScalar(Type type) =>
			type == typeof(string) || type == typeof(decimal) || type == typeof(Guid) || TypeAccessor.GetAccessor(type).Members.Count == 0;

		static object DefaultOf(Type type) =>
			typeof(EqualityComparer<>).MakeGenericType(type).GetProperty(nameof(EqualityComparer<>.Default))!.GetValue(null)!;

		sealed class NullableComparer<T>(IEqualityComparer<T> comparer) : IEqualityComparer<T?>
			where T : struct
		{
			public bool Equals(T? x, T? y) => x.HasValue ? y.HasValue && comparer.Equals(x.Value, y.Value) : !y.HasValue;

			public int GetHashCode(T? obj) => obj.HasValue ? comparer.GetHashCode(obj.Value) : 0;
		}

		// the element comparer is resolved on use: for a self-referential sequence, reading it while
		// Cache<TSequence> is still initialising would return null
		sealed class SequenceComparer<TSequence, TElement> : IEqualityComparer<TSequence>
			where TSequence : IEnumerable<TElement>
		{
			public bool Equals(TSequence? x, TSequence? y)
			{
				if (x is null || y is null)
					return x is null && y is null;

				return x.SequenceEqual(y, GetDefault<TElement>());
			}

			public int GetHashCode(TSequence obj)
			{
				if (obj is null)
					return 0;

				var comparer = GetDefault<TElement>();
				var hash     = 17;

				foreach (var item in obj)
					hash = unchecked(hash * 31 + (item is null ? 0 : comparer.GetHashCode(item)));

				return hash;
			}
		}
	}
}
