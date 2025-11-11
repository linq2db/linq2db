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
		}
	}
}
