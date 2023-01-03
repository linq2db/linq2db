using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace LinqToDB.Metadata
{
	using Common;
	using Extensions;
	using Mapping;

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

		public T[] GetAttributes<T>(Type type)
			where T : MappingAttribute
		{
			var readers = _readers;
			if (readers.Count == 0)
				return Array<T>.Empty;
			if (readers.Count == 1)
				return readers[0].GetAttributes<T>(type);

			var attrs = new T[readers.Count][];

			for (var i = 0; i < readers.Count; i++)
				attrs[i] = readers[i].GetAttributes<T>(type);

			return attrs.Flatten();
		}

		public T[] GetAttributes<T>(Type type, MemberInfo memberInfo)
			where T : MappingAttribute
		{
			var readers = _readers;
			if (readers.Count == 0)
				return Array<T>.Empty;
			if (readers.Count == 1)
				return readers[0].GetAttributes<T>(type, memberInfo);

			var attrs = new T[readers.Count][];

			for (var i = 0; i < readers.Count; i++)
				attrs[i] = readers[i].GetAttributes<T>(type, memberInfo);

			return attrs.Flatten();
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

			for (var i = 0; i < readers.Count; i++)
				cols[i] = readers[i].GetDynamicColumns(type);

			return cols.Flatten();
		}

		/// <summary>
		/// Enumerates types, registered by FluentMetadataBuilder.
		/// </summary>
		/// <returns>
		/// Returns array with all types, mapped by fluent mappings.
		/// </returns>
		public IEnumerable<Type> GetRegisteredTypes()
		{
			return      Readers.OfType<FluentMetadataReader>().SelectMany(fr => fr.GetRegisteredTypes())
				.Concat(Readers.OfType<MetadataReader      >().SelectMany(mr => mr.GetRegisteredTypes()));
		}
	}
}
