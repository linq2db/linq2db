using LinqToDB.EntityFrameworkCore.Tests.Models.Northwind;
using LinqToDB.EntityFrameworkCore.Tests.Pomelo.Models.Northwind.Mapping;

using Microsoft.EntityFrameworkCore;

namespace LinqToDB.EntityFrameworkCore.Tests.Pomelo.Models.Northwind
{
	public class NorthwindContext : NorthwindContextBase
	{
		public NorthwindContext(DbContextOptions options) : base(options)
		{
		}

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			base.OnModelCreating(modelBuilder);

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
		}
	}
}
