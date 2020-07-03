﻿using System;
using System.Threading;
using System.Threading.Tasks;

using JetBrains.Annotations;

namespace LinqToDB.Async
{
	/// <summary>
	/// Asynchronous version of the IEnumerator&lt;T&gt; interface, allowing elements to be retrieved asynchronously.
	/// </summary>
	/// <typeparam name="T">Element type.</typeparam>
	[PublicAPI]
	public interface IAsyncEnumerator<out T> : IDisposable
	{
		/// <summary>Gets the current element in the iteration.</summary>
		T Current { get; }

		/// <summary>
		/// Advances the enumerator to the next element in the sequence, returning the result asynchronously.
		/// </summary>
		/// <returns>
		/// Task containing the result of the operation: true if the enumerator was successfully advanced
		/// to the next element; false if the enumerator has passed the end of the sequence.
		/// </returns>
		Task<bool> MoveNextAsync();

		/// <summary>
		/// Disposes the object asynchonously.
		/// </summary>
		Task DisposeAsync();
	}
}
