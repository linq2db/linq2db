using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace LinqToDB.Metadata
{
	using Common;
	using Extensions;
	using Mapping;

	internal sealed class MappingAttributesCache
	{
		record struct CacheKey(Type AttributeType, Key SourceKey);
		record struct Key(ICustomAttributeProvider Source, Type? SourceOwner);

		private readonly Func<CacheKey, MappingAttribute[]?> _getMappingAttributesInternal;
		readonly ConcurrentDictionary<CacheKey, MappingAttribute[]?> _cache = new();

		readonly ConcurrentDictionary<Key, MappingAttribute[]> _noInheritMappingAttributes      = new ();
		readonly ConcurrentDictionary<Key, MappingAttribute[]> _orderedInheritMappingAttributes = new ();

		readonly Func<Type?, ICustomAttributeProvider, MappingAttribute[]> _attributesGetter;

		/// <param name="attributesGetter">Raw attribute getter delegate for cache misses.</param>
		public MappingAttributesCache(Func<Type?, ICustomAttributeProvider, MappingAttribute[]> attributesGetter)
		{
			_attributesGetter                 = attributesGetter;
			_getMappingAttributesInternal     = GetMappingAttributesInternal;
			_getMappingAttributesTreeInternal = GetMappingAttributesTreeInternal;
		}

		MappingAttribute[]? GetMappingAttributesInternal(CacheKey key)
		{
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

		MappingAttribute[] GetNoInheritMappingAttributes(Key key)
		{
#if NETFRAMEWORK
			var attrs = _noInheritMappingAttributes.GetOrAdd(key, key =>
			{
				var res = _attributesGetter(key.SourceOwner, key.Source);

				return res.Length == 0 ? Array<MappingAttribute>.Empty : res;
			});
#else
			var attrs = _noInheritMappingAttributes.GetOrAdd(key, static (key, attributesGetter) =>
			{
				var res = attributesGetter(key.SourceOwner, key.Source);

				return res.Length == 0 ? Array<MappingAttribute>.Empty : res;
			}, _attributesGetter);
#endif
			return attrs;
		}

		readonly Func<Key, MappingAttribute[]> _getMappingAttributesTreeInternal;
		MappingAttribute[] GetMappingAttributesTreeInternal(Key key)
		{
			var attrs = GetNoInheritMappingAttributes(key);

			Type? type = null;
			Func<Type, ICustomAttributeProvider, ICustomAttributeProvider?>? getSource = null;
			if (key.Source is Type t && !t.IsInterface)
			{
				type = t;
				getSource = static (t, s) => t;
			}
			else if (key.Source is MemberInfo m)
			{
				type      = m.ReflectedType;
				getSource = static (t, s) => t.GetMemberEx((MemberInfo)s);
			}

			if (type != null)
			{
				List<MappingAttribute>? list = null;

				foreach (var intf in type.GetInterfaces())
				{
					var src = getSource!(intf, key.Source);
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
								(list = new(attrs)).AddRange(ifaceAttrs);
						}
					}
				}

				if (type.BaseType != null && type.BaseType != typeof(object))
				{
					var src = getSource!(type.BaseType, key.Source);
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
								(list = new(attrs)).AddRange(baseAttrs);
						}
					}
				}

				if (list != null) return list.ToArray();
				if (attrs.Length > 0) return attrs;

				return Array<MappingAttribute>.Empty;
			}

			return attrs;
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
			// GetMappingAttributesInternal is not generic to avoid delegate allocation on each call
			return (T[]?)_cache.GetOrAdd(new(typeof(T), new(source, null)), _getMappingAttributesInternal) ?? Array<T>.Empty;
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
			// GetMappingAttributesInternal is not generic to avoid delegate allocation on each call
			return (T[]?)_cache.GetOrAdd(new(typeof(T), new(source, sourceOwner)), _getMappingAttributesInternal) ?? Array<T>.Empty;
		}
	}
}
