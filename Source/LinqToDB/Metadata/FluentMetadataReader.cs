using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

using LinqToDB.Extensions;
using LinqToDB.Internal.Common;
using LinqToDB.Mapping;

namespace LinqToDB.Metadata
{
	public class FluentMetadataReader : IMetadataReader
	{
		private readonly string _objectId;

		readonly ConcurrentDictionary<Type,MappingAttribute[]>       _types          = new();
		readonly ConcurrentDictionary<MemberInfo,MappingAttribute[]> _members        = new();
		readonly ConcurrentDictionary<Type, MemberInfo[]>            _dynamicColumns = new();

		readonly MappingAttributesCache _cache;

		public FluentMetadataReader(IReadOnlyDictionary<Type, List<MappingAttribute>> typeAttributes, IReadOnlyDictionary<MemberInfo, List<MappingAttribute>> memberAttributes, IReadOnlyList<MemberInfo> orderedMembers)
		{
			_types   = new(typeAttributes  .Select(kvp => new KeyValuePair<Type, MappingAttribute[]>      (kvp.Key, kvp.Value.ToArray())));
			_members = new(memberAttributes.Select(kvp => new KeyValuePair<MemberInfo, MappingAttribute[]>(kvp.Key, kvp.Value.ToArray())));

			// dynamic columns collection
			Dictionary<Type,List<MemberInfo>>? dynamicColumns = null;
			foreach (var mi in orderedMembers)
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
				foreach (var kvp in dynamicColumns)
					_dynamicColumns.TryAdd(kvp.Key, kvp.Value.ToArray());
			}

			_objectId = CalculateObjectID();
			_cache    = new(GetAllAttributes);
		}

		private MappingAttribute[] GetAllAttributes(Type? sourceType, ICustomAttributeProvider attributeProvider)
		{
			if (sourceType == null)
				return _types  .TryGetValue((Type      )attributeProvider, out var typeAttributes  ) ? typeAttributes   : [];
			else
				return _members.TryGetValue((MemberInfo)attributeProvider, out var memberAttributes) ? memberAttributes : [];
		}

		public MappingAttribute[] GetAttributes(Type type)
			=> _cache.GetMappingAttributes<MappingAttribute>(type);

		public MappingAttribute[] GetAttributes(Type type, MemberInfo memberInfo)
		{
			if (memberInfo.ReflectedType != type)
				memberInfo = type.GetMemberEx(memberInfo) ?? memberInfo;

			return _cache.GetMappingAttributes<MappingAttribute>(type, memberInfo);
		}

		/// <inheritdoc cref="IMetadataReader.GetDynamicColumns"/>
		public MemberInfo[] GetDynamicColumns(Type type)
		{
			if (_dynamicColumns.TryGetValue(type, out var dynamicColumns))
				return dynamicColumns;

			return [];
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
			using var sb = Pools.StringBuilder.Allocate();

			foreach (var type in _types)
			{
				sb.Value.Append('.')
					.Append(IdentifierBuilder.GetObjectID(type.Key))
					.Append('.')
					.Append(type.Value.Length.ToString(NumberFormatInfo.InvariantInfo))
					.Append('.')
					;

				foreach (var a in type.Value)
					sb.Value.Append(a.GetObjectID()).Append('.');
			}

			foreach (var member in _members)
			{
				sb.Value.Append('.')
					.Append(IdentifierBuilder.GetObjectID(member.Key.DeclaringType))
					.Append('.')
					.Append(member.Key.Name)
					.Append('.')
					.Append(member.Value.Length.ToString(NumberFormatInfo.InvariantInfo))
					.Append('.')
					;

				foreach (var a in member.Value)
					sb.Value.Append(a.GetObjectID()).Append('.');
			}

			foreach (var column in _dynamicColumns)
			{
				sb.Value.Append('.')
					.Append(IdentifierBuilder.GetObjectID(column.Key.DeclaringType))
					.Append('.')
					.Append(column.Key.Name)
					.Append('.')
					.Append(column.Value.Length.ToString(NumberFormatInfo.InvariantInfo))
					.Append('.')
					;

				foreach (var mi in column.Value)
					sb.Value.Append(IdentifierBuilder.GetObjectID(mi)).Append('.');
			}

			return sb.Value.ToString();
		}
	}
}
