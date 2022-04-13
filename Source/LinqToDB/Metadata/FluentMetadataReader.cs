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
		// don't forget to put lock on List<Attribute> when access it
		readonly ConcurrentDictionary<Type,List<MappingAttribute>>               _types          = new();
		readonly ConcurrentDictionary<MemberInfo,List<MappingAttribute>>         _members        = new ();
		// set used to guarantee uniqueness
		// list used to guarantee same order for columns in select queries
		readonly ConcurrentDictionary<Type,(ISet<MemberInfo>,IList<MemberInfo>)> _dynamicColumns = new();

		static bool IsSystemOrNullType(Type? type)
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

		public void AddAttribute(Type type, MappingAttribute attribute)
		{
			var attrs = _types.GetOrAdd(type, static _ => new ());

			lock (attrs)
				attrs.Add(attribute);
		}

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

		public void AddAttribute(MemberInfo memberInfo, MappingAttribute attribute)
		{
			if (memberInfo.IsDynamicColumnPropertyEx())
			{
				var dynamicColumns = _dynamicColumns.GetOrAdd(memberInfo.DeclaringType!, (new HashSet<MemberInfo>(), new List<MemberInfo>()));

				lock (dynamicColumns.Item1)
					if (dynamicColumns.Item1.Add(memberInfo))
						dynamicColumns.Item2.Add(memberInfo);
			}

			_members.GetOrAdd(memberInfo, static _ => new ()).Add(attribute);
		}

		/// <inheritdoc cref="IMetadataReader.GetDynamicColumns"/>
		public MemberInfo[] GetDynamicColumns(Type type)
		{
			if (_dynamicColumns.TryGetValue(type, out var dynamicColumns))
				lock (dynamicColumns.Item1)
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

		public IEnumerable<string> GetObjectIDs()
		{
			foreach (var type in _types)
			{
				var sb = new StringBuilder(".")
					.Append(IdentifierBuilder.GetObjectID(type.Key))
					.Append('.')
					.Append(type.Value.Count)
					.Append('.')
					;

				foreach (var a in type.Value)
					sb.Append(a.GetObjectID()).Append('.');

				yield return sb.ToString();
			}

			foreach (var member in _members)
			{
				var sb = new StringBuilder(".")
					.Append(IdentifierBuilder.GetObjectID(member.Key.DeclaringType))
					.Append('.')
					.Append(member.Key.Name)
					.Append('.')
					.Append(member.Value.Count)
					.Append('.')
					;

				foreach (var a in member.Value)
					sb.Append(a.GetObjectID()).Append('.');

				yield return sb.ToString();
			}
		}
	}
}
