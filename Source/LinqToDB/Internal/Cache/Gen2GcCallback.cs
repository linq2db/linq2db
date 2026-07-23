// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

namespace LinqToDB.Internal.Cache
{
	/// <summary>
	/// Schedules a callback roughly once each Gen2 garbage collection, for as long as the callback
	/// keeps returning <see langword="true"/>. A faithful port of the runtime's internal
	/// <c>Gen2GcCallback</c> (as used by <c>Microsoft.Extensions.Caching.Memory.MemoryCache</c>).
	/// </summary>
	/// <remarks>
	/// The registration object holds a <see cref="GCHandleType.Weak"/> handle to <c>target</c>, so it
	/// never keeps the target alive; once the target is collected the callback stops. The callback runs
	/// on the finalizer thread — it must be cheap, non-blocking, and must never throw.
	/// </remarks>
	sealed class Gen2GcCallback
	{
		readonly Func<object, bool> _callback;   // returns false to stop re-registering
		GCHandle                    _handle;      // weak — does not root the target

		Gen2GcCallback(Func<object, bool> callback, object target)
		{
			_callback = callback;
			_handle   = GCHandle.Alloc(target, GCHandleType.Weak);

			// Do NOT call GC.ReRegisterForFinalize here. A finalizable object is already registered for
			// finalization at allocation; re-registering in the ctor double-registers it, and on .NET
			// Framework the finalizer then runs twice at process shutdown — the first pass frees _handle,
			// the second reads the freed handle (GCHandle.Target throws "Handle is not initialized"), an
			// unhandled finalizer-thread exception that crashes the process. The finalizer re-registers
			// itself for the next Gen2 GC; that is the only place re-registration belongs.
		}

		/// <summary>
		/// Runs <paramref name="callback"/> after each Gen2 GC, passing <paramref name="target"/>, until
		/// the callback returns <see langword="false"/> or the target is collected.
		/// </summary>
		public static void Register(Func<object, bool> callback, object target) => _ = new Gen2GcCallback(callback, target);

		// The finalizer IS the mechanism: the runtime runs it after each Gen2 GC, which is precisely how
		// the callback fires. This is the documented Gen2GcCallback pattern, so a finalizer is required.
#pragma warning disable MA0055 // Do not use finalizer
		~Gen2GcCallback()
		{
			// Resilient to being finalized more than once (per the GC.ReRegisterForFinalize contract):
			// once the handle has been freed there is nothing left to do, and reading Target would throw.
			if (!_handle.IsAllocated)
				return;

			var target = _handle.Target;   // weak: null once the target has been collected

			if (target == null)
			{
				_handle.Free();
				return;
			}

			var reRegister = true;

			try
			{
				reRegister = _callback(target);
			}
			finally
			{
				if (reRegister && !Environment.HasShutdownStarted && !IsFinalizingForUnload())
					GC.ReRegisterForFinalize(this);
				else
					_handle.Free();
			}
		}
#pragma warning restore MA0055

		static bool IsFinalizingForUnload()
		{
#if NETFRAMEWORK
			// .NET Framework has unloadable AppDomains — don't re-register during a domain unload
			// (matches the canonical Gen2GcCallback guard). Available since .NET 2.0.
			return AppDomain.CurrentDomain.IsFinalizingForUnload();
#else
			return false; // no unloadable AppDomains on modern .NET
#endif
		}
	}
}
