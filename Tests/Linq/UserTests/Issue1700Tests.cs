using System;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	public class Issue1700Tests: TestBase
	{
		[Test]
		public void TestOuterApplySubFunction([IncludeDataSources(TestProvName.AllSqlServer2008Plus, TestProvName.AllPostgreSQL, TestProvName.AllSQLite)] string context)
		{
			var groupId = 5;

			using var db = GetDataContext(context);
			using var t1 = db.CreateLocalTable<Item>();
			using var t2 = db.CreateLocalTable<ItemAppType>();
			using var t3 = db.CreateLocalTable<AppType>();
			using var t4 = db.CreateLocalTable<AppSubType>();

			var items     = db.GetTable<Item>().AsQueryable();
			var itemTypes = db.GetTable<ItemAppType>().AsQueryable();
			var types     = db.GetTable<AppType>().AsQueryable();
			var subTypes  = db.GetTable<AppSubType>().AsQueryable();

			var data = (
				from item in items.Where(i => i.GroupId == groupId)
				let itemSubTypeDescription = SubFunction(itemTypes, types, subTypes, item)
				select new { item.ItemId, Description1 = item.Description, Description2 = itemSubTypeDescription.Description });

			var all_items = data.ToList();
		}

		[Table]
		class ItemAppType
		{
			[Column] public int AppTypeId { get; set; }
			[Column] public int ItemId { get; set; }
		}

		[Table]
		class Item
		{
			[Column] public int GroupId { get; set; }
			[Column] public int ItemId { get; set; }
			[Column] public string? Description { get; set; }
		}

		[Table]
		class AppType
		{
			[Column] public int AppTypeId { get; set; }
			[Column] public DateTime CreatedDate { get; set; }
		}

		[Table]
		class AppSubType
		{
			[Column] public int AppTypeId { get; set; }
			[Column] public int AppSubTypeId { get; set; }
			[Column] public string? Description { get; set; }
			[Column] public DateTime CreatedDate { get; set; }
		}

		[ExpressionMethod(nameof(SubFunctionImpl))]
		static TSome SubFunction(IQueryable<ItemAppType> itemTypes, IQueryable<AppType> types, IQueryable<AppSubType> subTypes, Item item)
		{
			throw new NotImplementedException();
		}

		public class TSome
		{
			public int AppSubTypeId { get; set; }
			public string? Description { get; set; }
			public DateTime MaxSubtypeCreatedDate { get; set; }
			public DateTime MaxTypeCreatedDate { get; set; }
			public int MaxTypeId { get; set; }
			public int CountDistinctTypeId { get; set; }
			public int CountDistinctSubTypeId { get; set; }
		}

		static Expression<Func<IQueryable<ItemAppType>, IQueryable<AppType>, IQueryable<AppSubType>, Item, TSome?>> SubFunctionImpl()
		{
			return (itemTypes, types, subTypes, item) => (
					from sub in
						from itemtype in itemTypes
						from type in types.LeftJoin(t => t.AppTypeId == itemtype.AppTypeId)
						from subtype in subTypes.LeftJoin(u => u.AppTypeId == type.AppTypeId)
						where itemtype.ItemId == item.ItemId
							  && type.AppTypeId == itemtype.AppTypeId
							  && subtype.AppTypeId == type.AppTypeId
						select new
						{
							subtype.Description,
							subtype.AppSubTypeId,
							subtypeCreatedDate = subtype.CreatedDate,
							typeCreatedDate = type.CreatedDate,
							type.AppTypeId
						}
					group sub by new { sub.Description, sub.AppSubTypeId }
					into grpby
					select new TSome
					{
						AppSubTypeId = grpby.Key.AppSubTypeId,
						Description = grpby.Key.Description,
						MaxSubtypeCreatedDate = grpby.Max(i => i.subtypeCreatedDate),
						MaxTypeCreatedDate = grpby.Max(i => i.typeCreatedDate),
						MaxTypeId = grpby.Max(i => i.AppTypeId),
						CountDistinctTypeId = grpby.CountExt(i => i.AppTypeId, Sql.AggregateModifier.Distinct),
						CountDistinctSubTypeId = grpby.CountExt(i => i.AppSubTypeId, Sql.AggregateModifier.Distinct)
					}
				)
				.OrderByDescending(ord1 => ord1.CountDistinctTypeId)
				.ThenByDescending(ord2 => ord2.MaxSubtypeCreatedDate)
				.ThenByDescending(ord3 => ord3.MaxTypeCreatedDate)
				.ThenByDescending(ord4 => ord4.MaxTypeId)
				.FirstOrDefault();
		}
	}
}
