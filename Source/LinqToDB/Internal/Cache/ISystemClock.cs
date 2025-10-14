// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace LinqToDB.Internal.Cache
{
	/// <summary>
	/// Abstracts the system clock to facilitate testing.
	/// </summary>
	interface ISystemClock
	{
		/// <summary>
		/// Retrieves the current system time in UTC.
		/// </summary>
		DateTimeOffset UtcNow { get; }
	}
}
