using System;
using System.Reflection;
using LinqToDB.Common;
using LinqToDB.Common.Internal.Cache;

namespace LinqToDB.Metadata
{
	// TODO: v4: replace arrays with IEnumerable and use generic GetCustomAttributes API
	public class AttributeReader : IMetadataReader
	{
		private static readonly MemoryCache<(Type type, Type attribute, bool inherit)>             _typeAttributesCache   = new (new ());
		private static readonly MemoryCache<(MemberInfo memberInfo, Type attribute, bool inherit)> _memberAttributesCache = new (new ());

		public static void ClearCaches()
		{
			_typeAttributesCache.Clear();
			_memberAttributesCache.Clear();
		}

		public T[] GetAttributes<T>(Type type, bool inherit = true)
			where T : Attribute
		{
			return _typeAttributesCache.GetOrCreate(
				(type, attribute: typeof(T), inherit),
				static e =>
				{
					var attrs = e.Key.type.GetCustomAttributes(e.Key.attribute, e.Key.inherit);
					if (attrs.Length == 0)
						return Array<T>.Empty;

					var arr   = new T[attrs.Length];

					for (var i = 0; i < attrs.Length; i++)
						arr[i] = (T)attrs[i];

					return arr;
				});
			
		}

		public T[] GetAttributes<T>(Type type, MemberInfo memberInfo, bool inherit = true)
			where T : Attribute
		{
			return _memberAttributesCache.GetOrCreate(
				(memberInfo, attribute: typeof(T), inherit),
				static e =>
				{
					var attrs = e.Key.memberInfo.GetCustomAttributes(e.Key.attribute, e.Key.inherit);
					if (attrs.Length == 0)
						return Array<T>.Empty;

					var arr   = new T[attrs.Length];

					for (var i = 0; i < attrs.Length; i++)
						arr[i] = (T)attrs[i];

					return arr;
				});
		}

		/// <inheritdoc cref="IMetadataReader.GetDynamicColumns"/>
		public MemberInfo[] GetDynamicColumns(Type type)
			=> Array<MemberInfo>.Empty;
	}
}
