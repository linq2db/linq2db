using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

using JetBrains.Annotations;

namespace LinqToDB.Tools.EntityServices
{
	using System.Diagnostics.CodeAnalysis;
	using Common;

	[PublicAPI]
	public class IdentityMap : IDisposable
	{
		public IdentityMap(IDataContext dataContext)
		{
			_dataContext = dataContext ?? throw new ArgumentNullException(nameof(dataContext));
			_dataContext.OnEntityCreated += OnEntityCreated;
		}

		readonly IDataContext                          _dataContext;
		readonly ConcurrentDictionary<Type,IEntityMap> _entityMapDic = new ConcurrentDictionary<Type,IEntityMap>();

		IEntityMap GetOrAddEntityMap(Type entityType)
		{
			return _entityMapDic.GetOrAdd(
				entityType,
				key => (IEntityMap)Activator.CreateInstance(typeof(EntityMap<>).MakeGenericType(key), _dataContext));
		}

		void OnEntityCreated(EntityCreatedEventArgs args)
		{
			GetOrAddEntityMap(args.Entity.GetType()).MapEntity(args);
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

		[return: MaybeNull]
		public T GetEntity<T>(object key)
			where T : class, new()
		{
			return GetEntityMap<T>().GetEntity(_dataContext, key);
		}

		public void Dispose()
		{
			_dataContext.OnEntityCreated -= OnEntityCreated;
		}
	}
}
