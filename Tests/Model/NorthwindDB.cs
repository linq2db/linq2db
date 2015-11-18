using System;
using System.Linq.Expressions;
using System.Reflection;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider.SqlServer;

namespace Tests.Model
{
	public class NorthwindDB : DataConnection
	{
		public NorthwindDB() : base("Northwind")
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
		
//#if !MONO
		
		public class FreeTextKey<T>
		{
			public T   Key;
			public int Rank;
		}

		[FreeTextTableExpression]
		public ITable<FreeTextKey<TKey>> FreeTextTable<TTable,TKey>(string field, string text)
		{
			return GetTable<FreeTextKey<TKey>>(
				this,
				((MethodInfo)(MethodBase.GetCurrentMethod())).MakeGenericMethod(typeof(TTable), typeof(TKey)),
				field,
				text);
		}

		[FreeTextTableExpression]
		public ITable<FreeTextKey<TKey>> FreeTextTable<TTable,TKey>(Expression<Func<TTable,string>> fieldSelector, string text)
		{
			return GetTable<FreeTextKey<TKey>>(
				this,
				((MethodInfo)(MethodBase.GetCurrentMethod())).MakeGenericMethod(typeof(TTable), typeof(TKey)),
				fieldSelector,
				text);
		}

//#endif

		[Sql.TableExpression("{0} {1} WITH (UPDLOCK)")]
		public ITable<T> WithUpdateLock<T>()
			where T : class 
		{
			return GetTable<T>(this, ((MethodInfo)(MethodBase.GetCurrentMethod())).MakeGenericMethod(typeof(T)));
		}
	}
}
