using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;

using LinqToDB.Internal.Extensions;
using LinqToDB.Mapping;

namespace LinqToDB.Metadata
{
	/// <summary>
	/// Aggregation metadata reader, that just delegates all calls to nested readers.
	/// </summary>
	public class MetadataReader : IMetadataReader
	{
		/// <summary>
		/// Default instance of <see cref="MetadataReader"/>, used by <see cref="MappingSchema.Default"/>.
		/// By default, contains only <see cref="AttributeReader"/>.
		/// </summary>
		public static MetadataReader Default = new (new AttributeReader());

		         Type[]?                                  _registeredTypes;
		readonly string                                   _objectId;
		readonly ConcurrentDictionary<Type, MemberInfo[]> _dynamicColumns = new();
		readonly Lock                                     _syncRoot = new();

		readonly IMetadataReader[]              _readers;
		public   IReadOnlyList<IMetadataReader> Readers => _readers;

		public MetadataReader(params IMetadataReader[] readers)
		{
			_readers  = readers ?? throw new ArgumentNullException(nameof(readers));
			_objectId = $"[{string.JoinStrings(',', _readers.Select(r => r.GetObjectID()))}]";
		}

		// Stateless fan-out: the active schema is threaded straight through to child readers on every call, so
		// schema-aware readers (e.g. EFCore / F#-option) resolve against it. This aggregator keeps no per-(type,
		// member) attribute cache of its own - memoization lives in MappingSchema's per-schema cache, which is 1:1
		// with a schema and already walks the inheritance tree. Holding no schema state, the aggregator can be
		// freely shared/borrowed across schemas without leaking one schema's answers to another.
		internal MappingAttribute[] GetAttributes(MappingSchema mappingSchema, Type type)
		{
			if (_readers.Length == 0)
				return [];
			if (_readers.Length == 1)
				return _readers[0].GetAttributes(mappingSchema, type);

			var attrs = new MappingAttribute[_readers.Length][];

			for (var i = 0; i < _readers.Length; i++)
				attrs[i] = _readers[i].GetAttributes(mappingSchema, type);

			return attrs.Flatten();
		}

		internal MappingAttribute[] GetAttributes(MappingSchema mappingSchema, Type type, MemberInfo memberInfo)
		{
			if (_readers.Length == 0)
				return [];
			if (_readers.Length == 1)
				return _readers[0].GetAttributes(mappingSchema, type, memberInfo);

			var attrs = new MappingAttribute[_readers.Length][];

			for (var i = 0; i < _readers.Length; i++)
				attrs[i] = _readers[i].GetAttributes(mappingSchema, type, memberInfo);

			return attrs.Flatten();
		}

		MappingAttribute[] IMetadataReader.GetAttributes(MappingSchema mappingSchema, Type type)
			=> GetAttributes(mappingSchema, type);

		MappingAttribute[] IMetadataReader.GetAttributes(MappingSchema mappingSchema, Type type, MemberInfo memberInfo)
			=> GetAttributes(mappingSchema, type, memberInfo);

		/// <inheritdoc cref="IMetadataReader.GetDynamicColumns"/>
		public MemberInfo[] GetDynamicColumns(Type type)
		{
			return _dynamicColumns.GetOrAdd(
				type,
				static (type, readers) =>
				{
					if (readers.Length == 0)
						return [];
					if (readers.Length == 1)
						return readers[0].GetDynamicColumns(type);

					var cols = new MemberInfo[readers.Length][];

					for (var i = 0; i < readers.Length; i++)
						cols[i] = readers[i].GetDynamicColumns(type);

					return cols.Flatten();
				},
				_readers);
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
				lock (_syncRoot)
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
