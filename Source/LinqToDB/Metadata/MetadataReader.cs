using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using JetBrains.Annotations;

namespace LinqToDB.Metadata
{
	/// <summary>
	/// Aggregation metadata reader, that just delegates all calls to nested readers.
	/// </summary>
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
			if (readers == null)
				throw new ArgumentNullException(nameof(readers));
			_readers = readers.ToArray();
		}

		IList<IMetadataReader> _readers;

		internal void AddReader(IMetadataReader reader)
		{
			// creation of new list is cheaper than lock on each method call
			_readers = new[] { reader }.Concat(_readers).ToArray();
		}

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

		/// <inheritdoc cref="IMetadataReader.GetDynamicColumns"/>
		public MemberInfo[] GetDynamicColumns(Type type)
			=> _readers.SelectMany(r => r.GetDynamicColumns(type)).ToArray();
	}
}
