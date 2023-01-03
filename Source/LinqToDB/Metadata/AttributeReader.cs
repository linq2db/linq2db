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

	public class AttributeReader : IMetadataReader
	{
		public T[] GetAttributes<T>(Type type)
			where T : MappingAttribute
			=> GetMappingAttributes<T>(type);

		public T[] GetAttributes<T>(Type type, MemberInfo memberInfo)
			where T : MappingAttribute
			=> GetMappingAttributes<T>(memberInfo);

		/// <inheritdoc cref="IMetadataReader.GetDynamicColumns"/>
		public MemberInfo[] GetDynamicColumns(Type type) => Array<MemberInfo>.Empty;

		#region MappingAttribute resolve cache

		static readonly ConcurrentDictionary<ICustomAttributeProvider, MappingAttribute[]> _noInheritMappingAttributes      = new ();
		static readonly ConcurrentDictionary<ICustomAttributeProvider, MappingAttribute[]> _orderedInheritMappingAttributes = new ();

		static T[] GetMappingAttributesInternal<T>(ICustomAttributeProvider source)
			where T : MappingAttribute
		{
			var res = _orderedInheritMappingAttributes.GetOrAdd(source, GetMappingAttributesTreeInternal).OfType<T>().ToArray();

			return res.Length == 0 ? Array<T>.Empty : res;
		}

		static MappingAttribute[] GetNoInheritMappingAttributes(ICustomAttributeProvider source)
		{
			var attrs = _noInheritMappingAttributes.GetOrAdd(source, static source =>
			{
#pragma warning disable RS0030 // Do not used banned APIs
				var res = (MappingAttribute[])source.GetCustomAttributes(typeof(MappingAttribute), inherit: false);
#pragma warning restore RS0030 // Do not used banned APIs

				return res.Length == 0 ? Array<MappingAttribute>.Empty : res;
			});
			return attrs;
		}

		static MappingAttribute[] GetMappingAttributesTreeInternal(ICustomAttributeProvider source)
		{
			var attrs = GetNoInheritMappingAttributes(source);

			if (source is not Type type || type.IsInterface)
				return attrs;

			var interfaces               = type.GetInterfaces();
			var nBaseInterfaces          = type.BaseType != null ? type.BaseType.GetInterfaces().Length : 0;
			List<MappingAttribute>? list = null;

			for (var i = 0; i < interfaces.Length; i++)
			{
				var intf = interfaces[i];

				if (i < nBaseInterfaces)
				{
					var getAttr = false;

					foreach (var mi in type.GetInterfaceMapEx(intf).TargetMethods)
					{
						// Check if the interface is reimplemented.
						if (mi.DeclaringType == type)
						{
							getAttr = true;
							break;
						}
					}

					if (getAttr == false)
						continue;
				}

				var ifaceAttrs = GetMappingAttributesTreeInternal(intf);
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

			if (type.BaseType != null && type.BaseType != typeof(object))
			{
				var baseAttrs = GetMappingAttributesTreeInternal(type.BaseType);
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

			if (list != null) return list.ToArray();
			if (attrs.Length > 0) return attrs;

			return Array<MappingAttribute>.Empty;
		}

		static class MappingAttributeCache<T>
			where T : MappingAttribute
		{
			public static readonly ConcurrentDictionary<ICustomAttributeProvider, T[]> Cache = new();
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
		static T[] GetMappingAttributes<T>(ICustomAttributeProvider source)
			where T : MappingAttribute
		{
			if (source == null) throw new ArgumentNullException(nameof(source));

			return MappingAttributeCache<T>.Cache.GetOrAdd(source, static source => GetMappingAttributesInternal<T>(source));
		}
		#endregion
	}
}
