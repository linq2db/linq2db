using System;
using System.Data;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB.Benchmarks.TestProvider;
using LinqToDB.Data;
using LinqToDB.DataProvider;
using LinqToDB.DataProvider.SqlServer;

namespace LinqToDB.Benchmarks.Models
{
	public class NorthwindDB : DataConnection
	{
		public NorthwindDB(DataOptions options) : base(options)
		{
		}

#pragma warning disable CA2000 // Dispose objects before losing scope
		public NorthwindDB(IDataProvider provider) : base(provider, new MockDbConnection(Array.Empty<QueryResult>(), ConnectionState.Open))
#pragma warning restore CA2000 // Dispose objects before losing scope
		{
		}

		public ITable<Northwind.Category>            Category            => this.GetTable<Northwind.Category>();
		public ITable<Northwind.Customer>            Customer            => this.GetTable<Northwind.Customer>();
		public ITable<Northwind.Employee>            Employee            => this.GetTable<Northwind.Employee>();
		public ITable<Northwind.EmployeeTerritory>   EmployeeTerritory   => this.GetTable<Northwind.EmployeeTerritory>();
		public ITable<Northwind.OrderDetail>         OrderDetail         => this.GetTable<Northwind.OrderDetail>();
		public ITable<Northwind.Order>               Order               => this.GetTable<Northwind.Order>();
		public ITable<Northwind.Product>             Product             => this.GetTable<Northwind.Product>();
		public ITable<Northwind.ActiveProduct>       ActiveProduct       => this.GetTable<Northwind.ActiveProduct>();
		public ITable<Northwind.DiscontinuedProduct> DiscontinuedProduct => this.GetTable<Northwind.DiscontinuedProduct>();
		public ITable<Northwind.Region>              Region              => this.GetTable<Northwind.Region>();
		public ITable<Northwind.Shipper>             Shipper             => this.GetTable<Northwind.Shipper>();
		public ITable<Northwind.Supplier>            Supplier            => this.GetTable<Northwind.Supplier>();
		public ITable<Northwind.Territory>           Territory           => this.GetTable<Northwind.Territory>();

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
