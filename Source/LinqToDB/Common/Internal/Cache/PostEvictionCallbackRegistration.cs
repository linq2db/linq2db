// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace LinqToDB.Common.Internal.Cache
{
	public class PostEvictionCallbackRegistration
	{
		public PostEvictionDelegate EvictionCallback { get; set; } = null!;

		public object? State { get; set; }
	}
}
