using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using LinqToDB.Common;

namespace LinqToDB.Metadata
{
	/// <summary>
	/// Aggregation metadata reader, that just delegates all calls to nested readers.
	/// </summary>
	public class MetadataReader : IMetadataReader
	{
		public static MetadataReader Default = new (
			new AttributeReader()
			, new SystemComponentModelDataAnnotationsSchemaAttributeReader()
#if NETFRAMEWORK
			, new SystemDataLinqAttributeReader()
#endif
		);

		public MetadataReader(params IMetadataReader[] readers)
		{
			if (readers == null)
				throw new ArgumentNullException(nameof(readers));
			_readers = readers.ToList();
		}

		IList<IMetadataReader> _readers;

		internal void AddReader(IMetadataReader reader)
		{
			// creation of new list is cheaper than lock on each method call
			var newReaders = new List<IMetadataReader>(_readers.Count + 1) { reader };
			newReaders.AddRange(_readers);
			Volatile.Write(ref _readers, newReaders);
		}

		public T[] GetAttributes<T>(Type type, bool inherit)
			where T : Attribute
		{
			if (_readers.Count == 0)
				return Array<T>.Empty;
			if (_readers.Count == 1)
				return _readers[0].GetAttributes<T>(type, inherit);

			var attributes = new List<T>();

			foreach (var reader in _readers)
				attributes.AddRange(reader.GetAttributes<T>(type,  inherit));

			return attributes.ToArray();
		}

		public T[] GetAttributes<T>(Type type, MemberInfo memberInfo, bool inherit)
			where T : Attribute
		{
			if (_readers.Count == 0)
				return Array<T>.Empty;
			if (_readers.Count == 1)
				return _readers[0].GetAttributes<T>(type, memberInfo, inherit);

			var attributes = new List<T>();

			foreach (var reader in _readers)
				attributes.AddRange(reader.GetAttributes<T>(type, memberInfo, inherit));

			return attributes.ToArray();
		}

		/// <inheritdoc cref="IMetadataReader.GetDynamicColumns"/>
		public MemberInfo[] GetDynamicColumns(Type type)
		{
			if (_readers.Count == 0)
				return Array<MemberInfo>.Empty;
			if (_readers.Count == 1)
				return _readers[0].GetDynamicColumns(type);

			var columns = new List<MemberInfo>();

			foreach (var reader in _readers)
				columns.AddRange(reader.GetDynamicColumns(type));

			return columns.ToArray();
		}
	}
}
