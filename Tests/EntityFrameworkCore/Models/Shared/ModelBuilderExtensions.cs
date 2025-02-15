using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace LinqToDB.EntityFrameworkCore.Tests.Models.Shared
{
	public static class ModelBuilderExtensions
	{
		public static ModelBuilder UseIdAsKey(this ModelBuilder modelBuilder)
		{
			var entities = modelBuilder.Model.GetEntityTypes().Select(e => e.ClrType).ToHashSet();

			// For all entities in the data model
			foreach (var entityType in modelBuilder.Model.GetEntityTypes())
			{
				// Find the properties that are our strongly-typed ID
				var properties = entityType
					.ClrType
					.GetProperties()
					.Where(p =>
					{
						var unwrappedType = p.PropertyType.UnwrapNullable();
						return unwrappedType.IsGenericType && unwrappedType.GetGenericTypeDefinition() == typeof(Id<,>);
					});

				foreach (var property in properties)
				{
					var entity = property.PropertyType.UnwrapNullable().GetGenericArguments()[0];

					if (!entities.Contains(entity))
						continue;

					if (entity == entityType.ClrType && property.Name == "Id")
					{
						modelBuilder
							.Entity(entityType.Name)
							.HasKey(property.Name);
						continue;
					}

					var oneNavigation = entityType.ClrType.GetProperties()
						.SingleOrDefault(p => p.PropertyType == entity);
					var manyNavigation = entity.GetProperties()
						.SingleOrDefault(p =>
						{
							var pt = p.PropertyType;
							return pt.IsGenericType
								   && pt.GetGenericTypeDefinition() == typeof(IEnumerable<>)
								   && pt.GetGenericArguments()[0] == entityType.ClrType;
						});

					modelBuilder
						.Entity(entityType.Name)
						.HasOne(entity, oneNavigation?.Name)
						.WithMany(manyNavigation?.Name)
						.HasForeignKey(property.Name)
						.OnDelete(DeleteBehavior.Restrict);
				}
			}

			return modelBuilder;
		}

		public static ModelBuilder UseOneIdSequence<T>(this ModelBuilder modelBuilder, string sequenceName, Func<string, string> nextval)
		{
			modelBuilder.HasSequence<T>(sequenceName);
			var entities = modelBuilder.Model.GetEntityTypes().Select(e => e.ClrType).ToHashSet();

			// For all entities in the data model
			foreach (var entityType in modelBuilder.Model.GetEntityTypes())
			{
				// Find the properties that are our strongly-typed ID
				var id = entityType
					.ClrType
					.GetProperties()
					.FirstOrDefault(p => p.PropertyType.IsGenericType
										 && p.PropertyType.GetGenericTypeDefinition() == typeof(Id<,>)
										 && p.PropertyType.GetGenericArguments()[0] == entityType.ClrType
										 && p.PropertyType.GetGenericArguments()[1] == typeof(T)
										 && p.Name == "Id");

				if (id == null)
					continue;

				modelBuilder
					.Entity(entityType.Name)
					.Property(id.Name)
					.HasDefaultValueSql(nextval(sequenceName))
					.ValueGeneratedOnAdd();
			}

			return modelBuilder;
		}

		public static ModelBuilder UseSnakeCase(this ModelBuilder modelBuilder)
		{
			modelBuilder.Model.SetDefaultSchema(modelBuilder.Model.GetDefaultSchema()?.ToSnakeCase());
			foreach (var entity in modelBuilder.Model.GetEntityTypes())
			{
				entity.SetTableName(entity.GetTableName()?.ToSnakeCase());
#if !NETFRAMEWORK
				var storeObjectId = StoreObjectIdentifier.Create(entity, StoreObjectType.Table)!;
#endif

				foreach (var property in entity.GetProperties())
				{
#if !NETFRAMEWORK
					property.SetColumnName(property.GetColumnName(storeObjectId.Value)?.ToSnakeCase());
#else
					property.SetColumnName(property.GetColumnName().ToSnakeCase());
#endif
				}

				foreach (var key in entity.GetKeys())
					key.SetName(key.GetName()?.ToSnakeCase());

				foreach (var key in entity.GetForeignKeys())
					key.SetConstraintName(key.GetConstraintName()?.ToSnakeCase());

				foreach (var index in entity.GetIndexes())
#if !NETFRAMEWORK
					index.SetDatabaseName(index.GetDatabaseName()?.ToSnakeCase());
#else
					index.SetName(index.GetName().ToSnakeCase());
#endif
			}

			return modelBuilder;
		}

		public static ModelBuilder UsePermanentId(this ModelBuilder modelBuilder)
		{
			// For all entities in the data model
			foreach (var entityType in modelBuilder.Model.GetEntityTypes())
			{
				// Find the properties that are our strongly-typed ID
				var properties = entityType
					.ClrType
					.GetProperties()
					.Where(p => p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(Id<,>));

				foreach (var property in properties)
				{
					var entity = property.PropertyType.GetGenericArguments()[0];

					if (entity != entityType.ClrType || property.Name != "PermanentId")
						continue;

					modelBuilder
						.Entity(entityType.Name)
						.HasIndex(property.Name)
						.IsUnique();
				}
			}

			return modelBuilder;
		}

		public static ModelBuilder UseCode(this ModelBuilder modelBuilder)
		{
			foreach (var entityType in modelBuilder.Model.GetEntityTypes())
			{
				var properties = entityType
					.ClrType
					.GetProperties();

				foreach (var property in properties)
				{
					if (property.Name != "Code")
						continue;

					modelBuilder
						.Entity(entityType.Name)
						.HasIndex(property.Name)
						.IsUnique();
				}
			}

			return modelBuilder;
		}
	}
}
