using LinqToDB.EntityFrameworkCore.Tests.Models.Northwind;
using LinqToDB.EntityFrameworkCore.Tests.SQLite.Models.Northwind.Mapping;

using Microsoft.EntityFrameworkCore;

namespace LinqToDB.EntityFrameworkCore.Tests.SQLite.Models.Northwind
{
	public class NorthwindContext : DbContext
	{
		public DbSet<Category>             Categories           { get; set; } = null!;
		public DbSet<CustomerCustomerDemo> CustomerCustomerDemo { get; set; } = null!;
		public DbSet<CustomerDemographics> CustomerDemographics { get; set; } = null!;
		public DbSet<Customer>             Customers            { get; set; } = null!;
		public DbSet<Employee>             Employees            { get; set; } = null!;
		public DbSet<EmployeeTerritory>    EmployeeTerritories  { get; set; } = null!;
		public DbSet<OrderDetail>          OrderDetails         { get; set; } = null!;
		public DbSet<Order>                Orders               { get; set; } = null!;
		public DbSet<Product>              Products             { get; set; } = null!;
		public DbSet<Region>               Region               { get; set; } = null!;
		public DbSet<Shipper>              Shippers             { get; set; } = null!;
		public DbSet<Supplier>             Suppliers            { get; set; } = null!;
		public DbSet<Territory>            Territories          { get; set; } = null!;

		public NorthwindContext(DbContextOptions options) : base(options)
		{
		}

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.ApplyConfiguration(new CategoriesMap());
			modelBuilder.ApplyConfiguration(new CustomerCustomerDemoMap());
			modelBuilder.ApplyConfiguration(new CustomerDemographicsMap());
			modelBuilder.ApplyConfiguration(new CustomersMap());
			modelBuilder.ApplyConfiguration(new EmployeesMap());
			modelBuilder.ApplyConfiguration(new EmployeeTerritoriesMap());
			modelBuilder.ApplyConfiguration(new OrderDetailsMap());
			modelBuilder.ApplyConfiguration(new OrderMap());
			modelBuilder.ApplyConfiguration(new ProductsMap());
			modelBuilder.ApplyConfiguration(new RegionMap());
			modelBuilder.ApplyConfiguration(new ShippersMap());
			modelBuilder.ApplyConfiguration(new SuppliersMap());
			modelBuilder.ApplyConfiguration(new TerritoriesMap());

			modelBuilder.Entity<Product>()
				.HasQueryFilter(e => !IsFilterProducts || e.ProductId > 2);
		}

		public bool IsFilterProducts { get; set; } 
	}
}
