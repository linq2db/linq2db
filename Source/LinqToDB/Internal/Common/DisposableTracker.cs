using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LinqToDB.Internal.Common
{
	/// <summary>
	/// Default <see cref="IDisposableTracker"/> implementation. Holds a list of registered
	/// <see cref="IAsyncDisposable"/> resources and exposes a snapshot + a sync/async drain
	/// for the owning context to call during Dispose.
	/// </summary>
	internal sealed class DisposableTracker : IDisposableTracker
	{
		List<IAsyncDisposable>? _resources;

		public void Register(IAsyncDisposable resource)
		{
			ArgumentNullException.ThrowIfNull(resource);

			(_resources ??= new()).Add(resource);
		}

		public bool Unregister(IAsyncDisposable resource)
		{
			ArgumentNullException.ThrowIfNull(resource);

			return _resources?.Remove(resource) == true;
		}

		public IReadOnlyList<IAsyncDisposable> ActiveDisposables =>
			_resources is null ? Array.Empty<IAsyncDisposable>() : _resources.ToArray();

		/// <summary>
		/// Synchronously dispose every tracked resource, isolating exceptions per resource.
		/// Prefers <see cref="IDisposable"/> when the resource implements it (avoids
		/// sync-over-async on the common temp-table path).
		/// </summary>
		public void DisposeAll()
		{
			if (_resources is not { Count: > 0 } resources)
				return;

			var snapshot = resources.ToArray();
			_resources.Clear();

			foreach (var resource in snapshot)
			{
				try
				{
					if (resource is IDisposable syncDisp)
						syncDisp.Dispose();
					else
						resource.DisposeAsync().AsTask().GetAwaiter().GetResult();
				}
				catch
				{
					// Per-resource isolation: one bad temp table must not block close.
				}
			}
		}

		/// <summary>
		/// Asynchronously dispose every tracked resource, isolating exceptions per resource.
		/// </summary>
		public async Task DisposeAllAsync()
		{
			if (_resources is not { Count: > 0 } resources)
				return;

			var snapshot = resources.ToArray();
			_resources.Clear();

			foreach (var resource in snapshot)
			{
				try
				{
					await resource.DisposeAsync().ConfigureAwait(false);
				}
				catch
				{
					// Per-resource isolation: one bad temp table must not block close.
				}
			}
		}
	}
}
