using System.Reflection;

using LinqToDB.Expressions;
using LinqToDB.Internal.Extensions;

using Microsoft.EntityFrameworkCore;

namespace LinqToDB.EntityFrameworkCore.Tests.Models.Northwind
{
	public abstract class NorthwindContextBase : DbContext
	{
		public DbSet<Category> Categories { get; set; } = null!;
		public DbSet<CustomerCustomerDemo> CustomerCustomerDemo { get; set; } = null!;
		public DbSet<CustomerDemographics> CustomerDemographics { get; set; } = null!;
		public DbSet<Customer> Customers { get; set; } = null!;
		public DbSet<Employee> Employees { get; set; } = null!;
		public DbSet<EmployeeTerritory> EmployeeTerritories { get; set; } = null!;
		public DbSet<OrderDetail> OrderDetails { get; set; } = null!;
		public DbSet<Order> Orders { get; set; } = null!;
		public DbSet<Product> Products { get; set; } = null!;
		public DbSet<Region> Region { get; set; } = null!;
		public DbSet<Shipper> Shippers { get; set; } = null!;
		public DbSet<Supplier> Suppliers { get; set; } = null!;
		public DbSet<Territory> Territories { get; set; } = null!;

		protected NorthwindContextBase(DbContextOptions options) : base(options)
		{
		}

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			base.OnModelCreating(modelBuilder);

#if EF10
			// EF10 supports keyed filters; use named filters for both constraints
			modelBuilder.Entity<Product>()
				.HasQueryFilter("ProductIdFilter", e => !IsFilterProducts || e.ProductId > 2)
				.HasQueryFilter("NotDiscontinued", e => !IsFilterProducts || !e.Discontinued);
#else
			// Earlier EF versions support only a single anonymous filter per entity
			modelBuilder.Entity<Product>()
				.HasQueryFilter(e => !IsFilterProducts || e.ProductId > 2);
#endif

			ConfigureGlobalQueryFilters(modelBuilder);
		}

		private void ConfigureGlobalQueryFilters(ModelBuilder builder)
		{
			foreach (var entityType in builder.Model.GetEntityTypes())
			{
				if (typeof(ISoftDelete).IsSameOrParentOf(entityType.ClrType))
				{
					var method = ConfigureEntityFilterMethodInfo.MakeGenericMethod(entityType.ClrType);
					method.Invoke(this, [builder]);
				}
			}
		}

		private static readonly MethodInfo ConfigureEntityFilterMethodInfo =
			MemberHelper.MethodOf(() => ((NorthwindContextBase)null!).ConfigureEntityFilter<BaseEntity>(null!)).GetGenericMethodDefinition();

		public void ConfigureEntityFilter<TEntity>(ModelBuilder builder)
			where TEntity : class, ISoftDelete
		{
			NorthwindContextBase? obj = null;

#if EF10
			// EF10: use named filter for soft-delete constraint to coexist with other named filters
#if !NETFRAMEWORK
			builder.Entity<TEntity>().HasQueryFilter("SoftDeleteFilter", e => !obj!.IsSoftDeleteFilterEnabled || !e.IsDeleted || !EF.Property<bool>(e, "IsDeleted"));
#else
			builder.Entity<TEntity>().HasQueryFilter("SoftDeleteFilter", e => !obj!.IsSoftDeleteFilterEnabled || !e.IsDeleted);
#endif
#else
			// Earlier EF versions: only one query filter per entity (a later HasQueryFilter call replaces an earlier one)
#if !NETFRAMEWORK
			builder.Entity<TEntity>().HasQueryFilter(e => !obj!.IsSoftDeleteFilterEnabled || !e.IsDeleted || !EF.Property<bool>(e, "IsDeleted"));
#else
			builder.Entity<TEntity>().HasQueryFilter(e => !obj!.IsSoftDeleteFilterEnabled || !e.IsDeleted);
#endif
#endif
		}

		public bool IsFilterProducts { get; set; }

		public bool IsSoftDeleteFilterEnabled { get; set; }
	}
}
