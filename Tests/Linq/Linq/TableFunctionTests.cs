using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider.SqlServer;

using NUnit.Framework;

using Tests.Model;

namespace Tests.Linq
{
	[TestFixture]
	public class TableFunctionTests : TestBase
	{
		[Test]
		public void Func1([IncludeDataSources(TestProvName.AllSqlServer2008Plus, TestProvName.AllSapHana)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q =
					from p in new Model.Functions(db).GetParentByID(1)
					select p;

				q.ToList();
			}
		}

		[Test]
		public void Func2([IncludeDataSources(TestProvName.AllSqlServer2008Plus, TestProvName.AllSapHana)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q =
					from c in db.Child
					from p in db.GetParentByID(2)
					select p;

				q.ToList();
			}
		}

		[Test]
		public void Func3([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q =
					from c in db.Child
					from p in db.GetParentByID(c.ParentID)
					select p;

				q.ToList();
			}
		}

		readonly Func<DataConnection,int,IQueryable<Parent>> _f1 = CompiledQuery.Compile(
			(DataConnection db, int id) => from p in new Model.Functions(db).GetParentByID(id) select p);

		[Test]
		public void CompiledFunc1([IncludeDataSources(TestProvName.AllSqlServer2008Plus, TestProvName.AllSapHana)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				var q = _f1(db, 1);
				q.ToList();
			}
		}

		readonly Func<TestDataConnection,int,IQueryable<Parent>> _f2 = CompiledQuery.Compile(
			(TestDataConnection db, int id) => from c in db.Child from p in db.GetParentByID(id) select p);

		[Test]
		public void CompiledFunc2([IncludeDataSources(TestProvName.AllSqlServer2008Plus, TestProvName.AllSapHana)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				var q = _f2(db, 1);
				q.ToList();
			}
		}

		[Test]
		public void WithTabLock([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q =
					from p in new Tests.Model.Functions(db).WithTabLock<Parent>()
					select p;

				q.ToList();
			}
		}

		[Test]
		public void WithTabLock1([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q =
					from p in new Tests.Model.Functions(db).WithTabLock<Parent>().SchemaName("dbo")
					select p;

				q.ToList();
			}
		}

		[Test]
		public void WithTabLock2([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q =
					from p in Functions.WithTabLock1<Parent>(db).SchemaName("dbo")
					select p;

				q.ToList();
			}
		}

		[Test]
		public void WithTabLock3([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q =
					from p in db.WithTabLock<Parent>().SchemaName("dbo")
					select p;

				q.ToList();
			}
		}

		[Test]
		public void WithTest([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q =
					from p in db.Parent.SchemaName("dbo").With("TABLOCK,UPDLOCK")
					select p;

				q.ToList();
			}
		}

		[Test]
		public void WithTableExpressionTest([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q =
					from p in db.Parent.SchemaName("dbo").WithTableExpression("{0} {1} with (UpdLock)")
					select p;

				q.ToList();
			}
		}

		[Test, Category(TestCategory.FTS)]
		public void FreeTextTable1([IncludeDataSources(TestProvName.AllNorthwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					join t in db.FreeTextTable<Northwind.Category,int>(db.Category, c => c.Description, "sweetest candy bread and dry meat")
					on c.CategoryID equals t.Key
					select c;

				q.ToList();
			}
		}

		[Test, Category(TestCategory.FTS)]
		public void FreeTextTable2([IncludeDataSources(TestProvName.AllNorthwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from t in db.FreeTextTable<Northwind.Category,int>(db.Category, c => c.Description, "sweetest candy bread and dry meat")
					join c in db.Category
					on t.Key equals c.CategoryID
					select c;

				q.ToList();
			}
		}

		[Test, Category(TestCategory.FTS)]
		public void FreeText1([IncludeDataSources(TestProvName.AllNorthwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from t in db.Category
					where Sql.Ext.SqlServer().FreeText("sweet", t.Description)
					select t;

				var list = q.ToList();

				Assert.That(list, Is.Not.Empty);
			}
		}

		[Test, Category(TestCategory.FTS)]
		public void FreeText2([IncludeDataSources(TestProvName.AllNorthwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from t in db.Category
					where Sql.Ext.SqlServer().FreeText("sweet", Sql.AllColumns())
					select t;

				var list = q.ToList();

				Assert.That(list, Is.Not.Empty);
			}
		}

		[Test, Category(TestCategory.FTS)]
		public void FreeText3([IncludeDataSources(TestProvName.AllNorthwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from t in db.Category
					where Sql.Ext.SqlServer().FreeText("sweet", t)
					select t;

				var list = q.ToList();

				Assert.That(list, Is.Not.Empty);
			}
		}

		[Test, Category(TestCategory.FTS)]
		public void FreeText4([IncludeDataSources(TestProvName.AllNorthwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from t in db.Category
					where !Sql.Ext.SqlServer().FreeText("sweet", t)
					select t;

				var list = q.ToList();

				Assert.That(list, Is.Not.Empty);
			}
		}

		[Test]
		public void WithUpdateLock([IncludeDataSources(TestProvName.AllNorthwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from t in db.WithUpdateLock<Northwind.Category>()
					select t;

				q.ToList();
			}
		}

		[Test, Category(TestCategory.FTS)]
		public void Issue386InnerJoinWithExpression([IncludeDataSources(TestProvName.AllNorthwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from t in db.Product
					join c in db.FreeTextTable<Northwind.Category, int>(db.Category, c => c.Description, "sweetest candy bread and dry meat") on t.CategoryID equals c.Key
					orderby t.ProductName descending
					select t;
				var list = q.ToList();
				Assert.That(list, Is.Not.Empty);
			}
		}

		[Test, Category(TestCategory.FTS)]
		public void Issue386LeftJoinWithText([IncludeDataSources(TestProvName.AllNorthwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from t in db.Product
					from c in db.FreeTextTable<Northwind.Category, int>(db.Category, c => c.Description, "sweetest candy bread and dry meat").Where(f => f.Key == t.CategoryID).DefaultIfEmpty()
					orderby t.ProductName descending
					select t;
				var list = q.ToList();
				Assert.That(list, Is.Not.Empty);
			}
		}

		[Test, Category(TestCategory.FTS)]
		public void Issue386LeftJoinWithExpression([IncludeDataSources(TestProvName.AllNorthwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q
					= from t in db.Product
					from c in db.FreeTextTable<Northwind.Category, int>(db.Category, c => c.Description, "sweetest candy bread and dry meat").Where(f => f.Key == t.CategoryID).DefaultIfEmpty()
					orderby t.ProductName descending
					select t;
				var list = q.ToList();
				Assert.That(list, Is.Not.Empty);
			}
		}
	}
}
