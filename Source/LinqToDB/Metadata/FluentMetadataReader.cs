using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LinqToDB.Extensions;

namespace LinqToDB.Metadata
{
	using Common;

	public class FluentMetadataReader : IMetadataReader
	{
		readonly ConcurrentDictionary<Type,List<Attribute>>                       _types          = new ConcurrentDictionary<Type,List<Attribute>>();
		readonly ConcurrentDictionary<Type,ConcurrentDictionary<MemberInfo,byte>> _dynamicColumns = new ConcurrentDictionary<Type,ConcurrentDictionary<MemberInfo,byte>>();

		private static bool IsSystemOrNullType(Type type)
			=> type == null || type == typeof(object) || type == typeof(ValueType) || type == typeof(Enum);

		public T[] GetAttributes<T>(Type type, bool inherit = true)
			where T : Attribute
		{
			List<Attribute> attrs;
			if (_types.TryGetValue(type, out attrs))
				return attrs.OfType<T>().ToArray();

			if (!inherit)
				return Array<T>.Empty;

			var parents = new [] { type.BaseTypeEx() }
				.Where(_ => !IsSystemOrNullType(_))
				.Concat(type.GetInterfacesEx());

			foreach(var p in parents)
			{
				var pattrs = GetAttributes<T>(p, inherit);
				if (pattrs.Length > 0)
					return pattrs;
			}

			return Array<T>.Empty;
		}

		public void AddAttribute(Type type, Attribute attribute)
		{
			_types.GetOrAdd(type, t => new List<Attribute>()).Add(attribute);
		}

		readonly ConcurrentDictionary<MemberInfo,List<Attribute>> _members = new ConcurrentDictionary<MemberInfo,List<Attribute>>();

		public T[] GetAttributes<T>(Type type, MemberInfo memberInfo, bool inherit = true)
			where T : Attribute
		{
			List<Attribute> attrs;

			var got = _members.TryGetValue(memberInfo, out attrs);

			if (got)
				return attrs.OfType<T>().ToArray();

			if (inherit == false)
				return Array<T>.Empty;

			var parents = new [] { type.BaseTypeEx() }
				.Where(_ => !IsSystemOrNullType(_))
				.Concat(type.GetInterfacesEx())
				.Select(_ => new { Type = _, Member = _.GetMemberEx(memberInfo) })
				.Where(_ => _.Member != null);

			foreach(var p in parents)
			{
				var pattrs = GetAttributes<T>(p.Type, p.Member, inherit);
				if (pattrs.Length > 0)
					return pattrs;
			}

			return Array<T>.Empty;
		}

		public void AddAttribute(MemberInfo memberInfo, Attribute attribute)
		{
			if (memberInfo.IsDynamicColumnPropertyEx())
				_dynamicColumns.GetOrAdd(memberInfo.DeclaringType, new ConcurrentDictionary<MemberInfo, byte>()).TryAdd(memberInfo, 0);

			_members.GetOrAdd(memberInfo, t => new List<Attribute>()).Add(attribute);
		}

		/// <inheritdoc cref="IMetadataReader.GetDynamicColumns"/>
		public MemberInfo[] GetDynamicColumns(Type type)
			=> _dynamicColumns.TryGetValue(type, out var dynamicColumns) ? dynamicColumns.Keys.ToArray() : new MemberInfo[0];
	}
}
