// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;

namespace LinqToDB.Internal.Cache
{
	[EditorBrowsable(EditorBrowsableState.Never)]
	public enum EvictionReason
	{
		None,

		/// <summary>
		/// Manually
		/// </summary>
		Removed,

		/// <summary>
		/// Overwritten
		/// </summary>
		Replaced,

		/// <summary>
		/// Timed out
		/// </summary>
		Expired,

		/// <summary>
		/// Event
		/// </summary>
		TokenExpired,

		/// <summary>
		/// Overflow
		/// </summary>
		Capacity,
	}
}
