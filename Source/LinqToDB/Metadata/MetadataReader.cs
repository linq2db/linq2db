using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;

using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.Mapping;
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
		readonly MappingAttributesCache                   _cache;
		readonly string                                   _objectId;
		readonly ConcurrentDictionary<Type, MemberInfo[]> _dynamicColumns = new();
		readonly Lock                                     _syncRoot = new();

		readonly IMetadataReader[]              _readers;
		readonly MappingSchema?                 _mappingSchema;
		public   IReadOnlyList<IMetadataReader> Readers => _readers;

		/// <summary>
		/// <see langword="true"/> when any nested reader implements <see cref="ISchemaAwareMetadataReader"/>.
		/// Such an aggregator must be paired with exactly one <see cref="MappingSchema"/> (its per-<c>(type, member)</c>
		/// cache holds schema-dependent output), so a schema-blind borrow of it across schemas must be re-bound
		/// via <see cref="WithSchema"/>.
		/// </summary>
		internal bool HasSchemaAwareReaders { get; }

		public MetadataReader(params IMetadataReader[] readers)
			: this(null, readers)
		{
		}

		// The active schema is captured here rather than threaded per call because the fan-out to child readers
		// goes through _cache, keyed by (type, member) with no schema. A schema-aware child's output depends on
		// the schema, so that cache is valid only when the aggregator is tied to one schema. Each combined schema
		// already builds its own fresh aggregator (CombineSchemas is memoized; AddMetadataReader builds fresh), so
		// capture keeps the existing cache correct for free. Threading per call would instead force a schema into
		// the hot-path cache key, or bypass the cache for schema-aware readers (recompute every resolution).
		// mappingSchema is null for the schema-blind fallback path (public ctor, MetadataReader.Default).
		internal MetadataReader(MappingSchema? mappingSchema, params IMetadataReader[] readers)
		{
			_readers       = readers ?? throw new ArgumentNullException(nameof(readers));
			_mappingSchema = mappingSchema;
			_objectId      = $"[{string.JoinStrings(',', _readers.Select(r => r.GetObjectID()))}]";

			var hasSchemaAware = false;
			foreach (var r in _readers)
				if (r is ISchemaAwareMetadataReader)
				{
					hasSchemaAware = true;
					break;
				}

			HasSchemaAwareReaders = hasSchemaAware;

			_cache = new(
				(type, source) =>
				{
					if (_readers.Length == 0)
						return [];
					if (_readers.Length == 1)
						return GetReaderAttributes(_readers[0], type, source);

					var attrs = new MappingAttribute[_readers.Length][];

					for (var i = 0; i < _readers.Length; i++)
						attrs[i] = GetReaderAttributes(_readers[i], type, source);

					return attrs.Flatten();
				});
		}

		// Dispatch to the schema-taking overload for a schema-aware child when this aggregator is bound to a
		// schema (the combined schema it resolves for); otherwise the schema-less fallback path.
		MappingAttribute[] GetReaderAttributes(IMetadataReader reader, Type? type, ICustomAttributeProvider source)
		{
			if (_mappingSchema != null && reader is ISchemaAwareMetadataReader schemaAware)
				return type != null
					? schemaAware.GetAttributes(_mappingSchema, type, (MemberInfo)source)
					: schemaAware.GetAttributes(_mappingSchema, (Type)source);

			return type != null
				? reader.GetAttributes(type, (MemberInfo)source)
				: reader.GetAttributes((Type)source);
		}

		/// <summary>
		/// Returns a copy of this aggregator bound to <paramref name="mappingSchema"/>, so nested
		/// <see cref="ISchemaAwareMetadataReader"/> children resolve against it. Same readers, same
		/// <see cref="GetObjectID"/>, fresh per-schema attribute cache.
		/// </summary>
		internal MetadataReader WithSchema(MappingSchema mappingSchema)
			=> new(mappingSchema, _readers);

		internal T[] GetAttributes<T>(Type type)
			where T : MappingAttribute
			=> _cache.GetMappingAttributes<T>(type);

		internal T[] GetAttributes<T>(Type type, MemberInfo memberInfo)
			where T: MappingAttribute
			=> _cache.GetMappingAttributes<T>(type, memberInfo);

		MappingAttribute[] IMetadataReader.GetAttributes(Type type)
			=> _cache.GetMappingAttributes<MappingAttribute>(type);

		MappingAttribute[] IMetadataReader.GetAttributes(Type type, MemberInfo memberInfo)
			=> _cache.GetMappingAttributes<MappingAttribute>(type, memberInfo);

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
