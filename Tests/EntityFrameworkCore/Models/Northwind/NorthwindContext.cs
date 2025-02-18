using System.Reflection;

using LinqToDB.Extensions;
using LinqToDB.Internal.Expressions;

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

			modelBuilder.Entity<Product>()
				.HasQueryFilter(e => !IsFilterProducts || e.ProductId > 2);

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

#if !NETFRAMEWORK
			builder.Entity<TEntity>().HasQueryFilter(e => !obj!.IsSoftDeleteFilterEnabled || !e.IsDeleted || !EF.Property<bool>(e, "IsDeleted"));
#else
			builder.Entity<TEntity>().HasQueryFilter(e => !obj!.IsSoftDeleteFilterEnabled || !e.IsDeleted);
#endif
		}

		public bool IsFilterProducts { get; set; }

		public bool IsSoftDeleteFilterEnabled { get; set; }
	}
}
