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
#if !SILVERLIGHT && !NETFX_CORE
			, new SystemDataLinqAttributeReader()
			, new SystemDataSqlServerAttributeReader()
#endif
#if NETSTANDARD
			, new SystemComponentModelDataAnnotationsSchemaAttributeReader()
#endif
		);

		public MetadataReader([NotNull] params IMetadataReader[] readers)
		{
			if (readers == null) throw new ArgumentNullException("readers");

			_readers = readers;
		}

		readonly IMetadataReader[] _readers;

		public T[] GetAttributes<T>(Type type, bool inherit)
			where T : Attribute
		{
			return _readers.SelectMany(r => r.GetAttributes<T>(type, inherit)).ToArray();
		}

		public T[] GetAttributes<T>(MemberInfo memberInfo, bool inherit)
			where T : Attribute
		{
			return _readers.SelectMany(r => r.GetAttributes<T>(memberInfo, inherit)).ToArray();
		}
	}
}
