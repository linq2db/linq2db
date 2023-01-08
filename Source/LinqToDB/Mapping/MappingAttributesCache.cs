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
		readonly ConcurrentDictionary<Type, ConcurrentDictionary<ICustomAttributeProvider, MappingAttribute[]>> _cache = new();

		readonly ConcurrentDictionary<ICustomAttributeProvider, MappingAttribute[]> _noInheritMappingAttributes      = new ();
		readonly ConcurrentDictionary<ICustomAttributeProvider, MappingAttribute[]> _orderedInheritMappingAttributes = new ();

		readonly Func<ICustomAttributeProvider, MappingAttribute[]> _attributesGetter;

		/// <param name="attributesGetter">Raw attribute getter delegate for cache misses.</param>
		public MappingAttributesCache(Func<ICustomAttributeProvider, MappingAttribute[]> attributesGetter)
		{
			_attributesGetter = attributesGetter;
		}

		T[] GetMappingAttributesInternal<T>(ICustomAttributeProvider source)
			where T : MappingAttribute
		{
			var res = _orderedInheritMappingAttributes.GetOrAdd(source, GetMappingAttributesTreeInternal).OfType<T>().ToArray();

			return res.Length == 0 ? Array<T>.Empty : res;
		}

		MappingAttribute[] GetNoInheritMappingAttributes(ICustomAttributeProvider source)
		{
#if NET45 || NET46 || NETSTANDARD2_0
			var attrs = _noInheritMappingAttributes.GetOrAdd(source, source =>
			{
				var res = _attributesGetter(source);

				return res.Length == 0 ? Array<MappingAttribute>.Empty : res;
			});
#else
			var attrs = _noInheritMappingAttributes.GetOrAdd(source, static (source, attributesGetter) =>
			{
				var res = attributesGetter(source);

				return res.Length == 0 ? Array<MappingAttribute>.Empty : res;
			}, _attributesGetter);
#endif
			return attrs;
		}

		MappingAttribute[] GetMappingAttributesTreeInternal(ICustomAttributeProvider source)
		{
			var attrs = GetNoInheritMappingAttributes(source);

			Type? type = null;
			Func<Type, ICustomAttributeProvider, ICustomAttributeProvider?>? getSource = null;
			if (source is Type t && !t.IsInterface)
			{
				type = t;
				getSource = static (t, s) => t;
			}
			else if (source is MemberInfo m)
			{
				type      = m.ReflectedType;
				getSource = static (t, s) => t.GetMemberEx((MemberInfo)s);
			}

			if (type != null)
			{
				List<MappingAttribute>? list = null;

				foreach (var intf in type.GetInterfaces())
				{
					var src = getSource!(intf, source);
					if (src != null)
					{
						var ifaceAttrs = GetMappingAttributesTreeInternal(src);
						if (ifaceAttrs.Length > 0)
						{
							if (list != null)
								list.AddRange(ifaceAttrs);
							else if (attrs.Length == 0)
								attrs = ifaceAttrs;
							else
								(list ??= new(attrs)).AddRange(ifaceAttrs);
						}
					}
				}

				if (type.BaseType != null && type.BaseType != typeof(object))
				{
					var src = getSource!(type.BaseType, source);
					if (src != null)
					{
						var baseAttrs = GetMappingAttributesTreeInternal(src);
						if (baseAttrs.Length > 0)
						{
							if (list != null)
								list.AddRange(baseAttrs);
							else if (attrs.Length == 0)
								attrs = baseAttrs;
							else
								(list ??= new(attrs)).AddRange(baseAttrs);
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
			if (source == null) throw new ArgumentNullException(nameof(source));

			return (T[])_cache.GetOrAdd(typeof(T), t => new()).GetOrAdd(source, GetMappingAttributesInternal<T>);
		}
	}
}
