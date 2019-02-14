using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using JetBrains.Annotations;

namespace LinqToDB.Tools.EntityServices
{
	using Common;
	using Mapper;
	using Mapping;
	using Reflection;

	interface IEntityMap
	{
		void        MapEntity(EntityCreatedEventArgs args);
		IEnumerable GetEntities();
	}

	public class EntityMap<T> : IEntityMap
		where T : class
	{
		public EntityMap(IDataContext dataContext)
		{
			_entities = new ConcurrentDictionary<T,EntityEntry<T>>(dataContext.GetKeyEqualityComparer<T>());
		}

		volatile ConcurrentDictionary<T,EntityEntry<T>> _entities;

		[CanBeNull]
		public IReadOnlyDictionary<T,EntityEntry<T>> Entities => _entities as IReadOnlyDictionary<T,EntityEntry<T>>;

		void IEntityMap.MapEntity(EntityCreatedEventArgs args)
		{
			var entity = (T)args.Entity;
			var entry  = _entities.GetOrAdd(entity, key => new EntityEntry<T> { Entity = key });

			if (ReferenceEquals(args.Entity, entry.Entity) == false)
				args.Entity = entry.Entity;

			entry.IncrementDBCount();
		}

		IEnumerable IEntityMap.GetEntities()
		{
			return _entities?.Values ?? (IEnumerable)Array<T>.Empty;
		}

		interface IKeyComparer
		{
			T                        MapKey      (MappingSchema mappingSchema, object key);
			Expression<Func<T,bool>> GetPredicate(MappingSchema mappingSchema, object key);
		}

		class KeyComparer<TK> : IKeyComparer
		{
			Func<TK,T>           _mapper;
			List<MemberAccessor> _keyColumns;

			void CreateMapper(MappingSchema mappingSchema)
			{
				if (_mapper != null) return;

				var entityDesc = mappingSchema.GetEntityDescriptor(typeof(T));

				_keyColumns = entityDesc.Columns.Where (c => c.IsPrimaryKey).Select(c => c.MemberAccessor).ToList();

				if (_keyColumns.Count == 0)
					_keyColumns = entityDesc.Columns.Select(c => c.MemberAccessor).ToList();

				if (typeof(T) == typeof(TK))
				{
					_mapper = k => (T)(object)k;
					return;
				}

				if (mappingSchema.IsScalarType(typeof(TK)))
				{
					if (_keyColumns.Count != 1)
						throw new LinqToDBConvertException($"Type '{typeof(T).Name}' must contain only one key column.");

					_mapper = v =>
					{
						var e = entityDesc.TypeAccessor.CreateInstanceEx();
						_keyColumns[0].Setter(e, v);
						return (T)e;
					};
				}
				else
				{
					var fromNames = new HashSet<string>(TypeAccessor.GetAccessor<TK>().Members.Select(m => m.Name));

					foreach (var column in _keyColumns)
						if (!fromNames.Contains(column.Name))
							throw new LinqToDBConvertException($"Type '{typeof(TK).Name}' must contain field or property '{column.Name}'.");

					_mapper = Map.GetMapper<TK,T>(m => m.SetToMemberFilter(ma => _keyColumns.Count == 0 || _keyColumns.Contains(ma))).GetMapper();
				}
			}

			T IKeyComparer.MapKey(MappingSchema mappingSchema, object key)
			{
				CreateMapper(mappingSchema);

				return _mapper((TK)key);
			}

			public Expression<Func<T,bool>> GetPredicate(MappingSchema mappingSchema, object key)
			{
				var p = Expression.Parameter(typeof(T), "entity");

				Expression bodyExpression;

				if (mappingSchema.IsScalarType(typeof(TK)))
				{
					var keyExpression = Expression.Constant(new { Value = key });

					bodyExpression = Expression.Equal(
						Expression.PropertyOrField(p, _keyColumns[0].Name),
						Expression.Convert(Expression.PropertyOrField(keyExpression, "Value"), _keyColumns[0].Type));
				}
				else
				{
					var keyExpression = Expression.Constant(key);
					var expressions   = _keyColumns.Select(kc =>
						Expression.Equal(
							Expression.PropertyOrField(p, kc.Name),
							Expression.Convert(Expression.PropertyOrField(keyExpression, kc.Name), kc.Type)) as Expression);

					bodyExpression = expressions.Aggregate(Expression.AndAlso);
				}

				return Expression.Lambda<Func<T,bool>>(bodyExpression, p);
			}
		}

		volatile ConcurrentDictionary<Type,IKeyComparer> _keyComparers;

		[CanBeNull]
		public T GetEntity([JetBrains.Annotations.NotNull] IDataContext context, [JetBrains.Annotations.NotNull] object key)
		{
			if (context == null) throw new ArgumentNullException(nameof(context));
			if (key     == null) throw new ArgumentNullException(nameof(key));

			if (_keyComparers == null)
				lock (this)
					if (_keyComparers == null)
						_keyComparers = new ConcurrentDictionary<Type,IKeyComparer>();

			var keyComparer = _keyComparers.GetOrAdd(
				key.GetType(),
				type => (IKeyComparer)Activator.CreateInstance(typeof(KeyComparer<>).MakeGenericType(typeof(T), type)));

			var entity = keyComparer.MapKey(context.MappingSchema, key);

			if (_entities.TryGetValue(entity, out var entry))
			{
				entry.IncrementCacheCount();
				return entry.Entity;
			}

			return context.GetTable<T>().Where(keyComparer.GetPredicate(context.MappingSchema, key)).FirstOrDefault();
		}
	}
}
