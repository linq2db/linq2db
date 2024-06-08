﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace LinqToDB.Common.Internal.Cache
{
	public class PostEvictionCallbackRegistration<TKey>
		where TKey: notnull
	{
		public PostEvictionDelegate<TKey> EvictionCallback { get; set; } = null!;

		public object? State { get; set; }
	}
}
