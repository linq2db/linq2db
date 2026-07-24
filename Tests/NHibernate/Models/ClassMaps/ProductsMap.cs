using FluentNHibernate.Mapping;
using LinqToDB.NHibernate.Tests.Models.Northwind;

namespace LinqToDB.NHibernate.Tests.Models.ClassMaps
{
	public class ProductsMap : ClassMap<Product>
	{
		public ProductsMap()
		{
			Table("Products");
			Id(x => x.ProductId).GeneratedBy.Native().Column("ProductID");
			References(x => x.Supplier).Column("SupplierID");
			References(x => x.Category).Column("CategoryID");
			Map(x => x.ProductName).Column("ProductName").Not.Nullable();
			Map(x => x.QuantityPerUnit).Column("QuantityPerUnit");
			Map(x => x.UnitPrice).Column("UnitPrice");
			Map(x => x.UnitsInStock).Column("UnitsInStock");
			Map(x => x.UnitsOnOrder).Column("UnitsOnOrder");
			Map(x => x.ReorderLevel).Column("ReorderLevel");
			Map(x => x.Discontinued).Column("Discontinued").Not.Nullable();
			Map(x => x.IsDeleted).Column("IsDeleted").Not.Nullable();
		}
	}
}
