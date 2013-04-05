using System;
using System.Collections.Generic;
using System.Data.Services.Providers;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.ServiceModel
{
	using Common;
	using Extensions;
	using Linq;
	using Mapping;
	using SqlBuilder;

	public class DataService<T> : System.Data.Services.DataService<T>, IServiceProvider
		where T : IDataContext
	{
		#region Init

		public DataService()
		{
			if (_defaultMetadata == null)
				_defaultMetadata = Tuple.Create(default(T), new MetadataInfo(MappingSchema.Default));

			_metadata = new MetadataProvider(_defaultMetadata.Item2);
			_query    = new QueryProvider   (_defaultMetadata.Item2);
			_update   = new UpdateProvider  (_defaultMetadata.Item2, _metadata, _query);
		}

		static Tuple<T,MetadataInfo> _defaultMetadata;

		public DataService(MappingSchema mappingSchema)
		{
			lock (_cache)
			{
				Tuple<T,MetadataInfo> data;

				if (!_cache.TryGetValue(mappingSchema, out data))
					data = Tuple.Create(default(T), new MetadataInfo(mappingSchema));

				_metadata = new MetadataProvider(data.Item2);
				_query    = new QueryProvider   (data.Item2);
				_update   = new UpdateProvider  (data.Item2, _metadata, _query);
			}
		}

		readonly static Dictionary<MappingSchema,Tuple<T,MetadataInfo>> _cache =
			new Dictionary<MappingSchema,Tuple<T,MetadataInfo>>();

		readonly MetadataProvider _metadata;
		readonly QueryProvider    _query;
		readonly UpdateProvider   _update;

		#endregion

		#region Public Members

		public object GetService(Type serviceType)
		{
			if (serviceType == typeof(IDataServiceMetadataProvider)) return _metadata;
			if (serviceType == typeof(IDataServiceQueryProvider))    return _query;
			if (serviceType == typeof(IDataServiceUpdateProvider))   return _update;

			return null;
		}

		#endregion

		#region MetadataInfo

		class TypeInfo
		{
			public ResourceType     Type;
			public SqlTable         Table;
			public EntityDescriptor Mapper;
		}

		class MetadataInfo
		{
			public MetadataInfo(MappingSchema mappingSchema)
			{
				_mappingSchema = mappingSchema;
				LoadMetadata();
			}

			readonly MappingSchema _mappingSchema;

			readonly public Dictionary<Type,TypeInfo>                  TypeDic     = new Dictionary<Type,TypeInfo>();
			readonly public Dictionary<string,ResourceType>            Types       = new Dictionary<string,ResourceType>();
			readonly public Dictionary<string,ResourceSet>             Sets        = new Dictionary<string,ResourceSet>();
			readonly public Dictionary<string,Func<object,IQueryable>> RootGetters = new Dictionary<string,Func<object,IQueryable>>();

			void LoadMetadata()
			{
				var n = 0;
				var list =
				(
					from p in typeof(T).GetProperties()
					let t   = p.PropertyType
					where typeof(ITable<>).IsSameOrParentOf(t)
					let tt  = t.GetGenericArguments()[0]
					let tbl = new SqlTable(_mappingSchema, tt)
					where tbl.Fields.Values.Any(f => f.IsPrimaryKey)
					let m   = _mappingSchema.GetEntityDescriptor(tt)
					select new
					{
						p.Name,
						ID     = n++,
						Type   = tt,
						Table  = tbl,
						Mapper = m
					}
				).ToList();

				var baseTypes = new Dictionary<Type,Type>();

				foreach (var item in list)
					foreach (var m in item.Mapper.InheritanceMapping)
						if (!baseTypes.ContainsKey(m.Type))
							baseTypes.Add(m.Type, item.Type);

				list.Sort((x,y) =>
				{
					Type baseType;

					if (baseTypes.TryGetValue(x.Type, out baseType))
						if (y.Type == baseType)
							return 1;

					if (baseTypes.TryGetValue(y.Type, out baseType))
						if (x.Type == baseType)
							return -1;

					return x.ID - y.ID;
				});

				foreach (var item in list)
				{
					Type baseType;
					baseTypes.TryGetValue(item.Type, out baseType);

					var type = GetTypeInfo(item.Type, baseType, item.Table, item.Mapper);
					var set  = new ResourceSet(item.Name, type.Type);

					set.SetReadOnly();

					Sets.Add(set.Name, set);
				}

				foreach (var item in list)
				{
					foreach (var m in item.Mapper.InheritanceMapping)
					{
						if (!TypeDic.ContainsKey(m.Type))
						{
							GetTypeInfo(
								m.Type,
								item.Type,
								new SqlTable(_mappingSchema, item.Type),
								_mappingSchema.GetEntityDescriptor(item.Type));
						}
					}
				}
			}

			TypeInfo GetTypeInfo(Type type, Type baseType, SqlTable table, EntityDescriptor mapper)
			{
				TypeInfo typeInfo;

				if (!TypeDic.TryGetValue(type, out typeInfo))
				{
					var baseInfo = baseType != null ? TypeDic[baseType] : null;

					typeInfo = new TypeInfo
					{
						Type   = new ResourceType(
							type,
							ResourceTypeKind.EntityType,
							baseInfo != null ? baseInfo.Type : null,
							type.Namespace,
							type.Name,
							type.IsAbstract),
						Table  = table,
						Mapper = mapper,
					};

					foreach (var field in table.Fields.Values)
					{
						if (baseType != null && baseInfo.Table.Fields.ContainsKey(field.Name))
							continue;

						var kind  = ResourcePropertyKind.Primitive;
						var ptype = ResourceType.GetPrimitiveResourceType(field.SystemType);

						if (baseType == null && field.IsPrimaryKey)
							kind |= ResourcePropertyKind.Key;

						var p = new ResourceProperty(field.Name, kind, ptype);

						typeInfo.Type.AddProperty(p);
					}

					typeInfo.Type.SetReadOnly();

					Types.  Add(typeInfo.Type.FullName, typeInfo.Type);
					TypeDic.Add(type, typeInfo);
				}

				return typeInfo;
			}

			public object CreateInstance(Type type)
			{
				return TypeDic[type].Mapper.TypeAccessor.CreateInstance();
			}
		}

		#endregion

		#region MetadataProvider

		class MetadataProvider : IDataServiceMetadataProvider
		{
			public MetadataProvider(MetadataInfo data)
			{
				_data = data;
			}

			readonly MetadataInfo _data;

			public bool TryResolveResourceSet(string name, out ResourceSet resourceSet)
			{
				return _data.Sets.TryGetValue(name, out resourceSet);
			}

			public ResourceAssociationSet GetResourceAssociationSet(ResourceSet resourceSet, ResourceType resourceType, ResourceProperty resourceProperty)
			{
				throw new NotImplementedException();
			}

			public bool TryResolveResourceType(string name, out ResourceType resourceType)
			{
				return _data.Types.TryGetValue(name, out resourceType);
			}

			public IEnumerable<ResourceType> GetDerivedTypes(ResourceType resourceType)
			{
				return _data.TypeDic[resourceType.InstanceType].Mapper.InheritanceMapping.Select(m => _data.TypeDic[m.Type].Type);
			}

			public bool HasDerivedTypes(ResourceType resourceType)
			{
				return _data.TypeDic[resourceType.InstanceType].Mapper.InheritanceMapping.Count > 0;
			}

			public bool TryResolveServiceOperation(string name, out ServiceOperation serviceOperation)
			{
				serviceOperation = null;
				return false;
			}

			public string                        ContainerNamespace { get { return typeof(T).Namespace; } }
			public string                        ContainerName      { get { return typeof(T).Name;      } }
			public IEnumerable<ResourceSet>      ResourceSets       { get { return _data.Sets.Values;   } }
			public IEnumerable<ResourceType>     Types              { get { return _data.Types.Values;  } }
			public IEnumerable<ServiceOperation> ServiceOperations  { get { return Enumerable.Empty<ServiceOperation>(); } }
		}

		#endregion

		#region QueryProvider

		class QueryProvider : IDataServiceQueryProvider
		{
			public QueryProvider(MetadataInfo data)
			{
				_data = data;
			}

			readonly MetadataInfo _data;

			public IQueryable GetQueryRootForResourceSet(ResourceSet resourceSet)
			{
				Func<object,IQueryable> func;

				lock (_data.RootGetters)
				{
					if (!_data.RootGetters.TryGetValue(resourceSet.Name, out func))
					{
						var p = Expression.Parameter(typeof(object), "p");
						var l = Expression.Lambda<Func<object,IQueryable>>(
							Expression.PropertyOrField(
								Expression.Convert(p, typeof(T)),
								resourceSet.Name),
							p);

						func = l.Compile();

						_data.RootGetters.Add(resourceSet.Name, func);
					}
				}

				return func(CurrentDataSource);
			}

			public ResourceType GetResourceType(object target)
			{
				return _data.TypeDic[target.GetType()].Type;
			}

			public object GetPropertyValue(object target, ResourceProperty resourceProperty)
			{
				throw new NotImplementedException();
			}

			public object GetOpenPropertyValue(object target, string propertyName)
			{
				throw new NotImplementedException();
			}

			public IEnumerable<KeyValuePair<string,object>> GetOpenPropertyValues(object target)
			{
				throw new NotImplementedException();
			}

			public object InvokeServiceOperation(ServiceOperation serviceOperation, object[] parameters)
			{
				throw new NotImplementedException();
			}

			public object CurrentDataSource         { get; set; }
			public bool   IsNullPropagationRequired { get { return true; } }
		}

		#endregion

		#region UpdateProvider

		abstract class ResourceAction
		{
			public object Resource;

			public class Create : ResourceAction {}
			public class Delete : ResourceAction {}
			public class Reset  : ResourceAction {}

			public class Update : ResourceAction
			{
				public string Property;
				public object Value;
			}
		}

		class UpdateProvider : IDataServiceUpdateProvider
		{
			#region Init

			public UpdateProvider(MetadataInfo data, MetadataProvider metadata, QueryProvider query)
			{
				_data     = data;
				_metadata = metadata;
				_query    = query;
			}

			readonly MetadataInfo         _data;
			readonly MetadataProvider     _metadata;
			readonly QueryProvider        _query;
			readonly List<ResourceAction> _actions = new List<ResourceAction>();

			#endregion

			#region IDataServiceUpdateProvider

			public void SetConcurrencyValues(object resourceCookie, bool? checkForEquality, IEnumerable<KeyValuePair<string,object>> concurrencyValues)
			{
				throw new NotImplementedException();
			}

			public void AddReferenceToCollection(object targetResource, string propertyName, object resourceToBeAdded)
			{
				throw new NotImplementedException();
			}

			public void ClearChanges()
			{
				_actions.Clear();
			}

			public object CreateResource(string containerName, string fullTypeName)
			{
				ResourceType type;

				if (_metadata.TryResolveResourceType(fullTypeName, out type)) 
				{
					var resource = _data.CreateInstance(type.InstanceType);
					_actions.Add(new ResourceAction.Create { Resource = resource });
					return resource; 
				}

				throw new Exception("Type '{0}' not found".Args(fullTypeName));
			}

			public void DeleteResource(object targetResource)
			{
				_actions.Add(new ResourceAction.Delete { Resource = targetResource });
			}

			public object GetResource(IQueryable query, string fullTypeName)
			{
				object resource = null;

				foreach (var item in query)
				{
					if (resource != null)
						throw new LinqException("Resource not uniquely identified");
					resource = item;
				}

				return resource;
			}

			public object GetValue(object targetResource, string propertyName)
			{
				var m = _data.TypeDic[targetResource.GetType()].Mapper;
				return m[propertyName].MemberAccessor.GetValue(targetResource);
			}

			public void RemoveReferenceFromCollection(object targetResource, string propertyName, object resourceToBeRemoved)
			{
				throw new NotImplementedException();
			}

			public object ResetResource(object resource)
			{
				_actions.Add(new ResourceAction.Reset { Resource = resource });
				return resource;
			}

			public object ResolveResource(object resource)
			{
				return resource;
			}

			public void SaveChanges()
			{
				throw new NotImplementedException();
			}

			public void SetReference(object targetResource, string propertyName, object propertyValue)
			{
				throw new NotImplementedException();
			}

			public void SetValue(object targetResource, string propertyName, object propertyValue)
			{
				var m = _data.TypeDic[targetResource.GetType()].Mapper;

				m[propertyName].MemberAccessor.SetValue(targetResource, propertyValue);

				_actions.Add(new ResourceAction.Update { Resource = targetResource, Property = propertyName, Value = propertyValue });
			}

			#endregion
		}

		#endregion
	}
}
