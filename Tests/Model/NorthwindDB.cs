#nullable disable
using System;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider.SqlServer;

namespace Tests.Model
{
	public class NorthwindDB : DataConnection
	{
		public NorthwindDB(string configurationString) : base(configurationString)
		{
		}

		public ITable<Northwind.Category>            Category            { get { return GetTable<Northwind.Category>();            } }
		public ITable<Northwind.Customer>            Customer            { get { return GetTable<Northwind.Customer>();            } }
		public ITable<Northwind.Employee>            Employee            { get { return GetTable<Northwind.Employee>();            } }
		public ITable<Northwind.EmployeeTerritory>   EmployeeTerritory   { get { return GetTable<Northwind.EmployeeTerritory>();   } }
		public ITable<Northwind.OrderDetail>         OrderDetail         { get { return GetTable<Northwind.OrderDetail>();         } }
		public ITable<Northwind.Order>               Order               { get { return GetTable<Northwind.Order>();               } }
		public ITable<Northwind.Product>             Product             { get { return GetTable<Northwind.Product>();             } }
		public ITable<Northwind.ActiveProduct>       ActiveProduct       { get { return GetTable<Northwind.ActiveProduct>();       } }
		public ITable<Northwind.DiscontinuedProduct> DiscontinuedProduct { get { return GetTable<Northwind.DiscontinuedProduct>(); } }
		public ITable<Northwind.Region>              Region              { get { return GetTable<Northwind.Region>();              } }
		public ITable<Northwind.Shipper>             Shipper             { get { return GetTable<Northwind.Shipper>();             } }
		public ITable<Northwind.Supplier>            Supplier            { get { return GetTable<Northwind.Supplier>();            } }
		public ITable<Northwind.Territory>           Territory           { get { return GetTable<Northwind.Territory>();           } }

		public IQueryable<SqlServerExtensions.FreeTextKey<TKey>> FreeTextTable<TTable,TKey>(
			ITable<TTable> table,
			Expression<Func<TTable, object>> columns,
			string search)
		{
			return Sql.Ext.SqlServer().FreeTextTable<TTable, TKey>(table, columns, search);
		}

		[Sql.TableExpression("{0} {1} WITH (UPDLOCK)")]
		public ITable<T> WithUpdateLock<T>()
			where T : class
		{
			var methodInfo = typeof(NorthwindDB).GetMethod("WithUpdateLock")
				.MakeGenericMethod(typeof(T));

			return GetTable<T>(this, methodInfo);
		}
	}
}
