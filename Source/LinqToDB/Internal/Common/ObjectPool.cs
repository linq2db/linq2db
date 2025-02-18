// based on
// https://raw.githubusercontent.com/dotnet/runtime/main/src/libraries/System.Reflection.Metadata/src/System/Reflection/Internal/Utilities/ObjectPool%601.cs
//
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;

namespace LinqToDB.Internal.Common
{
	/// <summary>
	/// Generic implementation of object pooling pattern with predefined pool size limit. The main
	/// purpose is that limited number of frequently used objects can be kept in the pool for
	/// further recycling.
	///
	/// Notes:
	/// 1) it is not the goal to keep all returned objects. Pool is not meant for storage. If there
	///    is no space in the pool, extra returned objects will be dropped.
	///
	/// 2) it is implied that if object was obtained from a pool, the caller will return it back in
	///    a relatively short time. Keeping checked out objects for long durations is ok, but
	///    reduces usefulness of pooling. Just new up your own.
	///
	/// Not returning objects to the pool in not detrimental to the pool's work, but is a bad practice.
	/// Rationale:
	///    If there is no intent for reusing the object, do not use pool - just use "new".
	/// </summary>
	internal sealed class ObjectPool<T> where T : class
	{
		private struct Element
		{
			internal T? Value;
		}

		public readonly struct RentedElement : IDisposable
		{
			private readonly ObjectPool<T> Pool;
			internal readonly T Value;

			public RentedElement(ObjectPool<T> pool, T value)
			{
				Pool = pool;
				Value = value;
			}

			public void Dispose()
			{
				Pool.Free(Value);
			}
		}

		// storage for the pool objects.
		private readonly Element[] _items;

		// factory is stored for the lifetime of the pool. We will call this only when pool needs to
		// expand. compared to "new T()", Func gives more flexibility to implementers and faster
		// than "new T()".
		private readonly Func<T> _factory;
		private readonly Action<T> _cleanup;

		internal ObjectPool(Func<T> factory, Action<T> cleanup, int size)
		{
			_factory = factory;
			_cleanup = cleanup;
			_items   = new Element[size];
		}

		private T CreateInstance()
		{
			return _factory();
		}

		/// <summary>
		/// Produces an instance.
		/// </summary>
		/// <remarks>
		/// Search strategy is a simple linear probing which is chosen for it cache-friendliness.
		/// Note that Free will try to store recycled objects close to the start thus statistically
		/// reducing how far we will typically search.
		/// </remarks>
		internal RentedElement Allocate()
		{
			var items = _items;
			T? inst;

			for (var i = 0; i < items.Length; i++)
			{
				// Note that the read is optimistically not synchronized. That is intentional.
				// We will interlock only when we have a candidate. in a worst case we may miss some
				// recently returned objects. Not a big deal.
				inst = items[i].Value;
				if (inst != null)
				{
					if (inst == Interlocked.CompareExchange(ref items[i].Value, null, inst))
					{
						return new (this, inst);
					}
				}
			}

			return new (this, CreateInstance());
		}

		/// <summary>
		/// Returns objects to the pool.
		/// </summary>
		/// <remarks>
		/// Search strategy is a simple linear probing which is chosen for it cache-friendliness.
		/// Note that Free will try to store recycled objects close to the start thus statistically
		/// reducing how far we will typically search in Allocate.
		/// </remarks>
		internal void Free(T obj)
		{
			_cleanup(obj);

			var items = _items;
			for (var i = 0; i < items.Length; i++)
			{
				if (items[i].Value == null)
				{
					// Intentionally not using interlocked here.
					// In a worst case scenario two objects may be stored into same slot.
					// It is very unlikely to happen and will only mean that one of the objects will get collected.
					items[i].Value = obj;
					break;
				}
			}
		}
	}
}
