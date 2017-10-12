using System;
using System.Linq;
using DataModels;

namespace Tests.Benchmark
{
	public static class NortwindExtensions
	{
		public static IQueryable<OrderSubtotal> VwOrdersSubtotals(this NorthwindDB db)
		{
			var result =
				from od in db.OrderDetails
				group od by new
				{
					od.OrderID
				}
				into g
				select new OrderSubtotal
				{
					OrderID = g.Key.OrderID,
					Subtotal = g.Sum(
						p => Convert.ToDecimal(p.UnitPrice * p.Quantity * (1 - (decimal) p.Discount) / 100) * 100)
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
			(from a in db.Orders
				join b in (from OrderDetails in db.OrderDetails
					group OrderDetails by new
					{
						OrderDetails.OrderID
					}
					into g
					select new
					{
						g.Key.OrderID,
						Subtotal = (decimal?) g.Sum(p => p.UnitPrice * p.Quantity * (1 - (decimal) p.Discount))
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
					Year = a.ShippedDate.Value.Year
				}).Distinct();

			return result;
		}

		public static IQueryable<OrderDetailsExtended> VwOrdersDetailsExtended(this NorthwindDB db)
		{
			var result =
			(from y in db.OrderDetails
				orderby
				y.OrderID
				select new OrderDetailsExtended
				{
					OrderID = y.OrderID,
					ProductID = y.ProductID,
					ProductName = y.OrderDetailsProduct.ProductName,
					UnitPrice = y.UnitPrice,
					Quantity = y.Quantity,
					Discount = y.Discount,
					ExtendedPrice = Math.Round(y.UnitPrice * y.Quantity * (1m - (decimal) y.Discount), 2,
						MidpointRounding.AwayFromZero)
				}).Distinct();

			return result;
		}

		public static IQueryable<SalesByCategory> VwSalesByCategory(this NorthwindDB db, int year)
		{
			var result =
				from c in db.Categories
				join p in (from p2 in db.Products
					join o in (from o2 in db.Orders
						join ode in db.VwOrdersDetailsExtended() on o2.OrderID equals ode.OrderID
						select new {Order = o2, OrderDetailsExtended = ode})
					on p2.ProductID equals o.OrderDetailsExtended.ProductID
					select new {o.Order, o.OrderDetailsExtended, Product = p2}
				) on c.CategoryID equals p.Product.CategoryID
				where p.Order.OrderDate.Value.Year == year
				group new {c, p} by new
				{
					c.CategoryID,
					c.CategoryName,
					p.Product.ProductName
				}
				into g
				select new SalesByCategory
				{
					CategoryID = g.Key.CategoryID,
					CategoryName = g.Key.CategoryName,
					ProductName = g.Key.ProductName,
					ProductSales = g.Sum(p => p.p.OrderDetailsExtended.ExtendedPrice)
				};

			return result;
		}

		public class ProductAndPrice
		{
			public string ProductName { get; set; }
			public decimal? UnitPrice { get; set; }
		}

		public static IQueryable<ProductAndPrice> TenMostExpensiveProducts(this NorthwindDB db)
		{
			var result =
				(from a in (from p in db.Products
						orderby
						p.UnitPrice descending
						select new {p.ProductName, p.UnitPrice}).Distinct()
					select new ProductAndPrice {ProductName = a.ProductName, UnitPrice = a.UnitPrice})
				.Take(10);

			return result;
		}

		public class ProductSalesByYear
		{
			public string CategoryName { get; set; }
			public string ProductName { get; set; }
			public decimal? ProductSales { get; set; }
			public DateTime? ShippedQuarter { get; set; }
		}

		public static IQueryable<ProductSalesByYear> VwProductSalesByYear(this NorthwindDB db, int year)
		{
			var result =
				(from c in db.OrderDetails
					where
					c.OrderDetailsOrder.ShippedDate.Value.Year == year
					group new {c.OrderDetailsProduct.Category, c.OrderDetailsProduct, c.OrderDetailsOrder, c} by new
					{
						c.OrderDetailsProduct.Category.CategoryName,
						c.OrderDetailsProduct.ProductName,
						c.OrderDetailsOrder.ShippedDate
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
						ShippedQuarter = g.Key.ShippedDate
					})
				.Distinct();

			return result;
		}

		public class CategorySalesForYear
		{
			public string CategoryName { get; set; }
			public decimal? CategorySales { get; set; }
		}

		public static IQueryable<AlphabeticalListOfProduct> VwAlphabeticalListOfProduct(this NorthwindDB db)
		{
			var result = from Products in db.Products
				where
				Products.Discontinued == false
				select new AlphabeticalListOfProduct
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
					CategoryName = Products.Category.CategoryName
				};
			return result;
		}

		public static IQueryable<CategorySalesForYear> VwCategorySalesByYear(this NorthwindDB db, int year)
		{
			var result = from s in VwProductSalesByYear(db, year)
				group s by new
				{
					s.CategoryName
				}
				into g
				select new CategorySalesForYear
				{
					CategoryName = g.Key.CategoryName,
					CategorySales = g.Sum(p => p.ProductSales)
				};

			return result;
		}

		public static IQueryable<CurrentProductList> VwCurrentProductList(this NorthwindDB db)
		{
			var result = from p in db.Products
				where
				p.Discontinued == false
				select new CurrentProductList
				{
					ProductID = p.ProductID,
					ProductName = p.ProductName
				};

			return result;
		}

		public static IQueryable<CustomerAndSuppliersByCity> VwCustomerAndSuppliersByCity(this NorthwindDB db)
		{
			var result = (
					from c in db.Customers
					select new CustomerAndSuppliersByCity
					{
						City = c.City,
						CompanyName = c.CompanyName,
						ContactName = c.ContactName,
						Relationship = "Customers"
					}
				).Union
				(
					from s in db.Suppliers
					select new CustomerAndSuppliersByCity
					{
						City = s.City,
						CompanyName = s.CompanyName,
						ContactName = s.ContactName,
						Relationship = "Suppliers"
					}
				);
			return result;
		}

		public static IQueryable<Invoice> VwInvoices(this NorthwindDB db)
		{
			var result = from s in db.Shippers
				join p in (from pi in db.Products
					join e in (
						from e2 in db.Employees
						join c in (from c2 in db.Customers
							join o in db.Orders on c2.CustomerID equals o.CustomerID
							select new {Customer = c2, Order = o}
						) on e2.EmployeeID equals c.Order.EmployeeID
						join od in db.OrderDetails on c.Order.OrderID equals od.OrderID
						select new {Customer = c.Customer, OrderDetail = od, Order = c.Order, Employee = e2}
					) on pi.ProductID equals e.OrderDetail.ProductID
					select new
					{
						e.Customer,
						e.OrderDetail,
						e.Order,
						e.Employee,
						Product = pi
					}
				) on s.ShipperID equals p.Order.ShipVia
				select new Invoice
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

		public static IQueryable<OrdersQry> VwOrdersQry(this NorthwindDB db)
		{
			return from o in db.Orders
				select new OrdersQry
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
					CompanyName = o.Customer.CompanyName,
					Address = o.Customer.Address,
					City = o.Customer.City,
					Region = o.Customer.Region,
					PostalCode = o.Customer.PostalCode,
					Country = o.Customer.Country
				};
		}

		public static IQueryable<ProductsAboveAveragePrice> VwProductsAboveAveragePrice(this NorthwindDB db)
		{
			var result = from p in db.Products
				where
				p.UnitPrice >
				db.Products.Average(_ => _.UnitPrice)
				select new ProductsAboveAveragePrice
				{
					ProductName = p.ProductName,
					UnitPrice = p.UnitPrice
				};

			return result;
		}

		public static IQueryable<ProductsByCategory> VwProductsByCategory(this NorthwindDB db)
		{
			var result = from p in db.Products
				where !p.Discontinued
				select new ProductsByCategory
				{
					CategoryName = p.Category.CategoryName,
					ProductName = p.ProductName,
					QuantityPerUnit = p.QuantityPerUnit,
					UnitsInStock = p.UnitsInStock,
					Discontinued = p.Discontinued
				};

			return result;
		}

		public static IQueryable<QuarterlyOrder> VwQuarterlyOrders(this NorthwindDB db, int year)
		{
			var result =
				(from o in db.Orders
					join c in db.Customers on o.CustomerID equals c.CustomerID into customersJoin
					from c2 in customersJoin.DefaultIfEmpty()
					where o.OrderDate.Value.Year == year
					select new QuarterlyOrder
					{
						CustomerID = c2.CustomerID,
						CompanyName = c2.CompanyName,
						City = c2.City,
						Country = c2.Country
					})
				.Distinct();

			return result;
		}

		public static IQueryable<SalesTotalsByAmount> VwSalesTotalsByAmount(this NorthwindDB db, int year, decimal amount)
		{
			var result =
				from c in db.Customers
				join o in (from o2 in db.Orders
					join os in VwOrdersSubtotals(db) on o2.OrderID equals os.OrderID
					select new {Order = o2, OrderSubtotal = os}) on c.CustomerID equals o.Order.CustomerID
				where o.OrderSubtotal.Subtotal > amount && o.Order.ShippedDate.Value.Year == year
				select new SalesTotalsByAmount
				{
					SaleAmount = o.OrderSubtotal.Subtotal,
					OrderID = o.Order.OrderID,
					CompanyName = c.CompanyName,
					ShippedDate = o.Order.ShippedDate
				};

			return result;
		}

		public static IQueryable<SummaryOfSalesByQuarter> VwGetSummaryOfSalesByQuarter(this NorthwindDB db)
		{
			var result =
				from o in db.Orders
				join os in VwOrdersSubtotals(db) on o.OrderID equals os.OrderID
				where o.ShippedDate != null
				select new SummaryOfSalesByQuarter
				{
					ShippedDate = o.ShippedDate,
					OrderID = o.OrderID,
					Subtotal = os.Subtotal
				};

			return result;
		}

		public static IQueryable<SummaryOfSalesByYear> VwGetSummaryOfSalesByYear(this NorthwindDB db, int year)
		{
			var result =
				from o in db.Orders
				join os in VwOrdersSubtotals(db) on o.OrderID equals os.OrderID
				where o.ShippedDate != null
				select new SummaryOfSalesByYear
				{
					ShippedDate = o.ShippedDate,
					OrderID = o.OrderID,
					Subtotal = os.Subtotal
				};

			return result;
		}
	}
}