using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace LinqToDB.Internal.Extensions
{
	public static class NumberTypeExtensions
	{
		extension(Type type)
		{
			public bool IsNumberType =>
				type is
				{
					UnwrappedNullableType.TypeCode:
						TypeCode.SByte
						or TypeCode.Byte
						or TypeCode.Int16
						or TypeCode.UInt16
						or TypeCode.Int32
						or TypeCode.UInt32
						or TypeCode.Int64
						or TypeCode.UInt64
						or TypeCode.Single
						or TypeCode.Double
						or TypeCode.Decimal,
				};

			public bool IsIntegerType =>
				type is
				{
					UnwrappedNullableType.TypeCode:
						TypeCode.SByte
						or TypeCode.Byte
						or TypeCode.Int16
						or TypeCode.UInt16
						or TypeCode.Int32
						or TypeCode.UInt32
						or TypeCode.Int64
						or TypeCode.UInt64,
				};

			public bool IsFloatType =>
				type is
				{
					UnwrappedNullableType.TypeCode:
						TypeCode.Single
						or TypeCode.Double
						or TypeCode.Decimal,
				};

			public bool IsSignedNumberType =>
				type is
				{
					UnwrappedNullableType.TypeCode:
						TypeCode.SByte
						or TypeCode.Int16
						or TypeCode.Int32
						or TypeCode.Int64
						or TypeCode.Single
						or TypeCode.Double
						or TypeCode.Decimal,
				};

			/// <summary>
			/// Returns <see langword="true"/> for a binary floating-point type (<see cref="float"/>, <see cref="double"/>,
			/// and <c>System.Half</c> where the target framework has it), nullable or not. Unlike <c>IsFloatType</c>,
			/// <see cref="decimal"/> is excluded.
			/// </summary>
			public bool IsFloatingPointType =>
				type is { UnwrappedNullableType.TypeCode: TypeCode.Single or TypeCode.Double }
#if NET8_0_OR_GREATER
				|| type.UnwrappedNullableType == typeof(Half)
#endif
				;

			/// <summary>
			/// Returns <see langword="true"/> for a signed integer type (<see cref="sbyte"/>, <see cref="short"/>,
			/// <see cref="int"/>, <see cref="long"/>), nullable or not, and for an enum with such an underlying type.
			/// </summary>
			public bool IsSignedIntegerType =>
				type is
				{
					UnwrappedNullableType.TypeCode:
						TypeCode.SByte
						or TypeCode.Int16
						or TypeCode.Int32
						or TypeCode.Int64,
				};

			/// <summary>
			/// The unsigned integer type of the same width as a signed integer type (<see cref="sbyte"/> to <see cref="byte"/>,
			/// <see cref="short"/> to <see cref="ushort"/>, <see cref="int"/> to <see cref="uint"/>, <see cref="long"/> to
			/// <see cref="ulong"/>), nullable or not and for an enum by its underlying type; <see langword="null"/> for any
			/// other type.
			/// </summary>
			public Type? UnsignedIntegerType =>
				type.UnwrappedNullableType.TypeCode switch
				{
					TypeCode.SByte => typeof(byte),
					TypeCode.Int16 => typeof(ushort),
					TypeCode.Int32 => typeof(uint),
					TypeCode.Int64 => typeof(ulong),
					_              => null,
				};
		}
	}
}
