﻿using System;

namespace LinqToDB.Common
{
	// see: https://stackoverflow.com/a/33490834/10646316
	public static class DecimalHelper
	{
		public static int GetScale(decimal value)
		{
			if (value == 0)
			{
				return 0;
			}

			var bits = decimal.GetBits(value);
			return (bits[3] >> 16) & 0x7F;
		}

		public static int GetPrecision(decimal value)
		{
			if (value == 0)
			{
				return 0;
			}

			var bits = decimal.GetBits(value);
			//We will use false for the sign (false =  positive), because we don't care about it.
			//We will use 0 for the last argument instead of bits[3] to eliminate the fraction point.
			var d = new decimal(bits[0], bits[1], bits[2], false, 0);
			return (int)Math.Floor(Math.Log10((double)d)) + 1;
		}
	}
}