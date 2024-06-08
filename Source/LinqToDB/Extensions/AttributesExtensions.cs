using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using JetBrains.Annotations;

namespace LinqToDB.Extensions
{
	using Common;

	/*
	 * 1. Implements library-wide Get(Custom)Attribute(s) cache.
	 * 2. Native reflection Get(Custom)Attribute(s) methods are banned to avoid direct non-cached queries for attributes.
	 *
	 * Behavior differences compared to runtime GetAttribute(s) methods:
	 * - results are cached and reused (runtime always create new attribute instances), so we cannot modify them to avoid cache poisoning
	 * - empty attribute lists explicitly replaced with Array.Empty<Attribute> instances. Runtime methods at least in .net framework don't use cached empty array instances
	 */
	[PublicAPI]
	public static class AttributesExtensions
	{
		static readonly ConcurrentDictionary<ICustomAttributeProvider, Attribute[]> _inheritAttributes   = new ();
		static readonly ConcurrentDictionary<ICustomAttributeProvider, Attribute[]> _noInheritAttributes = new ();

		static T[] GetAttributesInternal<T>(ICustomAttributeProvider source, bool inherit)
			where T : Attribute
		{
			var attrs = inherit ? GetInheritAttributes(source) : GetNoInheritAttributes(source);

			List<T>? results = null;
			foreach (var attr in attrs)
				if (typeof(T).IsAssignableFrom(attr.GetType()))
					(results ??= new()).Add((T)attr);

			return results == null ? [] : results.ToArray();
		}

		static Attribute[] GetNoInheritAttributes(ICustomAttributeProvider source)
		{
			var attrs = _noInheritAttributes.GetOrAdd(source, static source =>
			{
#pragma warning disable RS0030 // Do not used banned APIs
				var res = source.GetCustomAttributes(typeof(Attribute), inherit: false);
#pragma warning restore RS0030 // Do not used banned APIs
				// API returns object[] for old frameworks and typed array for new
				return res.Length == 0 ? [] : res is Attribute[] attrRes ? attrRes : res.Cast<Attribute>().ToArray();
			});
			return attrs;
		}

		static Attribute[] GetInheritAttributes(ICustomAttributeProvider source)
		{
#pragma warning disable RS0030 // Do not used banned APIs
			var attrs = _inheritAttributes.GetOrAdd(source, static source =>
			{
				// workaround for issue in non-generic GetCustomAttributes(inherit: true) API:
				// https://github.com/dotnet/runtime/issues/30219
				IEnumerable<Attribute>? attrs = null;

				     if (source is PropertyInfo pi) attrs = pi.GetCustomAttributes<Attribute>(inherit: true);
				else if (source is EventInfo    ei) attrs = ei.GetCustomAttributes<Attribute>(inherit: true);

				if (attrs != null)
				{
					// internally it returns Attribute[] already
					if (attrs is Attribute[] arr)
						return arr.Length == 0 ? [] : arr;

					arr = attrs.ToArray();
					return arr.Length == 0 ? [] : arr;
				}

				var res = source.GetCustomAttributes(typeof(Attribute), inherit: true);
				// API returns object[] for old frameworks and typed array for new
				return res.Length == 0 ? [] : res is Attribute[] attrRes ? attrRes : res.Cast<Attribute>().ToArray();
			});
			return attrs;
#pragma warning restore RS0030 // Do not used banned APIs
		}

		static class InheritAttributeCache<T>
			where T: Attribute
		{
			public static readonly ConcurrentDictionary<ICustomAttributeProvider, T[]> Cache = new();
		}

		static class NoInheritAttributeCache<T>
			where T : Attribute
		{
			public static readonly ConcurrentDictionary<ICustomAttributeProvider, T[]> Cache = new();
		}

		/// <summary>
		/// Returns a list of custom attributes applied to a type or type member.
		/// If there are multiple attributes found and <paramref name="inherit"/> set to <c>true</c>, attributes ordered from current to base type in inheritance hierarchy.
		/// </summary>
		/// <param name="source">An attribute owner.</param>
		/// <param name="inherit">When <c>true</c>, look up the hierarchy chain for the inherited custom attribute.</param>
		/// <typeparam name="T">The type of attribute to search for.
		/// Only attributes that are assignable to this type are returned.</typeparam>
		/// <returns>A list of custom attributes applied to this type,
		/// or a list with zero (0) elements if no attributes have been applied.</returns>
		public static T[] GetAttributes<T>(this ICustomAttributeProvider source, bool inherit = true)
			where T : Attribute
		{
			if (source == null) throw new ArgumentNullException(nameof(source));

			if (inherit)
				return InheritAttributeCache<T>.Cache.GetOrAdd(source, static source => GetAttributesInternal<T>(source, true));
			else
				return NoInheritAttributeCache<T>.Cache.GetOrAdd(source, static source => GetAttributesInternal<T>(source, false));
		}

		/// <summary>
		/// Retrieves first custom attribute applied to a type or type member.
		/// If there are multiple attributes found and <paramref name="inherit"/> set to <c>true</c>, attribute from <paramref name="source"/> preferred.
		/// </summary>
		/// <param name="source">An attribute owner.</param>
		/// <param name="inherit">When true, look up the hierarchy chain for the inherited custom attribute.</param>
		/// <typeparam name="T">The type of attribute to search for. Only attributes that are assignable to this type are returned.</typeparam>
		/// <returns>A reference to the first custom attribute of type <typeparamref name="T"/> that is applied to element, or <c>null</c> if there is no such attribute.</returns>
		public static T? GetAttribute<T>(this ICustomAttributeProvider source, bool inherit = true)
			where T : Attribute
		{
			var attrs = GetAttributes<T>(source, inherit);

			return attrs.Length > 0 ? attrs[0] : null;
		}

		/// <summary>
		/// Check if attribute that implements <typeparamref name="T"/> type exists on <paramref name="source"/>.
		/// </summary>
		/// <param name="source">An attribute owner.</param>
		/// <param name="inherit">When true, look up the hierarchy chain for the inherited custom attribute.</param>
		/// <typeparam name="T">The type of attribute to search for. Only attributes that are assignable to this type are covered.</typeparam>
		/// <returns>Returns <c>true</c> if at least one attribute found.</returns>
		public static bool HasAttribute<T>(this ICustomAttributeProvider source, bool inherit = true)
			where T : Attribute
		{
			return GetAttributes<T>(source, inherit).Length > 0;
		}
	}
}
