using System;
using System.Collections.Generic;
using System.Linq;

using FluentAssertions;

using IBM.Data.Db2;

using LinqToDB;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue4336Tests : TestBase
	{
		// bug in informix
		[ThrowsForProvider(typeof(DB2Exception), TestProvName.AllInformix, ErrorMessage = "ERROR [IX000] [IBM][IDS/UNIX64] Internal error in routine opjoin().")]
		[Test]
		public void Issue4336Test([DataSources(TestProvName.AllAccess)] string context)
		{
			using var db = GetDataContext(context);
			using var t1 = db.CreateLocalTable<Product>();
			using var t2 = db.CreateLocalTable<ProductCategory>();
			using var t3 = db.CreateLocalTable<OrderHeader>();
			using var t4 = db.CreateLocalTable<OrderItem>();
			using var t5 = db.CreateLocalTable<OrderPeriod>();
			using var t6 = db.CreateLocalTable<ProductsPerOrderPeriod>();

			var q = (from r in ViewCapacityAndOrderedByPeriod(db) select r).Take(10);
			var l = q.ToList();
			var c = l.Count();
		}

		[Table]
		public class Product
		{
			[PrimaryKey] public int Id { get; set; }
			[Column(CanBeNull = false)] public string Name { get; set; } = null!;
			[Column(CanBeNull = false)] public int? CategoryId { get; set; }
		}

		[Table]
		public class ProductCategory
		{
			[PrimaryKey] public int Id { get; set; } // integer
			[Column(CanBeNull = false)] public string Name { get; set; } = null!;
			[Column] public short ProductOrderLimit { get; set; }
			[Column] public short CustomerOrderLimit { get; set; }
			[Column] public short PeriodOrderLimit { get; set; }
		}

		[Table]
		public class OrderHeader
		{
			[PrimaryKey] public int Id { get; set; } // integer
			[Column] public int PeriodId { get; set; } // integer

			[Association(ThisKey = nameof(Id), OtherKey = nameof(OrderItem.OrderHeaderId))]
			public IEnumerable<OrderItem> OrderItems { get; set; } = null!;
		}

		[Table]
		public partial class OrderItem
		{
			[PrimaryKey] public int Id { get; set; }
			[Column] public int OrderHeaderId { get; set; }
			[Column] public int ProductId { get; set; }
			[Column] public short Quantity { get; set; }

		}

		[Table]
		public class OrderPeriod
		{
			[PrimaryKey] public int Id { get; set; }

			[Association(ThisKey = nameof(Id), OtherKey = nameof(OrderHeader.PeriodId))]
			public IEnumerable<OrderHeader> OrderHeaders { get; set; } = null!;

			[Association(ThisKey = nameof(Id), OtherKey = nameof(ProductsPerOrderPeriod.OrderPeriodId))]
			public IEnumerable<ProductsPerOrderPeriod> ProductsPerOrderPeriods { get; set; } = null!;
		}

		[Table]
		public class ProductsPerOrderPeriod
		{
			[PrimaryKey] public int Id { get; set; }
			[Column] public int OrderPeriodId { get; set; }
			[Column] public int ProductId { get; set; }
		}

		public record ViewProductCategoryCapacityRow(int Id, string Name, short ProductOrderLimit, short CustomerOrderLimit, short PeriodOrderLimit);
		public record ViewProductCapacityRow(int ProductId, int? CategoryId, string Name, short ProductOrderLimit, short CustomerOrderLimit, short PeriodOrderLimit);
		public record ViewSumOfOrderedProductsByPeriodRow(int OrderPeriodId, int ProductId, int Quantity);
		public record ViewCapacityAndOrderedByPeriodAndProductRow(int OrderPeriodId, int ProductId, int? CategoryId, int MaxCapacity, int Quantity, int FreeCapacity);
		public record ViewSumOfOrderedCategoriesByPeriodRow(int OrderPeriodId, int? CategoryId, int Quantity);
		public record ViewCapacityAndOrderedByPeriodAndProductCategoryRow(int OrderPeriodId, int? ProductCategoryId, string ProductCategoryName, int MaxCapacity, int Quantity, int FreeCapacity);
		public record ViewCapacityAndOrderedByPeriodRow(int OrderPeriodId, int ProductId, int? ProductCategoryId, int ProductMaxCapacity, int ProductQuantity, int ProductFreeCapacity, int CategoryMaxCapacity, int CategoryQuantity, int CategoryFreeCapacity);

		IQueryable<ViewProductCategoryCapacityRow> ViewProductCategoryCapacity(IDataContext db) =>
		   from pc in db.GetTable<ProductCategory>()
		   select new ViewProductCategoryCapacityRow(pc.Id, pc.Name, pc.ProductOrderLimit, pc.CustomerOrderLimit, pc.PeriodOrderLimit);

		IQueryable<ViewProductCapacityRow> ViewProductCapacity(IDataContext db) =>
		   from p in db.GetTable<Product>()
		   from pcc in ViewProductCategoryCapacity(db).Where(o => o.Id == p.CategoryId).DefaultIfEmpty()
		   select new ViewProductCapacityRow(p.Id, p.CategoryId, p.Name, pcc.ProductOrderLimit, pcc.CustomerOrderLimit, pcc.PeriodOrderLimit);

		IQueryable<ViewSumOfOrderedProductsByPeriodRow> ViewSumOfOrderedProductsByPeriod(IDataContext db) =>
					from op in db.GetTable<OrderPeriod>()
					from oh in op.OrderHeaders.DefaultIfEmpty()
					from oi in oh.OrderItems.DefaultIfEmpty()
					group new { oi.Quantity } by new { op.Id, oi.ProductId } into agroup
					select new ViewSumOfOrderedProductsByPeriodRow(agroup.Key.Id, agroup.Key.ProductId, Coalesce(agroup.Sum(a => Coalesce(a.Quantity, (short)0)), 0));

		IQueryable<ViewCapacityAndOrderedByPeriodAndProductRow> ViewCapacityAndOrderedByPeriodAndProduct(IDataContext db) =>
						  from op in db.GetTable<OrderPeriod>()
						  from pop in op.ProductsPerOrderPeriods
						  from vpc in ViewProductCapacity(db).Where(o => o.ProductId == pop.ProductId).DefaultIfEmpty()
						  from vsp in ViewSumOfOrderedProductsByPeriod(db).Where(o => o.OrderPeriodId == op.Id && o.ProductId == pop.ProductId).DefaultIfEmpty()
						  let PeriodOrderLimit = Coalesce(vpc.PeriodOrderLimit, 0)
						  let Quantity = Coalesce(vsp.Quantity, 0)
						  select new ViewCapacityAndOrderedByPeriodAndProductRow(op.Id, pop.ProductId, vpc.CategoryId, PeriodOrderLimit, Quantity, PeriodOrderLimit - Quantity);

		IQueryable<ViewSumOfOrderedCategoriesByPeriodRow> ViewSumOfOrderedCategoriesByPeriod(IDataContext db) =>
		   from op in db.GetTable<OrderPeriod>()
		   from oh in op.OrderHeaders.DefaultIfEmpty()
		   from oi in oh.OrderItems.DefaultIfEmpty()
		   from p in db.GetTable<Product>().Where(o => o.Id == oi.ProductId).DefaultIfEmpty()
		   group new { oi.Quantity } by new { op.Id, p.CategoryId } into agroup
		   select new ViewSumOfOrderedCategoriesByPeriodRow(agroup.Key.Id, agroup.Key.CategoryId, agroup.Sum(a => a.Quantity));

		 IQueryable<ViewCapacityAndOrderedByPeriodAndProductCategoryRow> ViewCapacityAndOrderedByPeriodAndProductCategory(IDataContext db) =>
		   from op in db.GetTable<OrderPeriod>()
		   from vpcc in ViewProductCategoryCapacity(db)
		   from vsopc in ViewSumOfOrderedCategoriesByPeriod(db).Where(o => o.OrderPeriodId == op.Id && o.CategoryId == vpcc.Id).DefaultIfEmpty()
		   let PeriodOrderLimit = Coalesce(vpcc.PeriodOrderLimit, 0)
		   let Quantity = vsopc.Quantity
		   select new ViewCapacityAndOrderedByPeriodAndProductCategoryRow(op.Id, vpcc.Id, vpcc.Name, PeriodOrderLimit, Quantity, PeriodOrderLimit - Quantity);

		IQueryable<ViewCapacityAndOrderedByPeriodRow> ViewCapacityAndOrderedByPeriod(IDataContext db) =>
		   from v1 in ViewCapacityAndOrderedByPeriodAndProduct(db)
		   from v2 in ViewCapacityAndOrderedByPeriodAndProductCategory(db).Where(o => o.OrderPeriodId == v1.OrderPeriodId && o.ProductCategoryId == v1.CategoryId).DefaultIfEmpty()
		   select new ViewCapacityAndOrderedByPeriodRow(v1.OrderPeriodId, v1.ProductId, v1.CategoryId, v1.MaxCapacity, v1.Quantity, v1.FreeCapacity, v2.MaxCapacity, v2.Quantity, v2.FreeCapacity);

		[Sql.Extension("COALESCE({expr},{nullValue})", ServerSideOnly = true, CanBeNull = true, Precedence = Precedence.Primary)]
		static T Coalesce<T>([ExprParameter] T expr, [ExprParameter] T nullValue)
		{
			throw new LinqToDBException($"'{nameof(Coalesce)}' is server-side method.");
		}
	}
}
