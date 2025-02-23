using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

using LinqToDB.Common;
using LinqToDB.Common.Internal;
using LinqToDB.Expressions;
using LinqToDB.Internal.Common;
using LinqToDB.Metadata;
using LinqToDB.SqlQuery;

namespace LinqToDB.Mapping
{
	class MappingSchemaInfo : IConfigurationID
	{
		public MappingSchemaInfo(string configuration)
		{
			Configuration = configuration;

			if (configuration.Length == 0)
				_configurationID = 0;
		}

		public  string           Configuration;
		private MetadataReader? _metadataReader;
		readonly Lock           _syncRoot = new();

		public MetadataReader? MetadataReader
		{
			get => _metadataReader;
			set
			{
				_metadataReader = value;
				ResetID();
			}
		}

		#region Default Values

		volatile ConcurrentDictionary<Type,object?>? _defaultValues;

		public Option<object?> GetDefaultValue(Type type)
		{
			if (_defaultValues == null)
				return Option<object?>.None;

			return _defaultValues.TryGetValue(type, out var o) ? Option<object?>.Some(o) : Option<object?>.None;
		}

		public void SetDefaultValue(Type type, object? value, bool resetId = true)
		{
			if (_defaultValues == null)
				lock (_syncRoot)
					_defaultValues ??= new ();

			_defaultValues[type] = value;

			if (resetId)
				ResetID();
		}

		#endregion

		#region CanBeNull

		volatile ConcurrentDictionary<Type,bool>? _canBeNull;

		public Option<bool> GetCanBeNull(Type type)
		{
			if (_canBeNull == null)
				return Option<bool>.None;

			return _canBeNull.TryGetValue(type, out var o) ? Option<bool>.Some(o) : Option<bool>.None;
		}

		public void SetCanBeNull(Type type, bool value, bool resetId = true)
		{
			if (_canBeNull == null)
				lock (_syncRoot)
					_canBeNull ??= new ();

			_canBeNull[type] = value;

			if (resetId)
				ResetID();
		}

		#endregion

		#region GenericConvertProvider

		volatile Dictionary<Type,List<Type[]>>? _genericConvertProviders;

		public bool InitGenericConvertProvider(Type[] types, MappingSchema mappingSchema)
		{
			var changed = false;

			if (_genericConvertProviders != null)
			{
				lock (_genericConvertProviders)
				{
					foreach (var type in _genericConvertProviders)
					{
						var args = type.Key.GetGenericArguments();

						if (args.Length == types.Length)
						{
							var stop = false;
							foreach (var value in type.Value)
								if (value.SequenceEqual(types))
								{
									stop = true;
									break;
								}

							if (stop)
								continue;

							var gtype    = type.Key.MakeGenericType(types);
							var provider = ActivatorExt.CreateInstance<IGenericInfoProvider>(gtype);

							provider.SetInfo(new MappingSchema(this));

							type.Value.Add(types);

							changed = true;
						}
					}
				}
			}

			return changed;
		}

		public void SetGenericConvertProvider(Type type)
		{
			if (_genericConvertProviders == null)
				lock (_syncRoot)
					_genericConvertProviders ??= new Dictionary<Type,List<Type[]>>();

			if (!_genericConvertProviders.ContainsKey(type))
				lock (_genericConvertProviders)
					if (!_genericConvertProviders.ContainsKey(type))
						_genericConvertProviders[type] = new List<Type[]>();
		}

		#endregion

		#region ConvertInfo

		ConvertInfo? _convertInfo;

		public void SetConvertInfo(DbDataType from, DbDataType to, ConversionType conversionType, ConvertInfo.LambdaInfo expr, bool resetId)
		{
			_convertInfo ??= new ();
			_convertInfo.Set(from, to, conversionType, expr);

			if (resetId)
				ResetID();
		}

		public void SetConvertInfo(Type from, Type to, ConversionType conversionType, ConvertInfo.LambdaInfo expr)
		{
			SetConvertInfo(new DbDataType(from), new DbDataType(to), conversionType, expr, true);
		}

		public ConvertInfo.LambdaInfo? GetConvertInfo(DbDataType from, DbDataType to, ConversionType conversionType)
		{
			return _convertInfo?.Get(from, to, conversionType);
		}

		private ConcurrentDictionary<object,Func<object,object>>? _converters;
		public  ConcurrentDictionary<object,Func<object,object>>   Converters => _converters ??= new ();

		#endregion

		#region Scalar Types

		volatile ConcurrentDictionary<Type,bool>? _scalarTypes;

		public Option<bool> GetScalarType(Type type)
		{
			if (_scalarTypes != null && _scalarTypes.TryGetValue(type, out var isScalarType))
				return Option<bool>.Some(isScalarType);

			return Option<bool>.None;
		}

		public void SetScalarType(Type type, bool isScalarType = true)
		{
			if (_scalarTypes == null)
				lock (_syncRoot)
					_scalarTypes ??= new ();

			_scalarTypes[type] = isScalarType;

			ResetID();
		}

		#endregion

		#region DataTypes

		volatile ConcurrentDictionary<Type,SqlDataType>? _dataTypes;

		public Option<SqlDataType> GetDataType(Type type)
		{
			if (_dataTypes != null)
			{
				if (_dataTypes.TryGetValue(type, out var dataType))
					return Option<SqlDataType>.Some(dataType);
			}

			return Option<SqlDataType>.None;
		}

		public void SetDataType(Type type, DataType dataType)
		{
			SetDataType(type, new SqlDataType(dataType, type, null, null, null, null));
		}

		public void SetDataType(Type type, SqlDataType dataType)
		{
			if (_dataTypes == null)
				lock (_syncRoot)
					_dataTypes ??= new ();

			_dataTypes[type] = dataType;

			var nullableType = type.MakeNullable();
			if (nullableType != type)
				_dataTypes[nullableType] = new SqlDataType(dataType.Type.WithSystemType(dataType.Type.SystemType.MakeNullable()));

			ResetID();
		}

		#endregion

		#region Comparers

		private StringComparer? _columnNameComparer;
		public  StringComparer?  ColumnNameComparer
		{
			get => _columnNameComparer;
			set
			{
				_columnNameComparer = value;
				ResetID();
			}
		}

		#endregion

		#region Enum

		volatile ConcurrentDictionary<Type,Type>? _defaultFromEnumTypes;

		public Type? GetDefaultFromEnumType(Type enumType)
		{
			if (_defaultFromEnumTypes == null)
				return null;

			_defaultFromEnumTypes.TryGetValue(enumType, out var defaultFromType);
			return defaultFromType;
		}

		public void SetDefaultFromEnumType(Type enumType, Type defaultFromType)
		{
			if (_defaultFromEnumTypes == null)
				lock (_syncRoot)
					_defaultFromEnumTypes ??= new ();

			_defaultFromEnumTypes[enumType] = defaultFromType;

			ResetID();
		}

		#endregion

		#region EntityDescriptor

		/// <summary>
		/// Enumerates types, registered by FluentMetadataBuilder.
		/// </summary>
		/// <returns>
		/// Returns array with all types, mapped by fluent mappings.
		/// </returns>
		public IEnumerable<Type> GetRegisteredTypes() => MetadataReader?.GetRegisteredTypes() ?? [];

		#endregion

		#region ConfigurationID

		int? _configurationID;

		internal bool HasConfigurationID => _configurationID != null;

		public virtual void ResetID()
		{
			_configurationID = null;
		}

		/// <summary>
		/// Unique schema configuration identifier. For internal use only.
		/// </summary>
		public int ConfigurationID
		{
			get          => _configurationID ??= GenerateID();
			internal set => _configurationID = value;
		}

		protected virtual int GenerateID()
		{
			using var idBuilder = new IdentifierBuilder(Configuration);

			ProcessDictionary(_defaultValues);
			ProcessDictionary(_canBeNull);
			ProcessDictionary(_scalarTypes);
			ProcessDictionary(_dataTypes);
			ProcessDictionary(_defaultFromEnumTypes);

			void ProcessDictionary<T>(ConcurrentDictionary<Type,T>? dic)
			{
				idBuilder.Add(dic?.Count);

				if (dic?.Count > 0)
				{
					foreach (var (id, value) in
						from t in dic
						let id = IdentifierBuilder.GetObjectID(t.Key)
						orderby id
						select (id, t.Value))
					{
						idBuilder
							.Add(id)
							.Add(IdentifierBuilder.GetObjectID(value))
							;
					}
				}
			}

			if (_convertInfo == null)
				idBuilder.Add(string.Empty);
			else
				idBuilder.Add(_convertInfo.GetConfigurationID());

			idBuilder.Add(IdentifierBuilder.GetObjectID(_columnNameComparer));

			return idBuilder.CreateID();
		}

		public virtual bool IsLocked => false;

		#endregion
	}
}
