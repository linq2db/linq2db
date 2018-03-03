using System;
using System.Reflection;

using JetBrains.Annotations;

namespace LinqToDB.Metadata
{
	using Extensions;

	public class AttributeReader : IMetadataReader
	{
		[NotNull]
		public T[] GetAttributes<T>(Type type, bool inherit = true)
			where T : Attribute
		{
			var attrs = type.GetCustomAttributesEx(typeof(T), inherit);
			var arr   = new T[attrs.Length];

			for (var i = 0; i < attrs.Length; i++)
				arr[i] = (T)attrs[i];

			return arr;
		}

		[NotNull]
		public T[] GetAttributes<T>(Type type, MemberInfo memberInfo, bool inherit = true)
			where T : Attribute
		{
			var attrs = memberInfo.GetCustomAttributesEx(typeof(T), inherit);
			var arr   = new T[attrs.Length];

			for (var i = 0; i < attrs.Length; i++)
				arr[i] = (T)attrs[i];

			return arr;
		}
	}
}
