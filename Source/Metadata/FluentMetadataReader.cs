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
		readonly ConcurrentDictionary<Type,List<Attribute>> _types = new ConcurrentDictionary<Type,List<Attribute>>();

		public T[] GetAttributes<T>(Type type, bool inherit = true)
			where T : Attribute
		{
			List<Attribute> attrs;
			return _types.TryGetValue(type, out attrs) ? attrs.OfType<T>().ToArray() : Array<T>.Empty;
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

			var parent = type.BaseTypeEx();
			if (parent == null || parent == typeof(object) || parent == typeof(ValueType) || parent == typeof(Enum))
				return Array<T>.Empty;

			var mi = parent.GetMemberEx(memberInfo);
			if (mi == null)
				return Array<T>.Empty;

			return GetAttributes<T>(parent, mi, inherit);
		}

		public void AddAttribute(MemberInfo memberInfo, Attribute attribute)
		{
			_members.GetOrAdd(memberInfo, t => new List<Attribute>()).Add(attribute);
		}
	}
}
