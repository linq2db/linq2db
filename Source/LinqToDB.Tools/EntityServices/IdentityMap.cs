using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

using JetBrains.Annotations;

namespace LinqToDB.Tools.EntityServices
{
	using Common.Internal;
	using Interceptors;

	[PublicAPI]
	public class IdentityMap : EntityServiceInterceptor, IDisposable
	{
		public IdentityMap(IDataContext dataContext)
		{
			_dataContext = dataContext ?? throw new ArgumentNullException(nameof(dataContext));
			_dataContext.AddInterceptor(this);
		}

		IDataContext?                                  _dataContext;
		readonly ConcurrentDictionary<Type,IEntityMap> _entityMapDic = new ();

		IEntityMap GetOrAddEntityMap(Type entityType)
		{
			return _entityMapDic.GetOrAdd(
				entityType,
				key => ActivatorExt.CreateInstance<IEntityMap>(typeof(EntityMap<>).MakeGenericType(key), _dataContext));
		}

		public IEnumerable GetEntities(Type entityType)
		{
			return GetOrAddEntityMap(entityType).GetEntities();
		}

		public IEnumerable<T> GetEntities<T>()
			where T : class
		{
			return GetEntityMap<T>().Entities.Values.Select(e => e.Entity);
		}

		public IEnumerable<EntityMapEntry<T>> GetEntityEntries<T>()
			where T : class
		{
			return GetEntityMap<T>().Entities.Values;
		}

		public EntityMap<T> GetEntityMap<T>()
			where T : class
		{
			return (EntityMap<T>)GetOrAddEntityMap(typeof(T));
		}

		public T? GetEntity<T>(object key)
			where T : class, new()
		{
			if (_dataContext != null)
				return GetEntityMap<T>().GetEntity(_dataContext, key);

			throw new ObjectDisposedException(nameof(IdentityMap));
		}

		public void Dispose()
		{
			_dataContext = null;
		}

		public override object EntityCreated(EntityCreatedEventData eventData, object entity)
		{
			if (_dataContext != null)
			{
				var args = new EntityCreatedEventArgs(_dataContext, entity);
				GetOrAddEntityMap(entity.GetType()).MapEntity(args);
				return args.Entity;
			}

			return entity;
		}
	}
}
