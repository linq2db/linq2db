using LinqToDB;
using LinqToDB.Data;

namespace Tests.OrmBattle.Helper
{
	public class NorthwindDB : DataConnection
	{
		public NorthwindDB()
			: base("NorthwindSqlite")
		{
			//			TraceSwitch = new TraceSwitch("DbManager", "DbManager trace switch", "Info");
			LinqToDB.Common.Configuration.Linq.AllowMultipleQuery = true;
		}

		public ITable<Category>            Categories           { get { return GetTable<Category>();            } }
		public ITable<Customer>            Customers            { get { return GetTable<Customer>();            } }
		public ITable<Employee>            Employees            { get { return GetTable<Employee>();            } }
		public ITable<EmployeeTerritory>   EmployeeTerritories  { get { return GetTable<EmployeeTerritory>();   } }
		public ITable<OrderDetail>         OrderDetails         { get { return GetTable<OrderDetail>();         } }
		public ITable<Order>               Orders               { get { return GetTable<Order>();               } }
		public ITable<Product>             Products             { get { return GetTable<Product>();             } }
		public ITable<ActiveProduct>       ActiveProducts       { get { return GetTable<ActiveProduct>();       } }
		public ITable<DiscontinuedProduct> DiscontinuedProducts { get { return GetTable<DiscontinuedProduct>(); } }
		public ITable<Region>              Region               { get { return GetTable<Region>();              } }
		public ITable<Shipper>             Shipper              { get { return GetTable<Shipper>();             } }
		public ITable<Supplier>            Suppliers            { get { return GetTable<Supplier>();            } }
		public ITable<Territory>           Territories          { get { return GetTable<Territory>();           } }
	}
}
