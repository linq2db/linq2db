using System;
using System.Reflection;
using System.Threading;

namespace LinqToDB.Metadata
{
	using System.Collections.Concurrent;
	using Common;
	using Extensions;
	using Mapping;

	/// <summary>
	/// Aggregation metadata reader, that just delegates all calls to nested readers.
	/// </summary>
	public class MetadataReader : IMetadataReader
	{
		/// <summary>
		/// Default instance of <see cref="MetadataReader"/>, used by <see cref="MappingSchema.Default"/>.
		/// By default contains only <see cref="AttributeReader"/>.
		/// </summary>
		public static MetadataReader Default = new (new AttributeReader());

		         Type[]?                                  _registeredTypes;
		readonly MappingAttributesCache                   _cache;
		readonly string                                   _objectId;
		readonly ConcurrentDictionary<Type, MemberInfo[]> _dynamicColumns = new();

		readonly IMetadataReader[]             _readers;
		public   IReadOnlyList<IMetadataReader> Readers => _readers;

		public MetadataReader(params IMetadataReader[] readers)
		{
			if (readers == null)
				throw new ArgumentNullException(nameof(readers));

			_readers  = readers;
			_objectId = $"[{string.Join(",", _readers.Select(r => r.GetObjectID()))}]";

			_cache = new(
				(type, source) =>
				{
					if (_readers.Length == 0)
						return Array<MappingAttribute>.Empty;
					if (_readers.Length == 1)
						if (type != null)
							return _readers[0].GetAttributes<MappingAttribute>(type, (MemberInfo)source);
						else
							return _readers[0].GetAttributes<MappingAttribute>((Type)source);

					var attrs = new MappingAttribute[_readers.Length][];

					for (var i = 0; i < _readers.Length; i++)
						if (type != null)
							attrs[i] = _readers[i].GetAttributes<MappingAttribute>(type, (MemberInfo)source);
						else
							attrs[i] = _readers[i].GetAttributes<MappingAttribute>((Type)source);

					return attrs.Flatten();
				});
		}

		public T[] GetAttributes<T>(Type type)
			where T : MappingAttribute
			=> _cache.GetMappingAttributes<T>(type);

		public T[] GetAttributes<T>(Type type, MemberInfo memberInfo)
			where T : MappingAttribute
			=> _cache.GetMappingAttributes<T>(type, memberInfo);

		/// <inheritdoc cref="IMetadataReader.GetDynamicColumns"/>
		public MemberInfo[] GetDynamicColumns(Type type)
		{
			return _dynamicColumns.GetOrAdd(type,
#if NET45 || NET46 || NETSTANDARD2_0
			type =>
			{
				if (_readers.Length == 0)
					return Array<MemberInfo>.Empty;
				if (_readers.Length == 1)
					return _readers[0].GetDynamicColumns(type);

				var cols = new MemberInfo[_readers.Length][];

				for (var i = 0; i < _readers.Length; i++)
					cols[i] = _readers[i].GetDynamicColumns(type);

				return cols.Flatten();
			}
#else
			static (type, readers) =>
			{
				if (readers.Length == 0)
					return Array<MemberInfo>.Empty;
				if (readers.Length == 1)
					return readers[0].GetDynamicColumns(type);

				var cols = new MemberInfo[readers.Length][];

				for (var i = 0; i < readers.Length; i++)
					cols[i] = readers[i].GetDynamicColumns(type);

				return cols.Flatten();
			}
			, _readers
#endif
			);
		}

		/// <summary>
		/// Enumerates types, registered by FluentMetadataBuilder.
		/// </summary>
		/// <returns>
		/// Returns array with all types, mapped by fluent mappings.
		/// </returns>
		public IReadOnlyList<Type> GetRegisteredTypes()
		{
			if (_registeredTypes == null)
			{
				lock (this)
				{
					_registeredTypes ??= Readers.OfType<FluentMetadataReader>().SelectMany(fr => fr.GetRegisteredTypes())
						.Concat(Readers.OfType<MetadataReader>().SelectMany(mr => mr.GetRegisteredTypes()))
						.Distinct()
						.ToArray();
				}
			}
			return _registeredTypes;
		}

		public string GetObjectID() => _objectId;
	}
}
