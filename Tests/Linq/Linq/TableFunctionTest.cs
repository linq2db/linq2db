﻿using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Data;

using NUnit.Framework;

namespace Tests.Linq
{
	using Model;

	[TestFixture]
	public class TableFunctionTest : TestBase
	{
		[Test, IncludeDataContextSource(ProviderName.SqlServer2008)]
		public void Func1(string context)
		{
			using (var db = GetDataContext(context))
			{
				var q =
					from p in new Model.Functions(db).GetParentByID(1)
					select p;

				q.ToList();
			}
		}

		[Test, IncludeDataContextSource(ProviderName.SqlServer2008)]
		public void Func2(string context)
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

		[Test, IncludeDataContextSource(ProviderName.SqlServer2008)]
		public void Func3(string context)
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

		[Test, IncludeDataContextSource(ProviderName.SqlServer2008)]
		public void CompiledFunc1(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				var q = _f1(db, 1);
				q.ToList();
			}
		}

		readonly Func<TestDataConnection,int,IQueryable<Parent>> _f2 = CompiledQuery.Compile(
			(TestDataConnection db, int id) => from c in db.Child from p in db.GetParentByID(id) select p);

		[Test, IncludeDataContextSource(ProviderName.SqlServer2008)]
		public void CompiledFunc2(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				var q = _f2(db, 1);
				q.ToList();
			}
		}

		[Test, IncludeDataContextSource(ProviderName.SqlServer2008)]
		public void WithTabLock(string context)
		{
			using (var db = GetDataContext(context))
			{
				var q =
					from p in new Tests.Model.Functions(db).WithTabLock<Parent>()
					select p;

				q.ToList();
			}
		}

		[Test, NorthwindDataContext]
		public void FreeText1(string context)
		{
			using (var db = new NorthwindDB())
			{
				var q =
					from c in db.Category
					join t in db.FreeTextTable<Northwind.Category,int>("[Description]", "sweetest candy bread and dry meat")
					on c.CategoryID equals t.Key
					select c;

				q.ToList();
			}
		}

		[Test, NorthwindDataContext]
		public void FreeText2(string context)
		{
			using (var db = new NorthwindDB())
			{
				var q =
					from c in db.Category
					join t in db.FreeTextTable<Northwind.Category,int>(c => c.Description, "sweetest candy bread and dry meat")
					on c.CategoryID equals t.Key
					select c;

				q.ToList();
			}
		}

		[Test, NorthwindDataContext]
		public void FreeText3(string context)
		{
			using (var db = new NorthwindDB())
			{
				var q =
					from t in db.FreeTextTable<Northwind.Category,int>(c => c.Description, "sweetest candy bread and dry meat")
					join c in db.Category
					on t.Key equals c.CategoryID
					select c;

				q.ToList();
			}
		}

		[Test, NorthwindDataContext]
		public void WithUpdateLock(string context)
		{
			using (var db = new NorthwindDB())
			{
				var q =
					from t in db.WithUpdateLock<Northwind.Category>()
					select t;

				q.ToList();
			}
		}
	}
}
