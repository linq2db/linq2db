using System;
using System.Linq;
using System.Reflection;

using JetBrains.Annotations;

namespace LinqToDB.Metadata
{
	public class MetadataReader : IMetadataReader
	{
		public static MetadataReader Default = new MetadataReader(
			new AttributeReader(),
			new SystemDataLinqAttributeReader()
		);

		public MetadataReader([NotNull] params IMetadataReader[] readers)
		{
			if (readers == null) throw new ArgumentNullException("readers");

			_readers = readers;
		}

		readonly IMetadataReader[] _readers;

		public T[] GetAttributes<T>(Type type)
			where T : Attribute
		{
			return _readers.SelectMany(r => r.GetAttributes<T>(type)).ToArray();
		}

		public T[] GetAttributes<T>(MemberInfo memberInfo)
			where T : Attribute
		{
			return _readers.SelectMany(r => r.GetAttributes<T>(memberInfo)).ToArray();
		}
	}
}
