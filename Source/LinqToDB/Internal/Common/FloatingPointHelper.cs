using System;

namespace LinqToDB.Internal.Common
{
	// NaN/Infinity constants, and comparisons against them, make the x86 .NET Framework JIT fault when the host
	// unmasks FP exceptions (#6051), so the values are built and tested from their bit patterns.
	// Don't cache them in static readonly fields: the JIT folds those back into constants.
	internal static class FloatingPointHelper
	{
		const long NaNBits              = unchecked((long)0xFFF8000000000000); // == double.NaN
		const long PositiveInfinityBits = 0x7FF0000000000000;
		const long NegativeInfinityBits = unchecked((long)0xFFF0000000000000);

		public static double DoubleNaN              => BitConverter.Int64BitsToDouble(NaNBits);
		public static double DoublePositiveInfinity => BitConverter.Int64BitsToDouble(PositiveInfinityBits);
		public static double DoubleNegativeInfinity => BitConverter.Int64BitsToDouble(NegativeInfinityBits);

		public static float SingleNaN              => (float)DoubleNaN;
		public static float SinglePositiveInfinity => (float)DoublePositiveInfinity;
		public static float SingleNegativeInfinity => (float)DoubleNegativeInfinity;

		public static bool IsPositiveInfinity(double value) => BitConverter.DoubleToInt64Bits(value) == PositiveInfinityBits;
		public static bool IsNegativeInfinity(double value) => BitConverter.DoubleToInt64Bits(value) == NegativeInfinityBits;

		public static bool IsPositiveInfinity(float value) => IsPositiveInfinity((double)value);
		public static bool IsNegativeInfinity(float value) => IsNegativeInfinity((double)value);
	}
}
