using System;
using System.Collections.Concurrent;

namespace LinqToDB.Mapping
{
	using Common;
	using Extensions;
	using Metadata;

	class MappingSchemaInfo
	{
		public MappingSchemaInfo(string configuration)
		{
			Configuration = configuration;
		}

		public string          Configuration;
		public IMetadataReader MetadataReader;

		#region Default Values

		volatile ConcurrentDictionary<Type,object> _defaultValues;

		public Option<object> GetDefaultValue(Type type)
		{
			if (_defaultValues == null)
				return Option<object>.None;

			object o;
			return _defaultValues.TryGetValue(type, out o) ? Option<object>.Some(o) : Option<object>.None;
		}

		public void SetDefaultValue(Type type, object value)
		{
			if (_defaultValues == null)
				lock (this)
					if (_defaultValues == null)
						_defaultValues = new ConcurrentDictionary<Type,object>();

			_defaultValues[type] = value;
		}

		#endregion

		#region ConvertInfo

		ConvertInfo _convertInfo;

		public void SetConvertInfo(Type from, Type to, ConvertInfo.LambdaInfo expr)
		{
			if (_convertInfo == null)
				_convertInfo = new ConvertInfo();
			_convertInfo.Set(from, to, expr);
		}

		public ConvertInfo.LambdaInfo GetConvertInfo(Type from, Type to)
		{
			return _convertInfo == null ? null : _convertInfo.Get(@from, to);
		}

		private ConcurrentDictionary<object,Func<object,object>> _converters;
		public  ConcurrentDictionary<object,Func<object,object>>  Converters
		{
			get { return _converters ?? (_converters = new ConcurrentDictionary<object,Func<object,object>>()); }
		}

		#endregion

		#region Scalar Types

		volatile ConcurrentDictionary<Type,bool> _scalarTypes;

		static readonly Option<bool> TrueOption = Option<bool>.Some(true);

		public Option<bool> GetScalarType(Type type)
		{
			type = type.ToNullableUnderlying();

			if (type.IsEnum || type.IsPrimitive)
				return TrueOption;

			if (_scalarTypes != null)
			{
				bool isScalarType;
				if (_scalarTypes.TryGetValue(type, out isScalarType))
					return Option<bool>.Some(isScalarType);
			}

			return Option<bool>.None;
		}

		public void SetScalarType(Type type, bool isScalarType = true)
		{
			if (_scalarTypes == null)
				lock (this)
					if (_scalarTypes == null)
						_scalarTypes = new ConcurrentDictionary<Type,bool>();

			_scalarTypes[type] = isScalarType;
		}

		#endregion
	}
}
