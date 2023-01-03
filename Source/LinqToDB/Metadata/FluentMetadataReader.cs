using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace LinqToDB.Metadata
{
	using Common;
	using Common.Internal;
	using Extensions;
	using Mapping;

	public class FluentMetadataReader : IMetadataReader
	{
		readonly ConcurrentDictionary<Type,MappingAttribute[]>       _types          = new();
		readonly ConcurrentDictionary<MemberInfo,MappingAttribute[]> _members        = new();
		readonly ConcurrentDictionary<Type, MemberInfo[]>            _dynamicColumns = new();

		readonly MappingAttributesCache _cache = new (static source => (MappingAttribute[])source.GetCustomAttributes(typeof(MappingAttribute), inherit: false));

		private MappingAttribute[] GetAllAttributes(ICustomAttributeProvider attributeProvider)
		{
			if (attributeProvider is Type       type) return _types  .TryGetValue(type, out var typeAttributes  ) ? typeAttributes   : Array<MappingAttribute>.Empty;
			if (attributeProvider is MemberInfo mi  ) return _members.TryGetValue(mi  , out var memberAttributes) ? memberAttributes : Array<MappingAttribute>.Empty;
			return Array<MappingAttribute>.Empty;
		}

		static bool IsSystemOrNullType(Type? type)
			=> type == null || type == typeof(object) || type == typeof(ValueType) || type == typeof(Enum);

		public FluentMetadataReader(IReadOnlyDictionary<Type, List<MappingAttribute>> typeAttributes, IReadOnlyDictionary<MemberInfo, List<MappingAttribute>> memberAttributes)
		{
			_types   = new(typeAttributes  .Select(kvp => new KeyValuePair<Type      , MappingAttribute[]>(kvp.Key, kvp.Value.ToArray())));
			_members = new(memberAttributes.Select(kvp => new KeyValuePair<MemberInfo, MappingAttribute[]>(kvp.Key, kvp.Value.ToArray())));

			// dynamic columns collection
			Dictionary<Type,List<MemberInfo>>? dynamicColumns = null;
			foreach (var mi in memberAttributes.Keys)
			{
				if (mi.IsDynamicColumnPropertyEx())
				{
					if (!(dynamicColumns ??= new()).TryGetValue(mi.DeclaringType!, out var members))
						dynamicColumns.Add(mi.DeclaringType!, members = new());
					members.Add(mi);
				}
			}

			if (dynamicColumns != null)
			{
				// OrderBy: add stable ordering for same sql generation
				foreach (var kvp in dynamicColumns)
					_dynamicColumns.TryAdd(kvp.Key, kvp.Value.OrderBy(m => m.Name).ToArray());
			}
		}

		public T[] GetAttributes<T>(Type type)
			where T : MappingAttribute
		{
			if (_types.TryGetValue(type, out var attrs))
				lock (attrs)
					return attrs.OfType<T>().ToArray();

			var parents = new [] { type.BaseType }
				.Where(_ => !IsSystemOrNullType(_))
				.Concat(type.GetInterfaces())!;

			foreach(var p in parents)
			{
				var pattrs = GetAttributes<T>(p!);
				if (pattrs.Length > 0)
					return pattrs;
			}

			return Array<T>.Empty;
		}

		public T[] GetAttributes<T>(Type type, MemberInfo memberInfo)
			where T : MappingAttribute
		{
			if (memberInfo.DeclaringType != type)
				memberInfo = type.GetMemberEx(memberInfo) ?? memberInfo;

			if (_members.TryGetValue(memberInfo, out var attrs))
				return attrs.OfType<T>().ToArray();

			var parents = new [] { type.BaseType }
				.Where(_ => !IsSystemOrNullType(_))
				.Concat(type.GetInterfaces())!
				.Select(_ => new { Type = _!, Member = _!.GetMemberEx(memberInfo) })
				.Where(_ => _.Member != null)!;

			foreach(var p in parents)
			{
				var pattrs = GetAttributes<T>(p!.Type, p.Member!);
				if (pattrs.Length > 0)
					return pattrs;
			}

			return Array<T>.Empty;
		}

		/// <inheritdoc cref="IMetadataReader.GetDynamicColumns"/>
		public MemberInfo[] GetDynamicColumns(Type type)
		{
			if (_dynamicColumns.TryGetValue(type, out var dynamicColumns))
					return dynamicColumns;

			return Array<MemberInfo>.Empty;
		}

		/// <summary>
		/// Gets all types, registered by  by current fluent mapper.
		/// </summary>
		/// <returns>
		/// Returns array with all types, mapped by current fluent mapper.
		/// </returns>
		public IEnumerable<Type> GetRegisteredTypes()
		{
			return _types.Keys;
		}
	}
}
