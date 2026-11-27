using System;
using System.Collections.Generic;
using System.Linq;
using LinqToDB;
using LinqToDB.Mapping;

namespace AssociationsSample.Models
{
	public static class NorthwindExtensions
	{
		[Association(ThisKey = nameof(Order.EmployeeID), OtherKey = nameof(Models.Employee.EmployeeID), CanBeNull = true)]
		public static Employee Employee(this Order order)
		{
			throw new InvalidOperationException("Used only as Association helper");
		}

		[Association(ThisKey = nameof(Order.OrderID), OtherKey = nameof(OrderDetail.OrderID))]
		public static IEnumerable<OrderDetail> Details(this Order order)
		{
			throw new InvalidOperationException("Used only as Association helper");
		}

		[Association(ThisKey = nameof(Order.OrderID), OtherKey = nameof(OrderDetail.OrderID))]
		public static IQueryable<OrderDetail> DetailsQuery(this Order order, IDataContext db)
		{
			return db.GetTable<OrderDetail>().Where(d => d.OrderID == order.OrderID);
		}
	}
}