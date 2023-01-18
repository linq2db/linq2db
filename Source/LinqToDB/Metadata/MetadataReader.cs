using System;
using System.Reflection;
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

		private List<IMetadataReader>          _readers;
		public  IReadOnlyList<IMetadataReader>  Readers => _readers;

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
			var readers = _readers;
			if (readers.Count == 0)
				return Array<T>.Empty;
			if (readers.Count == 1)
				return readers[0].GetAttributes<T>(type, inherit);

			var length = 0;
			var attrs = new T[readers.Count][];

			for (var i = 0; i < readers.Count; i++)
			{
				attrs[i] = readers[i].GetAttributes<T>(type, inherit);
				length += attrs[i].Length;
			}

			var attributes = length == 0 ? Array<T>.Empty : new T[length];
			length = 0;

			for (var i = 0; i < attrs.Length; i++)
			{
				if (attrs[i].Length > 0)
				{
					Array.Copy(attrs[i], 0, attributes, length, attrs[i].Length);
					length += attrs[i].Length;
				}
			}

			return attributes;
		}

		public T[] GetAttributes<T>(Type type, MemberInfo memberInfo, bool inherit)
			where T : Attribute
		{
			var readers = _readers;
			if (readers.Count == 0)
				return Array<T>.Empty;
			if (readers.Count == 1)
				return readers[0].GetAttributes<T>(type, memberInfo, inherit);

			var attrs = new T[readers.Count][];
			var length = 0;

			for (var i = 0; i < readers.Count; i++)
			{
				attrs[i] = readers[i].GetAttributes<T>(type, memberInfo, inherit);
				length += attrs[i].Length;
			}

			var attributes = length == 0 ? Array<T>.Empty : new T[length];
			length = 0;

			for (var i = 0; i < attrs.Length; i++)
			{
				if (attrs[i].Length > 0)
				{
					Array.Copy(attrs[i], 0, attributes, length, attrs[i].Length);
					length += attrs[i].Length;
				}
			}

			return attributes;
		}

		/// <inheritdoc cref="IMetadataReader.GetDynamicColumns"/>
		public MemberInfo[] GetDynamicColumns(Type type)
		{
			var readers = _readers;
			if (readers.Count == 0)
				return Array<MemberInfo>.Empty;
			if (readers.Count == 1)
				return readers[0].GetDynamicColumns(type);

			var cols = new MemberInfo[readers.Count][];
			var length = 0;

			for (var i = 0; i < readers.Count; i++)
			{
				cols[i] = readers[i].GetDynamicColumns(type);
				length  += cols[i].Length;
			}

			var columns = length == 0 ? Array<MemberInfo>.Empty : new MemberInfo[length];
			length = 0;
			for (var i = 0; i < cols.Length; i++)
			{
				if (cols[i].Length > 0)
				{
					Array.Copy(cols[i], 0, columns, length, cols[i].Length);
					length += cols[i].Length;
				}
			}

			return columns;
		}
	}
}
