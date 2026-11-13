using System;

namespace Tests.Model
{
	public static class FloatingPoint
	{
		/// <summary>
		/// Default <b>relative</b> tolerance for comparing a provider round-trip against an in-memory value.
		/// Four orders of magnitude above the conversion noise (|value| * 2^-53, i.e. ~1.1e-16 relative) and far
		/// below any difference these tests assert - the tightest is five decimal places on values of order 10.
		/// </summary>
		/// <remarks>
		/// .NET 11 converts <see cref="double"/> to <see cref="decimal"/> exactly rather than rounding to 15
		/// significant digits (<see href="https://learn.microsoft.com/dotnet/core/compatibility/core-libraries/11/decimal-biginteger-floating-point-conversions"/>),
		/// so a value a provider stores as REAL - or, on Sybase, reads back through a double - no longer equals
		/// the in-memory decimal it is compared against.
		/// </remarks>
		public const decimal Delta = 0.000000000001m;

		/// <summary>
		/// <see cref="Delta"/> for NUnit's <c>Within(...).Percent</c>, which takes a percentage rather than a ratio.
		/// </summary>
		public const double DeltaPercent = 0.0000000001d;

		/// <summary>
		/// Compares two values within <see cref="Delta"/>, relative to the magnitude of <paramref name="expected"/>.
		/// </summary>
		public static bool AreClose(decimal expected, decimal actual)
			=> Math.Abs(expected - actual) <= Delta * Math.Max(1m, Math.Abs(expected));
	}
}
