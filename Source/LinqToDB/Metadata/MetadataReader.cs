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

			var length = 0;
			var attrs = new T[_readers.Count][];

			for (var i = 0; i < _readers.Count; i++)
			{
				attrs[i] = _readers[i].GetAttributes<T>(type, inherit);
				length += attrs[i].Length;
			}

			var attributes = new T[length];
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
			if (_readers.Count == 0)
				return Array<T>.Empty;
			if (_readers.Count == 1)
				return _readers[0].GetAttributes<T>(type, memberInfo, inherit);

			var attrs = new T[_readers.Count][];
			var length = 0;

			for (var i = 0; i < _readers.Count; i++)
			{
				attrs[i] = _readers[i].GetAttributes<T>(type, memberInfo, inherit);
				length += attrs[i].Length;
			}

			var attributes = new T[length];
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
			if (_readers.Count == 0)
				return Array<MemberInfo>.Empty;
			if (_readers.Count == 1)
				return _readers[0].GetDynamicColumns(type);

			var cols = new MemberInfo[_readers.Count][];
			var length = 0;

			for (var i = 0; i < _readers.Count; i++)
			{
				cols[i] = _readers[i].GetDynamicColumns(type);
				length  += cols[i].Length;
			}

			length = 0;
			var columns = new MemberInfo[length];
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
