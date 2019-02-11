using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

using JetBrains.Annotations;

namespace LinqToDB.Tools.EntityServices
{
	using Common;

	[PublicAPI]
	public class IdentityMap
	{
		public IdentityMap(IEntityServices entityServices)
		{
			entityServices.OnEntityCreated += EntityCreated;
		}

		readonly ConcurrentDictionary<Type,IEntityMap> _entityMapDic = new ConcurrentDictionary<Type,IEntityMap>();

		[NotNull]
		IEntityMap GetOrAddEntityMap(Type entityType)
		{
			return _entityMapDic.GetOrAdd(
				entityType,
				key => (IEntityMap)Activator.CreateInstance(typeof(EntityMap<>).MakeGenericType(key)));
		}

		void EntityCreated(EntityCreatedEventArgs args)
		{
			GetOrAddEntityMap(args.Entity.GetType()).StoreEntity(args);
		}

		[NotNull]
		public IEnumerable GetEntities(Type entityType)
		{
			return GetOrAddEntityMap(entityType).GetEntities();
		}

		[NotNull]
		public IEnumerable<T> GetEntities<T>()
			where T : class
		{
			return GetEntityMap<T>().Entities?.Values.Select(e => e.Entity) ?? Array<T>.Empty;
		}

		[NotNull]
		public IEnumerable<EntityEntry<T>> GetEntityEntries<T>()
			where T : class
		{
			return GetEntityMap<T>().Entities?.Values ?? Array<EntityEntry<T>>.Empty;
		}

		[NotNull]
		public EntityMap<T> GetEntityMap<T>()
			where T : class
		{
			return (EntityMap<T>)GetOrAddEntityMap(typeof(T));
		}

		[CanBeNull]
		public T GetEntity<T>([NotNull] IDataContext context, [NotNull] object key)
			where T : class, new()
		{
			return GetEntityMap<T>().GetEntity(context, key);
		}
	}
}
