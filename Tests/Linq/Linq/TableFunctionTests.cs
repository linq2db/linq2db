﻿using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Data;

using NUnit.Framework;

namespace Tests.Linq
{
	using Model;

	[TestFixture]
	public class TableFunctionTests : TestBase
	{
		[Test]
		public void Func1([IncludeDataSources(TestProvName.AllSqlServer2008Plus, ProviderName.SapHana)] string context)
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
		public void Func2([IncludeDataSources(TestProvName.AllSqlServer2008Plus, ProviderName.SapHana)] string context)
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
		public void CompiledFunc1([IncludeDataSources(TestProvName.AllSqlServer2008Plus, ProviderName.SapHana)] string context)
		{
			using (var db = new TestDataConnection(context))
			{
				var q = _f1(db, 1);
				q.ToList();
			}
		}

		readonly Func<TestDataConnection,int,IQueryable<Parent>> _f2 = CompiledQuery.Compile(
			(TestDataConnection db, int id) => from c in db.Child from p in db.GetParentByID(id) select p);

		[Test]
		public void CompiledFunc2([IncludeDataSources(TestProvName.AllSqlServer2008Plus, ProviderName.SapHana)] string context)
		{
			using (var db = new TestDataConnection(context))
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
					from p in Model.Functions.WithTabLock1<Parent>(db).SchemaName("dbo")
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

		[Test, Category("FreeText")]
		public void FreeTextTable1([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					join t in db.FreeTextTable<Northwind.Category,int>("[Description]", "sweetest candy bread and dry meat")
					on c.CategoryID equals t.Key
					select c;

				q.ToList();
			}
		}

		[Test, Category("FreeText")]
		public void FreeTextTable2([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					join t in db.FreeTextTable<Northwind.Category,int>(c1 => c1.Description, "sweetest candy bread and dry meat")
					on c.CategoryID equals t.Key
					select c;

				q.ToList();
			}
		}

		[Test, Category("FreeText")]
		public void FreeTextTable3([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from t in db.FreeTextTable<Northwind.Category,int>(c => c.Description, "sweetest candy bread and dry meat")
					join c in db.Category
					on t.Key equals c.CategoryID
					select c;

				q.ToList();
			}
		}

#pragma warning disable CS0618
		[Test, Category("FreeText")]
		public void FreeText1([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from t in db.Category
					where Sql.FreeText(t.Description, "sweet")
					select t;

				var list = q.ToList();

				Assert.That(list.Count, Is.GreaterThan(0));
			}
		}

		[Test, Category("FreeText")]
		public void FreeText2([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from t in db.Category
					where Sql.FreeText(Sql.AllColumns(), "sweet")
					select t;

				var list = q.ToList();

				Assert.That(list.Count, Is.GreaterThan(0));
			}
		}

		[Test, Category("FreeText")]
		public void FreeText3([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from t in db.Category
					where Sql.FreeText(t, "sweet")
					select t;

				var list = q.ToList();

				Assert.That(list.Count, Is.GreaterThan(0));
			}
		}

		[Test, Category("FreeText")]
		public void FreeText4([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from t in db.Category
					where !Sql.FreeText(t, "sweet")
					select t;

				var list = q.ToList();

				Assert.That(list.Count, Is.GreaterThan(0));
			}
		}
#pragma warning restore CS0618

		[Test]
		public void WithUpdateLock([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from t in db.WithUpdateLock<Northwind.Category>()
					select t;

				q.ToList();
			}
		}

		[Test, Category("FreeText")]
		public void Issue386InnerJoinWithExpression([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from t in db.Product
					join c in db.FreeTextTable<Northwind.Category, int>(c => c.Description, "sweetest candy bread and dry meat") on t.CategoryID equals c.Key
					orderby t.ProductName descending
					select t;
				var list = q.ToList();
				Assert.That(list.Count, Is.GreaterThan(0));
			}
		}

		[Test, Category("FreeText")]
		public void Issue386LeftJoinWithText([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q = 
					from t in db.Product
					from c in db.FreeTextTable<Northwind.Category, int>("Description", "sweetest candy bread and dry meat").Where(f => f.Key == t.CategoryID).DefaultIfEmpty()
					orderby t.ProductName descending
					select t;
				var list = q.ToList();
				Assert.That(list.Count, Is.GreaterThan(0));
			}
		}

		[Test, Category("FreeText")]
		[ActiveIssue(386)]
		public void Issue386LeftJoinWithExpression([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q 
					= from t in db.Product
					from c in db.FreeTextTable<Northwind.Category, int>(c => c.Description, "sweetest candy bread and dry meat").Where(f => f.Key == t.CategoryID).DefaultIfEmpty()
					orderby t.ProductName descending
					select t;
				var list = q.ToList();
				Assert.That(list.Count, Is.GreaterThan(0));
			}
		}
	}
}
