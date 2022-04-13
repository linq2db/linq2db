using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace LinqToDB.Mapping
{
	using Common;
	using Common.Internal;
	using Expressions;
	using Metadata;
	using SqlQuery;

	class MappingSchemaInfo
	{
		public MappingSchemaInfo(string configuration)
		{
			Configuration    = configuration;

			if (configuration.Length == 0)
				_configurationID = 0;
		}

		public  string           Configuration;
		private IMetadataReader? _metadataReader;

		public IMetadataReader? MetadataReader
		{
			get => _metadataReader;
			set
			{
				_metadataReader  = value;
				_configurationID = null;
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

		public void SetDefaultValue(Type type, object? value)
		{
			if (_defaultValues == null)
				lock (this)
					if (_defaultValues == null)
						_defaultValues = new ();

			_defaultValues[type] = value;
			_configurationID     = null;
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

		public void SetCanBeNull(Type type, bool value)
		{
			if (_canBeNull == null)
				lock (this)
					if (_canBeNull == null)
						_canBeNull = new ();

			_canBeNull[type] = value;
			_configurationID = null;
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
							var provider = (IGenericInfoProvider)Activator.CreateInstance(gtype)!;

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
				lock (this)
					if (_genericConvertProviders == null)
						_genericConvertProviders = new Dictionary<Type,List<Type[]>>();

			if (!_genericConvertProviders.ContainsKey(type))
				lock (_genericConvertProviders)
					if (!_genericConvertProviders.ContainsKey(type))
						_genericConvertProviders[type] = new List<Type[]>();
		}

		#endregion

		#region ConvertInfo

		ConvertInfo? _convertInfo;

		public void SetConvertInfo(DbDataType from, DbDataType to, ConvertInfo.LambdaInfo expr)
		{
			_convertInfo ??= new ();
			_convertInfo.Set(from, to, expr);
			_configurationID = null;
		}

		public void SetConvertInfo(Type from, Type to, ConvertInfo.LambdaInfo expr)
		{
			SetConvertInfo(new DbDataType(from), new DbDataType(to), expr);
		}

		public ConvertInfo.LambdaInfo? GetConvertInfo(DbDataType from, DbDataType to)
		{
			return _convertInfo?.Get(from, to);
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
				lock (this)
					if (_scalarTypes == null)
						_scalarTypes = new ();

			_scalarTypes[type] = isScalarType;
			_configurationID   = null;
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
				lock (this)
					if (_dataTypes == null)
						_dataTypes = new ();

			_dataTypes[type] = dataType;
			_configurationID = null;
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
				_configurationID    = null;
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
				lock (this)
					if (_defaultFromEnumTypes == null)
						_defaultFromEnumTypes = new ();

			_defaultFromEnumTypes[enumType] = defaultFromType;
			_configurationID                = null;
		}

		#endregion

		#region EntityDescriptor

		/// <summary>
		/// Enumerates types, registered by FluentMetadataBuilder.
		/// </summary>
		/// <returns>
		/// Returns array with all types, mapped by fluent mappings.
		/// </returns>
		public Type[] GetRegisteredTypes()
		{
			switch (MetadataReader)
			{
				case FluentMetadataReader fr :
					return fr.GetRegisteredTypes();
				case MetadataReader mr :
					return
					(
						from f in mr.Readers.OfType<FluentMetadataReader>()
						from t in f.GetRegisteredTypes()
						select t
					)
					.ToArray();
				default : return Array<Type>.Empty;
			}
		}

		#endregion

		#region ConfigurationID

		int? _configurationID;

		internal bool HasConfigurationID =>_configurationID != null;

		/// <summary>
		/// Unique schema configuration identifier. For internal use only.
		/// </summary>
		internal int ConfigurationID
		{
			get
			{
				if (_configurationID == null)
				{
					var idBuilder = new IdentifierBuilder(Configuration);

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

					var list = new List<FluentMetadataReader>();

					switch (MetadataReader)
					{
						case FluentMetadataReader fr :
							list.Add(fr);
							break;
						case MetadataReader mr :
							foreach (var r in mr.Readers)
								if (r is FluentMetadataReader fr)
									list.Add(fr);
							break;
					}

					idBuilder.Add(list.Count);

					if (list.Count > 0)
					{
						foreach (var id in
							from id in list
							from a in id.GetObjectIDs()
							orderby a
							select a)
						{
							idBuilder.Add(id);
						}
					}

					if (_convertInfo == null)
						idBuilder.Add(string.Empty);
					else
						idBuilder.Add(_convertInfo.GetConfigurationID());

					idBuilder.Add(IdentifierBuilder.GetObjectID(_columnNameComparer));

					_configurationID = idBuilder.CreateID();
				}

				return _configurationID.Value;
			}

			set => _configurationID = value;
		}

		internal int CreateID()
		{
			if (_configurationID == null || _configurationID.Value == 0)
				_configurationID = IdentifierBuilder.CreateNextID();
			return _configurationID.Value;
		}

		#endregion
	}
}
