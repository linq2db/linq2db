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
		private readonly string _objectId;

		readonly ConcurrentDictionary<Type,MappingAttribute[]>       _types          = new();
		readonly ConcurrentDictionary<MemberInfo,MappingAttribute[]> _members        = new();
		readonly ConcurrentDictionary<Type, MemberInfo[]>            _dynamicColumns = new();

		readonly MappingAttributesCache _cache;

		public FluentMetadataReader(IReadOnlyDictionary<Type, List<MappingAttribute>> typeAttributes, IReadOnlyDictionary<MemberInfo, List<MappingAttribute>> memberAttributes)
		{
			_types   = new(typeAttributes  .Select(kvp => new KeyValuePair<Type, MappingAttribute[]>      (kvp.Key, kvp.Value.ToArray())));
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

			_objectId = CalculateObjectID();
			_cache    = new(GetAllAttributes);
		}

		private MappingAttribute[] GetAllAttributes(ICustomAttributeProvider attributeProvider)
		{
			if (attributeProvider is Type       type) return _types  .TryGetValue(type, out var typeAttributes  ) ? typeAttributes   : Array<MappingAttribute>.Empty;
			if (attributeProvider is MemberInfo mi  ) return _members.TryGetValue(mi  , out var memberAttributes) ? memberAttributes : Array<MappingAttribute>.Empty;
			return Array<MappingAttribute>.Empty;
		}

		public T[] GetAttributes<T>(Type type)
			where T : MappingAttribute
			=> _cache.GetMappingAttributes<T>(type);

		public T[] GetAttributes<T>(Type type, MemberInfo memberInfo)
			where T : MappingAttribute
		{
			if (memberInfo.ReflectedType != type)
				memberInfo = type.GetMemberEx(memberInfo) ?? memberInfo;

			return _cache.GetMappingAttributes<T>(memberInfo);
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

		public string GetObjectID() => _objectId;

		private string CalculateObjectID()
		{
			var sb = new StringBuilder();

			foreach (var type in _types)
			{
				sb.Append('.')
					.Append(IdentifierBuilder.GetObjectID(type.Key))
					.Append('.')
					.Append(type.Value.Length)
					.Append('.')
					;

				foreach (var a in type.Value)
					sb.Append(a.GetObjectID()).Append('.');
			}

			foreach (var member in _members)
			{
				sb.Append('.')
					.Append(IdentifierBuilder.GetObjectID(member.Key.DeclaringType))
					.Append('.')
					.Append(member.Key.Name)
					.Append('.')
					.Append(member.Value.Length)
					.Append('.')
					;

				foreach (var a in member.Value)
					sb.Append(a.GetObjectID()).Append('.');
			}

			foreach (var column in _dynamicColumns)
			{
				sb.Append('.')
					.Append(IdentifierBuilder.GetObjectID(column.Key.DeclaringType))
					.Append('.')
					.Append(column.Key.Name)
					.Append('.')
					.Append(column.Value.Length)
					.Append('.')
					;

				foreach (var mi in column.Value)
					sb.Append(IdentifierBuilder.GetObjectID(mi)).Append('.');
			}

			return sb.ToString();
		}
	}
}
