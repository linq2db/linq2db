using System;
using System.Collections.Concurrent;
using System.Reflection;
using LinqToDB.Common;
using LinqToDB.Extensions;

namespace LinqToDB.Metadata
{
	// TODO: v4: replace arrays with IEnumerable and use generic GetCustomAttributes API
	public class AttributeReader : IMetadataReader
	{
		private static readonly ConcurrentDictionary<(Type type, Type attribute, bool inherit), Attribute[]>                        _typeAttributes   = new ();
		private static readonly ConcurrentDictionary<(Type type, MemberInfo memberInfo, Type attribute, bool inherit), Attribute[]> _memberAttributes = new ();

		public static readonly IMetadataReader Instance = new AttributeReader();

		private AttributeReader() { }

		public T[] GetAttributes<T>(Type type, bool inherit = true)
			where T : Attribute
		{
			return (T[])_typeAttributes.GetOrAdd(
				(type, attribute: typeof(T), inherit),
				static key =>
				{
					var attrs = key.type.GetCustomAttributes(key.attribute, key.inherit);
					if (attrs.Length == 0)
						return Array<T>.Empty;

					if (attrs is not T[] typedArr)
					{
						typedArr = new T[attrs.Length];
						Array.Copy(attrs, typedArr, typedArr.Length);
					}

					return typedArr;
				});
		}

		public T[] GetAttributes<T>(Type type, MemberInfo memberInfo, bool inherit = true)
			where T : Attribute
		{
			return (T[])_memberAttributes.GetOrAdd(
				(type, memberInfo, attribute: typeof(T), inherit),
				static key =>
				{
					var attrs = key.memberInfo.GetCustomAttributes(key.attribute, key.inherit);
					if (attrs.Length == 0)
					{
						if (key.inherit && key.type.BaseType != null &&
							key.type.BaseType != typeof(object))
						{
							var baseInfo = key.type.BaseType.GetMemberEx(key.memberInfo);
							if (baseInfo != null)
								return Instance.GetAttributes<T>(key.type.BaseType, baseInfo, true);
						}
					}

					if (attrs.Length == 0)
						return Array<T>.Empty;

					// doesn't make sense, but:
					// in some cases GetCustomAttributes returns untyped array (object[])
					if (attrs is not T[] typedArr)
					{
						typedArr = new T[attrs.Length];
						Array.Copy(attrs, typedArr, typedArr.Length);
					}

					return typedArr;
				});
		}

		/// <inheritdoc cref="IMetadataReader.GetDynamicColumns"/>
		public MemberInfo[] GetDynamicColumns(Type type) => Array<MemberInfo>.Empty;
	}
}
