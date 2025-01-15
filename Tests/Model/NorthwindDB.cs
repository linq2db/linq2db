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

		public NorthwindDB(DataOptions options) : base(options)
		{
		}

		public ITable<Northwind.Category>            Category            { get { return this.GetTable<Northwind.Category>();            } }
		public ITable<Northwind.Customer>            Customer            { get { return this.GetTable<Northwind.Customer>();            } }
		public ITable<Northwind.Employee>            Employee            { get { return this.GetTable<Northwind.Employee>();            } }
		public ITable<Northwind.EmployeeTerritory>   EmployeeTerritory   { get { return this.GetTable<Northwind.EmployeeTerritory>();   } }
		public ITable<Northwind.OrderDetail>         OrderDetail         { get { return this.GetTable<Northwind.OrderDetail>();         } }
		public ITable<Northwind.Order>               Order               { get { return this.GetTable<Northwind.Order>();               } }
		public ITable<Northwind.Product>             Product             { get { return this.GetTable<Northwind.Product>();             } }
		public ITable<Northwind.ActiveProduct>       ActiveProduct       { get { return this.GetTable<Northwind.ActiveProduct>();       } }
		public ITable<Northwind.DiscontinuedProduct> DiscontinuedProduct { get { return this.GetTable<Northwind.DiscontinuedProduct>(); } }
		public ITable<Northwind.Region>              Region              { get { return this.GetTable<Northwind.Region>();              } }
		public ITable<Northwind.Shipper>             Shipper             { get { return this.GetTable<Northwind.Shipper>();             } }
		public ITable<Northwind.Supplier>            Supplier            { get { return this.GetTable<Northwind.Supplier>();            } }
		public ITable<Northwind.Territory>           Territory           { get { return this.GetTable<Northwind.Territory>();           } }

		public IQueryable<SqlServerExtensions.FreeTextKey<TKey>> FreeTextTable<TTable,TKey>(
			ITable<TTable> table,
			Expression<Func<TTable, object?>> columns,
			string search)
			where TTable : notnull
		{
			return Sql.Ext.SqlServer().FreeTextTable<TTable, TKey>(table, columns, search);
		}

		[Sql.TableExpression("{0} {1} WITH (UPDLOCK)")]
		public ITable<T> WithUpdateLock<T>()
			where T : class
		{
			var methodInfo = typeof(NorthwindDB).GetMethod("WithUpdateLock")!
				.MakeGenericMethod(typeof(T));

			return this.GetTable<T>(this, methodInfo);
		}
	}
}
