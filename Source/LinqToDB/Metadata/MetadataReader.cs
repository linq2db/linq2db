using System;
using System.Linq;
using System.Reflection;

using JetBrains.Annotations;

namespace LinqToDB.Metadata
{
	public class MetadataReader : IMetadataReader
	{
		public static MetadataReader Default = new MetadataReader(
			new AttributeReader()
#if NETSTANDARD1_6 || NETSTANDARD2_0
			, new SystemComponentModelDataAnnotationsSchemaAttributeReader()
#else
			, new SystemDataLinqAttributeReader()
			, new SystemDataSqlServerAttributeReader()
#endif
		);

		public MetadataReader([NotNull] params IMetadataReader[] readers)
		{
			_readers = readers ?? throw new ArgumentNullException(nameof(readers));
		}

		readonly IMetadataReader[] _readers;

		public T[] GetAttributes<T>(Type type, bool inherit)
			where T : Attribute
		{
			return _readers.SelectMany(r => r.GetAttributes<T>(type, inherit)).ToArray();
		}

		public T[] GetAttributes<T>(Type type, MemberInfo memberInfo, bool inherit)
			where T : Attribute
		{
			return _readers.SelectMany(r => r.GetAttributes<T>(type, memberInfo, inherit)).ToArray();
		}
	}
}
