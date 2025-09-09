#if !NETFRAMEWORK
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using LinqToDB;
using LinqToDB.Extensions;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	public class Issue5069Tests : TestBase
	{
		sealed class Issue5069
		{
			static int? UserId { get; set; } = 123;

			public interface IBaseEntity
			{ }

			public interface IIdentifiable<T> : IBaseEntity
			{
				T Id { get; set; }
			}

			[EntityFilter(typeof(IUserOwned<>), nameof(Filter))]
			public interface IUserOwned<out TOwner>
				where TOwner : IIdentifiable<int>
			{
				TOwner[] GetOwnerUsers();

				private static IQueryable<T> Filter<T>(IQueryable<T> q, IDataContext dbCtx)
					where T : IUserOwned<IIdentifiable<int>>
					=> q.Where(x => x.GetOwnerUsers().Any(y => y.Id == UserId));
			}

			public class Account : IBaseEntity, IIdentifiable<int>, IUserOwned<User>
			{
				[PrimaryKey]
				public int Id { get; set; }

				[Column]
				public int UserId { get; set; }

				[Association(CanBeNull = false, ThisKey = nameof(UserId), OtherKey = nameof(User.Id))]
				public User User { get; } = null!;

				[Association(ThisKey = nameof(UserId), OtherKey = nameof(User.Id), CanBeNull = false)]
				public User[] GetOwnerUsers() => null!;
			}

			public class User : IBaseEntity, IIdentifiable<int>
			{
				[PrimaryKey]
				public int Id { get; set; }

				[Association(ThisKey = nameof(Id), OtherKey = nameof(Account.UserId))]
				public Account[] Accounts { get; } = null!;
			}

			[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true)]
			public class EntityFilterAttribute(Type providerType, string propertyName) : Attribute
			{
				public Type ProviderType { get; } = providerType;

				public string PropertyName { get; } = propertyName;
			}

			internal static class EntityFilterHelper
			{
				public static Func<IQueryable<TEntity>, IDataContext, IQueryable<TEntity>>?
					GetEntityFilter<TEntity>()
				{
					var filters = FindAllInheritances(typeof(TEntity))
						.SelectMany(
							x => x
								.GetAttributes<EntityFilterAttribute>()
								.Select(y => (InheritType: x, Attribute: y)))
						.Select(x => GetLambda<TEntity>(x.InheritType, x.Attribute))
						.ToArray();

					return filters.Any()
						? (q, dbCtx) => filters.Aggregate(q, (currQ, nextFunc) => nextFunc(currQ, dbCtx))
						: null;
				}

				private static HashSet<Type> FindAllInheritances(Type entityType)
				{
					var relevantTypes = new HashSet<Type>();
					var processQueue = new Queue<Type>();

					processQueue.Enqueue(entityType);

					while (processQueue.TryDequeue(out var processType))
					{
						if (processType.BaseType != null)
							processQueue.Enqueue(processType.BaseType);

						foreach (var iFace in processType.GetInterfaces())
							processQueue.Enqueue(iFace);

						relevantTypes.Add(processType);
					}

					return relevantTypes;
				}

				private static Func<IQueryable<TEntity>, IDataContext, IQueryable<TEntity>> GetLambda<TEntity>(
					Type inheritType,
					EntityFilterAttribute entityFilter)
				{
					var providerType = entityFilter.ProviderType;

					if (providerType.IsGenericType)
						providerType = providerType.MakeGenericType(inheritType.GetGenericArguments());

					var methodInfo = providerType
						.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
						.FirstOrDefault(m => m.Name == entityFilter.PropertyName && m.IsGenericMethodDefinition)
						?.MakeGenericMethod(typeof(TEntity))
						?? throw new ArgumentException($"Method '{entityFilter.PropertyName}' not found in type '{providerType.FullName}' or is not a generic method definition.");

					var qParam = Expression.Parameter(typeof(IQueryable<TEntity>), "q");
					var dbCtxParam = Expression.Parameter(typeof(IDataContext), "dbCtx");

					var methodCall = Expression.Call(null, methodInfo, qParam, dbCtxParam);

					var lambda = Expression.Lambda<Func<IQueryable<TEntity>, IDataContext, IQueryable<TEntity>>>(
						methodCall,
						qParam,
						dbCtxParam);

					return lambda.Compile();
				}
			}

			public static class ModelBuilderExtensions
			{
				public static MappingSchema ApplyEntityFilters()
				{
					var ms = new MappingSchema();

					var builder = new FluentMappingBuilder(ms);
					var baseEntityType = typeof(IBaseEntity);

					ProcessEntity<Account>(builder);
					ProcessEntity<User>(builder);

					builder.Build();

					return ms;
				}

				private static void ProcessEntity<TEntity>(FluentMappingBuilder builder)
				{
					var filter = EntityFilterHelper.GetEntityFilter<TEntity>();

					if (filter != null)
						builder.Entity<TEntity>().HasQueryFilter(filter);
				}
			}
		}

		[Test]
		public void AssociationAsInterfaceMethod([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			var ms = Issue5069.ModelBuilderExtensions.ApplyEntityFilters();

			using var db = GetDataContext(context, ms);
			using var t1 = db.CreateLocalTable<Issue5069.User>();
			using var t2 = db.CreateLocalTable<Issue5069.Account>();

			var q = t2
				.Take(1)
				.Select(x => x.GetOwnerUsers());

			_ = q.ToList();
		}
	}
}
#endif
