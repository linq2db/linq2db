using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;

using LinqToDB.Internal.Cache;
using LinqToDB.Internal.Extensions;
using LinqToDB.Mapping;

namespace LinqToDB.Internal.Mapping
{
	internal sealed class MappingAttributesCache : ILinqToDBCache
	{
		readonly record struct CacheKey(Type AttributeType, Key SourceKey);
		readonly record struct Key(ICustomAttributeProvider Source, Type? SourceOwner);

		readonly ConcurrentDictionary<CacheKey, MappingAttribute[]?> _cache = new();

		readonly ConcurrentDictionary<Key, MappingAttribute[]> _noInheritMappingAttributes      = new ();
		readonly ConcurrentDictionary<Key, MappingAttribute[]> _orderedInheritMappingAttributes = new ();

		readonly Func<Type?, ICustomAttributeProvider, MappingAttribute[]> _attributesGetter;

		/// <summary>Stable identifier — kept for debugging/diagnostics.</summary>
		public string Name { get; }

		/// <summary>Total number of cached (non-empty) attribute entries across the three lookup maps.</summary>
		public long Count => _cache.Count + _noInheritMappingAttributes.Count + _orderedInheritMappingAttributes.Count;

		/// <param name="attributesGetter">Raw attribute getter delegate for cache misses.</param>
		/// <param name="name">Diagnostic identifier reported to the cache registry.</param>
		public MappingAttributesCache(Func<Type?, ICustomAttributeProvider, MappingAttribute[]> attributesGetter, string name = "MappingAttributes")
		{
			_attributesGetter = attributesGetter;
			Name              = name;
		}

		// Negative (empty-result) lookups are deliberately NOT cached. Most probed
		// (attributeType x member x owner) combinations carry no mapping attribute, so caching the empties
		// dominated the cache (~90% of entries) and drove unbounded growth (#5692). Recompute is cheap — the
		// underlying attribute fetch is cached one layer down (AttributesExtensions) — and infrequent, since
		// attribute lookups happen during EntityDescriptor construction, which is itself cached.

		MappingAttribute[]? GetMappingAttributesInternal(CacheKey key)
		{
			List<MappingAttribute>? results = null;

			foreach (var attr in GetOrderedInheritMappingAttributes(key.SourceKey))
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

		MappingAttribute[] GetOrderedInheritMappingAttributes(in Key sourceKey)
		{
			if (_orderedInheritMappingAttributes.TryGetValue(sourceKey, out var cached))
				return cached;

			var tree = GetMappingAttributesTreeInternal(sourceKey);

			// Cache only non-empty results (see class note).
			if (tree.Length != 0)
				_orderedInheritMappingAttributes.TryAdd(sourceKey, tree);

			return tree;
		}

		MappingAttribute[] GetNoInheritMappingAttributes(in Key key)
		{
			if (_noInheritMappingAttributes.TryGetValue(key, out var cached))
				return cached;

			var res = _attributesGetter(key.SourceOwner, key.Source);

			// Cache only non-empty results (see class note).
			if (res.Length == 0)
				return [];

			_noInheritMappingAttributes.TryAdd(key, res);

			return res;
		}

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

		T[] GetMappingAttributesFromCache<T>(in CacheKey key)
			where T : MappingAttribute
		{
			if (_cache.TryGetValue(key, out var cached))
				return (T[]?)cached ?? [];

			var result = GetMappingAttributesInternal(key);

			// Cache only non-empty results (see class note).
			if (result is { Length: > 0 })
				_cache.TryAdd(key, result);

			return (T[]?)result ?? [];
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
			=> GetMappingAttributesFromCache<T>(new(typeof(T), new(source, null)));

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
			=> GetMappingAttributesFromCache<T>(new(typeof(T), new(source, sourceOwner)));

		public void Clear()
		{
			_cache.Clear();
			_noInheritMappingAttributes.Clear();
			_orderedInheritMappingAttributes.Clear();
		}
	}
}
