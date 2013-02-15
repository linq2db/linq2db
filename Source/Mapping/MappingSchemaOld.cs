using System;
using System.Collections.Generic;

#region ReSharper disable
// ReSharper disable SuggestUseVarKeywordEvident
// ReSharper disable UseObjectOrCollectionInitializer
// ReSharper disable SuggestUseVarKeywordEverywhere
// ReSharper disable RedundantTypeArgumentsOfMethod
#endregion

namespace LinqToDB.Mapping
{
	using Extensions;
	using Reflection;
	using Reflection.Extension;
	using Reflection.MetadataProvider;

	public class MappingSchemaOld
	{
		public static implicit operator MappingSchema(MappingSchemaOld ms)
		{
			return ms.NewSchema;
		}

		public MappingSchema NewSchema = MappingSchema.Default;

		#region ObjectMapper Support

		private readonly Dictionary<Type,ObjectMapper> _mappers        = new Dictionary<Type,ObjectMapper>();
		private readonly Dictionary<Type,ObjectMapper> _pendingMappers = new Dictionary<Type,ObjectMapper>();

		public ObjectMapper GetObjectMapper(Type type)
		{
			ObjectMapper om;

			lock (_mappers)
			{
				if (_mappers.TryGetValue(type, out om))
					return om;

				// This object mapper is initializing right now.
				// Note that only one thread can access to _pendingMappers each time.
				//
				if (_pendingMappers.TryGetValue(type, out om))
					return om;

				om = CreateObjectMapper(type);

				if (om == null)
					throw new MappingException(
						string.Format("Cannot create object mapper for the '{0}' type.", type.FullName));

				_pendingMappers.Add(type, om);

				try
				{
					om.Init(this, type);
				}
				finally
				{
					_pendingMappers.Remove(type);
				}

				// Officially publish this ready to use object mapper.
				//
				SetObjectMapperInternal(type, om);

				return om;
			}
		}

		private void SetObjectMapperInternal(Type type, ObjectMapper om)
		{
			_mappers.Add(type, om);

			if (type.IsAbstract)
			{
				var actualType = TypeAccessor.GetAccessor(type).Type;

				if (!_mappers.ContainsKey(actualType))
					_mappers.Add(actualType, om);
			}
		}

		protected virtual ObjectMapper CreateObjectMapper(Type type)
		{
			var attr = type.GetFirstAttribute<ObjectMapperAttribute>();
			return attr == null ? CreateObjectMapperInstance(type) : attr.ObjectMapper;
		}

		protected virtual ObjectMapper CreateObjectMapperInstance(Type type)
		{
			return new ObjectMapper();
		}

		#endregion

		#region MetadataProvider

		private MetadataProviderBase _metadataProvider;
		public  MetadataProviderBase  MetadataProvider
		{
			//[DebuggerStepThrough] fix me
			get { return _metadataProvider ?? (_metadataProvider = CreateMetadataProvider()); }
			set { _metadataProvider = value; }
		}

		protected virtual MetadataProviderBase CreateMetadataProvider()
		{
			return MetadataProviderBase.CreateProvider();
		}

		#endregion

		#region Public Members

		public ExtensionList Extensions { get; set; }

		#endregion

		#region GetNullValue

		public virtual object GetNullValue(Type type)
		{
			return TypeAccessor.GetNullValue(type);
		}

		#endregion
	}
}
