using System;

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

		public T[] GetAttributes<T>(Type type, string memberName)
			where T : Attribute
		{
			var member = type.GetMember(memberName);

			if (member.Length == 1)
			{
				var attrs = member[0].GetCustomAttributes(typeof(T), true);
				var arr   = new T[attrs.Length];

				for (var i = 0; i < attrs.Length; i++)
					arr[i] = (T)attrs[i];

				return arr;
			}

			return new T[0];
		}
	}
}
