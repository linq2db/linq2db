﻿using System;

namespace LinqToDB.Linq
{
	[Flags]
	internal enum QueryFlags
	{
		None                 = 0,
		/// <summary>
		/// Bit set, when inline parameters enabled for connection.
		/// </summary>
		InlineParameters     = 0x02,
	}
}
