using System;

namespace LinqToDB.Internal.Common
{
	// see: https://stackoverflow.com/a/33490834/10646316
	public static class DecimalHelper
	{
		public static (int precision, int scale) GetFacets(decimal value)
		{
#if SUPPORTS_SPAN
			Span<int> bits = stackalloc int[4];
			decimal.GetBits(value, bits);
#else
			var bits = decimal.GetBits(value);
#endif

			var scale = (bits[3] >> 16) & 0x7F;

			int precision;
			if (value != 0m)
			{
				//We will use false for the sign (false =  positive), because we don't care about it.
				//We will use 0 for the last argument instead of bits[3] to eliminate the fraction point.
				var d      = new decimal(bits[0], bits[1], bits[2], false, 0);
				var digits = (int)Math.Floor(Math.Log10((double)d)) + 1;
				precision  = scale > digits ? scale : digits;
			}
			else
				precision = 1;

			return (precision, scale);
		}
	}
}
