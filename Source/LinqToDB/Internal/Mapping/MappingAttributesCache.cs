using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;

using LinqToDB.Internal.Extensions;
using LinqToDB.Mapping;

namespace LinqToDB.Internal.Mapping
{
	internal sealed class MappingAttributesCache
	{
		readonly record struct CacheKey(Type AttributeType, Key SourceKey);
		readonly record struct Key(ICustomAttributeProvider Source, Type? SourceOwner);

		private readonly Func<CacheKey, MappingAttribute[]?> _getMappingAttributesInternal;
		readonly ConcurrentDictionary<CacheKey, MappingAttribute[]?> _cache = new();

		readonly ConcurrentDictionary<Key, MappingAttribute[]> _noInheritMappingAttributes      = new ();
		readonly ConcurrentDictionary<Key, MappingAttribute[]> _orderedInheritMappingAttributes = new ();

		readonly Func<Type?, ICustomAttributeProvider, MappingAttribute[]> _attributesGetter;

		// Defensive bound against unbounded growth: an approximate cap on the number of cached entries in this
		// (per-schema) cache. When exceeded, all three dictionaries are cleared and repopulated on demand.
		// Captured once at construction from LinqToDB.Common.Configuration.MappingAttributesCacheMaxEntriesPerSchema; <= 0 disables it.
		readonly int _maxEntries;
		int          _entries;
		int          _clearing;

		/// <param name="attributesGetter">Raw attribute getter delegate for cache misses.</param>
		public MappingAttributesCache(Func<Type?, ICustomAttributeProvider, MappingAttribute[]> attributesGetter)
		{
			_attributesGetter                 = attributesGetter;
			_getMappingAttributesInternal     = GetMappingAttributesInternal;
			_getMappingAttributesTreeInternal = GetMappingAttributesTreeInternal;
			_maxEntries                       = LinqToDB.Common.Configuration.MappingAttributesCacheMaxEntriesPerSchema;
		}

		MappingAttribute[]? GetMappingAttributesInternal(CacheKey key)
		{
			// Runs once per _cache miss (the largest of the three dictionaries); approximate accounting for the
			// defensive bound enforced by MaybeTrim after the GetOrAdd returns.
			Interlocked.Increment(ref _entries);

			List<MappingAttribute>? results = null;

			foreach (var attr in _orderedInheritMappingAttributes.GetOrAdd(key.SourceKey, _getMappingAttributesTreeInternal))
				if (key.AttributeType.IsAssignableFrom(attr.GetType()))
					(results ??= new()).Add(attr);

			if (results != null)
			{
				var arr = (MappingAttribute[])Array.CreateInstance(key.AttributeType, results.Count);
				for (var i = 0; i < results.Count; i++)
					arr[i] = results[i];

				return arr;
			}

			return null;
		}

		MappingAttribute[] GetNoInheritMappingAttributes(in Key key)
		{
			var attrs = _noInheritMappingAttributes.GetOrAdd(key, static (key, attributesGetter) =>
			{
				var res = attributesGetter(key.SourceOwner, key.Source);

				return res.Length == 0 ? [] : res;
			}, _attributesGetter);

			return attrs;
		}

		readonly Func<Key, MappingAttribute[]> _getMappingAttributesTreeInternal;
		MappingAttribute[] GetMappingAttributesTreeInternal(Key key)
		{
			var attrs = GetNoInheritMappingAttributes(in key);

			var (type, getSource) = key.Source switch
			{
				Type { IsInterface: false } t =>
					(t, static (t, s) => t),

				MemberInfo m =>
					(m.ReflectedType, static (t, s) => t.GetMemberEx((MemberInfo)s)),

				_ => default((Type?, Func<Type, ICustomAttributeProvider, ICustomAttributeProvider?>?)),
			};

			if (type is null || getSource is null)
				return attrs;

			List<MappingAttribute>? list = null;

			foreach (var intf in type.GetInterfaces())
			{
				var src = getSource(intf, key.Source);
				if (src != null)
				{
					var ifaceAttrs = GetMappingAttributesTreeInternal(new(src, key.SourceOwner == null ? null : intf));
					if (ifaceAttrs.Length > 0)
					{
						if (list != null)
							list.AddRange(ifaceAttrs);
						else if (attrs.Length == 0)
							attrs = ifaceAttrs;
						else
							(list = [.. attrs]).AddRange(ifaceAttrs);
					}
				}
			}

			if (type.BaseType != null && type.BaseType != typeof(object))
			{
				var src = getSource(type.BaseType, key.Source);
				if (src != null)
				{
					var baseAttrs = GetMappingAttributesTreeInternal(new(src, key.SourceOwner == null ? null : type.BaseType));
					if (baseAttrs.Length > 0)
					{
						if (list != null)
							list.AddRange(baseAttrs);
						else if (attrs.Length == 0)
							attrs = baseAttrs;
						else
							(list = [.. attrs]).AddRange(baseAttrs);
					}
				}
			}

			return list?.ToArray() ?? attrs;
		}

		/// <summary>
		/// Determines whether a lookup for the given <paramref name="source"/> / <paramref name="sourceOwner"/> may be cached.
		/// Anonymous / compiler-generated types are minted per query shape (one per distinct projection), so caching
		/// them grows the schema caches without bound. Such types also carry no mapping attributes, so the result is
		/// always empty and callers short-circuit to an empty array instead of caching it.
		/// </summary>
		static bool IsCacheableKey(ICustomAttributeProvider source, Type? sourceOwner)
		{
			if (sourceOwner is not null && sourceOwner.IsAnonymous())
				return false;

			return source switch
			{
				Type       t => !t.IsAnonymous(),
				MemberInfo m => m.ReflectedType?.IsAnonymous() != true && m.DeclaringType?.IsAnonymous() != true,
				_            => true,
			};
		}

		/// <summary>
		/// Enforces the approximate per-schema entry bound: once the number of cached entries exceeds
		/// <see cref="LinqToDB.Common.Configuration.MappingAttributesCacheMaxEntriesPerSchema"/>, clears the caches so they are
		/// repopulated on demand. A bound of <c>0</c> or less disables the check.
		/// </summary>
		void MaybeTrim()
		{
			if (_maxEntries > 0 && Volatile.Read(ref _entries) > _maxEntries)
				ClearCaches();
		}

		void ClearCaches()
		{
			// Single-flight: only the thread that flips _clearing performs the clear; concurrent callers skip it
			// (a benign race — the bound is approximate and the next miss re-triggers the check when needed).
			if (Interlocked.CompareExchange(ref _clearing, 1, 0) != 0)
				return;

			try
			{
				_cache                          .Clear();
				_orderedInheritMappingAttributes.Clear();
				_noInheritMappingAttributes     .Clear();
				Interlocked.Exchange(ref _entries, 0);
			}
			finally
			{
				Volatile.Write(ref _clearing, 0);
			}
		}

		/// <summary>
		/// Returns a list of mapping attributes applied to a type or type member.
		/// If there are multiple attributes found, attributes ordered from current to base type in inheritance hierarchy.
		/// </summary>
		/// <param name="source">An attribute owner.</param>
		/// <typeparam name="T">The type of attribute to search for.
		/// Only attributes that are assignable to this type are returned.</typeparam>
		/// <returns>A list of custom attributes applied to this type,
		/// or a list with zero (0) elements if no attributes have been applied.</returns>
		public T[] GetMappingAttributes<T>(ICustomAttributeProvider source)
			where T : MappingAttribute
		{
			if (!IsCacheableKey(source, null))
				return [];

			// GetMappingAttributesInternal is not generic to avoid delegate allocation on each call
			var attrs = (T[]?)_cache.GetOrAdd(new(typeof(T), new(source, null)), _getMappingAttributesInternal) ?? [];

			MaybeTrim();

			return attrs;
		}

		/// <summary>
		/// Returns a list of mapping attributes applied to a type or type member.
		/// If there are multiple attributes found, attributes ordered from current to base type in inheritance hierarchy.
		/// </summary>
		/// <param name="sourceOwner">An attribute owner's owner type.</param>
		/// <param name="source">An attribute owner.</param>
		/// <typeparam name="T">The type of attribute to search for.
		/// Only attributes that are assignable to this type are returned.</typeparam>
		/// <returns>A list of custom attributes applied to this type,
		/// or a list with zero (0) elements if no attributes have been applied.</returns>
		public T[] GetMappingAttributes<T>(Type sourceOwner, ICustomAttributeProvider source)
			where T : MappingAttribute
		{
			if (!IsCacheableKey(source, sourceOwner))
				return [];

			// GetMappingAttributesInternal is not generic to avoid delegate allocation on each call
			var attrs = (T[]?)_cache.GetOrAdd(new(typeof(T), new(source, sourceOwner)), _getMappingAttributesInternal) ?? [];

			MaybeTrim();

			return attrs;
		}
	}
}
