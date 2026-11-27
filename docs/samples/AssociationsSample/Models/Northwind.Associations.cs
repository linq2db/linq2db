using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using LinqToDB.Mapping;

namespace AssociationsSample.Models
{
	public partial class EmployeeTerritory
	{
		[Association(ThisKey = nameof(EmployeeID), OtherKey = nameof(Models.Employee.EmployeeID), CanBeNull = false)]
		public Employee Employee { get; set; }

		[Association(ThisKey = nameof(TerritoryID), OtherKey = nameof(Models.Territory.TerritoryID), CanBeNull = false)]
		public Territory Territory { get; set; }
	}

	public partial class Order
	{
		[Association(ThisKey = nameof(EmployeeID), OtherKey = nameof(Models.Employee.EmployeeID), CanBeNull = true)]
		public Employee Employee { get; set; }

		[Association(ThisKey = nameof(OrderID), OtherKey = nameof(OrderDetail.OrderID))]
		public IEnumerable<OrderDetail> Details { get; set; }

		[Association(ExpressionPredicate = nameof(DetailsWithBigDiscountFilter))]
		public IEnumerable<OrderDetail> DetailsWithBigDiscount { get; set; }

		private static Expression<Func<Order, OrderDetail, bool>> DetailsWithBigDiscountFilter()
		{
			return (order, detail) => order.OrderID == detail.OrderID && detail.Discount > 0.06;
		}
	}
}