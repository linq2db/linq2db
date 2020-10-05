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
		// don't forget to put lock on List<Attribute> when access it
		readonly ConcurrentDictionary<Type,List<Attribute>>                            _types          = new ConcurrentDictionary<Type,List<Attribute>>();
		// set used to guarantee uniqueness
		// list used to guarantee same order for columns in select queries
		readonly ConcurrentDictionary<Type,Tuple<ISet<MemberInfo>, IList<MemberInfo>>> _dynamicColumns = new ConcurrentDictionary<Type,Tuple<ISet<MemberInfo>, IList<MemberInfo>>>();

		private static bool IsSystemOrNullType(Type? type)
			=> type == null || type == typeof(object) || type == typeof(ValueType) || type == typeof(Enum);

		public T[] GetAttributes<T>(Type type, bool inherit = true)
			where T : Attribute
		{
			if (_types.TryGetValue(type, out var attrs))
				lock (attrs)
					return attrs.OfType<T>().ToArray();

			if (!inherit)
				return Array<T>.Empty;

			var parents = new [] { type.BaseType }
				.Where(_ => !IsSystemOrNullType(_))
				.Concat(type.GetInterfaces())!;

			foreach(var p in parents)
			{
				var pattrs = GetAttributes<T>(p!, inherit);
				if (pattrs.Length > 0)
					return pattrs;
			}

			return Array<T>.Empty;
		}

		public void AddAttribute(Type type, Attribute attribute)
		{
			var attrs = _types.GetOrAdd(type, t => new List<Attribute>());

			lock (attrs)
				attrs.Add(attribute);
		}

		readonly ConcurrentDictionary<MemberInfo,List<Attribute>> _members = new ConcurrentDictionary<MemberInfo,List<Attribute>>();

		public T[] GetAttributes<T>(Type type, MemberInfo memberInfo, bool inherit = true)
			where T : Attribute
		{
			if (memberInfo.DeclaringType != type)
				memberInfo = type.GetMemberEx(memberInfo) ?? memberInfo;

			if (_members.TryGetValue(memberInfo, out var attrs))
				return attrs.OfType<T>().ToArray();

			if (inherit == false)
				return Array<T>.Empty;

			var parents = new [] { type.BaseType }
				.Where(_ => !IsSystemOrNullType(_))
				.Concat(type.GetInterfaces())!
				.Select(_ => new { Type = _!, Member = _!.GetMemberEx(memberInfo) })
				.Where(_ => _.Member != null)!;

			foreach(var p in parents)
			{
				var pattrs = GetAttributes<T>(p!.Type, p.Member!, inherit);
				if (pattrs.Length > 0)
					return pattrs;
			}

			return Array<T>.Empty;
		}

		public void AddAttribute(MemberInfo memberInfo, Attribute attribute)
		{
			if (memberInfo.IsDynamicColumnPropertyEx())
			{
				var dynamicColumns = _dynamicColumns.GetOrAdd(memberInfo.DeclaringType!, new Tuple<ISet<MemberInfo>, IList<MemberInfo>>(new HashSet<MemberInfo>(), new List<MemberInfo>()));

				lock (dynamicColumns)
					if (dynamicColumns.Item1.Add(memberInfo))
						dynamicColumns.Item2.Add(memberInfo);
			}

			_members.GetOrAdd(memberInfo, t => new List<Attribute>()).Add(attribute);
		}

		/// <inheritdoc cref="IMetadataReader.GetDynamicColumns"/>
		public MemberInfo[] GetDynamicColumns(Type type)
		{
			if (_dynamicColumns.TryGetValue(type, out var dynamicColumns))
				lock (dynamicColumns)
					return dynamicColumns.Item2.ToArray();
			
			return Array<MemberInfo>.Empty;
		}

		/// <summary>
		/// Gets all types, registered by  by current fluent mapper.
		/// </summary>
		/// <returns>
		/// Returns array with all types, mapped by current fluent mapper.
		/// </returns>
		public Type[] GetRegisteredTypes()
		{
			// CD.Keys is probably thread-safe for enumeration (but it is not documented behavior)
			// https://stackoverflow.com/questions/10479867
			return _types.Keys.ToArray();
		}
	}
}
