using System;
using System.Linq;

namespace LinqToDB.Benchmarks.Models
{
	public static class NortwindExtensions
	{
		public static IQueryable<Northwind.OrderSubtotal> VwOrdersSubtotals(this NorthwindDB db)
		{
			var result =
				from od in db.OrderDetail
				group od by new
				{
					od.OrderID,
				}
				into g
				select new Northwind.OrderSubtotal
				{
					OrderID = g.Key.OrderID,
					Subtotal = g.Sum(
						p => Convert.ToDecimal(p.UnitPrice * p.Quantity * (1 - (decimal) p.Discount) / 100) * 100),
				};
			return result;
		}

		public class SalesByYear
		{
			public DateTime? ShippedDate { get; set; }
			public int OrderID { get; set; }
			public decimal? Subtotal { get; set; }
			public int? Year { get; set; }
		}

		public static IQueryable<SalesByYear> VwSalesByYear(this NorthwindDB db, int year)
		{
			var result =
			(from a in db.Order
				join b in (from OrderDetails in db.OrderDetail
					group OrderDetails by new
					{
						OrderDetails.OrderID,
					}
					into g
					select new
					{
						g.Key.OrderID,
						Subtotal = (decimal?) g.Sum(p => p.UnitPrice * p.Quantity * (1 - (decimal) p.Discount)),
					}).Distinct() on new {OrderID = a.OrderID} equals new {OrderID = b.OrderID}
				where
				a.ShippedDate != null &&
				a.ShippedDate.Value.Year == year
				orderby
				a.ShippedDate
				select new SalesByYear
				{
					ShippedDate = a.ShippedDate,
					OrderID = a.OrderID,
					Subtotal = b.Subtotal,
					Year = a.ShippedDate!.Value.Year,
				}).Distinct();

			return result;
		}

		public static IQueryable<Northwind.OrderDetailsExtended> VwOrdersDetailsExtended(this NorthwindDB db)
		{
			var result =
			(from y in db.OrderDetail
				orderby
				y.OrderID
				select new Northwind.OrderDetailsExtended
				{
					OrderID = y.OrderID,
					ProductID = y.ProductID,
					ProductName = y.Product.ProductName,
					UnitPrice = y.UnitPrice,
					Quantity = y.Quantity,
					Discount = y.Discount,
					ExtendedPrice = Math.Round(y.UnitPrice * y.Quantity * (1m - (decimal) y.Discount), 2,
						MidpointRounding.AwayFromZero),
				}).Distinct();

			return result;
		}

		public static IQueryable<Northwind.SalesByCategory> VwSalesByCategory(this NorthwindDB db, int year)
		{
			var result =
				from c in db.Category
				join p in (from p2 in db.Product
					join o in (from o2 in db.Order
						join ode in db.VwOrdersDetailsExtended() on o2.OrderID equals ode.OrderID
						select new {Order = o2, OrderDetailsExtended = ode})
					on p2.ProductID equals o.OrderDetailsExtended.ProductID
					select new {o.Order, o.OrderDetailsExtended, Product = p2}
				) on c.CategoryID equals p.Product.CategoryID
				where p.Order.OrderDate!.Value.Year == year
				group new {c, p} by new
				{
					c.CategoryID,
					c.CategoryName,
					p.Product.ProductName,
				}
				into g
				select new Northwind.SalesByCategory
				{
					CategoryID = g.Key.CategoryID,
					CategoryName = g.Key.CategoryName,
					ProductName = g.Key.ProductName,
					ProductSales = g.Sum(p => p.p.OrderDetailsExtended.ExtendedPrice),
				};

			return result;
		}

		public class ProductAndPrice
		{
			public string?  ProductName { get; set; }
			public decimal? UnitPrice   { get; set; }
		}

		public static IQueryable<ProductAndPrice> TenMostExpensiveProducts(this NorthwindDB db)
		{
			var result =
				(from a in (from p in db.Product
						orderby
						p.UnitPrice descending
						select new {p.ProductName, p.UnitPrice}).Distinct()
					select new ProductAndPrice {ProductName = a.ProductName, UnitPrice = a.UnitPrice})
				.Take(10);

			return result;
		}

		public class ProductSalesByYear
		{
			public string?   CategoryName   { get; set; }
			public string?   ProductName    { get; set; }
			public decimal?  ProductSales   { get; set; }
			public DateTime? ShippedQuarter { get; set; }
		}

		public static IQueryable<ProductSalesByYear> VwProductSalesByYear(this NorthwindDB db, int year)
		{
			var result =
				(from c in db.OrderDetail
					where
					c.Order.ShippedDate!.Value.Year == year
					group new {c.Product.Category, c.Product, c.Order, c} by new
					{
						c.Product.Category!.CategoryName,
						c.Product.ProductName,
						c.Order.ShippedDate,
					}
					into g
					orderby
					g.Key.CategoryName,
					g.Key.ProductName,
					g.Key.ShippedDate
					select new ProductSalesByYear
					{
						CategoryName = g.Key.CategoryName,
						ProductName = g.Key.ProductName,
						ProductSales =
							g.Sum(p => p.c.UnitPrice * p.c.Quantity * (1 - (decimal) p.c.Discount)),
						ShippedQuarter = g.Key.ShippedDate,
					})
				.Distinct();

			return result;
		}

		public class CategorySalesForYear
		{
			public string?  CategoryName  { get; set; }
			public decimal? CategorySales { get; set; }
		}

		public static IQueryable<Northwind.AlphabeticalListOfProduct> VwAlphabeticalListOfProduct(this NorthwindDB db)
		{
			var result = from Products in db.Product
				where
				Products.Discontinued == false
				select new Northwind.AlphabeticalListOfProduct
				{
					ProductID = Products.ProductID,
					ProductName = Products.ProductName,
					SupplierID = Products.SupplierID,
					CategoryID = Products.CategoryID,
					QuantityPerUnit = Products.QuantityPerUnit,
					UnitPrice = Products.UnitPrice,
					UnitsInStock = Products.UnitsInStock,
					UnitsOnOrder = Products.UnitsOnOrder,
					ReorderLevel = Products.ReorderLevel,
					Discontinued = Products.Discontinued,
					CategoryName = Products.Category!.CategoryName,
				};
			return result;
		}

		public static IQueryable<CategorySalesForYear> VwCategorySalesByYear(this NorthwindDB db, int year)
		{
			var result = from s in VwProductSalesByYear(db, year)
				group s by new
				{
					s.CategoryName,
				}
				into g
				select new CategorySalesForYear
				{
					CategoryName = g.Key.CategoryName,
					CategorySales = g.Sum(p => p.ProductSales),
				};

			return result;
		}

		public static IQueryable<Northwind.CurrentProductList> VwCurrentProductList(this NorthwindDB db)
		{
			var result = from p in db.Product
				where
				p.Discontinued == false
				select new Northwind.CurrentProductList
				{
					ProductID = p.ProductID,
					ProductName = p.ProductName,
				};

			return result;
		}

		public static IQueryable<Northwind.CustomerAndSuppliersByCity> VwCustomerAndSuppliersByCity(this NorthwindDB db)
		{
			var result = (
					from c in db.Customer
					select new Northwind.CustomerAndSuppliersByCity
					{
						City = c.City,
						CompanyName = c.CompanyName,
						ContactName = c.ContactName,
						Relationship = "Customers",
					}
				).Union
				(
					from s in db.Supplier
					select new Northwind.CustomerAndSuppliersByCity
					{
						City = s.City,
						CompanyName = s.CompanyName,
						ContactName = s.ContactName,
						Relationship = "Suppliers",
					}
				);
			return result;
		}

		public static IQueryable<Northwind.Invoice> VwInvoices(this NorthwindDB db)
		{
			var result = from s in db.Shipper
				join p in (from pi in db.Product
					join e in (
						from e2 in db.Employee
						join c in (from c2 in db.Customer
							join o in db.Order on c2.CustomerID equals o.CustomerID
							select new {Customer = c2, Order = o}
						) on e2.EmployeeID equals c.Order.EmployeeID
						join od in db.OrderDetail on c.Order.OrderID equals od.OrderID
						select new {Customer = c.Customer, OrderDetail = od, Order = c.Order, Employee = e2}
					) on pi.ProductID equals e.OrderDetail.ProductID
					select new
					{
						e.Customer,
						e.OrderDetail,
						e.Order,
						e.Employee,
						Product = pi,
					}
				) on s.ShipperID equals p.Order.ShipVia
				select new Northwind.Invoice
				{
					ShipName = p.Order.ShipName,
					ShipAddress = p.Order.ShipAddress,
					ShipCity = p.Order.ShipCity,
					ShipRegion = p.Order.ShipRegion,
					ShipPostalCode = p.Order.ShipPostalCode,
					ShipCountry = p.Order.ShipCountry,
					CustomerID = p.Order.CustomerID,
					CustomerName = p.Customer.CompanyName,
					Address = p.Customer.Address,
					City = p.Customer.City,
					Region = p.Customer.Region,
					PostalCode = p.Customer.PostalCode,
					Country = p.Customer.Country,
					Salesperson = p.Employee.FirstName + " " + p.Employee.LastName,
					OrderID = p.Order.OrderID,
					OrderDate = p.Order.OrderDate,
					RequiredDate = p.Order.OrderDate,
					ShippedDate = p.Order.ShippedDate,
					ShipperName = s.CompanyName,
					ProductID = p.OrderDetail.ProductID,
					ProductName = p.Product.ProductName,
					UnitPrice = p.OrderDetail.UnitPrice,
					Quantity = p.OrderDetail.Quantity,
					Discount = p.OrderDetail.Discount,
				};

			return result;
		}

		public static IQueryable<Northwind.OrdersQry> VwOrdersQry(this NorthwindDB db)
		{
			return from o in db.Order
				select new Northwind.OrdersQry
				{
					OrderID = o.OrderID,
					CustomerID = o.CustomerID,
					EmployeeID = o.EmployeeID,
					OrderDate = o.OrderDate,
					RequiredDate = o.RequiredDate,
					ShippedDate = o.ShippedDate,
					ShipVia = o.ShipVia,
					Freight = o.Freight,
					ShipName = o.ShipName,
					ShipAddress = o.ShipAddress,
					ShipCity = o.ShipCity,
					ShipRegion = o.ShipRegion,
					ShipPostalCode = o.ShipPostalCode,
					ShipCountry = o.ShipCountry,
					CompanyName = o.Customer!.CompanyName,
					Address = o.Customer.Address,
					City = o.Customer.City,
					Region = o.Customer.Region,
					PostalCode = o.Customer.PostalCode,
					Country = o.Customer.Country,
				};
		}

		public static IQueryable<Northwind.ProductsAboveAveragePrice> VwProductsAboveAveragePrice(this NorthwindDB db)
		{
			var result = from p in db.Product
				where
				p.UnitPrice >
				db.Product.Average(_ => _.UnitPrice)
				select new Northwind.ProductsAboveAveragePrice
				{
					ProductName = p.ProductName,
					UnitPrice = p.UnitPrice,
				};

			return result;
		}

		public static IQueryable<Northwind.ProductsByCategory> VwProductsByCategory(this NorthwindDB db)
		{
			var result = from p in db.Product
				where !p.Discontinued
				select new Northwind.ProductsByCategory
				{
					CategoryName = p.Category!.CategoryName,
					ProductName = p.ProductName,
					QuantityPerUnit = p.QuantityPerUnit,
					UnitsInStock = p.UnitsInStock,
					Discontinued = p.Discontinued,
				};

			return result;
		}

		public static IQueryable<Northwind.QuarterlyOrder> VwQuarterlyOrders(this NorthwindDB db, int year)
		{
			var result =
				(from o in db.Order
					join c in db.Customer on o.CustomerID equals c.CustomerID into customersJoin
					from c2 in customersJoin.DefaultIfEmpty()
					where o.OrderDate!.Value.Year == year
					select new Northwind.QuarterlyOrder
					{
						CustomerID = c2.CustomerID,
						CompanyName = c2.CompanyName,
						City = c2.City,
						Country = c2.Country,
					})
				.Distinct();

			return result;
		}

		public static IQueryable<Northwind.SalesTotalsByAmount> VwSalesTotalsByAmount(this NorthwindDB db, int year, decimal amount)
		{
			var result =
				from c in db.Customer
				join o in (from o2 in db.Order
					join os in VwOrdersSubtotals(db) on o2.OrderID equals os.OrderID
					select new {Order = o2, OrderSubtotal = os}) on c.CustomerID equals o.Order.CustomerID
				where o.OrderSubtotal.Subtotal > amount && o.Order.ShippedDate!.Value.Year == year
				select new Northwind.SalesTotalsByAmount
				{
					SaleAmount = o.OrderSubtotal.Subtotal,
					OrderID = o.Order.OrderID,
					CompanyName = c.CompanyName,
					ShippedDate = o.Order.ShippedDate,
				};

			return result;
		}

		public static IQueryable<Northwind.SummaryOfSalesByQuarter> VwGetSummaryOfSalesByQuarter(this NorthwindDB db)
		{
			var result =
				from o in db.Order
				join os in VwOrdersSubtotals(db) on o.OrderID equals os.OrderID
				where o.ShippedDate != null
				select new Northwind.SummaryOfSalesByQuarter
				{
					ShippedDate = o.ShippedDate,
					OrderID = o.OrderID,
					Subtotal = os.Subtotal,
				};

			return result;
		}

		public static IQueryable<Northwind.SummaryOfSalesByYear> VwGetSummaryOfSalesByYear(this NorthwindDB db, int year)
		{
			var result =
				from o in db.Order
				join os in VwOrdersSubtotals(db) on o.OrderID equals os.OrderID
				where o.ShippedDate != null
				select new Northwind.SummaryOfSalesByYear
				{
					ShippedDate = o.ShippedDate,
					OrderID = o.OrderID,
					Subtotal = os.Subtotal,
				};

			return result;
		}
	}
}
