using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

using NUnit.Framework;

namespace Tests.Linq
{
	using Model;

	[TestFixture]
	public class QueryInheritanceTests : TestBase
	{
		static IEnumerable<T> QueryTable<T>(IDataContext dataContext)
		{
			var query = new SelectQuery();
			var table = new SqlTable(typeof(T));
			var tableSource = new SelectQuery.TableSource(table, "t");
			query.From.Tables.Add(tableSource);

			var connection = (DataConnection) dataContext;

			var sqlBuilder = connection.DataProvider.CreateSqlBuilder();
			var sb = new StringBuilder();
			sqlBuilder.BuildSql(0, query, sb);

			return connection.Query<T>(sb.ToString());
		}

		[Test, DataContextSource(false)]
		public void Test1(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(ParentInheritance, QueryTable<ParentInheritanceBase>(db));
		}

		[Test, DataContextSource(false)]
		public void Test2(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(ParentInheritance, QueryTable<ParentInheritanceBase>(db).Select(p => p));
		}

		[Test, DataContextSource(false)]
		public void Test3(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    ParentInheritance where p is ParentInheritance1 select p,
					from p in QueryTable<ParentInheritanceBase>(db) where p is ParentInheritance1 select p);
		}

		[Test, DataContextSource(false)]
		public void Test4(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    ParentInheritance where !(p is ParentInheritanceNull) select p,
					from p in QueryTable<ParentInheritanceBase>(db) where !(p is ParentInheritanceNull) select p);
		}

		[Test, DataContextSource(false)]
		public void Test5(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    ParentInheritance where p is ParentInheritanceValue select p,
					from p in QueryTable<ParentInheritanceBase>(db) where p is ParentInheritanceValue select p);
		}

		[Test, DataContextSource(false)]
		public void Test6(string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in QueryTable<ParentInheritance12>(db) where p is ParentInheritance12 select p;
				q.ToList();
			}
		}

		[Test, DataContextSource(false)]
		public void Test7(string context)
		{
#pragma warning disable 183
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    ParentInheritance where p is ParentInheritanceBase select p,
					from p in QueryTable<ParentInheritanceBase>(db) where p is ParentInheritanceBase select p);
#pragma warning restore 183
		}

		[Test, DataContextSource(false)]
		public void Test8(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					ParentInheritance.OfType<ParentInheritance1>(),
					QueryTable<ParentInheritanceBase>(db).OfType<ParentInheritance1>());
		}

		[Test, DataContextSource(false)]
		public void Test9(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					ParentInheritance
						.Where(p => p.ParentID == 1 || p.ParentID == 2 || p.ParentID == 4)
						.OfType<ParentInheritanceNull>(),
					QueryTable<ParentInheritanceBase>(db)
						.Where(p => p.ParentID == 1 || p.ParentID == 2 || p.ParentID == 4)
						.OfType<ParentInheritanceNull>());
		}

		[Test, DataContextSource(false)]
		public void Test10(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					ParentInheritance.OfType<ParentInheritanceValue>(),
					QueryTable<ParentInheritanceBase>(db).OfType<ParentInheritanceValue>());
		}

		[Test, DataContextSource(false)]
		public void Test11(string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in QueryTable<ParentInheritance13>(db) where p is ParentInheritance13 select p;
				q.ToList();
			}
		}

		[Test, DataContextSource(false)]
		public void Test12(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    ParentInheritance1 where p.ParentID == 1 select p,
					from p in QueryTable<ParentInheritance1>(db) where p.ParentID == 1 select p);
		}

		[Test, NorthwindDataContext]
		public void TypeCastAsTest1(string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var dd = GetNorthwindAsList(context);
				AreEqual(
					dd.DiscontinuedProduct.ToList()
						.Select(p => p as Northwind.Product)
						.Select(p => p == null ? "NULL" : p.ProductName),
					QueryTable<Northwind.DiscontinuedProduct>(db).Where(p => p.Discontinued)
						.Select(p => p as Northwind.Product)
						.Select(p => p == null ? "NULL" : p.ProductName));
			}
		}

		[Test, NorthwindDataContext]
		public void TypeCastAsTest11(string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var dd = GetNorthwindAsList(context);
				AreEqual(
					dd.DiscontinuedProduct.ToList()
						.Select(p => new { p = p as Northwind.Product })
						.Select(p => p.p == null ? "NULL" : p.p.ProductName),
					QueryTable<Northwind.DiscontinuedProduct>(db).Where(p => p.Discontinued)
						.Select(p => new { p = p as Northwind.Product })
						.Select(p => p.p == null ? "NULL" : p.p.ProductName));
			}
		}

		[Test, NorthwindDataContext]
		public void TypeCastAsTest2(string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var dd = GetNorthwindAsList(context);
				AreEqual(
					dd.Product.ToList()
						.Select(p => p as Northwind.DiscontinuedProduct)
						.Select(p => p == null ? "NULL" : p.ProductName),
					QueryTable<Northwind.Product>(db)
						.Select(p => p as Northwind.DiscontinuedProduct)
						.Select(p => p == null ? "NULL" : p.ProductName));
			}
		}

		[Test, NorthwindDataContext]
		public void FirstOrDefault(string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var dd = GetNorthwindAsList(context);
				Assert.AreEqual(
					dd.DiscontinuedProduct.FirstOrDefault().ProductID,
					QueryTable<Northwind.DiscontinuedProduct>(db).Where(p => p.Discontinued).FirstOrDefault().ProductID);
			}
		}

		[Test, DataContextSource(false)]
		public void Cast1(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					ParentInheritance.OfType<ParentInheritance1>().Cast<ParentInheritanceBase>(),
					QueryTable<ParentInheritanceBase>(db).OfType<ParentInheritance1>().Cast<ParentInheritanceBase>());
		}

		class ParentEx : Parent
		{
			[NotColumn]
			protected bool Field1;

			public static void Test(QueryInheritanceTests inheritance, string context)
			{
				using (var db = inheritance.GetDataContext(context))
					inheritance.AreEqual(
						Enumerable.Select<Parent,ParentEx>(inheritance.Parent, p => new ParentEx { Field1 = true, ParentID = p.ParentID, Value1 = p.Value1 }).Cast<Parent>(),
						QueryTable<Parent>(db).Select(p => new ParentEx { Field1 = true, ParentID = p.ParentID, Value1 = p.Value1 }).Cast<Parent>());
			}
		}

		[Test, DataContextSource(false)]
		public void Cast2(string context)
		{
			ParentEx.Test(this, context);
		}

		[Table("Person", IsColumnAttributeRequired = false)]
		class PersonEx : Person
		{
		}

		[Test]
		public void SimplTest()
		{
			using (var db = new TestDataConnection())
				Assert.AreEqual(1, QueryTable<PersonEx>(db).Where(_ => _.FirstName == "John").Select(_ => _.ID).Single());
		}

		[Test, NorthwindDataContext]
		public void TypeCastIsChildConditional2(string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var result   = QueryTable<Northwind.Product>(db).Select(x => x is Northwind.DiscontinuedProduct).ToArray();
				var expected = db.Product.ToList()              .Select(x => x is Northwind.DiscontinuedProduct);

				var list = result.ToList();

				Assert.Greater(list.Count, 0);
				Assert.AreEqual(expected.Count(), list.Count);
				Assert.IsTrue(list.Except(expected).Count() == 0);
			}
		}

		public enum TypeCodeEnum
		{
			Base,
			A,
			A1,
			A2,
		}

		[Table(Name="LinqDataTypes")]
		public abstract class InheritanceBase
		{
			[Column] public Guid GuidValue { get; set; }

			[Column("ID")]
			public virtual TypeCodeEnum TypeCode
			{
				get { return TypeCodeEnum.Base; }
			}
		}

		[InheritanceMapping(Code = TypeCodeEnum.A1, Type = typeof(InheritanceA1), IsDefault = false)]
		[InheritanceMapping(Code = TypeCodeEnum.A2, Type = typeof(InheritanceA2), IsDefault = true)]
		public abstract class InheritanceA : InheritanceBase
		{
			[Association(CanBeNull = true, ThisKey = "GuidValue", OtherKey = "GuidValue")]
			public List<InheritanceB> Bs { get; set; }

			[Column("ID", IsDiscriminator = true)]
			public override TypeCodeEnum TypeCode
			{
				get { return TypeCodeEnum.A; }
			}
		}

		class InheritanceA1 : InheritanceA
		{
			[Column("ID", IsDiscriminator = true)]
			public override TypeCodeEnum TypeCode
			{
				get { return TypeCodeEnum.A1; }
			}
		}

		class InheritanceA2 : InheritanceA
		{
			[Column("ID", IsDiscriminator = true)]
			public override TypeCodeEnum TypeCode
			{
				get { return TypeCodeEnum.A2; }
			}
		}

		public class InheritanceB : InheritanceBase
		{
		}

	}
}
