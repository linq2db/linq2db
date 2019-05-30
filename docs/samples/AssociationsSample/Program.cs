using System;
using System.Collections.Generic;
using System.Linq;
using AssociationsSample.Models;
using LinqToDB;
using LinqToDB.Configuration;
using LinqToDB.Data;

namespace AssociationsSample
{
    class Program
    {
		class ConnectionStringSettings : IConnectionStringSettings
		{
			public string ConnectionString { get; set; }
			public string Name { get; set; }
			public string ProviderName { get; set; }
			public bool IsGlobal { get; }
		}

		public class MySettings : ILinqToDBSettings
		{
		    public IEnumerable<IDataProviderSettings> DataProviders => Enumerable.Empty<IDataProviderSettings>();

		    public string DefaultConfiguration => "Northiwnd";
		    public string DefaultDataProvider => "System.Data.SQLite";

		    public IEnumerable<IConnectionStringSettings> ConnectionStrings
		    {
		        get
		        {
		            yield return
		                new ConnectionStringSettings
		                {
		                    Name = "Northwind",
		                    ProviderName = "System.Data.SQLite",
		                    ConnectionString = @"Data Source=..\..\..\Data\Northwind.sqlite"
		                };
		        }
		    }
		}

	    public static void RetrieveOrderDetails()
	    {
	        using (var db = new NorthwindDB())
	        {
		        var query = from order in db.Orders
			        where order.Details.Any(d => d.Discount > 0.06)
			        select new
			        {
						order.EmployeeID,
						MaxDiscount = order.Details.Max(d => d.Discount)
			        };

		        query = query.Take(10);

		        foreach (var r in query)
		        {
			        Console.WriteLine(r);
		        }
	        }
	    }

	    public static void RetrieveOrderDetailsWithBigDiscount()
	    {
	        using (var db = new NorthwindDB())
	        {
		        var query = from order in db.Orders
			        where order.DetailsWithBigDiscount.Any()
			        select new
			        {
						order.EmployeeID,
						MaxDiscount = order.DetailsWithBigDiscount.Max(d => d.Discount)
			        };

		        query = query.Take(10);

		        foreach (var r in query)
		        {
			        Console.WriteLine(r);
		        }
	        }
	    }

	    public static void RetrieveOrderInformation()
	    {
	        using (var db = new NorthwindDB())
	        {
		        var query = from order in db.Orders
			        where order.Employee.Address.StartsWith("B")
			        select new
			        {
						order.OrderID,
						order.OrderDate,
						order.Employee.Address,
			        };

		        query = query.Take(10);

		        foreach (var r in query)
		        {
			        Console.WriteLine(r);
		        }
	        }
	    }

        static void Main(string[] args)
        {
	        DataConnection.DefaultSettings = new MySettings();
			DataConnection.TurnTraceSwitchOn();
	        DataConnection.WriteTraceLine = (s, _) =>
	        {
		        Console.WriteLine(s);
	        };

	        RetrieveOrderInformation();
	        RetrieveTerritoryLinks();
			RetrieveOrderDetails();
	        RetrieveOrderDetailsWithBigDiscount();
		    Console.ReadLine();
        }

	    private static void RetrieveTerritoryLinks()
	    {
		    using (var db = new NorthwindDB())
		    {
			    var query = from et in db.EmployeeTerritories
				    where et.Territory.TerritoryDescription.StartsWith("B")
				    select new
				    {
					    et.Employee.EmployeeID,
					    et.Employee.BirthDate,
					    Territory = et.Territory.TerritoryDescription.Trim(),
					    et.Employee.Address,
				    };

			    var result = query.ToArray();

			    foreach (var employee in result)
			    {
				    Console.WriteLine(employee);
			    }
		    }
	    }
    }
}
