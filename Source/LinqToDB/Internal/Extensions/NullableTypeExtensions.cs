using System;

namespace LinqToDB.Internal.Extensions
{
	public static class NullableTypeExtensions
	{
		public static Type UnwrapNullableType(this Type type)
			=> Nullable.GetUnderlyingType(type) ?? type;

		public static Type MakeNullable(this Type type)
			=> type.IsNullableOrReferenceType()
				? type
				: typeof(Nullable<>).MakeGenericType(type);

		/// <summary>
		/// Returns <c>true</c> if type is reference type or <see cref="Nullable{T}"/>.
		/// </summary>
		/// <param name="type">Type to test.</param>
		/// <returns><c>true</c> if type is reference type or <see cref="Nullable{T}"/>.</returns>
		internal static bool IsNullableOrReferenceType(this Type type)
			=> !type.IsValueType || type.IsNullableType;

		/// <summary>
		/// Wraps type into <see cref="Nullable{T}"/> class.
		/// </summary>
		/// <param name="type">Value type to wrap. Must be value type (except <see cref="Nullable{T}"/> itself).</param>
		/// <returns>Type, wrapped by <see cref="Nullable{T}"/>.</returns>
		public static Type AsNullable(this Type type)
			=> type switch
			{
				null => throw new ArgumentNullException(nameof(type)),
				{ IsValueType: false } => throw new ArgumentException($"{type} is not a value type"),
				{ IsNullableType: true } => throw new ArgumentException($"{type} is nullable type already"),
				_ => typeof(Nullable<>).MakeGenericType(type),
			};

		extension(Type type)
		{
			/// <summary>
			///		Returns the underlying type argument of the specified potentially nullable type,
			///		or the type itself if it is not nullable.
			/// </summary>
			public Type UnwrappedNullableType => Nullable.GetUnderlyingType(type) ?? type;

			/// <summary>
			/// Returns true, if type is <see cref="Nullable{T}"/> type.
			/// </summary>
			public bool IsNullableType =>
				type.IsConstructedGenericType
				&& type.GetGenericTypeDefinition() == typeof(Nullable<>);
		}
	}
}
