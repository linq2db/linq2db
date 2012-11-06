using System;
using System.Reflection;

namespace LinqToDB.Metadata
{
	public class AttributeReader : IMetadataReader
	{
		public T[] GetAttributes<T>(Type type)
			where T : Attribute
		{
			var attrs = type.GetCustomAttributes(typeof(T), true);
			var arr   = new T[attrs.Length];

			for (var i = 0; i < attrs.Length; i++)
				arr[i] = (T)attrs[i];

			return arr;
		}

		public T[] GetAttributes<T>(MemberInfo memberInfo)
			where T : Attribute
		{
			var attrs = memberInfo.GetCustomAttributes(typeof(T), true);
			var arr   = new T[attrs.Length];

			for (var i = 0; i < attrs.Length; i++)
				arr[i] = (T)attrs[i];

			return arr;
		}
	}
}
