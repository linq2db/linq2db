// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace LinqToDB.Internal.Cache
{
	/// <summary>
	/// Provides access to the normal system clock.
	/// </summary>
	sealed class SystemClock : ISystemClock
	{
		/// <summary>
		/// Retrieves the current system time in UTC.
		/// </summary>
		public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
	}
}
