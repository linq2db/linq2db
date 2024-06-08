﻿#if NATIVE_ASYNC
global using IAsyncDisposableEx = System.IAsyncDisposable;
global using AsyncConfigured    = LinqToDB.Tools.ActivityService.AsyncDisposableWrapper;
#else
global using IAsyncDisposableEx = LinqToDB.Async.IAsyncDisposable;
global using AsyncConfigured    = LinqToDB.Async.IAsyncDisposable;

using System;
using System.Threading.Tasks;

using JetBrains.Annotations;

namespace LinqToDB.Async
{
	/// <summary>
	/// Provides a mechanism for releasing unmanaged resources asynchronously.
	/// </summary>
	[PublicAPI]
	public interface IAsyncDisposable
	{
		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or
		/// resetting unmanaged resources asynchronously.
		/// </summary>
		Task DisposeAsync();
	}
}
#endif
