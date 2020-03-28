using System;
using System.Reflection;

namespace LinqToDB.Metadata
{
	public class AttributeReader : IMetadataReader
	{
		public T[] GetAttributes<T>(Type type, bool inherit = true)
			where T : Attribute
		{
			var attrs = type.GetCustomAttributes(typeof(T), inherit);
			var arr   = new T[attrs.Length];

			for (var i = 0; i < attrs.Length; i++)
				arr[i] = (T)attrs[i];

			return arr;
		}

		public T[] GetAttributes<T>(Type type, MemberInfo memberInfo, bool inherit = true)
			where T : Attribute
		{
			var attrs = memberInfo.GetCustomAttributes(typeof(T), inherit);
			var arr   = new T[attrs.Length];

			for (var i = 0; i < attrs.Length; i++)
				arr[i] = (T)attrs[i];

			return arr;
		}

		/// <inheritdoc cref="IMetadataReader.GetDynamicColumns"/>
		public MemberInfo[] GetDynamicColumns(Type type)
			=> new MemberInfo[0];
	}
}
